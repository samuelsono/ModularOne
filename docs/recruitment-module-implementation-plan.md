# Recruitment Module — Implementation Plan

## Overview

Phased roadmap for the **Recruitment / ATS** app (`/recruitment`) inside Chronos Portal. Follows modular monolith rules ([ADR 0001](./adr/0001-module-boundaries.md)) and existing launcher / permission placeholders.

**Goal:** HR can post jobs, track applicants through a simple pipeline, schedule interviews, and issue offers — then hand off a hired candidate to Users/Core HR without sharing DB tables.

Parent overview: [hcm-modules-overview.md](./hcm-modules-overview.md).

---

## Current State (July 2026)

| Area | What exists |
|------|-------------|
| Launcher card | Placeholder `recruitment` → `/recruitment` |
| Nav shell | Job posts, Applications, Interviews, Offers |
| Permissions | `recruitment.jobs.*`, `applications.*`, `interviews.*`, `offers.*` |
| Role grants | HR (full); others typically none |
| Backend / frontend modules | **None** |
| Hire destination | Users create employees + `StaffProfile` today (manual) |

---

## Product scope (MVP vs later)

### In scope (Phases 0–4)

- Internal job requisitions / posts (status: Draft → Open → Closed)
- Candidate + application pipeline (stage board)
- Interview scheduling (internal attendees = Users)
- Offers (accept / decline / expire)
- Hire handoff event → manual or assisted user creation in Users
- Email/in-app notifications for stage changes (HR + interviewer)

### Out of scope / deferred

| Idea | Why deferred |
|------|----------------|
| Public careers site / anonymous apply portal | Needs public Host endpoints + spam protection |
| Resume parsing / AI screening | External services |
| Video interview integrations | Vendor-specific |
| Background check providers | Compliance + contracts |
| Multi-country visa workflows | Niche |
| Employee referral program + payouts | Payroll coupling |
| Full e-sign for offers | Document + signature stack |

MVP assumes **HR enters candidates** (or email invitation later). External self-apply is Phase 5+.

---

## Architecture Decisions

1. **Module key:** `recruitment`  
   - Backend: `Modules/CarTrack.Modules.Recruitment`  
   - Frontend: `frontend/src/modules/recruitment`
2. **Candidates are not users** until hired. Store PII in Recruitment schema; protect with `recruitment.*` permissions and field-level care (ID numbers optional Phase 3+).
3. **Pipeline stages:** configurable per org as a small lookup table (seeded defaults), not a full workflow engine.
4. **Interviews:** store `InterviewerUserId` list as opaque user IDs; resolve names via org directory.
5. **Hire:** publish `CandidateHired` (Contracts) with payload `{ CandidateId, OfferId, Email, DisplayName, Department?, JobTitle?, StartDate?, SuggestedManagerUserId? }`. Users module owns account creation (UI or future auto-provision).
6. **No FK to Positions/Departments tables** — store string codes / names snapshots on JobPost for historical accuracy.
7. **Approvals:** job open / offer approve can reuse single HR or Finance sign-off later; MVP = HR-only status transitions.

---

## Target Data Model

### `JobPost`

```text
Id, Title, Department, Branch, EmploymentType,
Description, Requirements,
Status (Draft | Open | OnHold | Closed | Cancelled),
OpeningsCount, HiringManagerUserId,
OpensAt, ClosesAt, CreatedByUserId, CreatedAt, UpdatedAt
```

### `Candidate`

```text
Id, FirstName, LastName, Email, Phone,
Source (Referral | Agency | LinkedIn | Other),
Notes, CreatedAt, UpdatedAt
```

### `Application`

```text
Id, JobPostId, CandidateId,
StageId, Status (Active | Withdrawn | Rejected | Hired),
AppliedAt, RejectedAt, RejectReason,
AssignedRecruiterUserId
```

### `PipelineStage` (seeded)

```text
Id, Name, SortOrder, IsTerminal, MapsToHired (bool), IsActive
```

Default seed: `Applied → Screening → Interview → Offer → Hired` (+ terminal `Rejected`).

### `Interview`

```text
Id, ApplicationId, ScheduledAt, DurationMinutes, LocationOrLink,
Status (Scheduled | Completed | Cancelled | NoShow),
FeedbackSummary, Recommendation (Advance | Hold | Reject | null),
CreatedByUserId
```

### `InterviewPanelist`

```text
InterviewId, UserId, Role (Interviewer | Observer)
```

### `Offer`

```text
Id, ApplicationId,
ProposedTitle, ProposedDepartment, ProposedBranch,
SalaryAmount, SalaryCurrency, StartDate,
Status (Draft | Sent | Accepted | Declined | Expired | Withdrawn),
SentAt, RespondedAt, ExpiresAt,
Notes, CreatedByUserId
```

### Not building (deferred)

- `ResumeFile` / blob storage
- Public `ApplyToken`
- Scorecard templates with weighted competencies
- Agency portal users

