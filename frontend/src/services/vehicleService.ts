import type {
  CreateVehicleRequest,
  TripsResponse,
  UpdateVehicleRequest,
  Vehicle,
  VehicleEventsResponse,
  VehicleSyncResponse,
  VehiclesResponse,
} from '../types/vehicle';
import { authorizedFetch } from './authService';

export async function getVehicles(): Promise<VehiclesResponse> {
  return authorizedFetch<VehiclesResponse>('/api/vehicles');
}

export async function getVehicle(id: string): Promise<Vehicle> {
  return authorizedFetch<Vehicle>(`/api/vehicles/${encodeURIComponent(id)}`);
}

export async function createVehicle(request: CreateVehicleRequest): Promise<Vehicle> {
  return authorizedFetch<Vehicle>('/api/vehicles', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function updateVehicle(id: string, request: UpdateVehicleRequest): Promise<Vehicle> {
  return authorizedFetch<Vehicle>(`/api/vehicles/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

function toCarTrackTimestamp(date: Date): string {
  const pad = (value: number) => `${value}`.padStart(2, '0');
  return (
    `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` +
    ` ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
  );
}

interface HistoryQueryOptions {
  start?: Date;
  end?: Date;
  page?: number;
  perPage?: number;
}

function buildHistoryQuery(options: HistoryQueryOptions): string {
  const params = new URLSearchParams();
  if (options.start) {
    params.set('start', toCarTrackTimestamp(options.start));
  }
  if (options.end) {
    params.set('end', toCarTrackTimestamp(options.end));
  }
  if (options.page != null) {
    params.set('page', `${options.page}`);
  }
  if (options.perPage != null) {
    params.set('perPage', `${options.perPage}`);
  }

  const query = params.toString();
  return query ? `?${query}` : '';
}

export async function getVehicleEvents(
  registration: string,
  options: HistoryQueryOptions = {},
): Promise<VehicleEventsResponse> {
  return authorizedFetch<VehicleEventsResponse>(
    `/api/vehicles/${encodeURIComponent(registration)}/events${buildHistoryQuery(options)}`,
  );
}

export async function getVehicleTrips(
  registration: string,
  options: HistoryQueryOptions = {},
): Promise<TripsResponse> {
  return authorizedFetch<TripsResponse>(
    `/api/vehicles/${encodeURIComponent(registration)}/trips${buildHistoryQuery(options)}`,
  );
}

export async function getTripEvents(
  registration: string,
  trip: { startTimestamp: string | null; endTimestamp: string | null },
): Promise<VehicleEventsResponse> {
  if (!trip.startTimestamp || !trip.endTimestamp) {
    throw new Error('Trip start and end timestamps are required.');
  }

  const start = new Date(trip.startTimestamp);
  const end = new Date(trip.endTimestamp);
  const params = new URLSearchParams({
    start: toCarTrackTimestamp(start),
    end: toCarTrackTimestamp(end),
  });

  return authorizedFetch<VehicleEventsResponse>(
    `/api/vehicles/${encodeURIComponent(registration)}/trips/events?${params.toString()}`,
  );
}

export async function syncVehiclesFromCarTrack(): Promise<VehicleSyncResponse> {
  return authorizedFetch<VehicleSyncResponse>('/api/vehicles/sync', {
    method: 'POST',
  });
}

export async function deleteVehicles(ids: string[]): Promise<{ deletedCount: number }> {
  return authorizedFetch<{ deletedCount: number }>('/api/vehicles/delete', {
    method: 'POST',
    body: JSON.stringify({ ids }),
  });
}

export function formatLastSyncedAt(lastSyncedAt: string | null): string | null {
  if (!lastSyncedAt) {
    return null;
  }

  const syncedDate = new Date(lastSyncedAt);
  const minutesAgo = Math.max(0, Math.round((Date.now() - syncedDate.getTime()) / 60000));

  if (minutesAgo < 1) {
    return 'just now';
  }

  if (minutesAgo === 1) {
    return '1 minute ago';
  }

  return `${minutesAgo} minutes ago`;
}

export function getVehicleDisplayName(vehicle: Vehicle): string {
  const label = `${vehicle.make} ${vehicle.model}`.trim();
  return label || vehicle.registrationNumber;
}

export function mapIgnitionToPresence(
  status: Vehicle['ignitionStatus'],
): 'available' | 'busy' | 'away' | 'offline' {
  if (status === 'moving') {
    return 'busy';
  }

  if (status === 'idling') {
    return 'away';
  }

  return 'offline';
}
