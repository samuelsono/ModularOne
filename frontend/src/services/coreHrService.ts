import type {
  Company,
  Department,
  Position,
  SaveCompanyRequest,
  SaveDepartmentRequest,
  SavePositionRequest,
} from '../types/coreHr';
import { authorizedFetch } from './authService';

export function getCompanies(): Promise<Company[]> {
  return authorizedFetch<Company[]>('/api/core/companies');
}

export function createCompany(request: SaveCompanyRequest): Promise<Company> {
  return authorizedFetch<Company>('/api/core/companies', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function updateCompany(id: string, request: SaveCompanyRequest): Promise<Company> {
  return authorizedFetch<Company>(`/api/core/companies/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export function getDepartments(): Promise<Department[]> {
  return authorizedFetch<Department[]>('/api/core/departments');
}

export function createDepartment(request: SaveDepartmentRequest): Promise<Department> {
  return authorizedFetch<Department>('/api/core/departments', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function updateDepartment(id: string, request: SaveDepartmentRequest): Promise<Department> {
  return authorizedFetch<Department>(`/api/core/departments/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export function getPositions(): Promise<Position[]> {
  return authorizedFetch<Position[]>('/api/core/positions');
}

export function createPosition(request: SavePositionRequest): Promise<Position> {
  return authorizedFetch<Position>('/api/core/positions', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function updatePosition(id: string, request: SavePositionRequest): Promise<Position> {
  return authorizedFetch<Position>(`/api/core/positions/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}
