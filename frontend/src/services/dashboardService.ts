import type {
  Dashboard,
  DashboardRender,
  DashboardSection,
  DashboardsResponse,
  ReorderDashboardLayoutRequest,
  SaveDashboardRequest,
  SaveDashboardSectionRequest,
} from '../types/dashboard';
import { authorizedFetch } from './authService';

export async function getDashboards(): Promise<DashboardsResponse> {
  return authorizedFetch<DashboardsResponse>('/api/dashboards');
}

export async function getDashboard(id: string): Promise<Dashboard> {
  return authorizedFetch<Dashboard>(`/api/dashboards/${encodeURIComponent(id)}`);
}

export async function createDashboard(request: SaveDashboardRequest): Promise<Dashboard> {
  return authorizedFetch<Dashboard>('/api/dashboards', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function updateDashboard(id: string, request: SaveDashboardRequest): Promise<Dashboard> {
  return authorizedFetch<Dashboard>(`/api/dashboards/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export async function deleteDashboard(id: string): Promise<void> {
  await authorizedFetch<void>(`/api/dashboards/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });
}

export async function createDashboardSection(
  dashboardId: string,
  request: SaveDashboardSectionRequest,
): Promise<DashboardSection> {
  return authorizedFetch<DashboardSection>(`/api/dashboards/${encodeURIComponent(dashboardId)}/sections`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function updateDashboardSection(
  sectionId: string,
  request: SaveDashboardSectionRequest,
): Promise<DashboardSection> {
  return authorizedFetch<DashboardSection>(`/api/dashboards/sections/${encodeURIComponent(sectionId)}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export async function deleteDashboardSection(sectionId: string): Promise<void> {
  await authorizedFetch<void>(`/api/dashboards/sections/${encodeURIComponent(sectionId)}`, {
    method: 'DELETE',
  });
}

export async function getDefaultDashboard(): Promise<Dashboard> {
  return authorizedFetch<Dashboard>('/api/dashboards/default');
}

export async function getDefaultDashboardRender(): Promise<DashboardRender> {
  return authorizedFetch<DashboardRender>('/api/dashboards/default/render');
}

export async function getDashboardRender(id: string): Promise<DashboardRender> {
  return authorizedFetch<DashboardRender>(`/api/dashboards/${encodeURIComponent(id)}/render`);
}

export async function reorderDashboardLayout(
  dashboardId: string,
  request: ReorderDashboardLayoutRequest,
): Promise<Dashboard> {
  return authorizedFetch<Dashboard>(`/api/dashboards/${encodeURIComponent(dashboardId)}/layout`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}
