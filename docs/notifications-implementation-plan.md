# Notifications Module — Implementation Plan

## Overview

A full notifications system covering:
- **System-triggered notifications** — automatically created when actions like vehicle/driver updates, deletes, and assignments occur
- **Admin-broadcast notifications** — manually created by an admin and targeted at an individual user, a named group, or everyone
- **In-app delivery via SignalR** — real-time push to connected clients
- **Notification drawer in the Navbar** — where users view, mark as read, and archive notifications
- **Automatic cleanup** — read notifications are permanently deleted 48 hours after they are marked as read

---

## Tech Stack Additions

| Concern | Technology |
|---------|-----------|
| Real-time push | **ASP.NET Core SignalR** (hub on the server, `@microsoft/signalr` client) |
| Scheduled cleanup | **`IHostedService` / `PeriodicTimer`** (background service, no extra package needed) |
| Frontend state | **React context** (`NotificationContext`) — consistent with existing `AuthContext` pattern |

---

## 1. Data Model

### 1.1 `Notification` (new EF Core entity)

```csharp
public class Notification
{
    public Guid Id { get; set; }

    // Content
    public string Title { get; set; } = string.Empty;
    public string Body  { get; set; } = string.Empty;

    // Categorisation
    public NotificationCategory Category { get; set; }  // SystemAction | AdminBroadcast
    public NotificationActionType? ActionType { get; set; }  // VehicleUpdated | VehicleDeleted | DriverUpdated | DriverDeleted | DriverAssigned | ...
    public string? RelatedEntityId { get; set; }  // e.g. vehicle registration or driver id

    // Targeting (null means "everyone")
    public string? TargetUserId { get; set; }   // FK → AspNetUsers (null = broadcast)
    public string? TargetGroupName { get; set; } // group label (null = not group-targeted)

    // Per-recipient state — stored in NotificationRecipient (see 1.2)
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public string? CreatedByUserId { get; set; }  // FK → AspNetUsers (admin who sent it)
}
```

### 1.2 `NotificationRecipient` (tracks per-user delivery state)

```csharp
public class NotificationRecipient
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }    // set when IsRead → true

    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
}
```

### 1.3 Enums

```csharp
public enum NotificationCategory
{
    SystemAction,   // triggered by application events
    AdminBroadcast  // manually created by an admin
}

public enum NotificationActionType
{
    // Vehicle
    VehicleCreated,
    VehicleUpdated,
    VehicleDeleted,
    VehicleSynced,

    // Driver
    DriverCreated,
    DriverUpdated,
    DriverDeleted,
    DriverAssigned,   // driver assigned to / unassigned from a vehicle
    DriverUnassigned,

    // Admin custom (no ActionType needed — null)
    Custom
}
```

### 1.4 `ApplicationDbContext` changes

- Add `DbSet<Notification> Notifications`
- Add `DbSet<NotificationRecipient> NotificationRecipients`
- Add index: `NotificationRecipients.UserId + IsRead + IsArchived` (drives inbox query)
- Add index: `NotificationRecipients.ReadAt` (drives cleanup job)
- Add `UserGroups` table (see Section 5) if group-based targeting is required beyond roles

---

## 2. Backend — SignalR Hub

### File: `CarTrack.Server/Notifications/NotificationHub.cs`

```csharp
[Authorize]
public class NotificationHub : Hub
{
    // Clients connect automatically; the hub uses user IDs as groups.
    // No custom methods needed on the hub itself — server pushes only.
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, Context.UserIdentifier!);
        await base.OnConnectedAsync();
    }
}
```

- Register in `Program.cs`:
  ```csharp
  builder.Services.AddSignalR();
  // ...
  app.MapHub<NotificationHub>("/hubs/notifications");
  ```
- Authentication: SignalR uses the same JWT bearer middleware. Pass the access token via the `access_token` query string (standard SignalR behaviour).

---

## 3. Backend — Notification Service

### File: `CarTrack.Server/Notifications/NotificationService.cs`

Responsible for:
1. **Creating** a `Notification` row
2. **Fanning out** `NotificationRecipient` rows to the correct users
3. **Pushing** via SignalR to connected clients

```
Interface: INotificationService
  CreateSystemNotificationAsync(NotificationActionType, title, body, relatedEntityId?, targetUserId?)
  CreateAdminNotificationAsync(title, body, target: Everyone | Group(name) | User(id), createdByUserId)
  MarkReadAsync(notificationRecipientId, userId)
  ArchiveAsync(notificationRecipientId, userId)
  GetInboxAsync(userId, includeArchived: false) → IEnumerable<NotificationRecipientDto>
  GetUnreadCountAsync(userId) → int
```

**Fan-out logic** inside `CreateAdminNotificationAsync`:
- `Everyone` → insert one `NotificationRecipient` row per registered user
- `Group(name)` → resolve users in that group, insert one row each
- `User(id)` → insert a single row

**SignalR push** — after fan-out, call:
```csharp
await _hubContext.Clients.User(userId).SendAsync("notification", recipientDto);
```
For large "Everyone" broadcasts, push in batches to avoid blocking.

---

