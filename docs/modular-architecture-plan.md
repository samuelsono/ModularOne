# Modular Architecture — Migration Plan

## Overview

This plan describes how to evolve Chronos Portal (CarTrack) from its current **folder-separated monolith** into a **modular monolith** where every business capability (Leave, Expense, Fleet, Invoicing, …) is a **self-contained module** that owns its own backend slice and frontend slice, and depends only on a small **Core** and **Shared** foundation — never on another module.

The end state keeps a single deployable (one API process, one SPA bundle, one database) but enforces **module boundaries in code** so future modules can be added — and shipped — without touching or breaking existing ones.

> This is a structural refactor plan. It is intentionally **incremental**: the app stays runnable at every phase. There is no "big bang" rewrite.

### Design goals

1. **Module independence** — a module references only `Core` and `Shared`. It never references a sibling module's internals.
2. **Own your stack** — each module owns its endpoints, services, entities, migrations slice, DTOs, and (frontend) its pages/components/services/types.
3. **Pluggability** — adding a module means adding a folder + one registration line. Removing it means deleting a folder + one line.
4. **Explicit contracts** — when modules must interact, they do so through published abstractions (interfaces / integration events) in `Shared`, not direct class references.
5. **Runnable at every step** — no phase leaves `main`/`modular` broken.

### Non-goals (for now)

- Splitting into separate processes / microservices (kept as a **modular monolith**).
- Per-module databases (kept **one physical DB**, logically partitioned by schema/table-ownership).
- Multi-tenancy changes.
- Rewriting business logic — this is about **boundaries**, not behavior.

---

## Current State (July 2026)

### Backend — `CarTrack.Server` (single `net10.0` web project)

- **Minimal APIs only** (no controllers). Each feature has a `*Endpoints` static class mapped in `Program.cs`.
- **One `ApplicationDbContext`** (`IdentityDbContext<ApplicationUser>`) with **29 custom DbSets** and **29 migrations**; all fluent config inline in `OnModelCreating`.
- **17 feature folders**: `Auth`, `Users`, `CoreHr`, `Leave`, `Expense`, `Vehicles`, `Drivers`, `CarTrack`, `Reports`, `Dashboard(*in Reports)`, `Notifications`, `Support`, `Help`, `Settings`, plus `Data`, `Configuration`, `Migrations`.
- **Cross-cutting** lives in `Data/` (entities, `IAuditable`, `AuditableEntityInterceptor`) and `Users/` (`ICurrentUserScope`, `UserDataScope`, `ManagerHierarchy`, `PermissionCatalog`, authorization handlers).
- **Composition root** `Program.cs` wires every service; **`Auth/DatabaseSeeder.cs`** calls every module's seeder.
- **Aspire**: `CarTrack.AppHost` orchestrates Postgres (`cartrack`), Redis (`cache`), the server, and the Vite frontend. Service defaults are inlined in `Extensions.cs`.

### Frontend — `frontend` (React 19 + Vite 8 + Fluent UI v9)

- **Hybrid layout**: strong per-feature folders (`components/leave`, `components/expense`, `page/leave`, …) + a large shared shell (`Navigation`, `SideNavigation`, contexts, `common/`).
- **Service-per-domain** over a shared `authorizedFetch` / `apiClient`.
- **`utils/permissions.ts`** is the RBAC + nav + app-module hub; **`utils/pageSearch.ts`** is a god-util that knows every feature's DTOs.
- **No path aliases**, **no state library** (React Context + per-page `useState`).

### Coupling hotspots to resolve (from codebase inventory)

| # | Coupling | Where |
|---|----------|-------|
| B1 | Monolithic `ApplicationDbContext` owns all 29 entities | `Data/ApplicationDbContext.cs` |
| B2 | Entities live in a shared `Data/` bag, not with their module | `Data/*.cs` |
| B3 | `UserDataScope` hard-codes Expense statuses + fleet/driver rules | `Users/UserDataScope.cs`, `Data/ExpenseClaim.cs` |
| B4 | `ManagerHierarchy` (in `Users/`) is core to Leave + Expense approvals | `Users/ManagerHierarchy.cs` |
| B5 | `DatabaseSeeder` knows every module | `Auth/DatabaseSeeder.cs` |
| B6 | `ReportQueryService` switches over 7 domains' tables | `Reports/ReportQueryService.cs`, `Reports/ReportTableRegistry.cs` |
| B7 | Endpoints call `NotificationTriggers` directly (no events) | `Leave`, `Vehicles`, `Drivers` endpoints |
| B8 | `StaffProfile` / `DriverProfileLink` bridge Users ↔ CoreHr ↔ Leave/Expense ↔ Drivers | `Data/StaffProfile.cs` |
| F1 | `utils/permissions.ts` is a central hub every feature depends on | frontend |
| F2 | `utils/pageSearch.ts` imports every feature's types | frontend |
| F3 | `ExpenseStatusBadge` reused by Core HR grids | `components/expense/expenseBadges` |
| F4 | `EmployeesList` shared by fleet + core; forms pull `coreHrService` | frontend |
| F5 | Legacy root tables (`VehicleTable`, etc.) outside feature folders | frontend |

