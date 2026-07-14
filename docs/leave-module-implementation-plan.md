# Leave Module — Implementation Plan

## Overview

This plan replaces the broad enterprise wishlist in [`leave-management.md`](./leave-management.md) with a **phased, CarTrack-specific roadmap**. It is grounded in what already exists in Chronos Portal today: single-tenant platform, ASP.NET Identity users, `StaffProfile` org data, permission-based auth, manager hierarchy, and the shared approval pattern used by Expense.

**Goal:** deliver a usable leave management module inside the existing app launcher (`/leave`) — apply leave, view balances, manager approval, team calendar, HR configuration — without rebuilding multi-company tenancy or a separate employee database.

---

## Current State (as of July 2026)

### Already implemented

| Area | What exists | Key files |
|------|-------------|-----------|
| **Platform auth & users** | JWT, roles, fine-grained permissions | `PermissionCatalog.cs`, `AuthContext` |
| **Staff / org data** | `StaffProfile`: employee number, job title, department, branch, manager | `StaffProfile.cs`, `EmployeesList.tsx`, `UserService` |
| **Manager hierarchy** | Direct + indirect reports, branch resolution | `ManagerHierarchy.cs`, `UserDataScope` |
| **Leave permissions** | `leave.requests.*`, `leave.approvals.*`, `leave.calendar.*`, `leave.policies.*`, `leave.balances.*` | `PermissionCatalog.cs`, `permissions.ts` |
| **App launcher** | Leave module registered; nav items defined | `APP_MODULES`, `LEAVE_NAV_ITEMS`, `/leave/*` route |
| **Leave requests (minimal)** | Create, pending queue, approve/reject | `LeaveRequest.cs`, `LeaveApprovalService.cs`, `LeaveEndpoints.cs` |
| **Approval stub UI** | Submit + approve in Settings → Approvals | `ApprovalsStubPanel.tsx`, `approvalService.ts` |
| **Row-level security** | Managers see their reports' approvals | `UserDataScope.CanApproveLeave`, `CanViewLeaveApproval` |
| **Notifications infra** | In-app notifications + SignalR (not wired to leave yet) | `NotificationService`, `NotificationTriggers` |
| **Audit infra** | Platform security audit log | `SecurityAuditLog`, `SecurityAuditPanel` |

### Current `LeaveRequest` model (simplified)

```
Id, RequesterUserId, ManagerUserId, LeaveType (free-text string),
StartDate, EndDate, Status, RequesterBranch, Notes,
CreatedAt, DecidedAt, DecidedByUserId
```

### API today

| Method | Route | Notes |
|--------|-------|-------|
| `POST` | `/api/leave/requests` | Create request |
| `GET` | `/api/leave/approvals/pending` | Manager pending queue |
| `POST` | `/api/leave/approvals/{id}/decide` | Approve / reject |

### Frontend today

- `/leave/*` → `ModulePlaceholder` (no real pages)
- Nav shell expects: Requests, Approvals, Calendar, Policies, Balances
- Only functional UI is the **Settings stub**, not the leave app

---

## Gap Analysis vs Original Wishlist

The original [`leave-management.md`](./leave-management.md) describes a multi-company LMS with 20+ entities, configurable workflow engine, accrual scheduler, payroll export, SMS/Teams, and mobile. **Most of that is out of scope for the current platform architecture.**

| Original concept | CarTrack approach |
|------------------|-------------------|
| Multi-company / multi-branch tenants | **Single platform** — use `StaffProfile.Branch` / `Department` for scoping |
| Separate `Employee` entity | **Reuse `ApplicationUser` + `StaffProfile`** |
| Configurable multi-step workflow engine | **Phase 1–4:** single manager approval (already built); **Later:** optional HR second step |
| Leave accrual scheduled jobs | **Phase 3:** manual + annual allocation; **Later:** background accrual job |
| LeaveType / LeavePolicy / LeaveBalance tables | **Phase 2–3:** add simplified versions |
| Document storage (medical certs) | **Deferred** — no file module yet |
| Payroll integration | **Deferred** — export-only report in Phase 6 |
| SMS / Teams / Push | **Deferred** — use existing in-app + email notifications |
| Mobile app | **Deferred** — responsive web first |