## 4. Backend — REST Endpoints

### File: `CarTrack.Server/Notifications/NotificationEndpoints.cs`

All routes under `/api/notifications`, all `[Authorize]`.

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/notifications` | Returns the calling user's inbox (`NotificationRecipientDto[]`). Query params: `?archived=true` |
| `GET` | `/api/notifications/unread-count` | Returns `{ count: int }` — used to drive the badge |
| `POST` | `/api/notifications/{recipientId}/read` | Mark a single notification as read (sets `ReadAt = UtcNow`) |
| `POST` | `/api/notifications/read-all` | Mark all unread for the calling user as read |
| `POST` | `/api/notifications/{recipientId}/archive` | Archive a notification |
| `DELETE` | `/api/notifications/{recipientId}` | Hard-delete a recipient row (user dismisses permanently) |
| `POST` | `/api/notifications/broadcast` | **Admin only** — create and send a notification. Body: `{ title, body, target }` |

### DTOs (`NotificationDtos.cs`)

```csharp
public record NotificationRecipientDto(
    Guid RecipientId,
    Guid NotificationId,
    string Title,
    string Body,
    NotificationCategory Category,
    NotificationActionType? ActionType,
    string? RelatedEntityId,
    bool IsRead,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime? ReadAt
);

public record BroadcastNotificationRequest(
    string Title,
    string Body,
    NotificationTarget Target  // { Type: Everyone | Group | User, Value?: string }
);
```

---

## 5. System-Triggered Notifications

Inject `INotificationService` into the relevant endpoint handlers and call it after successful DB writes.

### Trigger points

| Event | Handler file | Notification |
|-------|-------------|--------------|
| Vehicle created via sync or manual create | `VehicleEndpoints.cs` | `VehicleCreated` → all users |
| Vehicle updated | `VehicleEndpoints.cs` | `VehicleUpdated` → all users |
| Vehicle deleted (soft-delete) | `VehicleEndpoints.cs` | `VehicleDeleted` → all users |
| Driver created | `DriverEndpoints.cs` | `DriverCreated` → all users |
| Driver updated | `DriverEndpoints.cs` | `DriverUpdated` → all users |
| Driver deleted | `DriverEndpoints.cs` | `DriverDeleted` → all users |
| Driver assigned to vehicle | `DriverEndpoints.cs` / `VehicleEndpoints.cs` | `DriverAssigned` → all users |
| Driver unassigned from vehicle | same | `DriverUnassigned` → all users |

System notifications are always broadcast to `Everyone` unless a specific `targetUserId` makes more sense for a given action (e.g. a self-service update).

---

## 6. Admin Broadcast UI

A new section in `SettingsPage` (or a dedicated `/notifications/broadcast` route accessible only to admin-role users).

### Component: `SendNotificationDialog.tsx`

Fields:
- **Title** — text input
- **Body** — multiline textarea
- **Target** — `<Dropdown>` with three options:
  - `Everyone`
  - `Group` → reveals a group-name input (freeform or from a predefined list of role/group names)
  - `Specific User` → reveals a user-search combobox (fetches from `/api/auth/users`)

On submit: calls `POST /api/notifications/broadcast`.

### `/api/auth/users` endpoint (new, admin only)

Returns `{ id, displayName, email }[]` — needed to populate the user-search combobox in the broadcast dialog.

---

## 7. User Groups

For group-targeted broadcasts the simplest approach is to **reuse ASP.NET Identity roles** as groups:
- Each `ApplicationUser` has roles (e.g. `Admin`, `Manager`, `Viewer`)
- `INotificationService` resolves group → users via `UserManager<ApplicationUser>.GetUsersInRoleAsync(groupName)`

If custom groups beyond roles are needed later, add a `UserGroup` / `UserGroupMember` table in a future migration.

---

## 8. Cleanup Background Service

### File: `CarTrack.Server/Notifications/NotificationCleanupService.cs`

```csharp
public class NotificationCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var cutoff = DateTime.UtcNow.AddHours(-48);
            // Delete NotificationRecipient rows where IsRead = true AND ReadAt < cutoff
            // If a Notification has zero remaining recipients, delete it too (orphan cleanup)
        }
    }
}
```

- Registered in `Program.cs`: `builder.Services.AddHostedService<NotificationCleanupService>()`
- Uses a short-lived `IServiceScope` to resolve `ApplicationDbContext` (background services are singleton scope)

---

## 9. Frontend — `NotificationContext`

### File: `frontend/src/context/NotificationContext.tsx`

Responsibilities:
- Establish and maintain the SignalR connection after the user authenticates
- Expose `notifications`, `unreadCount`, `markRead`, `markAllRead`, `archive` to consumers
- Receive real-time `notification` events from the hub and prepend to local state
- Fetch the initial inbox on mount via `GET /api/notifications`

```tsx
interface NotificationContextValue {
  notifications: NotificationRecipientDto[];
  unreadCount: number;
  isLoading: boolean;
  markRead: (recipientId: string) => Promise<void>;
  markAllRead: () => Promise<void>;
  archive: (recipientId: string) => Promise<void>;
}
```

Wrap `<App>` with `<NotificationProvider>` inside `main.tsx` (inside `<AuthProvider>`).

---

## 10. Frontend — `notificationService.ts`

### File: `frontend/src/services/notificationService.ts`

Thin wrapper around `authorizedFetch` (from `authService.ts`):

```ts
getNotifications(archived?: boolean): Promise<NotificationRecipientDto[]>
getUnreadCount(): Promise<{ count: number }>
markRead(recipientId: string): Promise<void>
markAllRead(): Promise<void>
archive(recipientId: string): Promise<void>
broadcast(payload: BroadcastPayload): Promise<void>
```

---

## 11. Frontend — `NotificationsDialog.tsx` (refactor)

The existing component at `src/components/NotificationsDialog.tsx` is currently stub-only. Replace its hardcoded mock data with real data from `NotificationContext`.

### UI requirements

- **Bell icon badge** — red dot (or count bubble) when `unreadCount > 0`
- **Drawer header** — "Notifications" title + "Mark all as read" button (disabled when 0 unread)
- **Tab bar** — `Inbox` | `Archived` (Fluent UI `<TabList>`)
- **Notification list item** — title, truncated body, relative time ("2 min ago"), unread indicator (bold text + left accent bar)
- **Item actions** (on hover or via `...` menu):
  - "Mark as read" (if unread)
  - "Archive"
- **Empty state** — "You're all caught up" message when inbox is empty
- **Loading state** — skeleton list while fetching

---

## 12. Frontend — Types

### File: `frontend/src/types/notification.ts`

```ts
export type NotificationCategory = 'SystemAction' | 'AdminBroadcast';