---

## Target Architecture

### Backend layout

```
CarTrack.sln
├── Host/
│   └── CarTrack.Host.csproj            # thin composition root (ex-CarTrack.Server Program.cs)
│       ├── Program.cs                   # discovers + registers modules, builds pipeline
│       └── appsettings*.json / .env
│
├── Shared/
│   ├── CarTrack.Core/                   # domain-agnostic primitives (no EF, no ASP.NET)
│   │   ├── Abstractions/                #   IModule, IAuditable, integration-event contracts
│   │   ├── Results/ , Errors/ , Guards/
│   │   └── Messaging/                   #   IEventBus, IIntegrationEvent
│   ├── CarTrack.Infrastructure/         # EF base, interceptors, outbox, storage, email, caching
│   │   ├── Persistence/                 #   ModuleDbContext base, AuditableEntityInterceptor, migrator
│   │   ├── Messaging/                   #   in-process IEventBus impl
│   │   ├── Storage/ , Email/ , Caching/
│   ├── CarTrack.Identity/               # ApplicationUser, JWT, permissions, ICurrentUserScope
│   │   ├── PermissionCatalog (registry), authorization handlers
│   │   └── ICurrentUserScope + org-context abstractions
│   └── CarTrack.Api/                    # minimal-API helpers: IModuleEndpoints, RequirePermission
│
└── Modules/
    ├── CarTrack.Modules.Leave/
    │   ├── LeaveModule.cs               # IModule: AddServices + MapEndpoints + seed + migrate
    │   ├── Domain/                      # entities (LeaveRequest, LeaveType, …)
    │   ├── Persistence/                 # LeaveDbContext + Migrations/ (module-owned)
    │   ├── Features/                    # endpoints + services + DTOs (vertical slices)
    │   ├── Contracts/                   # PUBLIC: interfaces + integration events other modules may use
    │   └── Seed/
    ├── CarTrack.Modules.Expense/
    ├── CarTrack.Modules.Fleet/          # Vehicles + Drivers + CarTrack client
    ├── CarTrack.Modules.CoreHr/
    ├── CarTrack.Modules.Users/          # user/role/permission admin, StaffProfile, org tree
    ├── CarTrack.Modules.Reporting/      # dashboards + reports (consumes module read-contracts)
    ├── CarTrack.Modules.Notifications/
    ├── CarTrack.Modules.Support/
    ├── CarTrack.Modules.Help/
    └── CarTrack.Modules.Settings/
```

**Reference rule (enforced):**

```
Host        ──▶ every Module + Shared
Module X    ──▶ Shared only        (Core, Infrastructure, Identity, Api)
Module X    ──▶ Module Y.Contracts (allowed ONLY via published Contracts, ideally via events)
Shared      ──▶ nothing app-specific
```

### The `IModule` contract

Every module implements one interface so the Host can discover and wire it uniformly:

```csharp
public interface IModule
{
    string Name { get; }
    void AddModule(IHostApplicationBuilder builder);      // DI registrations, DbContext, options
    void MapEndpoints(IEndpointRouteBuilder endpoints);   // minimal-API groups
    Task SeedAsync(IServiceProvider services, CancellationToken ct);
    Task MigrateAsync(IServiceProvider services, CancellationToken ct);
}
```

`Program.cs` collapses to discovery + a loop:

```csharp
var modules = ModuleRegistry.Discover();          // reflection or explicit list
foreach (var m in modules) m.AddModule(builder);
// … build app …
foreach (var m in modules) m.MapEndpoints(api);
foreach (var m in modules) await m.MigrateAsync(app.Services, ct);
foreach (var m in modules) await m.SeedAsync(app.Services, ct);
```

Adding a module = add a `ProjectReference` in `Host` (or drop the DLL) + implement `IModule`. Nothing else changes.