---

## Phased Implementation

### Phase 0 — Scaffold & shell

Same pattern as Performance Phase 0 (`recruitment` slug, nav wired, empty pages).

**Exit:** `/recruitment` module shell live.

---

### Phase 1 — Jobs & applications pipeline

#### Backend

| API | Notes |
|-----|-------|
| CRUD `/api/recruitment/jobs` | HR write; list/filter by status |
| CRUD `/api/recruitment/candidates` | Deduplicate on email (soft) |
| CRUD `/api/recruitment/applications` | Link candidate ↔ job |
| `POST …/applications/{id}/stage` | Move stage + audit note |
| `GET …/pipeline-stages` | Seeded list |

#### Frontend

- Jobs list + editor
- Kanban or table pipeline for an open job
- Candidate quick-create from application

**Exit:** HR can open a job and move applicants through stages.

---

### Phase 2 — Interviews

| API | Notes |
|-----|-------|
| CRUD interviews under application | Panelists as user IDs |
| Status + recommendation | After meeting |

#### Frontend

- Calendar-ish list (`/recruitment/interviews`)
- Detail: attendees, feedback

#### Notifications

- Notify panelists when scheduled / cancelled

**Exit:** Interviews appear on the Interviews nav with feedback captured.

---

### Phase 3 — Offers & hire handoff

| API | Notes |
|-----|-------|
| Offer CRUD + send/accept/decline | Status machine |
| `POST …/offers/{id}/mark-hired` | Sets application Hired; publishes `CandidateHired` |

#### Frontend

- Offers list
- “Complete hire” CTA showing payload preview for Users admin

#### Users integration (thin)

- Document expected consumer in Users (admin checklist or future endpoint)
- Do **not** create users from Recruitment directly

**Exit:** Accepted offer can trigger a clear hire handoff.

---

### Phase 4 — Reporting & DX

- Time-to-hire, stage conversion funnel
- Page search (jobs, candidates)
- CSV export of applications for a job
- Optional: hiring manager read access (`recruitment.applications.read` scoped by `HiringManagerUserId`)

---

### Phase 5+ (later)

- Public apply form (anonymous rate-limited endpoint)
- Resume attachments
- Structured scorecards
- Agency / referral sources analytics
- Auto-provision user on hire (opt-in, with default Staff role)

---

## Suggested API surface (end of Phase 3)

```http
GET/POST       /api/recruitment/jobs
GET/PATCH      /api/recruitment/jobs/{id}
POST           /api/recruitment/jobs/{id}/open
POST           /api/recruitment/jobs/{id}/close

GET/POST       /api/recruitment/candidates
GET/PATCH      /api/recruitment/candidates/{id}

GET/POST       /api/recruitment/applications
POST           /api/recruitment/applications/{id}/stage
POST           /api/recruitment/applications/{id}/reject

GET            /api/recruitment/pipeline-stages

GET/POST       /api/recruitment/interviews
PATCH          /api/recruitment/interviews/{id}

GET/POST       /api/recruitment/offers
POST           /api/recruitment/offers/{id}/send
POST           /api/recruitment/offers/{id}/respond
POST           /api/recruitment/offers/{id}/mark-hired

GET            /api/recruitment/reports/funnel
```

---

## Frontend routes

| Path | Permission | Phase |
|------|------------|-------|
| `/recruitment` | `recruitment.jobs.read` | 0–1 |
| `/recruitment/applications` | `recruitment.applications.read` | 1 |
| `/recruitment/interviews` | `recruitment.interviews.read` | 2 |
| `/recruitment/offers` | `recruitment.offers.read` | 3 |

---

## Security & privacy notes

- Candidate PII is HR-sensitive — default deny outside HR role.
- Avoid logging full CVs / ID numbers.
- Hire event must not include draft salary if Users consumers are broad; prefer Users admin pull with `offers.read`.

---

## Testing & acceptance (MVP)

- [ ] Stage moves are audited (who/when/from/to)
- [ ] Cannot hire without an Accepted offer (or explicit HR override flag)
- [ ] Closing a job does not delete applications
- [ ] Interviewer without HR role sees only interviews they are on (Phase 4 scoping)
- [ ] Architecture + lint boundaries green

---

## Open questions

1. Is a **hiring manager** (non-HR) allowed to move stages, or only comment?
2. Single pipeline for all jobs, or custom stages per job family?
3. Store proposed salary on Offer as encrypted-at-rest field?
4. Should declined candidates stay searchable indefinitely (GDPR/retention policy)?

---

## Estimation (indicative)

| Phase | Effort (order of magnitude) |
|-------|-----------------------------|
| 0 Scaffold | 0.5–1 day |
| 1 Jobs + pipeline | 5–8 days |
| 2 Interviews | 3–4 days |
| 3 Offers + hire event | 3–5 days |
| 4 Reports | 2–3 days |
