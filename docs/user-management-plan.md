# User Management — Implementation Plan

## Overview

TalisTrack is evolving from a **fleet-only** application into a **multi-module platform**. Users are not a single flat type: a person may be a **driver**, **staff**, **manager**, and **finance user** at the same time. Managers are themselves users in the system and form an **organisational hierarchy**.

This plan defines how to model users, roles, and permissions in a way that:

1. Unifies **login identity** (`ApplicationUser`) with **business person records** (driver/staff profiles).
2. Supports **multiple roles per user** without duplicating accounts.
3. Gates access to **modules** (App Launcher apps) and **submodules** (in-app navigation areas) consistently on backend and frontend.
4. Aligns with the modules already listed in `frontend/src/components/AppLauncher.tsx` and the fleet submodules already shipped in `SideNavigation`.

---

## Current State

| Area | Status |
|------|--------|
| **Authentication** | ASP.NET Core Identity (`ApplicationUser`, JWT). Seeded `Admin` role only. |
| **`ApplicationUser`** | `IdentityUser` + optional `DisplayName`. No link to `Driver`. |
| **`Driver` entity** | Standalone HR/fleet record (`Drivers` table). `Manager` is a **free-text string**, not a user FK. |
| **Frontend auth** | `AuthUser` exposes `roles: string[]`. No module/submodule permissions. |
| **Authorisation** | Endpoints use `RequireAuthorization()`; few role checks beyond future Help/Notifications admin routes. |
| **User admin UI** | None. Settings → Profile shows the signed-in user only. |
| **App Launcher** | Seven modules declared (see below); only **Fleet** is implemented. |

**Gap:** Drivers and staff are managed as data records, not as platform users. There is no concept of “this driver can log in”, “this manager approves leave for these staff”, or “finance users see accounting but not payroll”.

---

## Goals

1. **Single sign-on identity** per person (`ApplicationUser`), with optional links to domain profiles.
2. **Many roles per user** (e.g. Driver + Staff + Manager + Finance).
3. **Manager hierarchy** where `ManagerUserId` points to another `ApplicationUser` (replacing string `Manager` on `Driver`).
4. **Module + submodule permissions** derived from roles, enforced server-side and reflected in App Launcher + side nav.
5. **Admin UX** to create users, assign roles, set manager, link/unlink driver profile, activate/deactivate access.
6. **Backward compatibility** with existing `Drivers` data and fleet features during migration.

---

## Module & Submodule Catalog

Modules match the **App Launcher** list (`AppLauncher.tsx`). Each module exposes **submodules** (routes or functional areas). Submodule slugs are stable permission keys.

### Platform (cross-cutting — not in App Launcher today)

| Module slug | Name | Submodules |
|-------------|------|------------|
| `platform` | Platform | `settings`, `help`, `notifications`, `support` |

Implemented or planned in `docs/` (Help, Notifications, Support).

### Fleet (`fleet`) — **implemented**

| Submodule slug | Name | Route / area |
|----------------|------|----------------|
| `fleet.dashboard` | Home | `/` |
| `fleet.tracking` | Live tracking | `/live-tracking` |
| `fleet.vehicles` | Vehicles | `/vehicle-list` |
| `fleet.drivers` | Drivers | `/drivers` |
| `fleet.reports` | Reports & dashboards | `/reports` |

Side nav: `SideNavigation.tsx`. CarTrack API integration, dashboards, reports.

### Accounting (`accounting`)

| Submodule slug | Name | Planned capability |
|----------------|------|--------------------|
| `accounting.billing` | Billing | Customer billing runs |
| `accounting.invoicing` | Invoicing | Issue & track invoices |
| `accounting.receivables` | Receivables | Outstanding balances |
| `accounting.settings` | Accounting settings | Tax, terms, templates |

App Launcher description: *Billing and Invoicing*.

### Leave (`leave`)