### Data ownership: one database, many DbContexts

The single physical Postgres DB is kept, but split by **DbContext-per-module**, each mapped to a **dedicated schema** and owning **its own migrations history table**:

- `LeaveDbContext` → schema `leave`, history `__EFMigrationsHistory_Leave`
- `ExpenseDbContext` → schema `expense`, …
- Identity + `ApplicationUser` stays in `CarTrack.Identity` (schema `identity`), the one table modules may reference **by ID only** (no FK across module schemas).

Rules:
- **No cross-module FKs.** A module references another module's data by **ID (Guid/string)**, never by navigation property.
- Cross-module reads go through a **read-contract** (e.g. `IOrgDirectory.GetManagerAsync(userId)` published by Users) or an **integration event** projection.
- `IAuditable` + `AuditableEntityInterceptor` move to `Shared/Infrastructure` and are applied by each module's DbContext base class.

### Module communication: integration events

Replace direct calls like `NotificationTriggers.LeaveSubmitted(...)` with an **in-process event bus** (start synchronous; upgrade to an outbox later if needed):

```
Leave module      publishes  LeaveRequestSubmitted (in Leave.Contracts)
Notifications     subscribes to it → creates notification
Reporting         subscribes to it → updates read model
```

This removes B7 and lets Notifications/Reporting depend on **contracts**, not on Leave internals.

### Frontend layout

```
frontend/src/
├── app/                      # shell: App.tsx, providers, router assembly
├── platform/                 # ex-"shared": Navigation, SideNavigation, AppLauncher,
│   ├── ui/                   #   common/ components, AppMap, status badges (generic)
│   ├── auth/ , permissions/  #   permission primitives + module registry
│   ├── search/               #   page-search framework (registry, not per-feature switch)
│   └── api/                  #   apiClient, authorizedFetch, tokenStorage
├── modules/
│   ├── leave/
│   │   ├── index.ts          # ModuleDefinition: routes, nav items, permissions, search config
│   │   ├── pages/ , components/ , services/ , types/ , hooks/
│   ├── expense/
│   ├── fleet/                # vehicles + drivers + live tracking + dashboard
│   ├── coreHr/
│   ├── users/
│   ├── reporting/
│   ├── settings/
│   └── …
└── main.tsx
```

**Frontend module contract** — each module exports a definition the shell composes:

```ts
export interface ModuleDefinition {
  id: string;                       // 'leave'
  routes: RouteObject[];            // mounted under the shell layout
  navItems: NavItem[];              // sidebar entries (permission-gated)
  permissions: string[];           // keys this module introduces
  searchProviders?: SearchProvider[]; // replaces pageSearch.ts god-util
}
```

The shell builds routes/nav/search by iterating a `modules` array. `utils/permissions.ts` (F1) and `utils/pageSearch.ts` (F2) are decomposed into a **registry** each module contributes to. Path aliases (`@platform/*`, `@modules/*`) are added to `tsconfig` + Vite to keep imports clean and prevent cross-module deep imports (enforced by an ESLint boundary rule).

---

## Migration Strategy (phased, non-breaking)

Each phase compiles, runs, and is independently mergeable. Backend and frontend tracks can proceed in parallel after Phase 0/1.

### Phase 0 — Guardrails & scaffolding (no behavior change) ✅ Done
- Add `docs/` ADR for the module boundary rules (this doc + a short ADR).
- Create empty `Shared/CarTrack.Core`, `Shared/CarTrack.Infrastructure`, `Shared/CarTrack.Identity`, `Shared/CarTrack.Api` class libraries; add to the solution. Nothing references them yet.
- Frontend: introduce path aliases (`@platform/*`, `@modules/*`) and an ESLint rule stub for import boundaries. No files moved yet.
- Add an **architecture test** (NetArchTest/ArchUnit-style) that will later assert "Modules don't reference each other." Start it green with the current single project excluded.

**Landed artifacts:** `docs/adr/0001-module-boundaries.md`, `Shared/*` (marker-only libraries), `Tests/CarTrack.ArchitectureTests`, frontend aliases + ESLint stub + empty `src/platform` / `src/modules` placeholders.

### Phase 1 — Extract Shared foundation ✅ Done
- Move to `Shared`:
  - `IAuditable`, `AuditableEntityInterceptor`, auditable config → `Infrastructure` / `Core`.
  - `ApplicationUser`, JWT options, `PermissionCatalog`, `AppRoles`, authorization handlers → `Identity`.
  - Minimal-API helpers (`RequirePermission`) + `IModule`/`ModuleRegistry` → `Api`.
  - Aspire service defaults → `Infrastructure`.
