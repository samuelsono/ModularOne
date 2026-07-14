# Support Module — Implementation Plan

## Overview

A unified **Support & Feedback** module that handles three closely related entry points from the `AvatarMenu`:

| Menu Item | Intent | Form Mode |
|-----------|--------|-----------|
| **Support** | Create a support ticket | `ticket` |
| **Feedback** | Submit general product feedback | `feedback` |
| **Report a Bug** | Report a specific defect | `bug` |

All three open the **same `SupportDialog` component** with the form pre-configured for the appropriate mode. Tickets/feedback/bugs are stored in the database and visible to admins in a new Settings section.

---

## Current State

In `Navigation.tsx`, the `AvatarMenu` already has these three `<MenuItem>` entries with no `onClick` handlers attached. They only need to be wired to open `SupportDialog` in the correct mode.

---

## 1. Data Model

### 1.1 `SupportTicket` (new EF Core entity)

```csharp
public class SupportTicket
{
    public Guid Id { get; set; }
    public TicketType Type { get; set; }           // Ticket | Feedback | BugReport
    public TicketStatus Status { get; set; }        // Open | InProgress | Resolved | Closed
    public TicketPriority Priority { get; set; }    // Low | Medium | High | Critical

    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;  // FK → TicketCategory

    // Bug-report extras (nullable, only used when Type = BugReport)
    public string? BugSeverity { get; set; }    // Minor | Major | Critical | Blocker
    public string? StepsToReproduce { get; set; }
    public string? ExpectedBehavior { get; set; }
    public string? ActualBehavior { get; set; }
    public string? BrowserOrEnvironment { get; set; }

    // Feedback extras (nullable, only used when Type = Feedback)
    public int? SatisfactionRating { get; set; }  // 1–5 star rating

    // Submitter
    public string SubmittedByUserId { get; set; } = string.Empty;
    public ApplicationUser SubmittedBy { get; set; } = null!;

    // Admin assignment / notes
    public string? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedTo { get; set; }
    public string? AdminNotes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
```

### 1.2 `TicketCategory` (admin-managed lookup)

```csharp
public class TicketCategory
{
    public string Id { get; set; } = string.Empty;   // e.g. "vehicle", "billing", "access"
    public string Label { get; set; } = string.Empty; // e.g. "Vehicle Issues"
    public TicketType[] AppliesTo { get; set; } = []; // which ticket types this category appears under
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
```

### 1.3 Enums

```csharp
public enum TicketType    { Ticket, Feedback, BugReport }
public enum TicketStatus  { Open, InProgress, Resolved, Closed }
public enum TicketPriority { Low, Medium, High, Critical }
```

### 1.4 `ApplicationDbContext` changes

- Add `DbSet<SupportTicket> SupportTickets`
- Add `DbSet<TicketCategory> TicketCategories`
- Migration: `AddSupportTicketsTable`
- Seed default `TicketCategory` rows (see Section 2.3)

---

## 2. Backend — REST Endpoints

### File: `CarTrack.Server/Support/SupportEndpoints.cs`

All routes under `/api/support`, all `[Authorize]`.

#### User-facing

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/support/categories` | Returns active `TicketCategory` rows, optionally filtered by `?type=Ticket\|Feedback\|BugReport` |
| `POST` | `/api/support/tickets` | Submit a new ticket / feedback / bug report |
| `GET` | `/api/support/tickets` | Returns the calling user's own submitted tickets |
| `GET` | `/api/support/tickets/{id}` | Returns a single ticket (must be owned by caller or caller is Admin) |

#### Admin only

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/support/admin/tickets` | All tickets with filters: `?type=&status=&priority=&search=` |
| `PUT` | `/api/support/admin/tickets/{id}` | Update status, priority, assignee, admin notes |
| `DELETE` | `/api/support/admin/tickets/{id}` | Hard-delete a ticket |
| `GET` | `/api/support/admin/categories` | All categories (including inactive) |
| `POST` | `/api/support/admin/categories` | Create a category |
| `PUT` | `/api/support/admin/categories/{id}` | Update a category |
| `DELETE` | `/api/support/admin/categories/{id}` | Delete a category |

