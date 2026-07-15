# Notifications persistence ownership

`NotificationsDbContext` owns the `Notifications` / `NotificationRecipients` tables
and uses migrations history table `__EFMigrationsHistory_Notifications`
(same physical PostgreSQL database as the host).

## Baseline (existing databases)

Environments that already created these tables via the host
`ApplicationDbContext` migration `AddNotificationsTable` will **not** re-run
`InitialNotifications`. On first Notifications migrate, `MigrateModuleAsync`
detects the probe table, finds an empty Notifications history table, and
inserts pending migration ids without executing `Up()`.

## Host model removal

The host migration `RemoveNotificationsFromHost` has **empty** `Up`/`Down`
methods on purpose: removing Notification entities from `ApplicationDbContext`
must not `DropTable` — ownership moved to this module.

The historical host migration `AddNotificationsTable` also has empty `Up`/`Down`
so fresh installs do not create the tables twice (Notifications migrates before
Legacy seed). Databases that already applied it keep their existing tables
(including any legacy FKs to `AspNetUsers`).
