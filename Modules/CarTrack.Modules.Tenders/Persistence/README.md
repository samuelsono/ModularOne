# Tenders persistence

- DbContext: `TendersDbContext`
- Migrations history table: `__EFMigrationsHistory_Tenders`
- Probe table: `TenderSources`

## Tables

- `TenderSources` (+ circuit; auth: `AuthKind`, `AuthUsername`, `ProtectedAuthSecret`)
- `TenderQueries`
- `TenderScrapeRuns` / `TenderScrapeRunSources`
- `TenderMatches` (unique `(SourceId, ExternalKey)`; `DocumentMetadataJson`, `DocumentsRefreshedAt`, `OwnerUserId`)
- `TenderWatchSubscriptions`

## Add a migration

```bash
dotnet ef migrations add <Name> \
  --project Modules/CarTrack.Modules.Tenders \
  --startup-project CarTrack.Host \
  --context TendersDbContext \
  --output-dir Persistence/Migrations
```
