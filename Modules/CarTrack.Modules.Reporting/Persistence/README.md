# Reporting persistence

`ReportingDbContext` owns Dashboards / Sections / ReportDefinitions / Placements
(history: `__EFMigrationsHistory_Reporting`).

B6 query executor (`ReportQueryService` / `ApiTableReportExecutor`) remains on the
Host so it can compose sibling module DbContexts without violating module→module
reference rules.

Existing DBs are baselined via probe table `Dashboards`.