### 2.1 DTOs (`SupportDtos.cs`)

```csharp
public record TicketCategoryDto(string Id, string Label, TicketType[] AppliesTo, bool IsActive, int SortOrder);

public record SubmitTicketRequest(
    TicketType Type,
    string Subject,
    string Description,
    string CategoryId,
    TicketPriority Priority,         // default: Medium
    // Ticket extras (optional)
    // Bug extras (optional)
    string? BugSeverity,
    string? StepsToReproduce,
    string? ExpectedBehavior,
    string? ActualBehavior,
    string? BrowserOrEnvironment,
    // Feedback extras (optional)
    int? SatisfactionRating
);

public record SupportTicketDto(
    Guid Id,
    TicketType Type,
    TicketStatus Status,
    TicketPriority Priority,
    string Subject,
    string Description,
    TicketCategoryDto Category,
    string? BugSeverity,
    string? StepsToReproduce,
    string? ExpectedBehavior,
    string? ActualBehavior,
    string? BrowserOrEnvironment,
    int? SatisfactionRating,
    string SubmittedByDisplayName,
    string? AssignedToDisplayName,
    string? AdminNotes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt
);

public record UpdateTicketAdminRequest(
    TicketStatus Status,
    TicketPriority Priority,
    string? AssignedToUserId,
    string? AdminNotes
);
```

### 2.2 Validation rules

| Field | Rule |
|-------|------|
| `Subject` | Required, max 200 chars |
| `Description` | Required, min 20 chars, max 4000 chars |
| `CategoryId` | Must reference an active `TicketCategory` that `AppliesTo` the given `Type` |
| `SatisfactionRating` | Only allowed when `Type = Feedback`, must be 1–5 |
| `BugSeverity` | Only allowed when `Type = BugReport` |
| Bug step fields | Only allowed when `Type = BugReport` |

Use Data Annotations or a thin inline validator in the endpoint handler.

### 2.3 Default seed data for `TicketCategory`

```
ID               Label                    AppliesTo
──────────────── ──────────────────────── ──────────────────────────────
vehicles         Vehicle Issues           Ticket, BugReport
drivers          Driver Management        Ticket
reports          Reports & Dashboards     Ticket, BugReport
access           Access & Permissions     Ticket
integrations     Integrations             Ticket, BugReport
general          General                  Ticket, Feedback
ui               User Interface           Feedback, BugReport
performance      Performance              BugReport
data             Data Accuracy            Ticket, BugReport
```

Seeded via `DatabaseSeeder.cs` (already exists in the project) on first startup.

---

## 3. Frontend — Types

### File: `frontend/src/types/support.ts`

```ts
export type TicketType     = 'Ticket' | 'Feedback' | 'BugReport';
export type TicketStatus   = 'Open' | 'InProgress' | 'Resolved' | 'Closed';
export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type BugSeverity    = 'Minor' | 'Major' | 'Critical' | 'Blocker';

export interface TicketCategory {
  id: string;
  label: string;
  appliesTo: TicketType[];
  isActive: boolean;
  sortOrder: number;
}

export interface SubmitTicketRequest {
  type: TicketType;
  subject: string;
  description: string;
  categoryId: string;
  priority: TicketPriority;
  // Bug extras
  bugSeverity?: BugSeverity;
  stepsToReproduce?: string;
  expectedBehavior?: string;
  actualBehavior?: string;
  browserOrEnvironment?: string;
  // Feedback extras
  satisfactionRating?: number;
}

export interface SupportTicket {
  id: string;
  type: TicketType;
  status: TicketStatus;
  priority: TicketPriority;
  subject: string;
  description: string;
  category: TicketCategory;
  bugSeverity?: BugSeverity;
  stepsToReproduce?: string;
  expectedBehavior?: string;
  actualBehavior?: string;
  browserOrEnvironment?: string;
  satisfactionRating?: number;
  submittedByDisplayName: string;
  assignedToDisplayName?: string;
  adminNotes?: string;
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string;
}
```

