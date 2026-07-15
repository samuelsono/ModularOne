# ModuleName persistence

- DbContext: `ModuleNameDbContext`
- Migrations history table: `__EFMigrationsHistory_ModuleName`
- Probe table: replace `ModuleNamePlaceholder` in `ModuleNameModule` once the first real table exists

## First migration

```bash
dotnet ef migrations add InitialModuleName \
  --project Modules/CarTrack.Modules.ModuleName \
  --startup-project CarTrack.Host \
  --context ModuleNameDbContext \
  --output-dir Persistence/Migrations
```

Baseline existing shared tables with `MigrateModuleAsync` (probe-table check) so production data is not dropped.
