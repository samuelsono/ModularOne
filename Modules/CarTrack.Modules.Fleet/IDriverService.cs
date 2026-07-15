namespace CarTrack.Modules.Fleet;

public interface IDriverService
{
    Task<DriversResponse> GetAllAsync(CancellationToken cancellationToken = default);

    Task<DriverDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DriverDto> CreateAsync(SaveDriverRequest request, CancellationToken cancellationToken = default);

    Task<DriverDto> UpdateAsync(Guid id, SaveDriverRequest request, CancellationToken cancellationToken = default);
}