---

## 4. Frontend — `supportService.ts`

### File: `frontend/src/services/supportService.ts`

```ts
getCategories(type?: TicketType): Promise<TicketCategory[]>
submitTicket(data: SubmitTicketRequest): Promise<SupportTicket>
getMyTickets(): Promise<SupportTicket[]>
getTicket(id: string): Promise<SupportTicket>

// Admin
getAllTickets(filters?: { type?: TicketType; status?: TicketStatus; priority?: TicketPriority; search?: string }): Promise<SupportTicket[]>
updateTicket(id: string, data: UpdateTicketAdminRequest): Promise<SupportTicket>
deleteTicket(id: string): Promise<void>
getAdminCategories(): Promise<TicketCategory[]>
createCategory(data: SaveCategoryRequest): Promise<TicketCategory>
updateCategory(id: string, data: SaveCategoryRequest): Promise<TicketCategory>
deleteCategory(id: string): Promise<void>
```

---

## 5. Frontend — `SupportDialog.tsx`

### File: `frontend/src/components/SupportDialog.tsx`

A **Fluent UI `<Dialog>`** (not a drawer — a centred modal is more appropriate for a form submission).

### Props

```ts
interface SupportDialogProps {
  open: boolean;
  onClose: () => void;
  initialMode: TicketType;  // 'Ticket' | 'Feedback' | 'BugReport'
}
```

### Dialog structure

```
┌────────────────────────────────────────────────┐
│  [mode icon]  Create Support Ticket             │  ← title changes per mode
│               ─────────────────────────────────│
│  Category *        [General ▼]                 │
│  Subject *         [____________]              │
│  Priority          [Medium ▼]                  │
│  Description *     [                        ]  │
│                    [                        ]  │
│                                                │
│  ── shown only for Feedback ──                 │
│  How satisfied are you?  ★ ★ ★ ★ ☆             │
│                                                │
│  ── shown only for Bug Report ──               │
│  Bug Severity      [Major ▼]                   │
│  Steps to Reproduce [                       ]  │
│  Expected Behavior  [____________]             │
│  Actual Behavior    [____________]             │
│  Browser / Environment [______]               │
│                                                │
│                    [Cancel]  [Submit]           │
└────────────────────────────────────────────────┘
```

### Title & icon per mode

| Mode | Title | Icon |
|------|-------|------|
| `Ticket` | "Create Support Ticket" | `TicketRegular` |
| `Feedback` | "Share Feedback" | `ThumbLikeRegular` |
| `BugReport` | "Report a Bug" | `BugRegular` |

### Category dropdown

- Fetches `getCategories(initialMode)` on dialog open
- Shows only categories with `appliesTo` containing the current mode

### Form state

- Managed with `useState` per field (no external form library — consistent with project patterns)
- Inline validation on submit: highlights empty required fields with Fluent UI `validationState="error"`
- After successful submission: shows a success message inside the dialog for 2 seconds, then closes

### Success state

```
┌────────────────────────────────────────────────┐
│  ✅  Your ticket has been submitted!            │
│      We'll get back to you shortly.            │
│                    [Close]                      │
└────────────────────────────────────────────────┘
```

---

## 6. Wiring the AvatarMenu

In `Navigation.tsx`, add state to `AvatarMenu` (or lift to `Navigation`):

```tsx
const [supportMode, setSupportMode] = useState<TicketType | null>(null);

// In menu items:
<MenuItem icon={<ChatHelpRegular />} onClick={() => setSupportMode('Ticket')}>Support</MenuItem>
<MenuItem onClick={() => setSupportMode('Feedback')}>Feedback</MenuItem>
<MenuItem onClick={() => setSupportMode('BugReport')}>Report a Bug</MenuItem>

// Dialog:
<SupportDialog
  open={supportMode !== null}
  onClose={() => setSupportMode(null)}
  initialMode={supportMode ?? 'Ticket'}
/>
```

