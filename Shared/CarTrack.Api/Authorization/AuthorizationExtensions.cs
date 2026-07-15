using CarTrack.Server.Users.Authorization;
using Microsoft.AspNetCore.Builder;

namespace CarTrack.Server.Users.Authorization;

/// <summary>
/// Minimal-API authorization helpers. Lives in CarTrack.Api; keeps the historical
/// namespace so endpoint call sites remain unchanged.
/// </summary>
public static class AuthorizationExtensions
{
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission) =>
        builder.RequireAuthorization(policy => policy.AddRequirements(new PermissionRequirement(permission)));

    public static RouteHandlerBuilder RequireAnyPermission(this RouteHandlerBuilder builder, params string[] permissions) =>
        builder.RequireAuthorization(policy => policy.AddRequirements(new AnyPermissionRequirement(permissions)));
}
