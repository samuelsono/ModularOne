using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarTrack.Modules.Help;

public sealed class HelpDbContextFactory : IDesignTimeDbContextFactory<HelpDbContext>
{
    public HelpDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HelpDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=cartrack;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsHistoryTable(HelpModule.MigrationsHistoryTable))
            .Options;

        return new HelpDbContext(options);
    }
}
