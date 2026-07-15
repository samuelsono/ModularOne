using CarTrack.Server.Data;
using Microsoft.AspNetCore.Identity;

namespace CarTrack.Modules.Fleet;

public class DriverService(
    FleetDbContext dbContext,
    ICurrentUserScope currentUserScope,
    IOrgDirectory orgDirectory,
    UserManager<ApplicationUser> userManager) : IDriverService
{
    public async Task<DriversResponse> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var drivers = await dbContext.Drivers
            .AsNoTracking()
            .OrderBy(driver => driver.LastName)
            .ThenBy(driver => driver.FirstName)
            .ToListAsync(cancellationToken);

        var linkedUsers = await orgDirectory.GetLinkedUsersByDriverIdAsync(cancellationToken);
        var names = await ResolveDisplayNamesAsync(drivers, cancellationToken);
        var items = drivers
            .Where(scope.CanAccessDriver)
            .Select(driver => DriverMapper.ToDto(
                driver,
                ToLinkedUserDto(linkedUsers.GetValueOrDefault(driver.Id)),
                names))
            .ToList();

        return new DriversResponse(items, items.Count);
    }

    public async Task<DriverDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var driver = await dbContext.Drivers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (driver is null || !scope.CanAccessDriver(driver))
        {
            return null;
        }

        var linkedUser = await orgDirectory.GetLinkedUserForDriverAsync(driver.Id, cancellationToken);
        var names = await ResolveDisplayNamesAsync([driver], cancellationToken);
        return DriverMapper.ToDto(driver, ToLinkedUserDto(linkedUser), names);
    }

    public async Task<DriverDto> CreateAsync(
        SaveDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureUniqueAsync(null, request, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var driver = new Driver
        {
            Id = Guid.NewGuid(),
            DriverCode = await GenerateDriverCodeAsync(cancellationToken),
            FirstName = string.Empty,
            LastName = string.Empty,
            WorkEmail = string.Empty,
            WorkPhone = string.Empty,
            LicenceNumber = string.Empty,
            IdNumber = string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
        };

        DriverMapper.ApplyRequest(driver, request);
        dbContext.Drivers.Add(driver);
        await dbContext.SaveChangesAsync(cancellationToken);

        return DriverMapper.ToDto(driver);
    }

    public async Task<DriverDto> UpdateAsync(
        Guid id,
        SaveDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        var driver = await dbContext.Drivers
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Driver '{id}' was not found.");

        await EnsureUniqueAsync(id, request, cancellationToken);

        DriverMapper.ApplyRequest(driver, request);
        driver.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var linkedUser = await orgDirectory.GetLinkedUserForDriverAsync(driver.Id, cancellationToken);
        var names = await ResolveDisplayNamesAsync([driver], cancellationToken);
        return DriverMapper.ToDto(driver, ToLinkedUserDto(linkedUser), names);
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveDisplayNamesAsync(
        IEnumerable<Driver> drivers,
        CancellationToken cancellationToken)
    {
        var userIds = drivers
            .SelectMany(driver => new[] { driver.CreatedByUserId, driver.UpdatedByUserId })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (userIds.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return await userManager.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(
                user => user.Id,
                user => user.DisplayName ?? user.UserName ?? "User",
                StringComparer.Ordinal,
                cancellationToken);
    }

    private static LinkedUserDto? ToLinkedUserDto(DriverLinkedUserInfo? info) =>
        info is null
            ? null
            : new LinkedUserDto(info.UserId, info.Username, info.Email, info.DisplayName, info.IsActive);

    private async Task EnsureUniqueAsync(
        Guid? driverId,
        SaveDriverRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.WorkEmail.Trim();
        var licenceNumber = request.LicenceNumber.Trim();

        var emailExists = await dbContext.Drivers.AnyAsync(
            driver => driver.WorkEmail == email && driver.Id != driverId,
            cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException($"A driver with email '{email}' already exists.");
        }

        var licenceExists = await dbContext.Drivers.AnyAsync(
            driver => driver.LicenceNumber == licenceNumber && driver.Id != driverId,
            cancellationToken);

        if (licenceExists)
        {
            throw new InvalidOperationException($"A driver with licence number '{licenceNumber}' already exists.");
        }
    }

    private async Task<string> GenerateDriverCodeAsync(CancellationToken cancellationToken)
    {
        var existingCodes = await dbContext.Drivers
            .AsNoTracking()
            .Select(driver => driver.DriverCode)
            .ToListAsync(cancellationToken);

        var maxNumber = existingCodes
            .Select(code =>
            {
                var digits = new string(code.Where(char.IsDigit).ToArray());
                return int.TryParse(digits, out var number) ? number : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return $"DRV-{maxNumber + 1:D3}";
    }
}
