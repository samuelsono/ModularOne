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
        if (requirement.Permissions.Any(permission => AuthorizationPermissionHelper.HasPermission(context.User, permission)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

internal static class AuthorizationPermissionHelper
{
    internal static bool HasPermission(System.Security.Claims.ClaimsPrincipal user, string permission)
    {
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        var roles = user
            .FindAll(System.Security.Claims.ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToList();

        if (roles.Any(role =>
                role.Equals(AppRoles.SystemAdmin, StringComparison.OrdinalIgnoreCase)
                || role.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return user
            .FindAll(AuthClaimTypes.Permission)
            .Any(claim => claim.Value.Equals(permission, StringComparison.OrdinalIgnoreCase));
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
