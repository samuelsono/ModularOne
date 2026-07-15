# ADR 0001 — Module Boundaries

- **Status:** Accepted
- **Date:** 2026-07-14
- **Context:** Phase 0 of the [Modular Architecture Migration Plan](../modular-architecture-plan.md)

## Context

Chronos Portal (CarTrack) is a folder-separated monolith: one `CarTrack.Server`
web project with all features as namespaces, one `ApplicationDbContext`, and a
single React SPA with feature folders plus a large shared shell. We are moving to
a **modular monolith** where each business capability owns its backend and
frontend slice and depends only on a small `Core`/`Shared` foundation.

This ADR records the boundary rules that every later phase must respect. It is
intentionally established **before** any code is moved, so the rules can be
enforced by tooling as modules are carved out.

## Decision

### Backend reference rules

```
Host        ──▶ every Module + Shared
Module X    ──▶ Shared only            (Core, Infrastructure, Identity, Api)
Module X    ──▶ Module Y.Contracts     (allowed ONLY via published Contracts / events)
Shared      ──▶ nothing app-specific
Core        ──▶ nothing (no EF, no ASP.NET, no module)
```

1. **A module never references another module's internals.** Cross-module
   interaction happens only through a module's published `Contracts` (interfaces
   and integration events) or the in-process event bus in `Shared`.
2. **One physical database, many DbContexts.** Each module owns its `DbContext`,
   a dedicated schema, and its own migrations history table. **No cross-module
   foreign keys** — reference other modules' data by ID only.
3. **`Core` stays pure.** No Entity Framework, ASP.NET, or module dependency.
4. **The Host owns composition only.** No feature/business logic in the Host;
   it discovers modules and builds the pipeline.

### Frontend reference rules

```
app/shell   ──▶ platform + every module's public entry (index.ts)
module X    ──▶ platform only
module X    ──▶ module Y                (NOT allowed — no cross-module imports)
platform    ──▶ nothing module-specific
```

1. A module imports from `@platform/*` and its own folder only. It must not
   import `@modules/<other>/*`.
2. The shell composes routes, navigation, permissions, and page-search from a
   module registry — each module contributes via its `ModuleDefinition`.
3. Shared UI, API client, auth, and permission primitives live in `@platform`.

## Enforcement

- **Backend:** `Tests/CarTrack.ArchitectureTests` asserts Core purity, no
  module→module internals, Shared-only CarTrack references, and an `IModule`
  per module assembly. Wired in `.github/workflows/ci.yml`.
- **Frontend:** path aliases `@platform/*` / `@modules/*`; ESLint
  `eslint-plugin-boundaries` (layers) plus per-module `no-restricted-imports`
  blocking `@modules/<sibling>`. Run `npm run lint:boundaries` (also in CI).

## Consequences

- Adding a module becomes: create one backend project + one frontend folder and
  register each once — **no edits to sibling modules**.
- Some convenience (direct calls, shared entities, cross-feature imports) is
  traded for independence and shippability.
- The rules are cheap to state now and increasingly expensive to retrofit later,
  which is why they are fixed in Phase 0.
