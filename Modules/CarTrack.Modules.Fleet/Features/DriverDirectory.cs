using CarTrack.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Fleet;

/// <summary>
/// Fleet-backed driver directory for Users linking / RLS without cross-module EF FKs.
/// </summary>
public sealed class DriverDirectory(FleetDbContext dbContext) : IDriverDirectory
{
    public Task<bool> ExistsAsync(Guid driverId, CancellationToken cancellationToken = default) =>
        dbContext.Drivers.AnyAsync(driver => driver.Id == driverId, cancellationToken);

    public async Task<DriverUserSource?> GetForUserCreateAsync(
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        var driver = await dbContext.Drivers
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == driverId, cancellationToken);

        return driver is null
            ? null
            : new DriverUserSource(
                driver.Id,
                driver.DriverCode,
                driver.FirstName,
                driver.LastName,
                driver.WorkEmail,
                driver.EmployeeNumber,
                driver.JobTitle,
                driver.Department,
                driver.Branch);
    }

    public async Task<DriverScopeInfo?> GetScopeInfoAsync(
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        var driver = await dbContext.Drivers
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == driverId, cancellationToken);

        return driver is null
            ? null
            : new DriverScopeInfo(
                driver.Id,
                driver.LicenceNumber,
                driver.DriverCode,
                driver.CarTrackDriverId,
                driver.Branch);
    }

    public async Task<IReadOnlyDictionary<Guid, DriverLinkSummary>> GetLinkSummariesAsync(
        IEnumerable<Guid> driverIds,
        CancellationToken cancellationToken = default)
    {
        var ids = driverIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, DriverLinkSummary>();
        }

        var drivers = await dbContext.Drivers
            .AsNoTracking()
            .Where(driver => ids.Contains(driver.Id))
            .ToListAsync(cancellationToken);

        return drivers.ToDictionary(
            driver => driver.Id,
            driver => new DriverLinkSummary(
                driver.Id,
                driver.DriverCode,
                $"{driver.FirstName} {driver.LastName}".Trim()));
    }
}
