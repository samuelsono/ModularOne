import { authorizedFetch } from '@platform/api/authService';

/**
 * Cross-cutting directory lookups so modules do not import each other.
 * Calls the same REST endpoints as fleet/users/notifications services.
 */

export interface DirectoryDriver {
  id: string;
  driverId: string;
  firstName: string;
  lastName: string;
  name: string;
  email: string | null;
  linkedUserId: string | null;
}

export interface DirectoryDriversResponse {
  items: DirectoryDriver[];
  total: number;
}

export interface DirectoryManagerOption {
  id: string;
  displayName: string | null;
  email: string;
  username: string;
}

export interface DirectoryAuthUserLookup {
  id: string;
  displayName: string | null;
  email: string;
  username: string;
}

export interface CreateUserFromDriverRequest {
  email?: string | null;
  username?: string | null;
  password?: string | null;
  sendInvite?: boolean;
  managerUserId?: string | null;
  roles?: string[];
}

export function getDrivers(): Promise<DirectoryDriversResponse> {
  return authorizedFetch<DirectoryDriversResponse>('/api/drivers');
}

export function getManagerOptions(excludeUserId?: string): Promise<DirectoryManagerOption[]> {
  const query = excludeUserId ? `?excludeUserId=${encodeURIComponent(excludeUserId)}` : '';
  return authorizedFetch<DirectoryManagerOption[]>(`/api/users/managers${query}`);
}

export function createUserFromDriver(
  driverId: string,
  request: CreateUserFromDriverRequest,
): Promise<unknown> {
  return authorizedFetch(`/api/users/from-driver/${driverId}`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function getAuthUsersForBroadcast(): Promise<DirectoryAuthUserLookup[]> {
  return authorizedFetch<DirectoryAuthUserLookup[]>('/api/auth/users');
}
