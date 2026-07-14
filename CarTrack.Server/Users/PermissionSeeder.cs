using CarTrack.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Users;

public static class PermissionSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext dbContext,
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
        var roles = await dbContext.Roles.ToListAsync(cancellationToken);
        var roleLookup = roles.ToDictionary(role => role.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        var existingMappings = await dbContext.RolePermissions
            .Select(mapping => new { mapping.RoleId, mapping.PermissionId })
            .ToListAsync(cancellationToken);

        var existingMappingSet = existingMappings
            .Select(mapping => $"{mapping.RoleId}:{mapping.PermissionId}")
            .ToHashSet(StringComparer.Ordinal);

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

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
