namespace CarTrack.Identity.Contracts;

/// <summary>
/// Cross-module org directory (manager hierarchy, reports, branch, staff snapshots).
/// Implemented by the Host/Users layer; modules must not query StaffProfiles directly.
/// </summary>
public interface IOrgDirectory
{
    Task<string?> ResolveManagerUserIdAsync(string requesterUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDirectReportUserIdsAsync(string managerUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlySet<string>> GetAllReportUserIdsAsync(string managerUserId, CancellationToken cancellationToken = default);

    Task<string?> ResolveRequesterBranchAsync(string requesterUserId, CancellationToken cancellationToken = default);

    Task<StaffOrgInfo?> GetStaffOrgInfoAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, StaffOrgInfo>> GetStaffOrgInfoBatchAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffAccrualInfo>> GetActiveStaffForAccrualAsync(CancellationToken cancellationToken = default);

    Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Resolves the login linked to a driver (DriverProfileLinks owned by Host).</summary>
    Task<DriverLinkedUserInfo?> GetLinkedUserForDriverAsync(
        Guid driverId,
        CancellationToken cancellationToken = default);

    /// <summary>Batch map of driver id → linked login for fleet driver listings.</summary>
    Task<IReadOnlyDictionary<Guid, DriverLinkedUserInfo>> GetLinkedUsersByDriverIdAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>Opaque staff org attributes used for RLS / reports without StaffProfile EF types.</summary>
public sealed record StaffOrgInfo(string UserId, string? Department, string? Branch, string Gender);

/// <summary>Minimal active-staff projection for leave accrual.</summary>
public sealed record StaffAccrualInfo(string UserId, DateOnly? WorkStartDate, string Gender);

/// <summary>Login linked to a fleet driver without exposing ApplicationUser/Driver EF types.</summary>
public sealed record DriverLinkedUserInfo(
    string UserId,
    string Username,
    string Email,
    string? DisplayName,
    bool IsActive);
