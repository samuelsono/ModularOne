# CoreHr persistence ownership

`CoreHrDbContext` owns the `Companies`, `Departments`, and `Positions` tables and
uses migrations history table `__EFMigrationsHistory_CoreHr` (same physical
PostgreSQL database as the host).

## Baseline (existing databases)

Environments that already created these tables via the host
`ApplicationDbContext` migration `AddCoreHrOrganizationStructure` will **not**
re-run `InitialCoreHr`. On first CoreHr migrate, `MigrateModuleAsync` detects
the probe table (`Companies`), finds an empty CoreHr history table, and inserts
pending migration ids without executing `Up()`.

## Host model removal

The host migration `RemoveCoreHrFromHost` has **empty** `Up`/`Down` methods on
purpose: removing Company/Department/Position from the host model must not
`DropTable` — ownership moved to this module.

Historical host migrations that formerly created or altered these tables
(`AddCoreHrOrganizationStructure`, `CompleteIAuditableColumns`) have those
portions emptied so fresh installs do not create the tables twice (CoreHr
migrates before Legacy seed). Databases that already applied them keep their
existing tables (including any legacy FKs to `AspNetUsers` / `StaffProfiles`).
