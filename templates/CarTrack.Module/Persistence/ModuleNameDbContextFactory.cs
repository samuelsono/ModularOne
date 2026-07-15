using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarTrack.Modules.ModuleName;

public sealed class ModuleNameDbContextFactory : IDesignTimeDbContextFactory<ModuleNameDbContext>
{
    public ModuleNameDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ModuleNameDbContext>()
            .UseNpgsql("Host=localhost;Database=cartrack;Username=postgres;Password=postgres")
            .Options;

        return new ModuleNameDbContext(options);
    }
}
