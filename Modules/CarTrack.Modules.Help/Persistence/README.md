# Help persistence ownership

`HelpDbContext` owns the `HelpArticles` table and uses migrations history table
`__EFMigrationsHistory_Help` (same physical PostgreSQL database as the host).

## Baseline (existing databases)

Environments that already created `HelpArticles` via the host
`ApplicationDbContext` migration `AddHelpArticlesTable` will **not** re-run
`InitialHelp`. On first Help migrate, `MigrateModuleAsync` detects the probe
table, finds an empty Help history table, and inserts pending migration ids
without executing `Up()`.

## Host model removal

The host migration `RemoveHelpArticleFromHost` has **empty** `Up`/`Down`
methods on purpose: removing `HelpArticle` from `ApplicationDbContext` must not
`DropTable` — ownership moved to this module.

The historical host migration `AddHelpArticlesTable` also has empty `Up`/`Down`
so fresh installs do not create the table twice (Help migrates before Legacy
seed). Databases that already applied it keep their existing table (including
any legacy FK to `AspNetUsers`).
