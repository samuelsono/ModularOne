# Payroll Module — Implementation Plan

## Overview

Phased, **CarTrack-specific** roadmap for the **Payroll** app (`/payroll`). This plan **replaces** the broad enterprise wishlist in [`payroll-management.md`](./payroll-management.md) as the implementation source of truth. The wishlist remains useful as a long-term product backlog; most of it is intentionally out of scope for early phases.

**Goal:** run a trustworthy payroll cycle for Chronos employees — profiles, configurable components (SA-oriented), calculate → approve → payslips — integrating Leave unpaid days via Contracts, without rebuilding HR master data or multi-company tenancy.

Parent overview: [hcm-modules-overview.md](./hcm-modules-overview.md).

---

## Current State (July 2026)

| Area | What exists |
|------|-------------|
| Launcher card | Placeholder `payroll` → `/payroll` |
| Nav shell | Runs, Payslips, Deductions, Settings |
| Permissions | `payroll.runs.*`, `payslips.*`, `deductions.*`, `settings.*` |
| Role grants | Finance (full payroll); Staff (`payslips.read`); Driver (`payslips.read`) |
| Backend / frontend modules | **None** |
| People | Users + `StaffProfile` |
| Leave unpaid signals | Leave module exists; **no payroll export yet** ([defferred.md](./defferred.md)) |
| Expense reimbursements | Expense module — optional payroll input later |
| Enterprise wishlist | [`payroll-management.md`](./payroll-management.md) (multi-tenant, banking, tax engines, …) |

---

## Gap Analysis vs Enterprise Wishlist

| Wishlist concept | CarTrack approach |
|------------------|-------------------|
| Multi-company / multi-country product | **Single platform**; Country/currency settings as Payroll config rows |
| Separate Employee master | **Reuse Users + StaffProfile**; `PayrollProfile` keyed by `UserId` |
| Full statutory tax engines for many countries | **Phase 2–3:** SA-first tables (PAYE/UIF/SDL) as data-driven rules; formulas kept simple |
| Attendance & time clocks | **Deferred** — overtime as manual / imported variable lines |
| Banking file formats per bank | **Phase 4:** generic CSV/EFT export; bank-specific later |
| Accounting / ERP auto-post | **Phase 4+:** journal CSV export; live API later |
| Expense claims inside Payroll | **Stay in Expense module**; optional “ready for pay” import later |
| Loan management product | **Phase 5+** or separate submodule |
| 50k-employee batch SLAs | Design async jobs early; optimize when measured |

---

## Architecture Decisions

1. **Module key:** `payroll`  
   - Backend: `Modules/CarTrack.Modules.Payroll`  
   - Frontend: `frontend/src/modules/payroll`
2. **High sensitivity:** banking, tax numbers, net pay — encrypt secrets with Data Protection (same pattern as external auth / CarTrack API credentials); strict permission checks; audit salary changes.
3. **Payroll run is the aggregate root** for a period; calculations produce immutable `PayrollLine` rows once released (reverse = new corrective run).
4. **Leave integration:** Leave publishes or exposes Contract `ILeavePayrollExport` / event with unpaid days per user for a date range. Payroll **never** references Leave DbContext.
5. **No cross-module FKs.** `UserId` only.
6. **Processing:** sync calculate OK for small headcount; introduce queue/background worker when runs exceed interactive timeout.
7. **Currency:** single org currency in MVP (`PayrollSettings.Currency`, default `ZAR`).

---

## Target Data Model (MVP → Phase 4)

### `PayrollSettings`

```text
Id (singleton row), Currency, PayFrequency (Monthly|BiWeekly|Weekly),
CutOffDay, PayDay, TaxCountryCode (e.g. ZA), FinancialYearStartMonth,
UpdatedAt, UpdatedByUserId
```

### `PayrollProfile`

```text
Id, UserId,
SalaryType (Salaried|Hourly), BasicAmount, HourlyRate,
PaymentMethod (EFT|Cash|Other),
BankAccountProtected, BankBranchCode, AccountHolderName,
TaxNumberProtected, UifEligible, StartDate, EndDate (nullable),
IsActive
```

### `PayComponent` (catalog)

