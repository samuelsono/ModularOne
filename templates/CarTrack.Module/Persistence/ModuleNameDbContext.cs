using Microsoft.EntityFrameworkCore;
using CarTrack.Infrastructure.Persistence;

namespace CarTrack.Modules.ModuleName;

public sealed class ModuleNameDbContext(DbContextOptions<ModuleNameDbContext> options)
    : ModuleDbContext(options)
{
    // public DbSet<ExampleEntity> Examples => Set<ExampleEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Map tables in public schema initially; rename to module schema when ready.
        // Do not add foreign keys to other modules' tables — store opaque IDs only.
    }
}