---

## Architecture Decisions

1. **Module folder:** `CarTrack.Server/Leave/` (extend existing) + `frontend/src/page/leave/` + `frontend/src/components/leave/`
2. **Permissions:** keep existing `leave.*` keys; gate routes and UI with `usePermissions()`
3. **Approval pattern:** mirror Expense (`ExpenseApprovalService`) — shared `ApprovalStatuses`, manager from `StaffProfile`
4. **Dates:** use `DateOnly` for leave dates (already in `LeaveRequest`); frontend uses existing `dateOnly.ts` helpers
5. **Notifications:** add `NotificationTriggers` for leave events (same pattern as vehicle/driver)
6. **No new Company table** — holidays and policies are platform-wide (HR-managed), filtered by branch where needed

---

## Target Data Model (end state)

Entities to add across phases:

### `LeaveType` (Phase 2)
```csharp
Id, Name, Code, Color, IsPaid, DeductsBalance,
RequiresDocument, AllowHalfDay, MaxConsecutiveDays,
MinNoticeDays, IsActive, SortOrder
```

### `PublicHoliday` (Phase 2)
```csharp
Id, Name, Date, IsRecurring, Branch (nullable — null = all branches)
```

### `LeaveBalance` (Phase 3)
```csharp
Id, UserId, LeaveTypeId, CycleStart, CycleEnd,
Allocated, Used, Pending, Adjusted
// Remaining = Allocated + Adjusted - Used - Pending (computed)
```

### `LeaveRequest` (extend Phase 3–4)
Add to existing entity:
```csharp
LeaveTypeId (FK), WorkingDays, TotalDays,
CancelledAt, CancelledByUserId,
DocumentPath (nullable, Phase 7+)
```

### Not building (deferred)
- `Company`, `Branch`, `Department` as separate leave tables (use `StaffProfile`)
- `LeaveApproval` multi-step table (single decision on request for now)
- `Workflow` / `WorkflowStep`
- `WorkSchedule` (Phase 3 uses Sat/Sun + holidays as default)

---

## Phased Implementation

---

### Phase 0 — Foundation ✅ (Complete)

**Status:** Done. No further work unless gaps are found during Phase 1.

- [x] `StaffProfile` with manager link
- [x] Permission catalog for leave submodules
- [x] `LeaveRequest` + create / approve / reject
- [x] Manager hierarchy + RLS
- [x] App launcher + nav definitions
- [x] Settings approval stub

---

### Phase 1 — Leave App Shell & Request UX ✅

**Status:** Implemented (July 2026).

**Goal:** Replace `ModulePlaceholder` with a real leave module. Move request/approval flows out of Settings into `/leave`.

#### Backend
| Task | Detail |
|------|--------|
| `GET /api/leave/requests` | Caller's own requests (all statuses), optional `?status=` filter |
| `GET /api/leave/requests/{id}` | Single request (owner or approver) |
| `POST /api/leave/requests/{id}/cancel` | Cancel own pending request |
| Extend `LeaveApprovalService` | List own requests, get by id, cancel pending |

#### Frontend
| Task | Detail |
|------|--------|
| `LeaveLayout.tsx` | Submodule nav (Requests, Approvals, Calendar, …) using `LEAVE_NAV_ITEMS` |
| `LeaveRequestsPage.tsx` | My requests list + status badges |
| `LeaveRequestForm.tsx` | Apply leave drawer/dialog (type, dates, notes) |
| `LeaveApprovalsPage.tsx` | Manager queue (move logic from `ApprovalsStubPanel`) |
| `leaveService.ts` | Typed API client (replace/extend `approvalService` leave calls) |
| `App.tsx` routes | `/leave`, `/leave/approvals`, placeholder routes for calendar/policies/balances |
| Remove leave stub from Settings | Keep expense stub or split panels |