```text
Id, Code, Name,
Kind (Earning|Deduction|EmployerContribution),
Calculation (Fixed|PercentOfBasic|PercentOfTaxable|Manual),
IsTaxable, IsPensionable, IsRecurring, SortOrder, IsActive
```

### `EmployeeComponent`

```text
Id, UserId, PayComponentId, Amount, Percentage,
EffectiveFrom, EffectiveTo, IsActive
```

### `PayrollRun`

```text
Id, PeriodStart, PeriodEnd, PayDate,
Status (Draft | Calculated | InReview | Approved | Released | Reversed),
CreatedByUserId, ApprovedByUserId, ReleasedAt, Notes
```

### `PayrollEntry` (per employee per run)

```text
Id, PayrollRunId, UserId,
GrossEarnings, TaxableIncome, Tax, TotalDeductions,
EmployerContributions, NetPay,
UnpaidLeaveDays, CalculationVersion
```

### `PayrollLine`

```text
Id, PayrollEntryId, PayComponentId, Description,
Quantity, Rate, Amount, Direction (Earn|Deduct|Employer)
```

### `Payslip`

```text
Id, PayrollEntryId, GeneratedAt, StoragePathOrBlobId,
EmailSentAt
```

### Deferred entities (wishlist → later)

- `Loan`, `LoanInstallment`
- Multi-step `PayrollApprovalStep`
- `TaxBracket` versioning UI beyond seed tables
- `BankingBatch` per bank format
- `AccountingJournal`

---

## Phased Implementation

### Phase 0 — Scaffold & shell

Register module, routes, settings stub, permission-gated nav. No calculation yet.

**Exit:** `/payroll` shell for Finance role.

---

### Phase 1 — Profiles & components

| Task | Detail |
|------|--------|
| Payroll settings | Singleton config UI |
| Pay component catalog | Seed Basic, PAYE, UIF (ZA placeholders) |
| Employee payroll profiles | Link to UserId; bank/tax fields protected |
| Employee recurring components | Allowances / deductions |
| Audit | Log profile + component changes |

#### Frontend

- Settings page
- Deductions/components admin (`/payroll/deductions`)
- Employee payroll tab may live under Core HR later via app composition — **MVP:** list under Payroll Settings → Employees

**Exit:** Finance can configure who is paid and what recurring lines they have.

---

### Phase 2 — Payroll run calculate (happy path)

| Task | Detail |
|------|--------|
| Create run for period | Draft |
| Calculate | Load active profiles + components → entries + lines |
| Variable lines | Manual overtime/bonus capture on run |
| Leave unpaid | Call Leave Contract if available; else skip with warning |
| Recalculate while Draft/Calculated | Allowed; clear on Approved |

#### API sketch

```http
POST /api/payroll/runs
POST /api/payroll/runs/{id}/calculate
GET  /api/payroll/runs/{id}
GET  /api/payroll/runs/{id}/entries
```

**Exit:** Finance can produce net pay figures for a period in Draft/Calculated status.

---

### Phase 3 — Approval, payslips, employee self-service

| Task | Detail |
|------|--------|
| Approve / release | Status machine; Released freezes lines |
| Payslip PDF or HTML→PDF | Store path; employee download |
| `GET /api/payroll/payslips/mine` | Staff permission |
| Notifications | “Payslip available” |
| Reverse run | Marks reversed; optional corrective run link |

**Exit:** Staff see own payslips; Finance closes a cycle.

---

### Phase 4 — Exports & compliance basics

- Payroll register CSV / Excel
- Banking payment CSV (generic columns)
- Journal export (salary expense / liabilities sketch)
- ZA monthly summary report scaffolding (PAYE/UIF totals — not full eFiling)
- Hardening: background calculate job

---

### Phase 5+ (later)

- Loans & garnishees
- Multi-step finance approval
- Attendance overtime import
- Expense reimbursement import
- Bank-specific EFT formats
- Full SARS / statutory filing packs
- Multi-currency / multi-entity

---

## Leave & Expense Contracts

### Leave (required for accurate unpaid handling)

