using CarTrack.Server.Users;

namespace CarTrack.Identity.Contracts;

/// <summary>
/// Authenticated user data scope: identity, roles, org reports, and fleet link primitives.
/// Domain authorization predicates live in owning modules (Leave/Expense/Fleet).
/// </summary>
public interface ICurrentUserScope
{
    Task<UserDataScope> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Identity/org primitives for row-level decisions. Domain-specific helpers are
/// extensions in each feature module.
/// </summary>
public sealed class UserDataScope
{
    public required string UserId { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = [];

    public bool BypassRowLevelSecurity { get; init; }

    public bool HasFullFleetAccess { get; init; }

    public Guid? LinkedDriverId { get; init; }

    public string? LinkedDriverLicenceNumber { get; init; }

    public string? LinkedDriverCode { get; init; }

    public string? LinkedCarTrackDriverId { get; init; }

    public string? StaffBranch { get; init; }

    public IReadOnlySet<string> ReportUserIds { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    public IReadOnlySet<Guid> ReportLinkedDriverIds { get; init; } = new HashSet<Guid>();

    public bool IsDriverRestricted =>
        !BypassRowLevelSecurity
        && !HasFullFleetAccess
        && Roles.Any(role => role.Equals(AppRoles.Driver, StringComparison.OrdinalIgnoreCase));

    public bool IsManagerApprover =>
        !BypassRowLevelSecurity
        && Roles.Any(role => role.Equals(AppRoles.Manager, StringComparison.OrdinalIgnoreCase));

    public bool IsFinanceBranchScoped =>
        !BypassRowLevelSecurity
        && Roles.Any(role => role.Equals(AppRoles.Finance, StringComparison.OrdinalIgnoreCase))
        && !string.IsNullOrWhiteSpace(StaffBranch);

    public bool IsFinance =>
        !BypassRowLevelSecurity
        && Roles.Any(role => role.Equals(AppRoles.Finance, StringComparison.OrdinalIgnoreCase));
}
