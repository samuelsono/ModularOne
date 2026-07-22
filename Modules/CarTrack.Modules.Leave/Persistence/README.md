# Leave persistence ownership

`LeaveDbContext` owns `LeaveTypes`, `PublicHolidays`, `LeaveBalances`,
`LeaveRequests`, `WorkLocationTypes`, `ScheduleTemplates`, `ScheduleTemplateDays`,
`ScheduleDayOverrides`, `AttendanceDays`, and `AttendanceCollaborators`, and uses
migrations history table `__EFMigrationsHistory_Leave`
(same physical PostgreSQL database as the host).

## Baseline (existing databases)

Environments that already created Leave tables via host `ApplicationDbContext`
migrations will **not** re-run `InitialLeave`. On first Leave migrate,
`MigrateModuleAsync` detects the probe table `LeaveTypes`, finds an empty Leave
history table, and inserts pending migration ids without executing `Up()`.

## Host model removal

The host migration `RemoveLeaveAndExpenseFromHost` has **empty** `Up`/`Down`
methods on purpose: removing Leave/Expense entities from `ApplicationDbContext`
must not `DropTable` — ownership moved to this module (and Expense).

Historical host Leave migrations also have empty `Up`/`Down` so fresh installs
do not create the tables twice (Leave migrates before Legacy seed).