```csharp
// Leave.Contracts (sketch)
public interface ILeavePayrollReadModel
{
    Task<IReadOnlyList<UnpaidLeaveDaysDto>> GetUnpaidDaysAsync(
        DateOnly from, DateOnly to, CancellationToken ct);
}

public record UnpaidLeaveDaysDto(string UserId, decimal Days);
```

Until implemented, Phase 2 calculation shows “Leave integration unavailable” and allows manual unpaid-day override on `PayrollEntry`.

### Expense (optional later)

Only import claims explicitly marked reimbursable and approved — never all expenses.

---

## Suggested API surface (end of Phase 3)

```http
GET/PUT   /api/payroll/settings

GET/POST  /api/payroll/components
PATCH     /api/payroll/components/{id}

GET/PUT   /api/payroll/profiles/{userId}
GET/POST  /api/payroll/profiles/{userId}/components

GET/POST  /api/payroll/runs
GET       /api/payroll/runs/{id}
POST      /api/payroll/runs/{id}/calculate
POST      /api/payroll/runs/{id}/approve
POST      /api/payroll/runs/{id}/release
POST      /api/payroll/runs/{id}/reverse

GET       /api/payroll/runs/{id}/entries
PATCH     /api/payroll/runs/{id}/entries/{entryId}   # manual overrides while Draft

GET       /api/payroll/payslips/mine
GET       /api/payroll/payslips/{id}

GET       /api/payroll/reports/register
```

---

## Frontend routes

| Path | Permission | Phase |
|------|------------|-------|
| `/payroll` | `payroll.runs.read` | 0–2 (runs home) |
| `/payroll/payslips` | `payroll.payslips.read` | 3 |
| `/payroll/deductions` | `payroll.deductions.read` | 1 |
| `/payroll/settings` | `payroll.settings.read` | 1 |

---

## Security & compliance notes

- Treat payroll as **higher trust** than Leave/Expense UI.
- Field-level protection for bank account + tax number.
- Release/approve require `payroll.runs.write`; employees never see others’ payslips.
- Full audit: profile edits, calculate, approve, release, reverse, exports.
- Legal: ZA rules change — keep tax parameters **data-driven**, not hard-coded magic numbers in C#.

---

## Testing strategy

| Layer | Focus |
|-------|--------|
| Unit | Component calculation order, tax stub tables, unpaid leave math |
| Integration | Run lifecycle, permission denials, Leave contract fake |
| E2E | Calculate → approve → staff downloads payslip |
| Security | Cross-user payslip access attempts |

---

## Acceptance criteria (MVP = end of Phase 3)

- [ ] Active employees with profiles can be calculated in a monthly run
- [ ] Net pay = earnings − deductions − tax (per configured components)
- [ ] Released runs are immutable; reverse is explicit
- [ ] Staff can view only their payslips
- [ ] Salary/bank changes are audited
- [ ] Module boundaries enforced (architecture tests + ESLint)
- [ ] Unpaid leave either applied via Contract or manually overridden with visibility

---

## Open questions

1. Confirm **South Africa–first** statutory scope for MVP (PAYE/UIF/SDL only).
2. Who approves payroll — Finance only, or dual control with HR?
3. Payslip format: HTML template → PDF library choice?
4. Should Core HR host the “Payroll profile” UI (app composition) while Payroll owns APIs?
5. Minimum headcount / timeout before background workers are mandatory?

---

## Estimation (indicative)

| Phase | Effort (order of magnitude) |
|-------|-----------------------------|
| 0 Scaffold | 0.5–1 day |
| 1 Profiles & components | 5–8 days |
| 2 Calculate engine | 8–12 days |
| 3 Approvals & payslips | 5–8 days |
| 4 Exports | 4–6 days |

---

## Relation to other docs

| Doc | Role |
|-----|------|
| [payroll-management.md](./payroll-management.md) | Long-term enterprise backlog / inspiration |
| [leave-module-implementation-plan.md](./leave-module-implementation-plan.md) | Source of unpaid leave export work |
| [defferred.md](./defferred.md) | Leave payroll export still deferred until Leave Phase 6 / Contract |
| [hcm-modules-overview.md](./hcm-modules-overview.md) | Ordering vs Performance & Recruitment |
