using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace CarTrack.Api;

/// <summary>
/// Discovers and holds the set of modules registered with the Host.
/// </summary>
public sealed class ModuleRegistry
{
    private readonly List<IModule> _modules = [];

    public IReadOnlyList<IModule> Modules => _modules;

    public ModuleRegistry Add(IModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        if (_modules.Any(existing => existing.Name.Equals(module.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"A module named '{module.Name}' is already registered.");
        }

        _modules.Add(module);
        return this;
    }

    public void AddModules(IHostApplicationBuilder builder)
    {
        foreach (var module in _modules)
        {
            module.AddModule(builder);
        }
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        foreach (var module in _modules)
        {
            module.MapEndpoints(endpoints);
        }
    }

    public async Task MigrateAllAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        foreach (var module in _modules)
        {
            await module.MigrateAsync(services, cancellationToken);
        }
    }

    public async Task SeedAllAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        foreach (var module in _modules)
        {
            await module.SeedAsync(services, cancellationToken);
        }
    }
}
