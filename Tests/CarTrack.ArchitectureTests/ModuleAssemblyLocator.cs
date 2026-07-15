using System.Reflection;

namespace CarTrack.ArchitectureTests;

/// <summary>
/// Discovers <c>CarTrack.Modules.*</c> assemblies from the test output directory.
///
/// Modules are loaded from disk (rather than hard-coding names) so new modules
/// are picked up automatically once the test project references them.
/// </summary>
internal static class ModuleAssemblyLocator
{
    private const string ModuleAssemblyPrefix = "CarTrack.Modules.";

    public static IReadOnlyList<Assembly> LoadModuleAssemblies()
    {
        var directory = AppContext.BaseDirectory;
        var assemblies = new List<Assembly>();

        foreach (var path in Directory.EnumerateFiles(directory, $"{ModuleAssemblyPrefix}*.dll"))
        {
            // Skip published *.Contracts assemblies — they are the sanctioned
            // cross-module surface and are allowed to be referenced.
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.EndsWith(".Contracts", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                assemblies.Add(Assembly.LoadFrom(path));
            }
            catch (BadImageFormatException)
            {
                // Not a managed assembly — ignore.
            }
        }

        return assemblies;
    }
}
