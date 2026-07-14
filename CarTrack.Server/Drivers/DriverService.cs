using CarTrack.Server.Data;
using CarTrack.Server.Users;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Drivers;

public interface IDriverService
{
    Task<DriversResponse> GetAllAsync(CancellationToken cancellationToken = default);

    Task<DriverDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DriverDto> CreateAsync(SaveDriverRequest request, CancellationToken cancellationToken = default);

    Task<DriverDto> UpdateAsync(Guid id, SaveDriverRequest request, CancellationToken cancellationToken = default);
}

public class DriverService(
    ApplicationDbContext dbContext,
    ICurrentUserScope currentUserScope) : IDriverService
{
    public async Task<DriversResponse> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var drivers = await dbContext.Drivers
            .AsNoTracking()
            .Include(driver => driver.CreatedByUser)
            .Include(driver => driver.UpdatedByUser)
            .OrderBy(driver => driver.LastName)
            .ThenBy(driver => driver.FirstName)
            .ToListAsync(cancellationToken);

        var linkedUsers = await GetLinkedUsersByDriverIdAsync(cancellationToken);
        var items = drivers
            .Where(scope.CanAccessDriver)
            .Select(driver => DriverMapper.ToDto(
                driver,
                linkedUsers.GetValueOrDefault(driver.Id)))
            .ToList();

        return new DriversResponse(items, items.Count);
    }

    public async Task<DriverDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var driver = await dbContext.Drivers
            .AsNoTracking()
            .Include(item => item.CreatedByUser)
            .Include(item => item.UpdatedByUser)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (driver is null || !scope.CanAccessDriver(driver))
        {
            return null;
        }

        var linkedUser = await GetLinkedUserAsync(driver.Id, cancellationToken);
        return DriverMapper.ToDto(driver, linkedUser);
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

        var linkedUser = await GetLinkedUserAsync(driver.Id, cancellationToken);
        return DriverMapper.ToDto(driver, linkedUser);
    }

    private async Task<Dictionary<Guid, LinkedUserDto>> GetLinkedUsersByDriverIdAsync(
        CancellationToken cancellationToken)
    {
        var links = await dbContext.DriverProfileLinks
            .AsNoTracking()
            .Include(link => link.User)
            .ToListAsync(cancellationToken);

        return links.ToDictionary(
            link => link.DriverId,
            link => ToLinkedUserDto(link.User));
    }

    private async Task<LinkedUserDto?> GetLinkedUserAsync(Guid driverId, CancellationToken cancellationToken)
    {
        var link = await dbContext.DriverProfileLinks
            .AsNoTracking()
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.DriverId == driverId, cancellationToken);

        return link is null ? null : ToLinkedUserDto(link.User);
    }

    private static LinkedUserDto ToLinkedUserDto(ApplicationUser user) =>
        new(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.IsActive);

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