---

## 7. Admin — Ticket Management (Settings)

### Location in Settings

Extend the "Help & Support" category (added by the Help module plan) with a second section: **"Support Tickets"**.

### Component: `SupportTicketsManager.tsx`

**File:** `frontend/src/components/settings/SupportTicketsManager.tsx`

#### Ticket list view

- **Fluent UI `<DataGrid>`** with columns: Type badge, Subject, Category, Status, Priority, Submitted By, Created At
- Filter bar above the grid: type dropdown, status dropdown, priority dropdown, search box
- Row click opens `TicketDetailPanel` (inline right-panel, same pattern as vehicle details)

#### `TicketDetailPanel`

Shows full ticket details and provides:
- **Status** `<Dropdown>` (Open → In Progress → Resolved → Closed)
- **Priority** `<Dropdown>`
- **Assigned To** `<Combobox>` (user search)
- **Admin Notes** `<Textarea>`
- **Save Changes** button

---

## 8. Admin — Category Management (Settings)

### Location in Settings

Third section under "Help & Support": **"Ticket Categories"**.

### Component: `TicketCategoriesManager.tsx`

**File:** `frontend/src/components/settings/TicketCategoriesManager.tsx`

Simple CRUD table:
- Columns: Label, ID, Applies To (badge chips), Sort Order, Active toggle
- Inline row editing via a `<Dialog>` form
- Drag-to-reorder rows to set `SortOrder` (reuse the drag pattern already present in `DashboardLayoutEditor.tsx`)

---

## 9. Migration Plan

New EF Core migration: `AddSupportTicketsTable`

Adds:
- `TicketCategories` table
- `SupportTickets` table with FK to `TicketCategories`, `AspNetUsers` (submitter + assignee)
- Default seed data inserted via `DatabaseSeeder.cs`

---

## 10. Implementation Order

| # | Task | Layer |
|---|------|-------|
| 1 | Create `TicketCategory` + `SupportTicket` models, update `ApplicationDbContext` | Backend |
| 2 | Add seed data to `DatabaseSeeder.cs` | Backend |
| 3 | Run migration `AddSupportTicketsTable` | Backend |
| 4 | Create `SupportDtos.cs` | Backend |
| 5 | Implement `SupportEndpoints.cs` and register in `Program.cs` | Backend |
| 6 | Create `frontend/src/types/support.ts` | Frontend |
| 7 | Create `frontend/src/services/supportService.ts` | Frontend |
| 8 | Build `SupportDialog.tsx` with all three modes (Ticket, Feedback, Bug) | Frontend |
| 9 | Wire `SupportDialog` to the three `<MenuItem>` entries in `AvatarMenu` | Frontend |
| 10 | Build `SupportTicketsManager.tsx` (admin ticket list + detail panel) | Frontend |
| 11 | Build `TicketCategoriesManager.tsx` (admin category CRUD) | Frontend |
| 12 | Add "Support Tickets" + "Ticket Categories" sections to `SettingsPage.tsx` under "Help & Support" | Frontend |

---

## 11. Security Considerations

- **Ownership enforcement** — `GET /api/support/tickets/{id}` returns 403 if the caller is not the ticket submitter and not an `Admin`
- **`SubmittedByUserId` is set server-side** from the JWT — never trusted from the request body
- **Admin-only routes** use `RequireAuthorization("AdminPolicy")` (or `.RequireRole("Admin")`)
- **Input length limits** — enforced both in DTO validation (server) and `maxLength` attributes on `<Input>`/`<Textarea>` components (client)
- **No PII exposure** — `GET /api/support/admin/tickets` returns only `displayName`, never raw email or password fields
- **Category ID validation** — server verifies the submitted `categoryId` exists and `AppliesTo` includes the submitted `type` before persisting
