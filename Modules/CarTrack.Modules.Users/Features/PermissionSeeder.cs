using CarTrack.Server.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Users;

public static class PermissionSeeder
{
    public static async Task SeedAsync(
        UsersDbContext dbContext,
        RoleManager<IdentityRole> roleManager,
        CancellationToken cancellationToken = default)
    {
        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        var existingKeys = await dbContext.Permissions
            .AsNoTracking()
            .Select(permission => permission.Key)
            .ToListAsync(cancellationToken);

        var existingKeySet = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var permissionsByKey = new Dictionary<string, Permission>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in PermissionCatalog.All)
        {
            if (existingKeySet.Contains(definition.Key))
            {
                continue;
            }

            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Key = definition.Key,
                ModuleSlug = definition.ModuleSlug,
                SubmoduleSlug = definition.SubmoduleSlug,
                Action = definition.Action,
                Description = definition.Description,
            };

            dbContext.Permissions.Add(permission);
            permissionsByKey[definition.Key] = permission;
        }

        if (permissionsByKey.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var allPermissions = await dbContext.Permissions.ToListAsync(cancellationToken);
        var permissionLookup = allPermissions.ToDictionary(permission => permission.Key, StringComparer.OrdinalIgnoreCase);
        var roles = await roleManager.Roles.ToListAsync(cancellationToken);
        var roleLookup = roles.ToDictionary(role => role.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        var existingMappings = await dbContext.RolePermissions
            .Select(mapping => new { mapping.Id, mapping.RoleId, mapping.PermissionId })
            .ToListAsync(cancellationToken);

        var existingMappingSet = existingMappings
            .Select(mapping => $"{mapping.RoleId}:{mapping.PermissionId}")
            .ToHashSet(StringComparer.Ordinal);

        var desiredMappingKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (roleName, permissionKeys) in PermissionCatalog.RolePermissionKeys)
        {
            if (!roleLookup.TryGetValue(roleName, out var role))
            {
                continue;
            }

            foreach (var permissionKey in permissionKeys)
            {
                if (!permissionLookup.TryGetValue(permissionKey, out var permission))
                {
                    continue;
                }

                var mappingKey = $"{role.Id}:{permission.Id}";
                desiredMappingKeys.Add(mappingKey);
                if (existingMappingSet.Contains(mappingKey))
                {
                    continue;
                }

                dbContext.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                });
            }
        }

        // Revoke stale role→permission rows so catalog removals take effect on existing DBs.
        var stale = existingMappings
            .Where(mapping => !desiredMappingKeys.Contains($"{mapping.RoleId}:{mapping.PermissionId}"))
            .Select(mapping => mapping.Id)
            .ToList();

        if (stale.Count > 0)
        {
            await dbContext.RolePermissions
                .Where(mapping => stale.Contains(mapping.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        // Explicitly strip Company/Department/Position grants from non-HR roles (defense in depth).
        var structurePermissionIds = await dbContext.Permissions
            .AsNoTracking()
            .Where(permission =>
                permission.Key == "core.companies.read"
                || permission.Key == "core.companies.write"
                || permission.Key == "core.departments.read"
                || permission.Key == "core.departments.write"
                || permission.Key == "core.positions.read"
                || permission.Key == "core.positions.write")
            .Select(permission => permission.Id)
            .ToListAsync(cancellationToken);

        if (structurePermissionIds.Count > 0)
        {
            var nonHrRoleIds = roles
                .Where(role =>
                    !string.Equals(role.Name, AppRoles.Hr, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(role.Name, AppRoles.Admin, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(role.Name, AppRoles.SystemAdmin, StringComparison.OrdinalIgnoreCase))
                .Select(role => role.Id)
                .ToList();

            if (nonHrRoleIds.Count > 0)
            {
                await dbContext.RolePermissions
                    .Where(mapping =>
                        nonHrRoleIds.Contains(mapping.RoleId)
                        && structurePermissionIds.Contains(mapping.PermissionId))
                    .ExecuteDeleteAsync(cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
