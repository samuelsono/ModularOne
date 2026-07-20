using System.Security.Claims;
using CarTrack.Server.Users;
using Microsoft.AspNetCore.Authorization;

namespace CarTrack.Server.Users.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (AuthorizationPermissionHelper.HasPermission(context.User, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public class AnyPermissionAuthorizationHandler : AuthorizationHandler<AnyPermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AnyPermissionRequirement requirement)
    {
        if (requirement.Permissions.Any(permission =>
                AuthorizationPermissionHelper.HasPermission(context.User, permission)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

internal static class AuthorizationPermissionHelper
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

    internal static bool HasPermission(ClaimsPrincipal user, string permission)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var roles = GetRoles(user);

        if (roles.Any(role =>
                role.Equals(AppRoles.SystemAdmin, StringComparison.OrdinalIgnoreCase)
                || role.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Known app roles always authorize from the in-code catalog — never from stale JWT
        // permission claims (e.g. after Manager lost Company/Department access).
        var knownRoles = roles
            .Where(role => PermissionCatalog.RolePermissionKeys.ContainsKey(role))
            .ToList();

        if (knownRoles.Count > 0)
        {
            return BuildCatalogPermissions(knownRoles).Contains(permission);
        }

        // Fallback for unexpected custom roles not present in the catalog.
        var fromClaims = user
            .FindAll(AuthClaimTypes.Permission)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!roles.Any(role => role.Equals(AppRoles.Hr, StringComparison.OrdinalIgnoreCase)))
        {
            fromClaims.ExceptWith(CoreStructurePermissionKeys);
        }

        return fromClaims.Contains(permission);
    }

    internal static IReadOnlyList<string> GetRoles(ClaimsPrincipal user) =>
        user
            .FindAll(ClaimTypes.Role)
            .Concat(user.FindAll("role"))
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static HashSet<string> BuildCatalogPermissions(IReadOnlyList<string> roles)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles)
        {
            if (!PermissionCatalog.RolePermissionKeys.TryGetValue(role, out var keys))
            {
                continue;
            }

            foreach (var key in keys)
            {
                allowed.Add(key);
            }
        }

        var isHr = roles.Any(role => role.Equals(AppRoles.Hr, StringComparison.OrdinalIgnoreCase));
        if (!isHr)
        {
            allowed.ExceptWith(CoreStructurePermissionKeys);
        }

        return allowed;
    }
}

public sealed class AnyPermissionRequirement(params string[] permissions) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; } = permissions;
}

public static class AuthClaimTypes
{
    public const string Permission = "permission";

    public const string Module = "module";

    public const string TokenPurpose = "token_purpose";

    public const string MfaChallenge = "mfa_challenge";
}