#### Acceptance criteria
- Employee can apply leave and see their request history from `/leave`
- Manager can approve/reject from `/leave/approvals`
- Settings stub no longer required for leave

---

### Phase 2 — Leave Types & Public Holidays (Configuration) ✅

**Status:** Implemented (July 2026).

**Goal:** Replace free-text `LeaveType` string with admin-managed types and holiday calendar.

**Holiday sync:** Missing years are fetched from [OpenHolidays API](https://openholidaysapi.org) (ZA) and cached in `PublicHolidays`.

#### Backend
| Task | Detail |
|------|--------|
| `LeaveType` entity + migration | Seed defaults: Annual, Sick, Family, Unpaid |
| `PublicHoliday` entity + migration | Seed ZA public holidays |
| `GET /api/leave/types` | Active types for request form |
| Admin CRUD | `GET/POST/PUT/DELETE /api/leave/admin/types` — `leave.policies.write` |
| Admin holidays | `GET/POST/PUT/DELETE /api/leave/admin/holidays` |
| Update create request | Validate `LeaveTypeId`, store FK |

#### Frontend
| Task | Detail |
|------|--------|
| `LeavePoliciesPage.tsx` | HR: manage leave types (Settings or `/leave/policies`) |
| `PublicHolidaysManager.tsx` | HR: holiday CRUD |
| Update request form | Dropdown of leave types instead of free text |

#### Acceptance criteria
- HR can configure leave types and public holidays
- New requests reference a valid `LeaveTypeId`

---

### Phase 3 — Balances & Validation Engine ✅

**Status:** Implemented (July 2026).

**Goal:** Enforce business rules on submission; track balances per user per type.

#### Backend
| Task | Detail |
|------|--------|
| `LeaveBalance` entity + migration | One row per user/type/cycle |
| `LeaveBalanceService` | Get balance, allocate annual entitlement, adjust (HR) |
| Working days calculator | Exclude weekends + `PublicHoliday` (branch-aware) |
| Validators on create | Sufficient balance, no overlap, min notice, max consecutive |
| On approve | Move days from `Pending` → `Used` |
| On reject/cancel | Release `Pending` |
| `GET /api/leave/balances` | Own balances |
| `GET /api/leave/balances/{userId}` | HR/manager view — `leave.balances.read` |
| `POST /api/leave/admin/balances/adjust` | HR manual adjustment |

#### Frontend
| Task | Detail |
|------|--------|
| `LeaveBalancesPage.tsx` | Employee balance cards per type |
| Request form | Show remaining balance, working days preview, inline validation errors |
| HR balance adjustment | Dialog in policies/balances admin section |

#### Acceptance criteria
- Cannot submit leave that exceeds balance or overlaps existing approved/pending leave
- Working days exclude weekends and configured holidays
- Approved leave deducts from balance

---

### Phase 4 — Team Calendar ✅

**Status:** Implemented (July 2026).

**Goal:** Visual calendar for managers and staff to see who is away.

#### Backend
| Task | Detail |
|------|--------|
| `GET /api/leave/calendar` | Query params: `start`, `end`, `branch?`, `department?`, `userId?` |
| Return approved + pending leave in range | Scoped by RLS (manager sees reports, HR sees all) |
| Include public holidays in response | For calendar overlay |

#### Frontend
| Task | Detail |
|------|--------|
| `LeaveCalendarPage.tsx` | Month view (start simple: list-by-day or use existing chart/calendar component) |
| Filters | Branch, department, team |
| Legend | Leave type colors from `LeaveType.Color` |

#### Acceptance criteria
- Manager sees team leave on calendar
- Employee sees own + team (optional) leave

---

### Phase 5 — Notifications & Audit ✅

**Status:** Implemented (July 2026).

**Goal:** Wire leave lifecycle into platform notifications and audit trail.

#### Backend
| Task | Detail |
|------|--------|
| `NotificationTriggers` | `LeaveSubmitted`, `LeaveApproved`, `LeaveRejected`, `LeaveCancelled` |
| Fire on create / decide / cancel | Notify requester + manager |
| `SecurityAuditService` | Log leave create, decide, cancel, balance adjust |

#### Frontend
| Task | Detail |
|------|--------|
| No new UI required | Uses existing notifications bell |
| Optional email | If `IEmailService` enabled, send on approval/rejection |

#### Acceptance criteria
- Manager receives notification when report submits leave
- Employee receives notification on approval/rejection
- Audit log shows leave actions

---

### Phase 6 — Reports & HR Dashboard ✅

**Status:** Implemented (July 2026).

**Goal:** Operational reports for HR and managers.

#### Backend
| Task | Detail |
|------|--------|
| `GET /api/leave/reports/summary` | Counts by type/status/department |
| `GET /api/leave/reports/history` | Filterable leave history |
| `GET /api/leave/reports/liability` | Unused leave / balance liability |
| `GET /api/leave/reports/pending` | All pending approvals (HR) |
| CSV export | `?format=csv` on history endpoint |

#### Frontend
| Task | Detail |
|------|--------|
| Reports section in `/leave` or Reports module | Tables + export buttons |
| HR dashboard widgets | Pending count, on-leave today, liability summary |

#### Acceptance criteria
- HR can export leave history CSV
- Manager can see department leave summary

---

### Phase 7 — Document Uploads ✅

**Status:** Implemented (July 2026).

**Goal:** Allow employees to attach supporting documents (e.g. medical certificates) to leave requests.

#### Backend
| Task | Detail |
|------|--------|
| `LeaveDocumentStorage` | Local file storage under `App_Data/leave-documents/` |
| Extend `LeaveRequest` | `DocumentPath`, `DocumentFileName`, `DocumentContentType` |
| Multipart create | `POST /api/leave/requests` accepts `multipart/form-data` with optional `document` |
| Upload / download | `POST/GET /api/leave/requests/{id}/document` |
| Validation | Required when `LeaveType.RequiresDocument`; block approval without document |
| Audit | `leave.request.document.uploaded` |

#### Frontend
| Task | Detail |
|------|--------|
| Request form | File input when leave type requires a document |
| Tables | Download link on requests and approvals |

#### Acceptance criteria
- Sick leave (and other `RequiresDocument` types) cannot be submitted without a document
- Managers cannot approve document-required leave without an attachment
- Authorized users can download attached documents

---

### Phase 8+ — Deferred (Future)

Build only when there is a concrete business requirement:

| Feature | Notes |
|---------|-------|
| **Multi-step approval** | HR second step, finance sign-off; needs workflow table |
| **Payroll export** | Fixed-width or API integration |
| **Outlook / Google Calendar sync** | External calendar APIs |
| **Mobile app** | React Native or PWA |
| **Multi-company tenancy** | Would require platform-wide architectural change |

---

### Phase 8 — Accrual & Half-Day Leave ✅

**Status:** Implemented (July 2026).

**Goal:** Monthly leave accrual and half-day booking.

#### Backend
| Task | Detail |
|------|--------|
| `LeaveType.AccrualMethod` | `Upfront` or `Monthly` |
| `LeaveType.AnnualEntitlement` | Days per year for accrual/allocation |
| `LeaveBalance.LastAccruedYearMonth` | Tracks monthly accrual progress |
| `LeaveAccrualService` + background job | Daily accrual for active staff |
| `POST /api/leave/admin/accrual/run` | Manual accrual trigger (HR) |
| `LeaveRequest` day portions | `StartDayPortion` / `EndDayPortion` (`Full` / `Half`) |
| Working days calculator | Applies 0.5-day deductions for half days |

#### Frontend
| Task | Detail |
|------|--------|
| Request form | Half-day / first-last day options when type allows |
| Leave type admin | Accrual method, entitlement, run accrual button |

#### Acceptance criteria
- Monthly accrual adds entitlement/12 each month for configured types
- Half-day leave deducts 0.5 from balance per applicable day
- Types without `AllowHalfDay` reject half-day requests

---

## Implementation Order Summary

| Phase | Focus | Depends on |
|-------|-------|------------|
| **0** | Foundation (staff, permissions, basic API) | — ✅ |
| **1** | Leave app UI + own requests + approvals page | Phase 0 |
| **2** | Leave types + public holidays | Phase 1 |
| **3** | Balances + validation engine | Phase 2 |
| **4** | Team calendar | Phase 1, 3 |
| **5** | Notifications + audit | Phase 1, notifications module |
| **6** | Reports + HR dashboard | Phase 3 |
| **7** | Document uploads | Phase 1, 2 |
| **8** | Accrual + half-day leave | Phase 3 |
| **9+** | Payroll, multi-step approval, mobile | TBD |

---

## Suggested File Layout (Phases 1–6)

```
CarTrack.Server/
  Leave/
    LeaveEndpoints.cs          (extend)
    LeaveApprovalService.cs    (extend → LeaveService)
    LeaveBalanceService.cs     (Phase 3)
    LeaveCalendarService.cs    (Phase 4)
    LeaveReportService.cs      (Phase 6)
    LeaveDtos.cs
    LeaveValidation.cs         (Phase 3)
    LeaveNotificationTriggers.cs (Phase 5)
  Data/
    LeaveRequest.cs            (extend)
    LeaveType.cs               (Phase 2)
    PublicHoliday.cs           (Phase 2)
    LeaveBalance.cs            (Phase 3)

frontend/src/
  page/leave/
    LeaveLayout.tsx
    LeaveRequestsPage.tsx
    LeaveApprovalsPage.tsx
    LeaveCalendarPage.tsx      (Phase 4)
    LeaveBalancesPage.tsx      (Phase 3)
    LeavePoliciesPage.tsx      (Phase 2)
  components/leave/
    LeaveRequestForm.tsx
    LeaveRequestTable.tsx
    LeaveBalanceCard.tsx
    LeaveCalendarView.tsx
    PublicHolidaysManager.tsx
  services/leaveService.ts
  types/leave.ts
```

---

## Security Considerations

- **`RequesterUserId` / `ManagerUserId`** — always set server-side from JWT + `StaffProfile`, never from request body
- **Cancel** — only pending requests owned by caller
- **Approve** — enforce `UserDataScope.CanApproveLeave` (already implemented)
- **Balance adjust** — `leave.balances.write` or HR role only
- **Admin type/holiday CRUD** — `leave.policies.write`
- **Calendar/history** — RLS: managers see reports, HR/Admin see all

---

## Relationship to Expense Module

Leave and Expense share the same approval skeleton. When implementing Phase 1, consider extracting shared patterns (optional, not blocking):

- Shared `ApprovalDecisionRequest` DTO (already in frontend `approval.ts`)
- Shared pending-queue UI component
- Parallel notification triggers

Do **not** merge services — keep `LeaveApprovalService` and `ExpenseApprovalService` separate.

---

## Reference Documents

| Document | Purpose |
|----------|---------|
| [`leave-management.md`](./leave-management.md) | Original enterprise wishlist (archived reference) |
| [`notifications-implementation-plan.md`](./notifications-implementation-plan.md) | Pattern for Phase 5 triggers |
| [`support-module-implementation-plan.md`](./support-module-implementation-plan.md) | Pattern for module endpoints + settings UI |

---

## Next Step

**Apply migration:** `dotnet ef database update --project CarTrack.Server`

Configure leave types under **Leave → Policies** (set Annual to Monthly accrual if desired), then use **Run accrual** or wait for the daily background job.
