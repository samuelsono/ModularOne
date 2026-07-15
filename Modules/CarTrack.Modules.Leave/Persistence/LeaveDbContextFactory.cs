using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarTrack.Modules.Leave;

public sealed class LeaveDbContextFactory : IDesignTimeDbContextFactory<LeaveDbContext>
{
    public LeaveDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LeaveDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=cartrack;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsHistoryTable(LeaveModule.MigrationsHistoryTable))
            .Options;

        return new LeaveDbContext(options);
    }
}
