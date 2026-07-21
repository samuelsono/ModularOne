import type {
  CreateUserFromDriverRequest,
  CreateUserRequest,
  DirectReport,
  ManagerOption,
  PermissionDefinition,
  RoleSummary,
  SetUserActiveRequest,
  SetUserRolesRequest,
  AdminSetPasswordRequest,
  SetInvitePendingRequest,
  UpdateUserRequest,
  UserDetail,
  UserListResponse,
  UserOrg,
} from '@modules/users/types/user';
import type { SecurityAuditLogResponse } from '@modules/users/types/audit';
import { authorizedFetch } from '@platform/api/authService';

const USERS_BASE = '/api/users';
const ROLES_BASE = '/api/roles';
const PERMISSIONS_BASE = '/api/permissions';

export function getUsers(): Promise<UserListResponse> {
  return authorizedFetch<UserListResponse>(USERS_BASE);
}

export function getUser(id: string): Promise<UserDetail> {
  return authorizedFetch<UserDetail>(`${USERS_BASE}/${id}`);
}

export function getManagerOptions(excludeUserId?: string): Promise<ManagerOption[]> {
  const query = excludeUserId ? `?excludeUserId=${encodeURIComponent(excludeUserId)}` : '';
  return authorizedFetch<ManagerOption[]>(`${USERS_BASE}/managers${query}`);
}

export function createUser(request: CreateUserRequest): Promise<UserDetail> {
  return authorizedFetch<UserDetail>(USERS_BASE, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function updateUser(id: string, request: UpdateUserRequest): Promise<UserDetail> {
  return authorizedFetch<UserDetail>(`${USERS_BASE}/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export function setUserRoles(id: string, request: SetUserRolesRequest): Promise<UserDetail> {
  return authorizedFetch<UserDetail>(`${USERS_BASE}/${id}/roles`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export function setUserActive(id: string, request: SetUserActiveRequest): Promise<UserDetail> {
  return authorizedFetch<UserDetail>(`${USERS_BASE}/${id}/active`, {
    method: 'PATCH',
    body: JSON.stringify(request),
  });
}

export function getRoles(): Promise<RoleSummary[]> {
  return authorizedFetch<RoleSummary[]>(ROLES_BASE);
}

export function getPermissions(): Promise<PermissionDefinition[]> {
  return authorizedFetch<PermissionDefinition[]>(PERMISSIONS_BASE);
}

export function getDirectReports(userId: string): Promise<DirectReport[]> {
  return authorizedFetch<DirectReport[]>(`${USERS_BASE}/${userId}/reports`);
}

export function getUserOrg(userId: string): Promise<UserOrg> {
  return authorizedFetch<UserOrg>(`${USERS_BASE}/${userId}/org`);
}

export function createUserFromDriver(
  driverId: string,
  request: CreateUserFromDriverRequest,
): Promise<UserDetail> {
  return authorizedFetch<UserDetail>(`${USERS_BASE}/from-driver/${driverId}`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function sendUserInvite(id: string): Promise<UserDetail> {
  return authorizedFetch<UserDetail>(`${USERS_BASE}/${id}/invite`, {
    method: 'POST',
  });
}

export function adminSetUserPassword(
  id: string,
  request: AdminSetPasswordRequest,
): Promise<UserDetail> {
  return authorizedFetch<UserDetail>(`${USERS_BASE}/${id}/password`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function setUserInvitePending(
  id: string,
  request: SetInvitePendingRequest,
): Promise<UserDetail> {
  return authorizedFetch<UserDetail>(`${USERS_BASE}/${id}/invite-pending`, {
    method: 'PATCH',
    body: JSON.stringify(request),
  });
}

export function getSecurityAuditLog(limit = 100): Promise<SecurityAuditLogResponse> {
  return authorizedFetch(`${USERS_BASE}/audit-log?limit=${limit}`);
}
