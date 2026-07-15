import type { Driver, DriversResponse, SaveDriverRequest } from '@modules/fleet/types/driver';
import { authorizedFetch } from '@platform/api/authService';

export async function getDrivers(): Promise<DriversResponse> {
  return authorizedFetch<DriversResponse>('/api/drivers');
}

export async function getDriver(id: string): Promise<Driver> {
  return authorizedFetch<Driver>(`/api/drivers/${id}`);
}

export async function createDriver(request: SaveDriverRequest): Promise<Driver> {
  return authorizedFetch<Driver>('/api/drivers', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function updateDriver(id: string, request: SaveDriverRequest): Promise<Driver> {
  return authorizedFetch<Driver>(`/api/drivers/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}