export type NotificationActionType =
  | 'VehicleCreated' | 'VehicleUpdated' | 'VehicleDeleted' | 'VehicleSynced'
  | 'DriverCreated'  | 'DriverUpdated'  | 'DriverDeleted'
  | 'DriverAssigned' | 'DriverUnassigned'
  | 'Custom';

export interface NotificationRecipientDto {
  recipientId: string;
  notificationId: string;
  title: string;
  body: string;
  category: NotificationCategory;
  actionType: NotificationActionType | null;
  relatedEntityId: string | null;
  isRead: boolean;
  isArchived: boolean;
  createdAt: string;   // ISO 8601
  readAt: string | null;
}

export interface BroadcastPayload {
  title: string;
  body: string;
  target: NotificationTarget;
}

export type NotificationTarget =
  | { type: 'Everyone' }
  | { type: 'Group'; value: string }
  | { type: 'User'; value: string };
```

---

## 13. Migration Plan

New EF Core migrations (in order):

1. `AddNotificationsTable` — adds `Notifications` and `NotificationRecipients` with FK constraints and indexes
2. *(optional later)* `AddUserGroups` — if custom groups are needed beyond Identity roles

---

## 14. Implementation Order

The tasks below are sequenced so each step can be tested before the next begins.

| # | Task | Layer |
|---|------|-------|
| 1 | Install `Microsoft.AspNetCore.SignalR` (already in ASP.NET Core — no extra NuGet needed) and `@microsoft/signalr` npm package | Both |
| 2 | Create `Notification` + `NotificationRecipient` models and update `ApplicationDbContext` | Backend |
| 3 | Run EF Core migration `AddNotificationsTable` | Backend |
| 4 | Implement `INotificationService` + `NotificationService` | Backend |
| 5 | Implement `NotificationHub` and register SignalR in `Program.cs` | Backend |
| 6 | Implement `NotificationEndpoints` and wire into `Program.cs` | Backend |
| 7 | Add `INotificationService` calls to `VehicleEndpoints` and `DriverEndpoints` | Backend |
| 8 | Implement `NotificationCleanupService` and register it | Backend |
| 9 | Add `/api/auth/users` endpoint (admin only) | Backend |
| 10 | Create `frontend/src/types/notification.ts` | Frontend |
| 11 | Create `frontend/src/services/notificationService.ts` | Frontend |
| 12 | Create `frontend/src/context/NotificationContext.tsx` (SignalR connection + REST fetch) | Frontend |
| 13 | Wrap `<App>` with `<NotificationProvider>` in `main.tsx` | Frontend |
| 14 | Refactor `NotificationsDialog.tsx` to consume `NotificationContext` | Frontend |
| 15 | Build `SendNotificationDialog.tsx` (admin broadcast UI) and add to `SettingsPage` | Frontend |

---

## 15. Security Considerations

- **Authorization on all endpoints** — `[Authorize]` on every notification route; the `broadcast` endpoint additionally requires the `Admin` role
- **Ownership enforcement** — `markRead` and `archive` must verify that the `NotificationRecipient.UserId == calling user's id` before mutating
- **SignalR auth** — pass JWT as `access_token` query param (standard ASP.NET Core SignalR pattern); the hub is decorated with `[Authorize]`
- **No PII in titles/bodies** — system-triggered messages should contain vehicle registration or driver name only, never passwords or raw credentials
- **Input validation** — `BroadcastNotificationRequest` title (max 120 chars) and body (max 1000 chars) validated with `FluentValidation` or Data Annotations before persistence
