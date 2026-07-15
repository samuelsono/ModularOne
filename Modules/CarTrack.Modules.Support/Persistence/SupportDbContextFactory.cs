using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarTrack.Modules.Support;

public sealed class SupportDbContextFactory : IDesignTimeDbContextFactory<SupportDbContext>
{
    public SupportDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=cartrack;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsHistoryTable(SupportModule.MigrationsHistoryTable))
            .Options;

        return new SupportDbContext(options);
    }
}