| Submodule slug | Name | Planned capability |
|----------------|------|--------------------|
| `leave.requests` | My leave | Submit/view own leave |
| `leave.approvals` | Approvals | Manager approval queue |
| `leave.calendar` | Team calendar | Team availability |
| `leave.policies` | Policies | Leave types & rules |
| `leave.balances` | Balances | Entitlement tracking |

Typical actors: **Staff** (request), **Manager** (approve), **HR/Admin** (policies).

### Expense (`expense`)

| Submodule slug | Name | Planned capability |
|----------------|------|--------------------|
| `expense.claims` | My claims | Submit expenses |
| `expense.approvals` | Approvals | Manager/finance approval |
| `expense.categories` | Categories | GL mapping |
| `expense.reports` | Reports | Spend analytics |

### Payroll (`payroll`)

| Submodule slug | Name | Planned capability |
|----------------|------|--------------------|
| `payroll.runs` | Payroll runs | Process pay periods |
| `payroll.payslips` | Payslips | Employee payslip view |
| `payroll.deductions` | Deductions | Statutory & voluntary |
| `payroll.settings` | Payroll settings | Pay calendars, rules |

Restricted to **Finance** / **Payroll Admin** roles by default.

### Performance (`performance`)

| Submodule slug | Name | Planned capability |
|----------------|------|--------------------|
| `performance.reviews` | Reviews | Review cycles |
| `performance.goals` | Goals | OKRs / targets |
| `performance.feedback` | Feedback | Continuous feedback |
| `performance.reports` | Reports | Performance analytics |

### Recruitment (`recruitment`)

| Submodule slug | Name | Planned capability |
|----------------|------|--------------------|
| `recruitment.jobs` | Job posts | Vacancies |
| `recruitment.applications` | Applications | Candidate pipeline |
| `recruitment.interviews` | Interviews | Scheduling |
| `recruitment.offers` | Offers | Offer management |

---

## Domain Model

### Core principle: Identity vs profile vs role

```
ApplicationUser          ← authentication (login, lockout, JWT)
    │
    ├── StaffProfile     ← employment record (optional)
    │       └── ManagerUserId → ApplicationUser
    │
    ├── DriverProfile    ← link to existing Driver row (optional)
    │
    └── UserRoleAssignment[]  ← many roles per user
            └── Role → RolePermission[] → Permission (module.submodule + action)
```

A user may have **only** `StaffProfile`, **only** `DriverProfile`, or **both**. Example: a driver who is also office staff with manager duties carries Driver + Staff profiles and roles `Driver`, `Staff`, `Manager`.

### 1. `ApplicationUser` (extend existing)

```csharp
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    // Navigation
    public StaffProfile? StaffProfile { get; set; }
    public DriverProfileLink? DriverLink { get; set; }
}
```

Login is blocked when `IsActive == false`.

### 2. `StaffProfile` (new)

Employment data for non-driver or dual-hat users. Migrates concepts from `Driver` where overlapping (department, branch, job title).

