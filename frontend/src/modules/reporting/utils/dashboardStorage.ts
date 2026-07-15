const STORAGE_KEY = 'cartrack.selectedDashboardId';
export const DASHBOARD_CHANGED_EVENT = 'cartrack:dashboard-changed';

export function getSelectedDashboardId(): string | null {
  return localStorage.getItem(STORAGE_KEY);
}

export function setSelectedDashboardId(dashboardId: string): void {
  localStorage.setItem(STORAGE_KEY, dashboardId);
}

export function clearSelectedDashboardId(): void {
  localStorage.removeItem(STORAGE_KEY);
}

export function notifyDashboardChanged(): void {
  window.dispatchEvent(new Event(DASHBOARD_CHANGED_EVENT));
}
