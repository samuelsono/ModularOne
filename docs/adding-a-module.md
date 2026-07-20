# Adding a module

Checklist for shipping a new feature module end-to-end. Follows [ADR 0001](./adr/0001-module-boundaries.md).

**Guarantee:** these steps touch only the new module + one Host registration + one frontend registry entry. No sibling module edits.

---

## Backend

### 1. Scaffold the project

From the repo root (after a one-time install of the local template):

```bash
dotnet new install ./templates/CarTrack.Module
dotnet new cartrack-module -n Invoicing -o Modules/CarTrack.Modules.Invoicing --moduleKey invoicing
```

Or copy `templates/CarTrack.Module/` manually and rename `ModuleName` → your name, `modulekey` → lowercase key.

### 2. Wire the solution and Host

1. `dotnet sln CarTrack.sln add Modules/CarTrack.Modules.Invoicing/CarTrack.Modules.Invoicing.csproj`
2. Add a `ProjectReference` in `CarTrack.Host/CarTrack.Host.csproj`
3. Register in `CarTrack.Host/Program.cs` (or module discovery list):

```csharp
.Add(new CarTrack.Modules.Invoicing.InvoicingModule())
```

4. Add a `ProjectReference` in `Tests/CarTrack.ArchitectureTests` so reflection picks up the assembly in CI

Architecture tests will fail if the new module references another `CarTrack.Modules.*` assembly (except `*.Contracts`) or lacks an `IModule` implementation.

### 3. Domain, persistence, features

1. Add entities under `Domain/`
2. Map them in `*DbContext` (no cross-module FKs — store opaque IDs only)
3. Create the first EF migration (`Persistence/Migrations/`)
4. Update `ProbeTable` / README once the first real table exists
5. Implement services in `Features/` and minimal APIs in `*Endpoints.cs`
6. Contribute permission keys from this module (catalog / authorize attributes)

### 4. Cross-module needs

- Consume other modules only via published `*.Contracts` or Shared `IEventBus` / org APIs
- Never reference another module’s internals project

---

## Frontend

### 1. Scaffold the folder

```bash
cp -R frontend/src/modules/_template frontend/src/modules/invoicing
```

Rename `templateModule`, route paths, `appModule.slug`, and permissions inside `index.tsx`.

### 2. Register once

Add the export to `frontend/src/app/modules.tsx`:

```ts
import { invoicingModule } from '@modules/invoicing';
// …include in the modules array
```

### 3. Enforce lint boundaries

Add `'invoicing'` to `FEATURE_MODULES` in:

- `frontend/eslint.config.js`
- `frontend/eslint.boundaries.config.js`

### 4. Build inside the module

Pages, components, services, and types stay under `modules/invoicing/*`.

Allowed imports:

- `@platform/*`
- `@modules/invoicing/*` (own module)

Not allowed:

- `@modules/<other>/*`

If the shell must compose UI from several modules (settings hub, fleet home + reporting), put that screen under `src/app/` — not inside a feature module.

### 5. Activate top-bar search for a route

Register `searchProviders` on the module and consume `usePageSearchQuery()` on the page. Step-by-step: [frontend-page-search.md](./frontend-page-search.md).

---

## Verify

```bash
# Backend boundaries
dotnet test Tests/CarTrack.ArchitectureTests/CarTrack.ArchitectureTests.csproj

# Frontend boundaries + compile
cd frontend
npm run lint:boundaries
npm run build
```

CI runs the same checks (see `.github/workflows/ci.yml`).
