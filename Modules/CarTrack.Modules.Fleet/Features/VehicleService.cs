using System.Text.Json;
using CarTrack.Server.CarTrack;
using CarTrack.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Fleet;

public class VehicleService(
    FleetDbContext dbContext,
    ICarTrackApiClient carTrackApi,
    ICurrentUserScope currentUserScope,
    IFleetLifecycleNotifier fleetLifecycleNotifier,
    UserManager<ApplicationUser> userManager,
    ILogger<VehicleService> logger) : IVehicleService
{
    public const int CacheTtlMinutes = 5;

    public const int HistoryCacheTtlMinutes = 5;

    public const int TripEventsCacheThreshold = 5;

    private const string TripEventsResource = "trip-events";

    private const int TripEventsCachePage = 1;

    private const int TripEventsCachePerPage = 1;

    private static readonly JsonSerializerOptions CachePayloadOptions = new();

    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    public async Task<VehiclesResponse> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var vehicles = await dbContext.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.AssignedDriver)
            .Where(vehicle => !vehicle.IsDeleted)
            .OrderBy(vehicle => vehicle.RegistrationNumber)
            .ToListAsync(cancellationToken);

        var names = await ResolveDisplayNamesAsync(vehicles, cancellationToken);
        var items = vehicles
            .Where(scope.CanAccessVehicle)
            .Select(vehicle => VehicleMapper.ToDto(vehicle, names))
            .ToList();
        var (lastSyncedAt, isCacheFresh) = GetCacheStatus(vehicles);
        return new VehiclesResponse(
            items,
            items.Count,
            lastSyncedAt?.ToString("O"),
            isCacheFresh,
            CacheTtlMinutes);
    }

    public async Task<VehicleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var vehicle = await dbContext.Vehicles
            .AsNoTracking()
            .Include(item => item.AssignedDriver)
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);

        if (vehicle is null || !scope.CanAccessVehicle(vehicle))
        {
            return null;
        }

        var names = await ResolveDisplayNamesAsync([vehicle], cancellationToken);
        return VehicleMapper.ToDto(vehicle, names);
    }

    public async Task<VehicleDto> CreateLocalAsync(
        CreateVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        var registration = request.RegistrationNumber.Trim();
        if (string.IsNullOrWhiteSpace(registration))
        {
            throw new ArgumentException("Registration number is required.", nameof(request));
        }

        var exists = await dbContext.Vehicles
            .AnyAsync(vehicle => vehicle.RegistrationNumber == registration && !vehicle.IsDeleted, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"A vehicle with registration '{registration}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            RegistrationNumber = registration,
            Make = request.Make.Trim(),
            Model = request.Model.Trim(),
            Year = request.Year,
            Colour = request.Colour.Trim(),
            VehicleType = request.VehicleType.Trim(),
            FuelType = string.IsNullOrWhiteSpace(request.FuelType) ? "Diesel" : request.FuelType.Trim(),
            Vin = request.Vin?.Trim() ?? string.Empty,
            EngineNumber = request.EngineNumber?.Trim() ?? string.Empty,
            Tare = request.Tare,
            Gvm = request.Gvm,
            RegisteredOwner = request.RegisteredOwner?.Trim() ?? string.Empty,
            LicenceDiscExpiry = ParseDateOnly(request.LicenceDiscExpiry),
            AssignedDriverId = request.AssignedDriverId,
            IgnitionStatus = "off",
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.Vehicles.Add(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.AssignedDriverId is not null)
        {
            await NotifyAssignmentChangeAsync(
                registration,
                previousDriverId: null,
                newDriverId: request.AssignedDriverId,
                cancellationToken);
        }

        var names = await ResolveDisplayNamesAsync([vehicle], cancellationToken);
        return VehicleMapper.ToDto(vehicle, names);
    }

    public async Task<VehicleDto> UpdateLocalAsync(
        Guid id,
        UpdateVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await dbContext.Vehicles
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Vehicle '{id}' was not found.");

        var registration = request.RegistrationNumber.Trim();
        if (string.IsNullOrWhiteSpace(registration))
        {
            throw new ArgumentException("Registration number is required.", nameof(request));
        }

        var duplicateRegistration = await dbContext.Vehicles
            .AnyAsync(
                item => item.RegistrationNumber == registration && item.Id != id && !item.IsDeleted,
                cancellationToken);

        if (duplicateRegistration)
        {
            throw new InvalidOperationException($"A vehicle with registration '{registration}' already exists.");
        }

        vehicle.RegistrationNumber = registration;
        vehicle.Make = request.Make.Trim();
        vehicle.Model = request.Model.Trim();
        vehicle.Year = request.Year;
        vehicle.Colour = request.Colour.Trim();
        vehicle.VehicleType = request.VehicleType.Trim();
        vehicle.FuelType = string.IsNullOrWhiteSpace(request.FuelType) ? "Diesel" : request.FuelType.Trim();
        vehicle.Vin = request.Vin?.Trim() ?? string.Empty;
        vehicle.EngineNumber = request.EngineNumber?.Trim() ?? string.Empty;
        vehicle.Tare = request.Tare;
        vehicle.Gvm = request.Gvm;
        vehicle.RegisteredOwner = request.RegisteredOwner?.Trim() ?? string.Empty;
        vehicle.LicenceDiscExpiry = ParseDateOnly(request.LicenceDiscExpiry);
        var previousDriverId = vehicle.AssignedDriverId;
        vehicle.AssignedDriverId = request.AssignedDriverId;
        vehicle.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await NotifyAssignmentChangeAsync(
            registration,
            previousDriverId,
            request.AssignedDriverId,
            cancellationToken);

        var names = await ResolveDisplayNamesAsync([vehicle], cancellationToken);
        return VehicleMapper.ToDto(vehicle, names);
    }

    public async Task<VehicleSyncResponse> SyncFromCarTrackAsync(CancellationToken cancellationToken = default)
    {
        var carTrackResponse = await carTrackApi.GetVehiclesAsync(cancellationToken);
        var statusResponse = await carTrackApi.GetVehicleStatusesAsync(cancellationToken);

        var remoteVehicles = carTrackResponse.Data
            .GroupBy(vehicle => vehicle.VehicleId)
            .Select(group => group.Last())
            .ToList();

        var remoteStatuses = statusResponse.Data
            .GroupBy(status => status.VehicleId)
            .Select(group => group.Last())
            .ToDictionary(status => status.VehicleId);

        await SyncLock.WaitAsync(cancellationToken);
        try
        {
            var trackedVehicles = await dbContext.Vehicles
                .Where(vehicle => !vehicle.IsDeleted)
                .ToListAsync(cancellationToken);

            var existingByCarTrackId = trackedVehicles
                .Where(vehicle => vehicle.CarTrackVehicleId != null)
                .ToDictionary(vehicle => vehicle.CarTrackVehicleId!.Value);

            var localOnlyByRegistration = trackedVehicles
                .Where(vehicle => vehicle.CarTrackVehicleId == null)
                .GroupBy(vehicle => vehicle.RegistrationNumber, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            var now = DateTimeOffset.UtcNow;
            var created = 0;
            var updated = 0;
            var autoAssignments = new List<(string Registration, Guid DriverId)>();

            foreach (var remote in remoteVehicles)
            {
                Vehicle vehicle;
                if (existingByCarTrackId.TryGetValue(remote.VehicleId, out vehicle!))
                {
                    ApplyCarTrackData(vehicle, remote, now);
                    updated++;
                }
                else
                {
                    var registration = remote.Registration ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(registration)
                        && localOnlyByRegistration.TryGetValue(registration, out var localVehicle))
                    {
                        localVehicle.CarTrackVehicleId = remote.VehicleId;
                        ApplyCarTrackData(localVehicle, remote, now);
                        vehicle = localVehicle;
                        localOnlyByRegistration.Remove(registration);
                        existingByCarTrackId[remote.VehicleId] = localVehicle;
                        updated++;
                    }
                    else
                    {
                        vehicle = MapFromCarTrack(remote, now);
                        dbContext.Vehicles.Add(vehicle);
                        existingByCarTrackId[remote.VehicleId] = vehicle;
                        created++;
                    }
                }

                if (remoteStatuses.TryGetValue(remote.VehicleId, out var vehicleStatus))
                {
                    ApplyCarTrackStatus(vehicle, vehicleStatus, now);
                    if (await TryAutoAssignDriverAsync(vehicle, cancellationToken) is { } assignedDriverId)
                    {
                        autoAssignments.Add((vehicle.RegistrationNumber, assignedDriverId));
                    }
                }
            }

            foreach (var status in remoteStatuses.Values)
            {
                if (existingByCarTrackId.TryGetValue(status.VehicleId, out var vehicle))
                {
                    ApplyCarTrackStatus(vehicle, status, now);
                    if (await TryAutoAssignDriverAsync(vehicle, cancellationToken) is { } assignedDriverId)
                    {
                        autoAssignments.Add((vehicle.RegistrationNumber, assignedDriverId));
                    }
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var (registration, driverId) in autoAssignments)
            {
                await NotifyAssignmentChangeAsync(
                    registration,
                    previousDriverId: null,
                    newDriverId: driverId,
                    cancellationToken);
            }

            logger.LogInformation(
                "CarTrack vehicle sync completed. Created {Created}, updated {Updated}, statuses {StatusCount}.",
                created,
                updated,
                remoteStatuses.Count);

            var allVehicles = await GetAllAsync(cancellationToken);
            return new VehicleSyncResponse(
                created,
                updated,
                allVehicles.Items,
                allVehicles.Total,
                allVehicles.LastSyncedAt,
                allVehicles.IsCacheFresh,
                allVehicles.CacheTtlMinutes);
        }
        finally
        {
            SyncLock.Release();
        }
    }

    public async Task<VehicleEventsResponse> GetEventsAsync(
        string registration,
        DateTimeOffset start,
        DateTimeOffset end,
        int page,
        int perPage,
        CancellationToken cancellationToken = default)
    {
        var trimmedRegistration = registration.Trim();
        if (string.IsNullOrWhiteSpace(trimmedRegistration))
        {
            throw new ArgumentException("Registration is required.", nameof(registration));
        }

        await EnsureVehicleRegistrationAccessAsync(trimmedRegistration, cancellationToken);

        var (normalizedPage, normalizedPerPage) = NormalizePaging(page, perPage);
        var rangeKey = BuildRangeKey(start, end);

        var cached = await TryGetCachedAsync<VehicleEventDto>(
            "events",
            trimmedRegistration,
            rangeKey,
            normalizedPage,
            normalizedPerPage,
            cancellationToken);

        if (cached is not null)
        {
            return new VehicleEventsResponse(
                trimmedRegistration,
                start.ToString("O"),
                end.ToString("O"),
                cached.Items,
                new PaginationDto(normalizedPage, normalizedPerPage, cached.LastPage, cached.Total),
                FromCache: true);
        }

        var eventsResponse = await carTrackApi.GetVehicleEventsAsync(
            trimmedRegistration,
            start,
            end,
            normalizedPage,
            normalizedPerPage,
            cancellationToken);

        var events = eventsResponse.Data;

        await FillVehicleFromLatestEventAsync(trimmedRegistration, events, cancellationToken);

        var items = events.Select(MapEvent).ToList();
        var total = eventsResponse.Meta?.Total ?? items.Count;
        var lastPage = eventsResponse.Meta?.LastPage ?? 1;

        await StoreCacheAsync(
            "events",
            trimmedRegistration,
            rangeKey,
            normalizedPage,
            normalizedPerPage,
            items,
            total,
            lastPage,
            cancellationToken);

        return new VehicleEventsResponse(
            trimmedRegistration,
            start.ToString("O"),
            end.ToString("O"),
            items,
            new PaginationDto(normalizedPage, normalizedPerPage, lastPage, total),
            FromCache: false);
    }

    public async Task<VehicleEventsResponse> GetAllEventsInRangeAsync(
        string registration,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken = default)
    {
        var trimmedRegistration = registration.Trim();
        if (string.IsNullOrWhiteSpace(trimmedRegistration))
        {
            throw new ArgumentException("Registration is required.", nameof(registration));
        }

        await EnsureVehicleRegistrationAccessAsync(trimmedRegistration, cancellationToken);

        var rangeKey = BuildRangeKey(start, end);

        var cachedTripEvents = await TryGetTripEventsCacheAsync(
            trimmedRegistration,
            rangeKey,
            cancellationToken);

        if (cachedTripEvents is not null)
        {
            return new VehicleEventsResponse(
                trimmedRegistration,
                start.ToString("O"),
                end.ToString("O"),
                cachedTripEvents.Items,
                new PaginationDto(
                    1,
                    cachedTripEvents.Items.Count,
                    1,
                    cachedTripEvents.Total > 0 ? cachedTripEvents.Total : cachedTripEvents.Items.Count),
                FromCache: true);
        }

        const int perPage = 100;
        var page = 1;
        var allItems = new List<VehicleEventDto>();
        var lastPage = 1;
        var total = 0;
        var servedFromPageCache = true;

        do
        {
            var response = await GetEventsAsync(
                trimmedRegistration,
                start,
                end,
                page,
                perPage,
                cancellationToken);
            allItems.AddRange(response.Items);
            lastPage = response.Pagination.LastPage;
            total = response.Pagination.Total;
            servedFromPageCache = servedFromPageCache && response.FromCache;
            page++;
        }
        while (page <= lastPage);

        var ordered = allItems
            .OrderBy(static eventItem => eventItem.EventTs ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        await StoreTripEventsCacheAsync(
            trimmedRegistration,
            rangeKey,
            ordered,
            total > 0 ? total : ordered.Count,
            cancellationToken);

        return new VehicleEventsResponse(
            trimmedRegistration,
            start.ToString("O"),
            end.ToString("O"),
            ordered,
            new PaginationDto(1, ordered.Count, 1, total > 0 ? total : ordered.Count),
            FromCache: servedFromPageCache);
    }

    public async Task<TripsResponse> GetTripsAsync(
        string registration,
        DateTimeOffset start,
        DateTimeOffset end,
        int page,
        int perPage,
        CancellationToken cancellationToken = default)
    {
        var trimmedRegistration = registration.Trim();
        if (string.IsNullOrWhiteSpace(trimmedRegistration))
        {
            throw new ArgumentException("Registration is required.", nameof(registration));
        }

        await EnsureVehicleRegistrationAccessAsync(trimmedRegistration, cancellationToken);

        var (normalizedPage, normalizedPerPage) = NormalizePaging(page, perPage);
        var rangeKey = BuildRangeKey(start, end);

        var cached = await TryGetCachedAsync<TripDto>(
            "trips",
            trimmedRegistration,
            rangeKey,
            normalizedPage,
            normalizedPerPage,
            cancellationToken);

        if (cached is not null)
        {
            return new TripsResponse(
                trimmedRegistration,
                start.ToString("O"),
                end.ToString("O"),
                cached.Items,
                new PaginationDto(normalizedPage, normalizedPerPage, cached.LastPage, cached.Total),
                FromCache: true);
        }

        var tripsResponse = await carTrackApi.GetTripsAsync(
            trimmedRegistration,
            start,
            end,
            normalizedPage,
            normalizedPerPage,
            cancellationToken);

        var items = tripsResponse.Data.Select(MapTrip).ToList();
        var total = tripsResponse.Meta?.Total ?? items.Count;
        var lastPage = tripsResponse.Meta?.LastPage ?? 1;

        await StoreCacheAsync(
            "trips",
            trimmedRegistration,
            rangeKey,
            normalizedPage,
            normalizedPerPage,
            items,
            total,
            lastPage,
            cancellationToken);

        return new TripsResponse(
            trimmedRegistration,
            start.ToString("O"),
            end.ToString("O"),
            items,
            new PaginationDto(normalizedPage, normalizedPerPage, lastPage, total),
            FromCache: false);
    }

    private async Task FillVehicleFromLatestEventAsync(
        string registration,
        IReadOnlyList<CarTrackVehicleEvent> events,
        CancellationToken cancellationToken)
    {
        var latestEvent = events
            .Select(evt => new { Event = evt, Timestamp = ParseDateTimeOffset(evt.EventTs) })
            .Where(item => item.Timestamp != null)
            .OrderByDescending(item => item.Timestamp)
            .Select(item => item.Event)
            .FirstOrDefault()
            ?? events.LastOrDefault();

        if (latestEvent is null)
        {
            return;
        }

        var vehicle = await dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.RegistrationNumber == registration, cancellationToken);

        if (vehicle is not null)
        {
            FillMissingFromEvent(vehicle, latestEvent);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static (int Page, int PerPage) NormalizePaging(int page, int perPage)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPerPage = perPage switch
        {
            < 1 => 15,
            > 100 => 100,
            _ => perPage,
        };
        return (normalizedPage, normalizedPerPage);
    }

    private static string BuildRangeKey(DateTimeOffset start, DateTimeOffset end) =>
        $"{start.UtcDateTime:yyyyMMddHHmmss}-{end.UtcDateTime:yyyyMMddHHmmss}";

    private sealed record CachedPage<T>(IReadOnlyList<T> Items, int Total, int LastPage);

    private async Task<CachedPage<T>?> TryGetCachedAsync<T>(
        string resource,
        string registration,
        string rangeKey,
        int page,
        int perPage,
        CancellationToken cancellationToken)
    {
        var entry = await dbContext.CarTrackPageCache
            .AsNoTracking()
            .FirstOrDefaultAsync(
                cache => cache.Resource == resource
                    && cache.Registration == registration
                    && cache.RangeKey == rangeKey
                    && cache.Page == page
                    && cache.PerPage == perPage,
                cancellationToken);

        if (entry is null)
        {
            return null;
        }

        var isFresh = DateTimeOffset.UtcNow - entry.FetchedAt < TimeSpan.FromMinutes(HistoryCacheTtlMinutes);
        if (!isFresh)
        {
            return null;
        }

        var items = JsonSerializer.Deserialize<List<T>>(entry.PayloadJson, CachePayloadOptions) ?? [];
        return new CachedPage<T>(items, entry.Total, entry.LastPage);
    }

    private async Task StoreCacheAsync<T>(
        string resource,
        string registration,
        string rangeKey,
        int page,
        int perPage,
        IReadOnlyList<T> items,
        int total,
        int lastPage,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(items, CachePayloadOptions);
        var now = DateTimeOffset.UtcNow;

        var entry = await dbContext.CarTrackPageCache
            .FirstOrDefaultAsync(
                cache => cache.Resource == resource
                    && cache.Registration == registration
                    && cache.RangeKey == rangeKey
                    && cache.Page == page
                    && cache.PerPage == perPage,
                cancellationToken);

        if (entry is null)
        {
            dbContext.CarTrackPageCache.Add(new CarTrackPageCache
            {
                Id = Guid.NewGuid(),
                Resource = resource,
                Registration = registration,
                RangeKey = rangeKey,
                Page = page,
                PerPage = perPage,
                PayloadJson = payload,
                Total = total,
                LastPage = lastPage,
                CurrentPage = page,
                FetchedAt = now,
            });
        }
        else
        {
            entry.PayloadJson = payload;
            entry.Total = total;
            entry.LastPage = lastPage;
            entry.CurrentPage = page;
            entry.FetchedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<CachedPage<VehicleEventDto>?> TryGetTripEventsCacheAsync(
        string registration,
        string rangeKey,
        CancellationToken cancellationToken)
    {
        var entry = await dbContext.CarTrackPageCache
            .AsNoTracking()
            .FirstOrDefaultAsync(
                cache => cache.Resource == TripEventsResource
                    && cache.Registration == registration
                    && cache.RangeKey == rangeKey
                    && cache.Page == TripEventsCachePage
                    && cache.PerPage == TripEventsCachePerPage,
                cancellationToken);

        if (entry is null)
        {
            return null;
        }

        var items = JsonSerializer.Deserialize<List<VehicleEventDto>>(entry.PayloadJson, CachePayloadOptions) ?? [];

        if (items.Count > TripEventsCacheThreshold)
        {
            return new CachedPage<VehicleEventDto>(items, entry.Total, entry.LastPage);
        }

        return null;
    }

    private Task StoreTripEventsCacheAsync(
        string registration,
        string rangeKey,
        IReadOnlyList<VehicleEventDto> items,
        int total,
        CancellationToken cancellationToken) =>
        StoreCacheAsync(
            TripEventsResource,
            registration,
            rangeKey,
            TripEventsCachePage,
            TripEventsCachePerPage,
            items,
            total,
            1,
            cancellationToken);

    private static TripDto MapTrip(CarTrackTrip trip) => new(
        TripId: trip.TripId,
        StartTimestamp: ParseDateTimeOffset(trip.StartTimestamp)?.ToString("O"),
        EndTimestamp: ParseDateTimeOffset(trip.EndTimestamp)?.ToString("O"),
        TripDuration: trip.TripDuration,
        TripDurationSeconds: trip.TripDurationSeconds,
        StartLocation: trip.StartLocation,
        StartCoordinates: trip.StartCoordinates is null
            ? null
            : new TripCoordinatesDto(trip.StartCoordinates.Latitude, trip.StartCoordinates.Longitude),
        EndLocation: trip.EndLocation,
        EndCoordinates: trip.EndCoordinates is null
            ? null
            : new TripCoordinatesDto(trip.EndCoordinates.Latitude, trip.EndCoordinates.Longitude),
        StartOdometer: trip.StartOdometer,
        EndOdometer: trip.EndOdometer,
        TripDistance: trip.TripDistance,
        StartGeofenceName: trip.StartGeofenceName,
        EndGeofenceName: trip.EndGeofenceName,
        ThresholdsSpeedingEvents: trip.ThresholdsSpeedingEvents,
        RoadSpeedingEvents: trip.RoadSpeedingEvents,
        MaxSpeed: trip.MaxSpeed,
        HarshBrakingEvents: trip.HarshBrakingEvents,
        HarshCorneringEvents: trip.HarshCorneringEvents,
        HarshAccelerationEvents: trip.HarshAccelerationEvents,
        IdleTime: trip.IdleTime,
        IdleTimeSeconds: trip.IdleTimeSeconds,
        DriverId: trip.DriverId,
        DriverName: trip.DriverName,
        TripTitle: trip.TripTitle,
        TripType: trip.TripType,
        IsPrivate: trip.IsPrivate);

    private static VehicleEventDto MapEvent(CarTrackVehicleEvent evt) => new(
        EventId: evt.EventId,
        EventDescription: evt.EventDescription,
        TerminalEventTypeId: evt.TerminalEventTypeId,
        EventTs: ParseDateTimeOffset(evt.EventTs)?.ToString("O"),
        ReceivedTs: ParseDateTimeOffset(evt.ReceivedTs)?.ToString("O"),
        Latitude: evt.Latitude,
        Longitude: evt.Longitude,
        Altitude: evt.Altitude,
        Odometer: evt.Odometer,
        Bearing: evt.Bearing,
        Speed: evt.Speed,
        RoadSpeed: evt.RoadSpeed,
        Rpm: evt.Rpm,
        Ignition: evt.Ignition,
        RoadSpeeding: evt.RoadSpeeding,
        PositionDescription: evt.PositionDescription,
        Vext: evt.Vext,
        BatteryPercentageLeft: evt.BatteryPercentageLeft,
        WaterTemp: evt.WaterTemp,
        OilTemp: evt.OilTemp,
        GpsFixType: evt.GpsFixType,
        DriverId: evt.DriverId);

    // Only populates fields that are currently missing (null) on the vehicle,
    // using the latest event as a fallback source of telemetry data.
    private static void FillMissingFromEvent(Vehicle vehicle, CarTrackVehicleEvent evt)
    {
        var eventTs = ParseDateTimeOffset(evt.EventTs);

        vehicle.EventTs ??= eventTs;
        vehicle.Bearing ??= evt.Bearing;
        vehicle.Speed ??= evt.Speed;
        vehicle.RoadSpeed ??= evt.RoadSpeed;
        vehicle.Ignition ??= evt.Ignition;
        vehicle.Odometer ??= evt.Odometer;
        vehicle.Altitude ??= evt.Altitude;
        vehicle.Rpm ??= evt.Rpm;
        vehicle.Latitude ??= evt.Latitude;
        vehicle.Longitude ??= evt.Longitude;
        vehicle.LocationUpdated ??= eventTs;
        vehicle.GpsFixType ??= evt.GpsFixType;
        vehicle.LvBatteryVoltage ??= evt.Vext;
        vehicle.UnitClock ??= evt.Clock;
        vehicle.WaterTemp ??= evt.WaterTemp;
        vehicle.OilTemp ??= evt.OilTemp;

        if (string.IsNullOrWhiteSpace(vehicle.PositionDescription))
        {
            vehicle.PositionDescription = evt.PositionDescription;
        }

        if (string.IsNullOrWhiteSpace(vehicle.DriverId))
        {
            vehicle.DriverId = evt.DriverId;
        }

        if (string.IsNullOrWhiteSpace(vehicle.Vin))
        {
            vehicle.Vin = evt.ChassisNumber ?? vehicle.Vin;
        }

        vehicle.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public async Task<int> DeleteAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return 0;
        }

        var distinctIds = ids.Distinct().ToList();
        var vehicles = await dbContext.Vehicles
            .Where(vehicle => distinctIds.Contains(vehicle.Id) && !vehicle.IsDeleted)
            .ToListAsync(cancellationToken);

        if (vehicles.Count == 0)
        {
            return 0;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var vehicle in vehicles)
        {
            vehicle.IsDeleted = true;
            vehicle.DeletedAt = now;
            vehicle.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return vehicles.Count;
    }

    private static (DateTimeOffset? LastSyncedAt, bool IsCacheFresh) GetCacheStatus(IReadOnlyList<Vehicle> vehicles)
    {
        var syncedAtValues = vehicles
            .SelectMany(vehicle => new[] { vehicle.CarTrackSyncedAt, vehicle.StatusSyncedAt })
            .Where(timestamp => timestamp != null)
            .Select(timestamp => timestamp!.Value)
            .ToList();

        if (syncedAtValues.Count == 0)
        {
            return (null, false);
        }

        var lastSyncedAt = syncedAtValues.Max();
        var isFresh = DateTimeOffset.UtcNow - lastSyncedAt < TimeSpan.FromMinutes(CacheTtlMinutes);
        return (lastSyncedAt, isFresh);
    }

    private static Vehicle MapFromCarTrack(CarTrackVehicle remote, DateTimeOffset syncedAt)
    {
        var fuelType = remote.Sensors?.ElectricBattery == true ? "Electric" : "Diesel";
        var owner = remote.ClientVehicleDescription
            ?? remote.VehicleName
            ?? string.Empty;

        return new Vehicle
        {
            Id = Guid.NewGuid(),
            CarTrackVehicleId = remote.VehicleId,
            RegistrationNumber = remote.Registration ?? string.Empty,
            Make = remote.Manufacturer ?? string.Empty,
            Model = remote.Model ?? string.Empty,
            Year = remote.ModelYear ?? 0,
            Vin = remote.ChassisNumber ?? string.Empty,
            EngineNumber = remote.TerminalSerial ?? string.Empty,
            Colour = remote.Colour ?? string.Empty,
            VehicleType = string.IsNullOrWhiteSpace(remote.VehicleType) ? "Unknown" : remote.VehicleType,
            FuelType = fuelType,
            RegisteredOwner = owner,
            LicenceDiscExpiry = ParseDateOnly(remote.LicenceExpiryDate),
            IgnitionStatus = "off",
            CarTrackSyncedAt = syncedAt,
            CreatedAt = syncedAt,
            UpdatedAt = syncedAt,
        };
    }

    private static void ApplyCarTrackData(Vehicle vehicle, CarTrackVehicle remote, DateTimeOffset syncedAt)
    {
        var fuelType = remote.Sensors?.ElectricBattery == true ? "Electric" : "Diesel";
        var owner = remote.ClientVehicleDescription
            ?? remote.VehicleName
            ?? string.Empty;

        vehicle.RegistrationNumber = remote.Registration ?? vehicle.RegistrationNumber;
        vehicle.Make = remote.Manufacturer ?? vehicle.Make;
        vehicle.Model = remote.Model ?? vehicle.Model;
        vehicle.Year = remote.ModelYear ?? vehicle.Year;
        vehicle.Vin = remote.ChassisNumber ?? vehicle.Vin;
        vehicle.EngineNumber = remote.TerminalSerial ?? vehicle.EngineNumber;
        vehicle.Colour = remote.Colour ?? vehicle.Colour;
        vehicle.VehicleType = string.IsNullOrWhiteSpace(remote.VehicleType) ? vehicle.VehicleType : remote.VehicleType;
        vehicle.FuelType = fuelType;
        vehicle.RegisteredOwner = string.IsNullOrWhiteSpace(owner) ? vehicle.RegisteredOwner : owner;
        vehicle.LicenceDiscExpiry = ParseDateOnly(remote.LicenceExpiryDate) ?? vehicle.LicenceDiscExpiry;
        vehicle.CarTrackSyncedAt = syncedAt;
        vehicle.UpdatedAt = syncedAt;
    }

    private static void ApplyCarTrackStatus(Vehicle vehicle, CarTrackVehicleStatus status, DateTimeOffset syncedAt)
    {
        vehicle.EngineType = status.EngineType ?? vehicle.EngineType;
        vehicle.EventTs = ParseDateTimeOffset(status.EventTs) ?? vehicle.EventTs;
        vehicle.Bearing = status.Bearing ?? vehicle.Bearing;
        vehicle.Speed = status.Speed ?? vehicle.Speed;
        vehicle.RoadSpeed = status.RoadSpeed ?? vehicle.RoadSpeed;
        vehicle.Ignition = status.Ignition ?? vehicle.Ignition;
        vehicle.Idling = status.Idling ?? vehicle.Idling;
        vehicle.Odometer = status.Odometer ?? vehicle.Odometer;
        vehicle.Altitude = status.Altitude ?? vehicle.Altitude;
        vehicle.Rpm = status.Rpm ?? vehicle.Rpm;
        vehicle.TcuBatteryPercentage = status.TcuBatteryPercentage ?? vehicle.TcuBatteryPercentage;
        vehicle.LvBatteryVoltage = status.Vext ?? vehicle.LvBatteryVoltage;
        vehicle.UnitClock = status.Clock ?? vehicle.UnitClock;
        vehicle.WaterTemp = status.Temp1 ?? vehicle.WaterTemp;
        vehicle.OilTemp = status.Temp2 ?? vehicle.OilTemp;
        vehicle.CentralLockingStatus = status.CentralLockingStatus ?? vehicle.CentralLockingStatus;
        vehicle.IgnitionStatus = MapIgnitionStatus(status);

        if (!string.IsNullOrWhiteSpace(status.ChassisNumber))
        {
            vehicle.Vin = status.ChassisNumber;
        }

        if (!string.IsNullOrWhiteSpace(status.EngineType))
        {
            vehicle.FuelType = status.EngineType;
        }

        if (status.Location is not null)
        {
            vehicle.Latitude = status.Location.Latitude ?? vehicle.Latitude;
            vehicle.Longitude = status.Location.Longitude ?? vehicle.Longitude;
            vehicle.PositionDescription = status.Location.PositionDescription ?? vehicle.PositionDescription;
            vehicle.LocationUpdated = ParseDateTimeOffset(status.Location.Updated) ?? vehicle.LocationUpdated;
            vehicle.GpsFixType = status.Location.GpsFixType ?? vehicle.GpsFixType;
            vehicle.GeofenceIdsJson = SerializeGeofenceIds(status.Location.GeofenceIds) ?? vehicle.GeofenceIdsJson;
        }

        if (status.Fuel is not null)
        {
            vehicle.FuelLevel = status.Fuel.Level ?? vehicle.FuelLevel;
            vehicle.FuelPercentageLeft = status.Fuel.PercentageLeft ?? vehicle.FuelPercentageLeft;
            vehicle.FuelTotalConsumed = status.Fuel.TotalConsumed ?? vehicle.FuelTotalConsumed;
            vehicle.FuelUpdated = ParseDateTimeOffset(status.Fuel.Updated) ?? vehicle.FuelUpdated;
        }

        if (status.Electric is not null)
        {
            vehicle.ElectricBatteryPercentage = status.Electric.BatteryPercentageLeft ?? vehicle.ElectricBatteryPercentage;
            vehicle.ElectricChargingStatus = status.Electric.ChargingStatus ?? vehicle.ElectricChargingStatus;
            vehicle.ElectricBatteryTs = ParseDateTimeOffset(status.Electric.BatteryTs) ?? vehicle.ElectricBatteryTs;
            vehicle.ElectricChargingStatusTs = ParseDateTimeOffset(status.Electric.ChargingStatusTs) ?? vehicle.ElectricChargingStatusTs;
        }

        if (status.Driver is not null)
        {
            vehicle.DriverId = status.Driver.DriverId ?? vehicle.DriverId;
            vehicle.DriverFirstName = status.Driver.FirstName ?? vehicle.DriverFirstName;
            vehicle.DriverLastName = status.Driver.LastName ?? vehicle.DriverLastName;
            vehicle.DriverPhone = status.Driver.PhoneNumber ?? vehicle.DriverPhone;
            vehicle.DriverLicenseNumber = status.Driver.LicenseNumber ?? vehicle.DriverLicenseNumber;
        }

        vehicle.StatusSyncedAt = syncedAt;
        vehicle.UpdatedAt = syncedAt;
    }

    private static string MapIgnitionStatus(CarTrackVehicleStatus status)
    {
        if (status.Idling == true)
        {
            return "idling";
        }

        if (status.Ignition == true && status.Speed is > 0)
        {
            return "moving";
        }

        if (status.Ignition == true)
        {
            return "idling";
        }

        return "off";
    }

    private static DateOnly? ParseDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateOnly.TryParse(value, out var date) ? date : null;
    }

    private static DateTimeOffset? ParseDateTimeOffset(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Normalize to UTC: PostgreSQL 'timestamp with time zone' only accepts
        // DateTimeOffset values with a zero (UTC) offset. Strings without offset
        // info would otherwise be interpreted using the server's local offset.
        return DateTimeOffset.TryParse(value, out var timestamp)
            ? timestamp.ToUniversalTime()
            : null;
    }

    private static string? SerializeGeofenceIds(IReadOnlyList<string>? geofenceIds)
    {
        if (geofenceIds is null || geofenceIds.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(geofenceIds);
    }

    private async Task EnsureVehicleRegistrationAccessAsync(
        string registration,
        CancellationToken cancellationToken)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        if (scope.BypassRowLevelSecurity || scope.HasFullFleetAccess)
        {
            return;
        }

        var vehicle = await dbContext.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.RegistrationNumber == registration && !item.IsDeleted,
                cancellationToken);

        if (vehicle is null || !scope.CanAccessVehicle(vehicle))
        {
            throw new UnauthorizedAccessException("You do not have access to this vehicle.");
        }
    }

    private async Task<Guid?> TryAutoAssignDriverAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        if (vehicle.AssignedDriverId is not null)
        {
            return null;
        }

        Driver? matchedDriver = null;

        if (!string.IsNullOrWhiteSpace(vehicle.DriverLicenseNumber))
        {
            matchedDriver = await dbContext.Drivers
                .FirstOrDefaultAsync(
                    driver => driver.LicenceNumber == vehicle.DriverLicenseNumber,
                    cancellationToken);
        }

        if (matchedDriver is null && !string.IsNullOrWhiteSpace(vehicle.DriverId))
        {
            matchedDriver = await dbContext.Drivers.FirstOrDefaultAsync(
                driver =>
                    driver.CarTrackDriverId == vehicle.DriverId
                    || driver.DriverCode == vehicle.DriverId,
                cancellationToken);
        }

        if (matchedDriver is null)
        {
            return null;
        }

        vehicle.AssignedDriverId = matchedDriver.Id;

        if (string.IsNullOrWhiteSpace(matchedDriver.CarTrackDriverId)
            && !string.IsNullOrWhiteSpace(vehicle.DriverId))
        {
            matchedDriver.CarTrackDriverId = vehicle.DriverId;
            matchedDriver.UpdatedAt = DateTimeOffset.UtcNow;
        }

        return matchedDriver.Id;
    }

    private async Task NotifyAssignmentChangeAsync(
        string registration,
        Guid? previousDriverId,
        Guid? newDriverId,
        CancellationToken cancellationToken)
    {
        if (previousDriverId == newDriverId)
        {
            return;
        }

        if (previousDriverId is not null)
        {
            var driverName = await GetDriverDisplayNameAsync(previousDriverId.Value, cancellationToken);
            await TryNotifyAsync(
                () => fleetLifecycleNotifier.NotifyDriverUnassignedAsync(
                    driverName,
                    registration,
                    cancellationToken),
                logger);
        }

        if (newDriverId is not null)
        {
            var driverName = await GetDriverDisplayNameAsync(newDriverId.Value, cancellationToken);
            await TryNotifyAsync(
                () => fleetLifecycleNotifier.NotifyDriverAssignedAsync(
                    driverName,
                    registration,
                    cancellationToken),
                logger);
        }
    }

    private async Task<string> GetDriverDisplayNameAsync(Guid driverId, CancellationToken cancellationToken)
    {
        var driver = await dbContext.Drivers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == driverId, cancellationToken);

        return driver is null
            ? "Driver"
            : $"{driver.FirstName} {driver.LastName}".Trim();
    }

    private static async Task TryNotifyAsync(Func<Task> action, ILogger logger)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to dispatch notification.");
        }
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveDisplayNamesAsync(
        IEnumerable<Vehicle> vehicles,
        CancellationToken cancellationToken)
    {
        var userIds = vehicles
            .SelectMany(vehicle => new[] { vehicle.CreatedByUserId, vehicle.UpdatedByUserId })
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
}
