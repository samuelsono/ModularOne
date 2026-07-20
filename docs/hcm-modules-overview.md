# HCM Modules — Planning Overview

**Date:** 2026-07-15  
**Status:** Planning (implementation not started)  
**Related:** [ADR 0001 — Module Boundaries](./adr/0001-module-boundaries.md), [Adding a module](./adding-a-module.md), [System wishlist](./system-wishlist.md)

---

## Purpose

This document introduces the three planned HR capability modules that fill the app-launcher placeholders already registered in Chronos Portal:

| Module | Slug | Plan |
|--------|------|------|
| **Performance Management** | `performance` | [performance-module-implementation-plan.md](./performance-module-implementation-plan.md) |
| **Recruitment (ATS)** | `recruitment` | [recruitment-module-implementation-plan.md](./recruitment-module-implementation-plan.md) |
| **Payroll** | `payroll` | [payroll-module-implementation-plan.md](./payroll-module-implementation-plan.md) |

Each plan is **CarTrack-specific**: modular monolith, single tenant, reuse `ApplicationUser` + `StaffProfile`, published Contracts / events only across modules. Enterprise wishlists (especially [payroll-management.md](./payroll-management.md)) are treated as inspiration and then **scoped down**.

---

## Shared platform that already exists

Do **not** rebuild these inside each module:

| Capability | Source of truth |
|------------|-----------------|
| Auth / JWT / MFA / external login | Users + Identity |
| Org people graph (employee number, dept, branch, manager) | Users `StaffProfile` + org directory APIs |
| Approvals pattern | Leave / Expense (single manager decide; multi-step later) |
| Notifications | Notifications module + integration events |
| Permissions / roles | `PermissionCatalog` — keys for all three slugs already exist |
| App launcher + nav shells | `PLACEHOLDER_APP_MODULES` / `PLACEHOLDER_NAV_ITEMS` |
| Scaffolding | `dotnet new cartrack-module`, `frontend/src/modules/_template` |

---

## Suggested delivery order

```text
Performance (MVP) ──► Recruitment (MVP) ──► Payroll (foundation)
         │                    │                     │
         └──── shared patterns: approvals, prefs, ──┘
               contracts for leave / hire handoff
```

1. **Performance first** — closed loop around current employees; low compliance risk; reuses manager hierarchy.
2. **Recruitment next** — external candidates; hire handoff publishes events consumed by Users/Core HR.
3. **Payroll last among the three** — statutory sensitivity; consumes Leave unpaid/absence signals and (later) Recruitment/Performance only for reporting; largest engine.

Phases inside each plan can overlap once Contracts and permission seeding are stable.

---

## Cross-module contracts (planned)

| From → To | Contract / event (sketch) | Purpose |
|-----------|---------------------------|---------|
| Leave → Payroll | `UnpaidLeaveDaysExported` / query API in Leave.Contracts | Adjust pay for unpaid leave |
| Expense → Payroll | Reimbursement-ready claims (later) | Optional reimbursement in run |
| Recruitment → Users | `CandidateHired` | Create user / staff profile from accepted offer |
| Performance → (optional reporting) | Review cycle summaries | HR dashboards only — no hard Payroll dependency in MVP |
| Users → all three | Org directory read (existing) | Manager, department, active employment |

No cross-module EF foreign keys. Store opaque user/candidate IDs only.

---

## Permission keys (already catalogued)

Defined in `PermissionCatalog` / frontend nav:

- **performance:** `reviews`, `goals`, `feedback`, `reports` (+ `.read` / `.write`)
- **recruitment:** `jobs`, `applications`, `interviews`, `offers`
- **payroll:** `runs`, `payslips`, `deductions`, `settings`

Role seeds already grant HR the performance + recruitment modules, Finance the payroll module, Staff `payroll.payslips.read`, Manager limited performance read.

---

## Non-goals for this planning tranche

- Multi-tenant / multi-country payroll productization
- Separate employee master outside Users
- Mobile native apps
- Full LMS / training module (wishlist only)
- Rewriting Leave or Expense to become a mega-workflow engine

---

## Next step after plan review

1. Agree MVP cut lines in each plan (Phase 0–2).
2. Scaffold backend + frontend modules per [adding-a-module.md](./adding-a-module.md).
3. Implement Performance Phase 0–1 first; keep Contracts thin.