- `CarTrack.Server` references the Shared libraries; moved copies deleted. **App still monolithic**, foundation lives where modules will consume it.
- Temporary `LegacyModule` wraps all existing DI + endpoints; `Program.cs` discovers via `ModuleRegistry`.

**Deferred to later phases (documented):** `TokenService` stays in Server (DbContext-coupled); `ICurrentUserScope`/`UserDataScope` stay in Server until Phase 2 splits org contracts.

**Landed artifacts:** Shared projects populated; `CarTrack.Server/LegacyModule.cs`; slimmed `Program.cs`; Server → Shared project references.

### Phase 2 — Introduce the Core/Users contracts (break B3/B4/B8) ✅ Done
- Publish read-contracts in `Shared/Identity` (or a `Users.Contracts`):
  - `IOrgDirectory` → manager, direct/all reports, branch (extracted from `ManagerHierarchy`).
  - `ICurrentUserScope` stays generic; move **domain-specific** predicates (`CanApproveLeave`, expense-status logic) **out of** `UserDataScope` into the owning modules. `UserDataScope` keeps only identity/role/branch primitives.
- Add the **in-process `IEventBus`** to `Shared/Infrastructure`. Introduce integration-event types but keep old direct calls working (dual-write) until subscribers exist.

**Landed artifacts:** `Shared/CarTrack.Identity/Contracts/{IOrgDirectory,ICurrentUserScope}`; `OrgDirectory` adapter in Server; domain predicates in `LeaveAuthorization` / `ExpenseAuthorization` / `FleetAuthorization`; `IEventBus` + `InProcessEventBus` + domain events; `NotificationTriggers` dual-writes (publish event + create notification).

### Phase 3 — Carve modules one at a time (vertical slices) ✅ Done
Do the **cleanest module first** to prove the pattern, then the rest. Recommended order (low → high coupling):

1. **CoreHr** ✅ Domain + `CoreHrDbContext` + services/seeder + `IModule`.
2. **Help**, **Support** ✅ same (own DbContexts, services, seed/migrate).
3. **Notifications** ✅ own DbContext + hub + `NotificationEventHandlers` (event subscribers); producers publish only — no `NotificationTriggers`.
4. **Leave** ✅ Domain + `LeaveDbContext` + services/accrual/seed + `IModule`.
5. **Expense** ✅ Domain + `ExpenseDbContext` + services/seed; shared `ApprovalDecisionRequest` in `CarTrack.Api`.
6. **Fleet** ✅ Vehicles/Drivers Domain + `FleetDbContext` + services; CarTrack wire models/`ICarTrackApiClient` contract in Shared Infrastructure; client impl registered on Host.
7. **Users** ✅ Domain + `UsersDbContext` + Auth/`TokenService`/email + org directory/scope/permissions/seed; JWT options in Shared Identity; Identity user store remains on Host `ApplicationDbContext`.
8. **Reporting/Dashboard** ✅ Domain + `ReportingDbContext` + report/dashboard services; Host retains `ReportQueryService` / `ApiTableReportExecutor` composing module DbContexts (**B6 interim**).
9. **Settings** ✅ Domain + `SettingsDbContext` + settings/credential services; `CarTrackOptions` / credential abstractions in Shared Infrastructure.

**Persistence model (landed):** each module owns a DbContext and `__EFMigrationsHistory_<Module>` table against the **same physical PostgreSQL database** (`public` tables retained; physical schema rename deferred). Existing DBs are **baselined** via `MigrateModuleAsync` probe-table logic (see each `Persistence/README.md`). Host `ApplicationDbContext` is Identity-only; feature entities removed from the Host model with empty host migrations (no `DropTable`).

**For each module (done):**
- Create `CarTrack.Modules.X` project referencing only `Shared`.
- Move entities into `Domain/`; module `XDbContext` + initial migration + baseline README.
- Move endpoints/services/DTOs into the module; implement `IModule` (`AddModule` / `MapEndpoints` / `MigrateAsync` / `SeedAsync`).
- Cross-module interaction via Identity contracts + in-process `IEventBus` (Notifications subscribes).
- Feature folders deleted from `CarTrack.Server`; architecture tests assert ≥ 10 modules with no cross-module refs.

