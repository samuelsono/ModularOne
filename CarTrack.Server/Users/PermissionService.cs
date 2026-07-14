using CarTrack.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Users;

public interface IPermissionService
{
    Task<IReadOnlyList<string>> GetPermissionsForRolesAsync(
        IEnumerable<string> roleNames,
        CancellationToken cancellationToken = default);

    IReadOnlyList<string> GetModulesFromPermissions(IEnumerable<string> permissions);
}

public class PermissionService(ApplicationDbContext dbContext) : IPermissionService
{
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

        var roleIds = await dbContext.Roles
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
}
