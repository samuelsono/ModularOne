using System.Security.Claims;
using CarTrack.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Users;

public interface ICurrentUserScope
{
    Task<UserDataScope> GetAsync(CancellationToken cancellationToken = default);
}

public class CurrentUserScope(
    IHttpContextAccessor httpContextAccessor,
    ApplicationDbContext dbContext) : ICurrentUserScope
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
            .ToList();

        var bypass = roles.Any(role =>
            role.Equals(AppRoles.SystemAdmin, StringComparison.OrdinalIgnoreCase)
            || role.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase));

        var fullFleet = bypass
            || roles.Any(role =>
                role.Equals(AppRoles.FleetAdmin, StringComparison.OrdinalIgnoreCase)
                || role.Equals(AppRoles.FleetOperator, StringComparison.OrdinalIgnoreCase));

        var staffProfile = await dbContext.StaffProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

        var driverLink = await dbContext.DriverProfileLinks
            .AsNoTracking()
            .Include(link => link.Driver)
            .SingleOrDefaultAsync(link => link.UserId == userId, cancellationToken);

        var reportUserIds = bypass
            ? new HashSet<string>(StringComparer.Ordinal)
            : await ManagerHierarchy.GetAllReportUserIdsAsync(dbContext, userId, cancellationToken);

        var reportLinkedDriverIds = reportUserIds.Count == 0
            ? new HashSet<Guid>()
            : await dbContext.DriverProfileLinks
                .AsNoTracking()
                .Where(link => reportUserIds.Contains(link.UserId))
                .Select(link => link.DriverId)
                .ToHashSetAsync(cancellationToken);

        _cached = new UserDataScope
        {
            UserId = userId,
            Roles = roles,
            BypassRowLevelSecurity = bypass,
            HasFullFleetAccess = fullFleet,
            LinkedDriverId = driverLink?.DriverId,
            LinkedDriverLicenceNumber = driverLink?.Driver.LicenceNumber,
            LinkedDriverCode = driverLink?.Driver.DriverCode,
            LinkedCarTrackDriverId = driverLink?.Driver.CarTrackDriverId,
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
            BypassRowLevelSecurity = false,
            HasFullFleetAccess = false,
        };
}
