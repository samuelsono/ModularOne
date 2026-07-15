import type {
  AdjustLeaveBalanceRequest,
  ApprovalDecisionRequest,
  CreateLeaveRequest,
  LeaveBalance,
  LeaveCalendarResponse,
  LeaveHistoryFilters,
  LeaveHistoryRow,
  LeaveLiabilityRow,
  LeaveReportSummary,
  LeaveRequest,
  LeaveType,
  PublicHoliday,
  SaveLeaveTypeRequest,
  SavePublicHolidayRequest,
  WorkingDaysResult,
} from '@modules/leave/types/leave';
import { authorizedFetch } from '@platform/api/authService';

export async function runLeaveAccrual(): Promise<{ applied: number }> {
  return authorizedFetch<{ applied: number }>('/api/leave/admin/accrual/run', {
    method: 'POST',
  });
}

export function getLeaveTypes(): Promise<LeaveType[]> {
  return authorizedFetch<LeaveType[]>('/api/leave/types');
}

export function getAdminLeaveTypes(): Promise<LeaveType[]> {
  return authorizedFetch<LeaveType[]>('/api/leave/admin/types');
}

export function createLeaveType(request: SaveLeaveTypeRequest): Promise<LeaveType> {
  return authorizedFetch<LeaveType>('/api/leave/admin/types', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function updateLeaveType(id: string, request: SaveLeaveTypeRequest): Promise<LeaveType> {
  return authorizedFetch<LeaveType>(`/api/leave/admin/types/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export function deleteLeaveType(id: string): Promise<void> {
  return authorizedFetch<void>(`/api/leave/admin/types/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });
}

export function getAdminPublicHolidays(year?: number): Promise<PublicHoliday[]> {
  const query = year ? `?year=${encodeURIComponent(String(year))}` : '';
  return authorizedFetch<PublicHoliday[]>(`/api/leave/admin/holidays${query}`);
}

export function getUpcomingPublicHolidays(untilYear?: number): Promise<PublicHoliday[]> {
  const query = untilYear ? `?untilYear=${encodeURIComponent(String(untilYear))}` : '';
  return authorizedFetch<PublicHoliday[]>(`/api/leave/holidays/upcoming${query}`);
}

export function createPublicHoliday(request: SavePublicHolidayRequest): Promise<PublicHoliday> {
  return authorizedFetch<PublicHoliday>('/api/leave/admin/holidays', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function updatePublicHoliday(id: string, request: SavePublicHolidayRequest): Promise<PublicHoliday> {
  return authorizedFetch<PublicHoliday>(`/api/leave/admin/holidays/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export function deletePublicHoliday(id: string): Promise<void> {
  return authorizedFetch<void>(`/api/leave/admin/holidays/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });
}

export function syncPublicHolidays(year?: number): Promise<{ added: number }> {
  const query = year ? `?year=${encodeURIComponent(String(year))}` : '';
  return authorizedFetch<{ added: number }>(`/api/leave/admin/holidays/sync${query}`, {
    method: 'POST',
  });
}

export function getWorkingDaysPreview(
  startDate: string,
  endDate: string,
  startDayPortion?: string,
  endDayPortion?: string,
): Promise<WorkingDaysResult> {
  const query = new URLSearchParams({ startDate, endDate });
  if (startDayPortion) {
    query.set('startDayPortion', startDayPortion);
  }
  if (endDayPortion) {
    query.set('endDayPortion', endDayPortion);
  }
  return authorizedFetch<WorkingDaysResult>(`/api/leave/working-days?${query.toString()}`);
}

export function getMyLeaveBalances(year?: number): Promise<LeaveBalance[]> {
  const query = year ? `?year=${encodeURIComponent(String(year))}` : '';
  return authorizedFetch<LeaveBalance[]>(`/api/leave/balances${query}`);
}

export function getUserLeaveBalances(userId: string, year?: number): Promise<LeaveBalance[]> {
  const query = year ? `?year=${encodeURIComponent(String(year))}` : '';
  return authorizedFetch<LeaveBalance[]>(`/api/leave/balances/${encodeURIComponent(userId)}${query}`);
}

export function adjustLeaveBalance(request: AdjustLeaveBalanceRequest): Promise<LeaveBalance> {
  return authorizedFetch<LeaveBalance>('/api/leave/admin/balances/adjust', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function getLeaveCalendar(params: {
  start: string;
  end: string;
  branch?: string;
  department?: string;
  userId?: string;
}): Promise<LeaveCalendarResponse> {
  const query = new URLSearchParams({
    start: params.start,
    end: params.end,
  });

  if (params.branch) {
    query.set('branch', params.branch);
  }

  if (params.department) {
    query.set('department', params.department);
  }

  if (params.userId) {
    query.set('userId', params.userId);
  }

  return authorizedFetch<LeaveCalendarResponse>(`/api/leave/calendar?${query.toString()}`);
}

export function getLeaveReportSummary(params?: {
  year?: number;
  department?: string;
  branch?: string;
}): Promise<LeaveReportSummary> {
  const query = new URLSearchParams();
  if (params?.year) {
    query.set('year', String(params.year));
  }
  if (params?.department) {
    query.set('department', params.department);
  }
  if (params?.branch) {
    query.set('branch', params.branch);
  }
  const suffix = query.size > 0 ? `?${query.toString()}` : '';
  return authorizedFetch<LeaveReportSummary>(`/api/leave/reports/summary${suffix}`);
}

export function getLeaveHistory(filters: LeaveHistoryFilters = {}): Promise<LeaveHistoryRow[]> {
  const query = new URLSearchParams();
  if (filters.status && filters.status !== 'All') {
    query.set('status', filters.status);
  }
  if (filters.department) {
    query.set('department', filters.department);
  }
  if (filters.branch) {
    query.set('branch', filters.branch);
  }
  if (filters.startDate) {
    query.set('startDate', filters.startDate);
  }
  if (filters.endDate) {
    query.set('endDate', filters.endDate);
  }
  if (filters.userId) {
    query.set('userId', filters.userId);
  }
  if (filters.year) {
    query.set('year', String(filters.year));
  }
  const suffix = query.size > 0 ? `?${query.toString()}` : '';
  return authorizedFetch<LeaveHistoryRow[]>(`/api/leave/reports/history${suffix}`);
}

export async function downloadLeaveHistoryCsv(filters: LeaveHistoryFilters = {}): Promise<void> {
  const query = new URLSearchParams({ format: 'csv' });
  if (filters.status && filters.status !== 'All') {
    query.set('status', filters.status);
  }
  if (filters.department) {
    query.set('department', filters.department);
  }
  if (filters.branch) {
    query.set('branch', filters.branch);
  }
  if (filters.startDate) {
    query.set('startDate', filters.startDate);
  }
  if (filters.endDate) {
    query.set('endDate', filters.endDate);
  }
  if (filters.userId) {
    query.set('userId', filters.userId);
  }
  if (filters.year) {
    query.set('year', String(filters.year));
  }

  const { getAccessToken } = await import('@platform/api/tokenStorage');
  const token = getAccessToken();
  const response = await fetch(`/api/leave/reports/history?${query.toString()}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (!response.ok) {
    throw new Error('Failed to export leave history.');
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = 'leave-history.csv';
  link.click();
  URL.revokeObjectURL(url);
}

export function getLeaveLiability(): Promise<LeaveLiabilityRow[]> {
  return authorizedFetch<LeaveLiabilityRow[]>('/api/leave/reports/liability');
}

export function getLeavePendingReport(): Promise<LeaveRequest[]> {
  return authorizedFetch<LeaveRequest[]>('/api/leave/reports/pending');
}

export function getMyLeaveRequests(status?: string): Promise<LeaveRequest[]> {
  const query = status && status !== 'All' ? `?status=${encodeURIComponent(status)}` : '';
  return authorizedFetch<LeaveRequest[]>(`/api/leave/requests${query}`);
}

export function getLeaveRequest(id: string): Promise<LeaveRequest> {
  return authorizedFetch<LeaveRequest>(`/api/leave/requests/${encodeURIComponent(id)}`);
}

export function createLeaveRequest(request: CreateLeaveRequest, document?: File | null): Promise<LeaveRequest> {
  if (document) {
    return createLeaveRequestWithDocument(request, document);
  }

  return authorizedFetch<LeaveRequest>('/api/leave/requests', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

async function createLeaveRequestWithDocument(
  request: CreateLeaveRequest,
  document: File,
): Promise<LeaveRequest> {
  const created = await authorizedFetch<LeaveRequest>('/api/leave/requests', {
    method: 'POST',
    body: JSON.stringify(request),
  });

  const formData = new FormData();
  formData.append('document', document, document.name);

  try {
    return await authorizedFetch<LeaveRequest>(
      `/api/leave/requests/${encodeURIComponent(created.id)}/document`,
      { method: 'POST', body: formData },
    );
  } catch (error) {
    try {
      await cancelLeaveRequest(created.id, 'Automatic cancellation: document upload failed.');
    } catch {
      // Best-effort cleanup; surface the original upload error.
    }

    throw error;
  }
}

export async function downloadLeaveDocument(requestId: string, fileName?: string | null): Promise<void> {
  const { getAccessToken } = await import('@platform/api/tokenStorage');
  const token = getAccessToken();
  const response = await fetch(`/api/leave/requests/${encodeURIComponent(requestId)}/document`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (!response.ok) {
    throw new Error('Failed to download leave document.');
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName ?? 'leave-document';
  link.click();
  URL.revokeObjectURL(url);
}

export async function uploadLeaveDocument(requestId: string, document: File): Promise<LeaveRequest> {
  const formData = new FormData();
  formData.append('document', document, document.name);

  return authorizedFetch<LeaveRequest>(`/api/leave/requests/${encodeURIComponent(requestId)}/document`, {
    method: 'POST',
    body: formData,
  });
}

export function cancelLeaveRequest(id: string, notes: string): Promise<LeaveRequest> {
  return authorizedFetch<LeaveRequest>(`/api/leave/requests/${encodeURIComponent(id)}/cancel`, {
    method: 'POST',
    body: JSON.stringify({ notes }),
  });
}

export function getPendingLeaveApprovals(): Promise<LeaveRequest[]> {
  return authorizedFetch<LeaveRequest[]>('/api/leave/approvals/pending');
}

export function decideLeaveApproval(id: string, request: ApprovalDecisionRequest): Promise<LeaveRequest> {
  return authorizedFetch<LeaveRequest>(`/api/leave/approvals/${encodeURIComponent(id)}/decide`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}
