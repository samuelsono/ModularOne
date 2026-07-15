using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace CarTrack.Api;

/// <summary>
/// Contract for a self-contained feature module discovered by the Host.
/// </summary>
public interface IModule
{
    /// <summary>Stable module key (e.g. "leave", "expense").</summary>
    string Name { get; }

    /// <summary>Register services, DbContext, options, and hosted services.</summary>
    void AddModule(IHostApplicationBuilder builder);

    /// <summary>Map this module's HTTP endpoints under the application API root.</summary>
    void MapEndpoints(IEndpointRouteBuilder endpoints);

    /// <summary>Apply this module's EF migrations (no-op when none).</summary>
    Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default);

    /// <summary>Seed reference data for this module (idempotent).</summary>
    Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default);
}
