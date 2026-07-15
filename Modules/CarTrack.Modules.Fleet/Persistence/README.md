# Fleet persistence ownership

`FleetDbContext` owns `Vehicles`, `Drivers`, and `CarTrackPageCache`, and uses
migrations history table `__EFMigrationsHistory_Fleet` (same physical PostgreSQL
database as the host).

## Baseline (existing databases)

Environments that already created Fleet tables via host `ApplicationDbContext`
migrations will **not** re-run `InitialFleet`. On first Fleet migrate,
`MigrateModuleAsync` detects the probe table `Vehicles`, finds an empty Fleet
history table, and inserts pending migration ids without executing `Up()`.

## Host model removal

The host migration `RemoveFleetFromHost` has **empty** `Up`/`Down` methods on
purpose: removing Vehicle/Driver/CarTrackPageCache entities from
`ApplicationDbContext` must not `DropTable` — ownership moved to this module.

Historical host Fleet migrations also have empty `Up`/`Down` so fresh installs
do not create the tables twice (Fleet migrates before Legacy seed).