**Host leftovers (intentional → Phase 4):** slim `LegacyModule` (Identity migrate, `IEventBus`, CarTrack HTTP client registration, B6 report query executor); `Program.cs` composition + JWT bearer pipeline; `EnvFileConfiguration`.

### Phase 4 — Collapse the Host ✅ Done
- Renamed `CarTrack.Server` → **`CarTrack.Host`**: composition root whose `Program.cs` registers modules, Identity/JWT pipeline, and migrate/seed orchestration. Auth endpoints live in `Users` module; JwtOptions in Shared Identity.
- `DatabaseSeeder` (B5) gone — each module's `SeedAsync` via `ModuleRegistry.SeedAllAsync`.
- `CarTrack.AppHost` references `CarTrack.Host` (`Projects.CarTrack_Host`); Postgres/Redis/frontend wiring unchanged.
- `HostModule` (ex-LegacyModule): Identity DbContext migrate, in-process `IEventBus`, CarTrack `HttpClient`, B6 report query executor. `CarTrackApiClient` impl moved to Shared Infrastructure.

**Historical namespaces** such as `CarTrack.Server.Data` / `CarTrack.Server.CarTrack` remain on types that already lived in Shared or Host for EF Identity stability.

### Phase 5 — Frontend modularization (parallelizable with 3–4) ✅ Done
- `platform/`: api (`apiClient`/`authService`/`tokenStorage`/`platformSettingsApi`), auth context/types, RBAC (`permissions/rbac`), launcher/nav aggregation (`permissions/apps`), search registry, shell styles + SideNav/ActiveApp, shared UI (`StatusBadge`, `SummaryCard`, grids, drawers), org directory APIs (`org/` + `config/mapbox`).
- `modules/{auth,leave,expense,fleet,coreHr,users,reporting,settings,notifications,support,help}/`: each owns pages/components/services/types and exports a `ModuleDefinition` (`index.tsx`).
- `app/modules.tsx` registers modules (routes + nav + search + APP_MODULES); `app/App.tsx` + `app/Navigation.tsx` assemble shell chrome that composes feature dialogs; routes via `useRoutes`.
- F3: Core HR uses `@platform/ui/StatusBadge`; expense keeps `ExpenseStatusBadge`.
- F4: employees route under `/core/employees` from users module; org/driver lookups via `@platform/org/*` (not sibling module imports). Leave/expense summary tiles use `@platform/ui/SummaryCard`.
- F5: legacy `VehicleTable` / `DriversTable` / `EmployeesTable` live under their modules.
- ESLint: platform cannot import `@modules/*` (error); relative cross-module reaches blocked (error). Per-module `@modules/x` allowlist deferred to Phase 6 `eslint-plugin-boundaries`.
- Known interim: settings Approvals stub still imports expense types/services; settings hub still embeds panels from users/notifications/support/help; fleet Dashboard page still hosts reporting `DashboardRenderer` — all pending contribution/slot contracts in Phase 6.

### Phase 6 — Enforcement & "new module" DX ✅ Done
- Backend architecture tests: no cross-module refs, modules only reference Shared/`*.Contracts`, each assembly exposes `IModule` (≥10 modules).
- Frontend: `eslint-plugin-boundaries` layer graph + per-module `@modules/<sibling>` `no-restricted-imports`; `npm run lint:boundaries` for CI. Settings hub + fleet home composed in `app/` so feature modules stay sibling-clean.
- Templates: `templates/CarTrack.Module` (`dotnet new cartrack-module`) and `frontend/src/modules/_template/`.
- Docs: [`docs/adding-a-module.md`](./adding-a-module.md).
- CI: `.github/workflows/ci.yml` runs architecture tests + `lint:boundaries` + frontend build.

---

## How to add a new module (target-state DX)

**Backend**
1. `dotnet new cartrack-module -n Invoicing` → creates `CarTrack.Modules.Invoicing` referencing only `Shared`.
2. Add entities to `Domain/`, an `InvoicingDbContext` (schema `invoicing`), and an initial migration.
3. Add endpoints/services in `Features/`; register in `InvoicingModule : IModule`.
4. Add permission keys to the module's catalog contribution.
5. Add `ProjectReference` in `Host` (or drop the DLL). Done — discovery wires it.
6. If it needs other modules' data, consume their `*.Contracts` (interfaces/events) — never their internals.

**Frontend**
1. `modules/invoicing/index.ts` exports a `ModuleDefinition` (routes, navItems, permissions, searchProviders).
2. Add the module to the shell's `modules` array.
3. Build pages/components/services/types inside `modules/invoicing/*`.

