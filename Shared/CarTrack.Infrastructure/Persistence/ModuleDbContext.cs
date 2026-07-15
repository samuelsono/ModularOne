using Microsoft.EntityFrameworkCore;

namespace CarTrack.Infrastructure.Persistence;

/// <summary>
/// Marker base type for feature-module DbContexts that own their own migrations.
/// </summary>
public abstract class ModuleDbContext : DbContext
{
    protected ModuleDbContext(DbContextOptions options)
        : base(options)
    {
    }
}
