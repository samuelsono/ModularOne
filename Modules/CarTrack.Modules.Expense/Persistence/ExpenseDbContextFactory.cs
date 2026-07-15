using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarTrack.Modules.Expense;

public sealed class ExpenseDbContextFactory : IDesignTimeDbContextFactory<ExpenseDbContext>
{
    public ExpenseDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ExpenseDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=cartrack;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsHistoryTable(ExpenseModule.MigrationsHistoryTable))
            .Options;

        return new ExpenseDbContext(options);
    }
}
