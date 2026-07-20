using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarTrack.Modules.Tenders;

public sealed class TendersDbContextFactory : IDesignTimeDbContextFactory<TendersDbContext>
{
    public TendersDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TendersDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=cartrack;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsHistoryTable(TendersModule.MigrationsHistoryTable))
            .Options;

        return new TendersDbContext(options);
    }
}
