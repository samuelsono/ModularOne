namespace CarTrack.Modules.Fleet;

public interface IVehicleService
{
    Task<VehiclesResponse> GetAllAsync(CancellationToken cancellationToken = default);

    Task<VehicleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<VehicleDto> CreateLocalAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default);

    Task<VehicleDto> UpdateLocalAsync(Guid id, UpdateVehicleRequest request, CancellationToken cancellationToken = default);

    Task<VehicleSyncResponse> SyncFromCarTrackAsync(CancellationToken cancellationToken = default);

    Task<VehicleEventsResponse> GetEventsAsync(
        string registration,
        DateTimeOffset start,
        DateTimeOffset end,
        int page,
        int perPage,
        CancellationToken cancellationToken = default);

    Task<VehicleEventsResponse> GetAllEventsInRangeAsync(
        string registration,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken = default);

    Task<TripsResponse> GetTripsAsync(
        string registration,
        DateTimeOffset start,
        DateTimeOffset end,
        int page,
        int perPage,
        CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default);
}
