export interface StaffProfile {
  employeeNumber: string | null;
  jobTitle: string | null;
  department: string | null;
  branch: string | null;
  gender: 'Male' | 'Female' | 'Unspecified';
  employmentStatus: string;
  workStartDate: string | null;
  managerUserId: string | null;
  managerDisplayName: string | null;
  companyId: string | null;
  companyName: string | null;
  departmentId: string | null;
  departmentName: string | null;
  positionId: string | null;
  positionName: string | null;
}

export interface DriverLink {
  driverId: string;
  driverCode: string;
  driverName: string;
}

export interface UserListItem {
  id: string;
  username: string;
  email: string;
  displayName: string | null;
  roles: string[];
  isActive: boolean;
  lastLoginAt: string | null;
  invitePendingAt: string | null;
  managerDisplayName: string | null;
  linkedDriverId: string | null;
  companyName: string | null;
  departmentName: string | null;
  positionName: string | null;
  createdAt?: string | null;
  createdByUserId?: string | null;
  createdByDisplayName?: string | null;
  updatedAt?: string | null;
  updatedByUserId?: string | null;
  updatedByDisplayName?: string | null;
}

export interface UserDetail {
  id: string;
  username: string;
  email: string;
  displayName: string | null;
  firstName: string | null;
  lastName: string | null;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
  invitePendingAt: string | null;
  roles: string[];
  staffProfile: StaffProfile | null;
  driverLink: DriverLink | null;
}

export interface UserListResponse {
  items: UserListItem[];
}

export interface ManagerOption {
  id: string;
  displayName: string;
  email: string;
}

export interface RoleSummary {
  name: string;
  permissionKeys: string[];
}

export interface PermissionDefinition {
  id: string;
  key: string;
  moduleSlug: string;
  submoduleSlug: string;
  action: string;
  description: string | null;
}

export interface StaffProfileRequest {
  employeeNumber?: string | null;
  jobTitle?: string | null;
  department?: string | null;
  branch?: string | null;
  gender?: 'Male' | 'Female' | 'Unspecified';
  managerUserId?: string | null;
  companyId?: string | null;
  departmentId?: string | null;
  positionId?: string | null;
}

export interface CreateUserRequest {
  username: string;
  email: string;
  password?: string | null;
  displayName?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  roles: string[];
  isActive?: boolean;
  sendInvite?: boolean;
  staff?: StaffProfileRequest | null;
  driverId?: string | null;
}

export interface UpdateUserRequest {
  email: string;
  displayName?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  staff?: StaffProfileRequest | null;
  driverId?: string | null;
  clearDriverLink?: boolean;
}

export interface SetUserRolesRequest {
  roles: string[];
}

export interface SetUserActiveRequest {
  isActive: boolean;
}

export interface AdminSetPasswordRequest {
  newPassword: string;
  mustChangePassword?: boolean;
  clearInvitePending?: boolean;
}

export interface SetInvitePendingRequest {
  invitePending: boolean;
}

export interface DirectReport {
  id: string;
  displayName: string;
  email: string;
  jobTitle: string | null;
  department: string | null;
  isActive: boolean;
}

export interface OrgChartNode {
  id: string;
  displayName: string;
  jobTitle: string | null;
  department: string | null;
  reports: OrgChartNode[];
}

export interface UserOrg {
  id: string;
  displayName: string;
  email: string;
  manager: ManagerOption | null;
  directReports: DirectReport[];
  orgTree: OrgChartNode;
}

export interface CreateUserFromDriverRequest {
  username?: string | null;
  password?: string | null;
  roles?: string[] | null;
  managerUserId?: string | null;
  sendInvite?: boolean;
}

export const APP_ROLES = [
  'SystemAdmin',
  'Admin',
  'FleetAdmin',
  'FleetOperator',
  'Driver',
  'Staff',
  'Manager',
  'Finance',
  'HR',
] as const;

export type AppRole = (typeof APP_ROLES)[number];