**Guarantee:** steps above touch only the new module + one registration point. No sibling module changes.

---

## Key Challenges & Decisions

| Challenge | Decision |
|-----------|----------|
| **Splitting one DbContext (B1/B2)** | DbContext-per-module, schema-per-module, module-owned migrations. One physical DB. Baseline existing tables to avoid destructive migrations. |
| **Cross-module data (B8, StaffProfile)** | Users module owns org identity and publishes `IOrgDirectory` + org events. Others store user IDs only; no cross-schema FKs. |
| **Shared RLS logic (B3/B4)** | `UserDataScope` keeps only identity/role/branch primitives; domain predicates move into owning modules; manager hierarchy exposed via `IOrgDirectory`. |
| **Central seeder (B5)** | Each module implements `SeedAsync`; Host orchestrates generically. |
| **Reporting over all domains (B6)** | Reporting consumes per-module read-contracts / event-fed read models instead of querying foreign tables. Interim: keep a read-only reporting view layer until contracts exist. |
| **Notifications coupling (B7)** | Producers publish integration events; Notifications subscribes. No direct `NotificationTriggers` calls. |
| **Frontend hubs (F1/F2)** | Replace god-utils with registries each module contributes to. |
| **Migrations history collisions** | Distinct `MigrationsHistoryTable` per DbContext + schemas prevent clashes. |
| **Enforcement** | Backend architecture tests + frontend ESLint import-boundary rule in CI. |

---

## Risks & Mitigations

- **Migration baseline mistakes** → test the split on a **restored copy** of a real DB before applying anywhere; script idempotent baselines; back up first.
- **Hidden cross-module coupling surfaces late** → carve the cleanest module first; let arch tests fail loudly; keep the temporary LegacyModule until each area is fully extracted.
- **Scope creep into behavior changes** → strict rule: **moves + boundary edits only** per PR; no logic rewrites.
- **Reporting is the hardest** → do it last, after contracts/events exist; allow an interim read-only view.
- **Team churn / long-lived branch** → land each module extraction as its own small PR onto `modular`; keep phases mergeable.

---

## Success Criteria

1. `Host/Program.cs` contains **no feature logic** — only module discovery + pipeline.
2. Every module project references **only `Shared`** (verified by architecture tests).
3. Each module owns its **DbContext + migrations**; no cross-module FKs.
4. Notifications & Reporting depend on **contracts/events**, not module internals.
5. Frontend shell composes **routes/nav/search/permissions** from a `modules` registry; ESLint blocks cross-module imports.
6. A new module can be added end-to-end by creating one backend project + one frontend folder and registering each once — **no edits to sibling modules**.

---

## Appendix — Proposed module map

| Module | Backend owns | Frontend owns | Publishes (Contracts) | Consumes |
|--------|--------------|---------------|-----------------------|----------|
| **Users** | `ApplicationUser` admin, `StaffProfile`, `DriverProfileLink`, roles/permissions admin, org tree | EmployeesList, employees/*, users/* | `IOrgDirectory`, org events | Identity (Shared) |
| **CoreHr** | Company, Department, Position | core/* pages, coreHr/* | `IOrgStructure` (lookups) | — |
| **Leave** | LeaveType, LeaveRequest, LeaveBalance, PublicHoliday, accrual | leave/* | `LeaveRequest*` events | `IOrgDirectory`, EventBus |
| **Expense** | ExpenseCategory, ExpenseClaim, workflow | expense/* | `ExpenseClaim*` events | `IOrgDirectory`, EventBus |
| **Fleet** | Vehicle, Driver, CarTrack client, page cache | vehicles/*, drivers/*, live tracking, fleet dashboard | `Vehicle*`/`Driver*` events | Settings (credentials), EventBus |
| **Reporting** | Dashboard, ReportDefinition, ReportPlacement, query engine | reports/*, dashboard/* | — | module read-contracts/events |
| **Notifications** | Notification, NotificationRecipient, SignalR hub | notifications UI | — | all producers' events |
| **Support** | SupportTicket, TicketCategory | settings/support | — | — |
| **Help** | HelpArticle | help drawer, help admin | — | — |
| **Settings** | PlatformSettings, CarTrackSettings | settings/* | `ICarTrackCredentials` | — |
| **Identity/Auth** *(Shared or Auth module)* | JWT, refresh tokens, MFA, `ApplicationUser` base | auth/* | `ICurrentUserScope`, permissions | — |
