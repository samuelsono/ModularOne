import { authorizedFetch } from '@platform/api/authService';

/** Thin org-directory reads used by users/fleet without importing @modules/coreHr. */

export interface OrgCompany {
  id: string;
  name: string;
  code: string | null;
  isActive: boolean;
}

export interface OrgDepartment {
  id: string;
  companyId: string;
  companyName: string;
  name: string;
  code: string | null;
  isActive: boolean;
}

export interface OrgPosition {
  id: string;
  departmentId: string;
  departmentName: string;
  companyId: string;
  companyName: string;
  name: string;
  code: string | null;
  isActive: boolean;
}

export function getCompanies(): Promise<OrgCompany[]> {
  return authorizedFetch<OrgCompany[]>('/api/core/companies');
}

export function getDepartments(): Promise<OrgDepartment[]> {
  return authorizedFetch<OrgDepartment[]>('/api/core/departments');
}

export function getPositions(): Promise<OrgPosition[]> {
  return authorizedFetch<OrgPosition[]>('/api/core/positions');
}
