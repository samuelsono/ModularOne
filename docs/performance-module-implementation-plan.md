# Performance Management Module — Implementation Plan

## Overview

Phased roadmap for the **Performance** app (`/performance`) inside Chronos Portal. Grounded in the modular monolith ([ADR 0001](./adr/0001-module-boundaries.md)) and the existing launcher placeholders / permission keys.

**Goal:** managers and employees run goal tracking, continuous feedback, and structured review cycles without a separate HCM database or multi-tenant tenancy.

Parent overview: [hcm-modules-overview.md](./hcm-modules-overview.md).

---

## Current State (July 2026)

| Area | What exists |
|------|-------------|
| Launcher card | Placeholder `performance` → `/performance` |
| Nav shell | Reviews, Goals, Feedback, Reports (`PLACEHOLDER_NAV_ITEMS`) |
| Permissions | `performance.reviews.*`, `goals.*`, `feedback.*`, `reports.*` |
| Role grants | HR (full module); Manager (reviews/feedback read); Staff (none yet for write) |
| Backend module | **None** — no `CarTrack.Modules.Performance` |
| Frontend module | **None** — placeholder only |
| People / managers | Users `StaffProfile` + manager hierarchy |

---

## Product scope (MVP vs later)

### In scope (Phases 0–4)

- Goal setting (employee + manager) with progress
- Continuous feedback (praise / coaching notes)
- Review cycles (self + manager review forms)
- Manager inbox for direct reports
- Basic reports (completion %, rating distribution)
- In-app (+ email via Notifications) for cycle events

### Out of scope / deferred

| Idea | Why deferred |
|------|----------------|
| 360° / peer / upward reviews | Needs invite workflow + anonymity rules |
| Complex competency frameworks / job-family matrices | Heavy config; start with free-text + rating scales |
| Calibration sessions / forced ranking | Specialty HR process |
| OKR cascading org trees | Nice-to-have after goals work |
| Compensation / bonus recommendations tied to payroll | Wait for Payroll module |
| Document storage for review attachments | No first-class file module yet |

---

## Architecture Decisions

1. **Module key:** `performance`  
   - Backend: `Modules/CarTrack.Modules.Performance`  
   - Frontend: `frontend/src/modules/performance`
2. **Schema / DbContext:** own `PerformanceDbContext`, migrations history table, no FKs to Users tables — store `string SubjectUserId`, `ManagerUserId`, etc.
3. **Org data:** read via existing org directory / Users Contracts only.
4. **Approvals:** review **submission** is not an Expense-style approve/reject of money; use cycle **lock / submit / acknowledge** states. Optional manager “sign-off” is a status transition on `PerformanceReview`.
5. **Permissions:** keep catalog keys; extend Staff role later with `performance.goals.read/write`, `performance.feedback.write`, own `reviews.read/write`.
6. **Events:** publish `ReviewCycleOpened`, `ReviewSubmitted`, `GoalUpdated` for Notifications; consume nothing from Payroll in MVP.

---

## Target Data Model

### `ReviewCycle`

```text
Id, Name, StartDate, EndDate, Status (Draft | Open | Closed),
IncludesSelfReview, IncludesManagerReview,
RatingScaleMin, RatingScaleMax,
CreatedByUserId, CreatedAt, ClosedAt
```

### `PerformanceReview`

```text
Id, CycleId, SubjectUserId, ManagerUserId,
SelfRating (nullable), ManagerRating (nullable),
SelfSummary, ManagerSummary,
SelfSubmittedAt, ManagerSubmittedAt, AcknowledgedAt,
Status (NotStarted | SelfInProgress | ManagerInProgress | Completed | Cancelled)
```

### `ReviewCompetency` (Phase 3+)

```text
Id, ReviewId, Label, SelfScore, ManagerScore, Comments
```

### `Goal`

```text
Id, OwnerUserId, ManagerUserId (nullable),
Title, Description, TargetDate,
Status (Draft | Active | Completed | Cancelled),
ProgressPercent, Visibility (Private | Manager | Team),
LinkedCycleId (nullable), CreatedAt, UpdatedAt
```

### `GoalCheckIn` (Phase 2+)

```text
Id, GoalId, AuthorUserId, Note, ProgressPercent, CreatedAt
```

### `Feedback`

```text
Id, FromUserId, ToUserId,
Type (Praise | Coaching | Request),
Body, IsPrivate, CreatedAt
```

### Not building (deferred)

- Peer nomination tables
- CalibrationRoom / ForcedDistribution
- CompetencyLibrary with versioning
- Attachment blobs

---

## Phased Implementation

### Phase 0 — Scaffold & shell

**Goal:** empty but real module replacing the placeholder.

