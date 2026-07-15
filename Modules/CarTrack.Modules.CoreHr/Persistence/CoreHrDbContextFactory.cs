using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarTrack.Modules.CoreHr;

public sealed class CoreHrDbContextFactory : IDesignTimeDbContextFactory<CoreHrDbContext>
{
    public CoreHrDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CoreHrDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=cartrack;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsHistoryTable(CoreHrModule.MigrationsHistoryTable))
            .Options;

        return new CoreHrDbContext(options);
    }
}