```csharp
public class StaffProfile
{
    public Guid Id { get; set; }
    public string UserId { get; set; }              // FK → ApplicationUser (1:1)
    public string? EmployeeNumber { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? Branch { get; set; }
    public string EmploymentStatus { get; set; } = "Active";
    public DateOnly? WorkStartDate { get; set; }

    public string? ManagerUserId { get; set; }      // FK → ApplicationUser (nullable)
    public ApplicationUser? Manager { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

**Manager rules:**

- A user’s manager must be another active user with a `StaffProfile` (or `Manager` role).
- Prevent cycles in manager chain (validate on save).
- Delegation / acting manager is out of scope for v1.

### 3. `DriverProfileLink` (new — bridge to existing `Driver`)

Keep `Drivers` as the fleet domain table; link rather than merge in v1.

```csharp
public class DriverProfileLink
{
    public Guid Id { get; set; }
    public string UserId { get; set; }              // FK → ApplicationUser (1:1)
    public Guid DriverId { get; set; }              // FK → Driver (1:1)
    public Driver Driver { get; set; } = null!;
}
```

When creating a user who is a driver:

- Option A: link to existing `Driver` by `DriverCode` / email match.
- Option B: create `Driver` + link in one transaction.

Eventually `Driver.Manager` string column is **deprecated** in favour of `StaffProfile.ManagerUserId` when the driver is also staff; for driver-only users, manager may remain fleet-specific or gain `Driver.ManagerUserId` in phase 2.

### 4. Roles & permissions

Use **application roles** (Identity) for coarse bundles, plus a **permission matrix** for module/submodule access.

#### 4.1 Seeded roles (initial set)

| Role slug | Purpose | Typical modules |
|-----------|---------|-----------------|
| `SystemAdmin` | Full platform access | All |
| `FleetAdmin` | Fleet configuration | `fleet.*`, `platform.settings` (fleet slice) |
| `FleetOperator` | Day-to-day fleet ops | `fleet.dashboard`, `fleet.tracking`, `fleet.vehicles`, `fleet.drivers`, `fleet.reports` (read) |
| `Driver` | Driver self-service | `fleet.tracking` (own vehicle), `leave.requests`, `expense.claims`, `payroll.payslips` |
| `Staff` | General employee | Submodule defaults per HR policy |
| `Manager` | Line management | Approvals submodules in leave/expense/performance |
| `Finance` | Accounting & payroll | `accounting.*`, `payroll.*`, `expense.approvals` |
| `HR` | People operations | `leave.policies`, `recruitment.*`, `performance.*`, user read |

Users receive **multiple** Identity roles. Effective permissions = **union** of all role permissions.

#### 4.2 `Permission` (new)

```csharp
public class Permission
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;   // e.g. "fleet.vehicles.read"
    public string ModuleSlug { get; set; } = string.Empty;
    public string SubmoduleSlug { get; set; } = string.Empty;
    public string Action { get; set; } = "read";      // read | write | admin
    public string Description { get; set; } = string.Empty;
}
```

#### 4.3 `RolePermission` (new)

Maps `IdentityRole` → `Permission` (many-to-many).

#### 4.4 JWT / session claims

On login and token refresh, embed:

```json
{
  "roles": ["Driver", "Manager", "Finance"],
  "permissions": ["fleet.tracking.read", "leave.approvals.write", "accounting.invoicing.read"],
  "modules": ["fleet", "leave", "expense", "accounting"]
}
```

`modules` is derived from permissions for fast App Launcher filtering.

---

## Authorisation Behaviour

### Backend

1. **Policy-based** — `RequirePermission("fleet.vehicles.write")` on minimal APIs.
2. **Module group registration** — `MapFleetEndpoints().RequirePermission("fleet.vehicles.read")` etc.
3. **Row-level (phase 2)** — drivers see only assigned vehicles; managers see direct reports’ leave/expense claims.

### Frontend

1. **`usePermissions()`** hook — reads claims from `AuthUser` / token parse.
2. **App Launcher** — show only modules where user has any `module.*` permission; grey out vs hide based on product decision (recommend **hide** unavailable modules).
3. **Side navigation** — filter links by submodule permission (fleet already has five links).
4. **Route guards** — `ProtectedRoute` extended with `requiredPermission` prop; redirect to “Access denied” or home.
5. **Settings → Users** (admin) — new area under `platform.settings` for user CRUD.

### Example: user with Driver + Manager + Finance

| Role | Sees in App Launcher |
|------|----------------------|
| Driver | Fleet (limited), Leave, Expense, Payroll (payslips) |
| Manager | + Leave/Expense approvals |
| Finance | + Accounting, Payroll runs |

Fleet submodule `fleet.drivers` may be **read-only** or hidden for pure drivers; managers in fleet may get write on drivers in their branch (phase 2).

---

## User Lifecycle

### Create user (admin)

1. Enter identity: username, email, display name, temporary password (or invite email — phase 2).
2. Select **person type(s)**: Staff profile, Driver link, or both.
3. Assign **one or more roles** (checkboxes with role descriptions).
4. If Staff: set department, job title, **manager** (searchable user picker).
5. If Driver: link existing driver or create driver record.
6. Set `IsActive` (default true).

### Update user

- Roles added/removed → permissions recalculated on next token refresh.
- Manager change → update `StaffProfile.ManagerUserId`; optional notification to old/new manager.
- Deactivate → `IsActive = false`, revoke refresh tokens, hide from pickers.

### Self-service (phase 2)

- Profile photo, phone, password change (already partial via Identity).
- View own roles (read-only), not edit.

### Invite flow (phase 2)

- Admin creates user without password → email invite with setup link.

---

## API Surface (planned)

Base path: `/api/users` (admin) and `/api/me` (self).

| Method | Route | Permission | Description |
|--------|-------|------------|-------------|
| `GET` | `/api/users` | `platform.settings.admin` | Paginated user list with roles & profiles |
| `GET` | `/api/users/{id}` | `platform.settings.admin` | User detail |
| `POST` | `/api/users` | `platform.settings.admin` | Create user + profiles + roles |
| `PUT` | `/api/users/{id}` | `platform.settings.admin` | Update profiles & metadata |
| `PUT` | `/api/users/{id}/roles` | `platform.settings.admin` | Replace role set |
| `PATCH` | `/api/users/{id}/active` | `platform.settings.admin` | Activate/deactivate |
| `GET` | `/api/users/managers` | `platform.settings.admin` | Search users eligible as managers |
| `GET` | `/api/me` | authenticated | Current user + permissions + modules |
| `GET` | `/api/roles` | `platform.settings.admin` | Role catalog with permission summary |
| `GET` | `/api/permissions` | `platform.settings.admin` | Full permission matrix |

Extend existing auth login/refresh responses to include `permissions` and `modules`.

---

## Frontend UX (planned)

### Settings → Users & access (new section)

Submodule: `platform.settings.users`

- **Users table** — name, email, roles (badges), manager, active, last login.
- **User form dialog** — tabs: Identity | Roles | Staff | Driver link.
- **Role picker** — multi-select with module impact summary (“Grants access to: Fleet, Leave, …”).
- **Manager picker** — combobox of users with `Manager` or `Staff` role.

### App Launcher integration

- Replace static `apps` array with **permission-filtered** list from `GET /api/me` or static catalog + client filter.
- Checkbox on cards (future: “pin to home”) remains optional.

### Fleet driver screen

- Show **Linked user account** on driver detail when `DriverProfileLink` exists.
- Action: “Create login for this driver” → opens user create with driver pre-linked.

---

## Data Migration Strategy

### Phase 1 — Additive

1. Add `StaffProfile`, `DriverProfileLink`, `Permission`, `RolePermission` tables.
2. Extend `ApplicationUser` columns.
3. Seed permissions for all module/submodule slugs above; seed roles and role-permission mappings.
4. Migrate seeded admin → `SystemAdmin` role with full permissions.
5. **Do not** drop `Driver.Manager` string yet; display both until staff profiles backfilled.

### Phase 2 — Link existing drivers

- Optional admin tool: “Bulk link users by work email” (`Driver.WorkEmail` = `ApplicationUser.Email`).

### Phase 3 — Deprecate string manager

- Backfill `StaffProfile` for drivers who are employees; copy `Driver.Manager` text to manager lookup where possible.

---

## Implementation Phases

### Phase 1 — Foundation (MVP)

- [ ] Permission + RolePermission entities and seed data (all modules/submodules from this doc).
- [ ] Expand roles beyond `Admin` → `SystemAdmin`, `FleetAdmin`, `FleetOperator`, `Staff`, `Manager`, `Driver`, `Finance`, `HR`.
- [ ] `StaffProfile`, `DriverProfileLink` entities + migrations.
- [ ] User admin API (`/api/users`, `/api/roles`).
- [ ] JWT claims: roles + permissions + modules.
- [ ] Backend permission authorization handler.
- [ ] Frontend: extend `AuthUser`, `usePermissions`, filter App Launcher + fleet side nav.
- [ ] Settings UI: Users & access (CRUD).

**Exit criteria:** Admin can create a finance user who sees Accounting in App Launcher but not Fleet; create a driver user linked to a `Driver` row.

### Phase 2 — Manager hierarchy & approvals prep

- [ ] Manager picker, cycle detection, org chart API (`GET /api/users/{id}/reports`).
- [ ] Wire manager FK into leave/expense approval stubs (when those modules start).
- [ ] Fleet: show linked user on driver detail; create-login-from-driver flow.

### Phase 3 — Row-level security

- [ ] Drivers: scope vehicle/trip data to assignments.
- [ ] Managers: scope approval queues to direct/indirect reports.
- [ ] Finance: branch/cost-centre scoping if needed.

### Phase 4 — Self-service & invites

- [ ] Email invite, password setup, MFA (optional).
- [ ] Audit log for role and permission changes.

---

## Relationship to Existing `Driver` Entity

| `Driver` field | Future home |
|----------------|-------------|
| Identity/login | `ApplicationUser` |
| `FirstName`, `LastName`, `WorkEmail`, `WorkPhone` | Remain on `Driver` for fleet; duplicated on `ApplicationUser` for login display — sync on link |
| `Department`, `Branch`, `JobTitle`, `Manager` | Prefer `StaffProfile` when user is staff; keep on `Driver` for fleet-only records |
| `LicenceNumber`, `IdNumber`, PDP | Stay on `Driver` only |

**Rule:** One person → one `ApplicationUser`. Multiple **roles** and optional **profiles**, not multiple accounts.

---

## Security Notes

- Permission checks must run **server-side**; frontend hiding is UX only.
- Role changes should force token refresh or short access-token TTL (existing JWT pattern).
- `SystemAdmin` bypass should be explicit in code, not implicit absence of checks.
- Deactivated users: fail login with generic message; invalidate refresh tokens.
- Audit sensitive actions (role elevation, impersonation if added later).

---

## Open Questions

1. **Tenant / organisation** — single-tenant for now; if multi-tenant later, scope users and permissions by `OrganisationId`.
2. **Driver-only login** — may drivers use mobile-only fleet tracking without staff profile? (Recommend yes: `Driver` role + `DriverProfileLink` only.)
3. **Custom roles** — v1 seeded roles only, or admin-defined custom roles in UI? (Recommend seeded v1, custom roles phase 2.)
4. **Permission granularity** — submodule-level `read/write/admin` vs per-action keys; start with submodule `read` + `write`.
5. **CarTrack API credentials** — remain organisation-level in Settings, not per-user (current design).

---

## File Layout (target)

```
CarTrack.Server/
  Users/
    UserEndpoints.cs
    UserService.cs
    UserDtos.cs
    UserMapper.cs
    PermissionSeeder.cs
    Authorization/
      PermissionRequirement.cs
      PermissionAuthorizationHandler.cs
  Data/
    StaffProfile.cs
    DriverProfileLink.cs
    Permission.cs
    RolePermission.cs

frontend/src/
  types/user.ts
  services/userService.ts
  context/PermissionsContext.tsx   # or extend AuthContext
  components/users/
    UsersTable.tsx
    UserFormDialog.tsx
    ManagerPicker.tsx
    RolePicker.tsx
  page/settings/UsersSettingsPanel.tsx
  utils/permissions.ts             # hasPermission(), filterModules()
```

---

## Summary

User management in TalisTrack centres on **one identity per person**, **many roles**, optional **staff and driver profiles**, and a **manager hierarchy** between users. Access to the seven App Launcher modules (plus platform services) is controlled by a **module.submodule permission matrix** seeded from the catalog in this document. Fleet is the first implemented module; accounting, leave, expense, payroll, performance, and recruitment inherit the same pattern as they are built.

This plan should be read alongside:

- `docs/help-module-implementation-plan.md`
- `docs/notifications-implementation-plan.md`
- `docs/support-module-implementation-plan.md`
- `docs/dashboard-reports-implementation-plan.md`