| Task | Detail |
|------|--------|
| Scaffold backend | `dotnet new cartrack-module -n Performance …` |
| Register Host + architecture tests | Per [adding-a-module.md](./adding-a-module.md) |
| Scaffold frontend | Copy `_template` → `modules/performance` |
| Register module + lint boundaries | `modules.tsx`, ESLint FEATURE_MODULES |
| Nav + routes | Reviews home placeholder, Goals, Feedback, Reports stubs |
| Health endpoint | Prove module boots |

**Exit:** `/performance` renders module UI (not generic placeholder), no business data yet.

---

### Phase 1 — Goals (employee + manager)

**Goal:** usable goal tracking for staff and their managers.

#### Backend

| API | Notes |
|-----|-------|
| `GET/POST /api/performance/goals` | Own goals; managers can list reports |
| `GET/PATCH /api/performance/goals/{id}` | Owner or manager |
| `POST /api/performance/goals/{id}/check-ins` | Progress updates |
| Row security | Manager hierarchy via Users org APIs |

#### Frontend

- Goals list + create/edit drawer
- Progress slider / check-in timeline
- Manager filter: “My team”

#### Permissions

- Grant Staff `performance.goals.read/write`
- Manager inherits + can edit reports’ active goals (policy TBD: edit vs comment-only)

**Exit:** Staff can set and update goals; manager sees team goals.

---

### Phase 2 — Continuous feedback

| API | Notes |
|-----|-------|
| `GET/POST /api/performance/feedback` | Inbox / sent |
| Visibility | Private = only from/to + HR |

#### Frontend

- Feedback feed (praise/coaching)
- “Give feedback” compose

#### Notifications

- Notify recipient on new feedback

**Exit:** Managers and peers (same department optional later) can leave feedback; Staff read own.

---

### Phase 3 — Review cycles

| API | Notes |
|-----|-------|
| CRUD cycles | HR `performance.reviews.write` |
| `POST …/cycles/{id}/open` / `close` | Lifecycle |
| `GET/POST/PATCH …/reviews` | Auto-create reviews for eligible staff on open |
| Submit self / submit manager / acknowledge | Status machine |

#### Frontend

- HR: cycle configuration
- Employee: self-review form
- Manager: team review queue
- Completed review read-only summary

**Exit:** One end-to-end cycle for a pilot team.

---

### Phase 4 — Reports & polish

- Completion rates by department/branch
- Average ratings (with access control)
- Page search providers
- Export CSV for HR
- Wire role seeds for Staff review write on open cycles

**Exit:** HR can report on cycle health without SQL.

---

### Phase 5+ (later)

- Competency line items on reviews
- 360 invites
- Goals ↔ cycle automatic rollup
- Attachment upload
- Optional export event for future compensation planning

---

## Suggested API surface (end of Phase 3)

```http
GET    /api/performance/goals
POST   /api/performance/goals
GET    /api/performance/goals/{id}
PATCH  /api/performance/goals/{id}
POST   /api/performance/goals/{id}/check-ins

GET    /api/performance/feedback
POST   /api/performance/feedback

GET    /api/performance/cycles
POST   /api/performance/cycles
POST   /api/performance/cycles/{id}/open
POST   /api/performance/cycles/{id}/close

GET    /api/performance/reviews
GET    /api/performance/reviews/{id}
POST   /api/performance/reviews/{id}/submit-self
POST   /api/performance/reviews/{id}/submit-manager
POST   /api/performance/reviews/{id}/acknowledge

GET    /api/performance/reports/cycle-summary
```

---

## Frontend routes

| Path | Permission | Phase |
|------|------------|-------|
| `/performance` | `performance.reviews.read` | 0–3 (home = reviews) |
| `/performance/goals` | `performance.goals.read` | 1 |
| `/performance/feedback` | `performance.feedback.read` | 2 |
| `/performance/reports` | `performance.reports.read` | 4 |

---

## Testing & acceptance (MVP)

- [ ] Architecture tests: no module→module internals
- [ ] Staff cannot read another employee’s private feedback
- [ ] Manager can only review direct/indirect reports per org rule
- [ ] Cycle close freezes edits
- [ ] Notifications fire on feedback + review submit
- [ ] Lint boundaries + `npm run build` green

---

## Open questions

1. Should **peers** give feedback in Phase 2, or manager→report and HR-only at first?
2. Default rating scale (1–5 vs 1–4) for the org?
3. Auto-create reviews for all active Staff, or opt-in per cycle (department filter)?
4. Does Staff get `performance.reviews.write` only when assigned a review, or always?

---

## Estimation (indicative)

| Phase | Effort (order of magnitude) |
|-------|-----------------------------|
| 0 Scaffold | 0.5–1 day |
| 1 Goals | 3–5 days |
| 2 Feedback | 2–3 days |
| 3 Cycles | 5–8 days |
| 4 Reports | 2–3 days |
