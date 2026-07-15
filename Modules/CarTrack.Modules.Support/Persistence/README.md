# Support persistence ownership

`SupportDbContext` owns the `SupportTickets` and `TicketCategories` tables and
uses migrations history table `__EFMigrationsHistory_Support` (same physical
PostgreSQL database as the host).

## Baseline (existing databases)

Environments that already created these tables via the host
`ApplicationDbContext` migration `AddSupportTicketsTable` will **not** re-run
`InitialSupport`. On first Support migrate, `MigrateModuleAsync` detects the
probe table, finds an empty Support history table, and inserts pending
migration ids without executing `Up()`.

## Host model removal

The host migration `RemoveSupportFromHost` has **empty** `Up`/`Down` methods on
purpose: removing Support entities from `ApplicationDbContext` must not
`DropTable` — ownership moved to this module.

The historical host migration `AddSupportTicketsTable` also has empty
`Up`/`Down` so fresh installs do not create the tables twice (Support migrates
before Legacy seed). Databases that already applied it keep their existing
tables (including any legacy FKs to `AspNetUsers`).
