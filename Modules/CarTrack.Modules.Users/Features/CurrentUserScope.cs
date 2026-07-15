using System.Security.Claims;
using CarTrack.Identity.Contracts;
using CarTrack.Server.Users;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Users;

public class CurrentUserScope(
    IHttpContextAccessor httpContextAccessor,
    UsersDbContext dbContext,
    IDriverDirectory driverDirectory) : ICurrentUserScope
{
    private UserDataScope? _cached;

    public async Task<UserDataScope> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            _cached = CreateUnauthenticatedScope();
            return _cached;
        }

        var userId = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            _cached = CreateUnauthenticatedScope();
            return _cached;
        }

        var roles = principal
            .FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var bypass = roles.Any(role =>
            role.Equals(AppRoles.SystemAdmin, StringComparison.OrdinalIgnoreCase)
            || role.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase));

        var hasFullFleetAccess = bypass
            || roles.Any(role =>
                role.Equals(AppRoles.FleetAdmin, StringComparison.OrdinalIgnoreCase)
                || role.Equals(AppRoles.FleetOperator, StringComparison.OrdinalIgnoreCase));

        var staffProfile = await dbContext.StaffProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

        var driverLink = await dbContext.DriverProfileLinks
            .AsNoTracking()
            .SingleOrDefaultAsync(link => link.UserId == userId, cancellationToken);

        DriverScopeInfo? driverScope = null;
        if (driverLink is not null)
        {
            driverScope = await driverDirectory.GetScopeInfoAsync(driverLink.DriverId, cancellationToken);
        }

        HashSet<string> reportUserIds = bypass
            ? []
            : await ManagerHierarchy.GetAllReportUserIdsAsync(dbContext, userId, cancellationToken);

        HashSet<Guid> reportLinkedDriverIds = [];
        if (reportUserIds.Count > 0)
        {
            reportLinkedDriverIds = await dbContext.DriverProfileLinks
                .AsNoTracking()
                .Where(link => reportUserIds.Contains(link.UserId))
                .Select(link => link.DriverId)
                .ToHashSetAsync(cancellationToken);
        }

        _cached = new UserDataScope
        {
            UserId = userId,
            Roles = roles,
            BypassRowLevelSecurity = bypass,
            HasFullFleetAccess = hasFullFleetAccess,
            LinkedDriverId = driverLink?.DriverId,
            LinkedDriverLicenceNumber = driverScope?.LicenceNumber,
            LinkedDriverCode = driverScope?.DriverCode,
            LinkedCarTrackDriverId = driverScope?.CarTrackDriverId,
            StaffBranch = staffProfile?.Branch,
            ReportUserIds = reportUserIds,
            ReportLinkedDriverIds = reportLinkedDriverIds,
        };

        return _cached;
    }

    private static UserDataScope CreateUnauthenticatedScope() =>
        new()
        {
            UserId = string.Empty,
            Roles = [],
            BypassRowLevelSecurity = false,
            HasFullFleetAccess = false,
            ReportUserIds = new HashSet<string>(StringComparer.Ordinal),
            ReportLinkedDriverIds = new HashSet<Guid>(),
        };
}
