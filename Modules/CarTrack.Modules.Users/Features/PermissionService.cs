using CarTrack.Server.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Users;

public class PermissionService(
    UsersDbContext dbContext,
    RoleManager<IdentityRole> roleManager) : IPermissionService
{
    private static readonly HashSet<string> CoreStructurePermissionKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "core.companies.read",
        "core.companies.write",
        "core.departments.read",
        "core.departments.write",
        "core.positions.read",
        "core.positions.write",
    };

    public async Task<IReadOnlyList<string>> GetPermissionsForRolesAsync(
        IEnumerable<string> roleNames,
        CancellationToken cancellationToken = default)
    {
        var normalizedRoles = roleNames
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedRoles.Count == 0)
        {
            return [];
        }

        if (normalizedRoles.Any(role =>
                role.Equals(AppRoles.SystemAdmin, StringComparison.OrdinalIgnoreCase)
                || role.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase)))
        {
            return PermissionCatalog.All.Select(permission => permission.Key).ToList();
        }

        // Known app roles always resolve from the in-code catalog so newly granted keys
        // (e.g. leave.reports.read for Staff) apply without waiting for DB seed timing,
        // and revoked keys disappear even when RolePermissions rows are stale.
        var catalogAllowed = BuildCatalogUnion(normalizedRoles);
        if (catalogAllowed.Count > 0)
        {
            return catalogAllowed
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Fallback for unexpected custom roles not present in the catalog.
        var roleIds = await roleManager.Roles
            .AsNoTracking()
            .Where(role => normalizedRoles.Contains(role.Name!))
            .Select(role => role.Id)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
        {
            return [];
        }

        return await dbContext.RolePermissions
            .AsNoTracking()
            .Where(mapping => roleIds.Contains(mapping.RoleId))
            .Select(mapping => mapping.Permission.Key)
            .Distinct()
            .OrderBy(key => key)
            .ToListAsync(cancellationToken);
    }

    public IReadOnlyList<string> GetModulesFromPermissions(IEnumerable<string> permissions) =>
        permissions
            .Select(permission => permission.Split('.', 2)[0])
            .Where(module => !string.IsNullOrWhiteSpace(module))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(module => module)
            .ToList();

    private static HashSet<string> BuildCatalogUnion(IReadOnlyList<string> roleNames)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var roleName in roleNames)
        {
            if (!PermissionCatalog.RolePermissionKeys.TryGetValue(roleName, out var keys))
            {
                continue;
            }

            foreach (var key in keys)
            {
                allowed.Add(key);
            }
        }

        // Hard deny: non-HR roles never receive Company/Department/Position structure rights,
        // even if an older catalog entry or DB row tried to grant them.
        var isHr = roleNames.Any(role => role.Equals(AppRoles.Hr, StringComparison.OrdinalIgnoreCase));
        if (!isHr)
        {
            allowed.ExceptWith(CoreStructurePermissionKeys);
        }

        return allowed;
    }
}
