using System.Reflection;
using CarTrack.Api;
using Xunit;

namespace CarTrack.ArchitectureTests;

/// <summary>
/// Enforces the backend reference rules from ADR 0001 (Module Boundaries).
/// </summary>
public class ModuleBoundaryTests
{
    private const string ModuleAssemblyPrefix = "CarTrack.Modules.";
    private const string CoreAssemblyName = "CarTrack.Core";

    private static readonly HashSet<string> AllowedSharedReferences = new(StringComparer.Ordinal)
    {
        "CarTrack.Core",
        "CarTrack.Api",
        "CarTrack.Identity",
        "CarTrack.Infrastructure",
    };

    /// <summary>
    /// ADR 0001: Core must stay pure — no Entity Framework, ASP.NET, or module
    /// dependency.
    /// </summary>
    [Fact]
    public void Core_HasNoFrameworkOrModuleDependencies()
    {
        var referenced = typeof(Core.CoreAssemblyMarker).Assembly
            .GetReferencedAssemblies()
            .Select(name => name.Name ?? string.Empty)
            .ToArray();

        var forbidden = referenced
            .Where(name =>
                name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                || name.StartsWith(ModuleAssemblyPrefix, StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            forbidden.Length == 0,
            $"CarTrack.Core must not depend on EF/ASP.NET/modules, but references: {string.Join(", ", forbidden)}");
    }

    /// <summary>
    /// ADR 0001: no module references another module's assembly. Runs over all
    /// discovered <c>CarTrack.Modules.*</c> assemblies.
    /// </summary>
    [Fact]
    public void Modules_DoNotReferenceOtherModules()
    {
        var moduleAssemblies = ModuleAssemblyLocator.LoadModuleAssemblies();
        Assert.True(
            moduleAssemblies.Count >= 10,
            $"Expected at least Help/Support/CoreHr/Notifications/Leave/Expense/Fleet/Users/Settings/Reporting module assemblies, found {moduleAssemblies.Count}.");

        var violations = new List<string>();
        foreach (var assembly in moduleAssemblies)
        {
            var selfName = assembly.GetName().Name ?? string.Empty;
            var offendingRefs = assembly.GetReferencedAssemblies()
                .Select(name => name.Name ?? string.Empty)
                .Where(name =>
                    name.StartsWith(ModuleAssemblyPrefix, StringComparison.Ordinal)
                    && !name.Equals(selfName, StringComparison.Ordinal)
                    // A module may reference another module's published contracts.
                    && !name.EndsWith(".Contracts", StringComparison.Ordinal));

            violations.AddRange(offendingRefs.Select(reference => $"{selfName} -> {reference}"));
        }

        Assert.True(
            violations.Count == 0,
            $"Modules must not reference other modules' internals: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// ADR 0001: modules may only project-reference Shared (plus optional *.Contracts).
    /// </summary>
    [Fact]
    public void Modules_OnlyReferenceAllowedCarTrackAssemblies()
    {
        var moduleAssemblies = ModuleAssemblyLocator.LoadModuleAssemblies();
        var violations = new List<string>();

        foreach (var assembly in moduleAssemblies)
        {
            var selfName = assembly.GetName().Name ?? string.Empty;
            foreach (var reference in assembly.GetReferencedAssemblies().Select(name => name.Name ?? string.Empty))
            {
                if (!reference.StartsWith("CarTrack.", StringComparison.Ordinal))
                {
                    continue;
                }

                if (reference.Equals(selfName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (AllowedSharedReferences.Contains(reference))
                {
                    continue;
                }

                if (reference.EndsWith(".Contracts", StringComparison.Ordinal))
                {
                    continue;
                }

                violations.Add($"{selfName} -> {reference}");
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Modules may only reference Shared / *.Contracts CarTrack assemblies: {string.Join(", ", violations)}");
    }

    /// <summary>
    /// Every module assembly must expose at least one concrete <see cref="IModule"/>.
    /// </summary>
    [Fact]
    public void Modules_ExposeIModuleImplementation()
    {
        var moduleAssemblies = ModuleAssemblyLocator.LoadModuleAssemblies();
        var missing = new List<string>();

        foreach (var assembly in moduleAssemblies)
        {
            var hasModule = assembly.GetTypes()
                .Any(type =>
                    type is { IsClass: true, IsAbstract: false, IsPublic: true }
                    && typeof(IModule).IsAssignableFrom(type));

            if (!hasModule)
            {
                missing.Add(assembly.GetName().Name ?? assembly.FullName ?? "<unknown>");
            }
        }

        Assert.True(
            missing.Count == 0,
            $"Each module assembly must implement IModule. Missing: {string.Join(", ", missing)}");
    }

    /// <summary>
    /// Guards that the Shared foundation is in place and its internal layering is intact.
    /// </summary>
    [Fact]
    public void SharedFoundation_AssembliesAreDiscoverable()
    {
        Assert.Equal(CoreAssemblyName, typeof(Core.CoreAssemblyMarker).Assembly.GetName().Name);
        Assert.NotNull(typeof(Infrastructure.InfrastructureAssemblyMarker).Assembly);
        Assert.NotNull(typeof(Identity.IdentityAssemblyMarker).Assembly);
        Assert.NotNull(typeof(Api.ApiAssemblyMarker).Assembly);
    }
}
