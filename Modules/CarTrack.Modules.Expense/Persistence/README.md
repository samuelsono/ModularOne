# Expense persistence ownership

`ExpenseDbContext` owns `ExpenseCategories` and `ExpenseClaims`, and uses
migrations history table `__EFMigrationsHistory_Expense` (same physical
PostgreSQL database as the host).

## Baseline (existing databases)

Environments that already created Expense tables via host
`ApplicationDbContext` migrations will **not** re-run `InitialExpense`. On first
Expense migrate, `MigrateModuleAsync` detects the probe table
`ExpenseCategories`, finds an empty Expense history table, and inserts pending
migration ids without executing `Up()`.

## Host model removal

The host migration `RemoveLeaveAndExpenseFromHost` has **empty** `Up`/`Down`
methods on purpose: removing Leave/Expense entities from `ApplicationDbContext`
must not `DropTable` — ownership moved to this module (and Leave).

Historical host Expense migrations also have empty `Up`/`Down` so fresh installs
do not create the tables twice (Expense migrates before Legacy seed).
