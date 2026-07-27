import { api } from './api';
import type {
  LeaveBalance,
  LeaveDashboardData,
  LeaveHistoryItem,
  LeaveRequest,
} from '../types/leave';

function toNumber(value: unknown): number {
  if (typeof value === 'number' && Number.isFinite(value)) {
    return value;
  }
  if (typeof value === 'string') {
    const parsed = Number.parseFloat(value);
    return Number.isFinite(parsed) ? parsed : 0;
  }
  return 0;
}

function mapRequest(item: LeaveRequest): LeaveHistoryItem {
  return {
    id: String(item.id ?? ''),
    type: item.leaveType || 'Leave',
    status: item.status || 'Pending',
    startDate: item.startDate || '',
    endDate: item.endDate || '',
    workingDays: toNumber(item.workingDays),
    notes: item.notes ?? null,
    createdAt: item.createdAt ?? null,
  };
}

function sortHistoryNewestFirst(rows: LeaveHistoryItem[]): LeaveHistoryItem[] {
  return [...rows].sort((left, right) => {
    const leftDate = Date.parse(left.startDate);
    const rightDate = Date.parse(right.startDate);
    if (Number.isNaN(leftDate) && Number.isNaN(rightDate)) {
      return 0;
    }
    if (Number.isNaN(leftDate)) {
      return 1;
    }
    if (Number.isNaN(rightDate)) {
      return -1;
    }
    return rightDate - leftDate;
  });
}

function toUpcomingRequests(history: LeaveHistoryItem[]): LeaveHistoryItem[] {
  const today = new Date();
  const todayOnly = new Date(today.getFullYear(), today.getMonth(), today.getDate()).getTime();

  return history
    .filter((row) => {
      const parsed = Date.parse(row.startDate);
      if (Number.isNaN(parsed)) {
        return false;
      }
      const day = new Date(parsed);
      const dayOnly = new Date(day.getFullYear(), day.getMonth(), day.getDate()).getTime();
      return dayOnly >= todayOnly;
    })
    .sort((left, right) => left.startDate.localeCompare(right.startDate))
    .slice(0, 5);
}

export async function loadLeaveDashboard(): Promise<LeaveDashboardData> {
  const [balancesResponse, requestsResponse] = await Promise.all([
    api.get<LeaveBalance[]>('/leave/balances'),
    api.get<LeaveRequest[]>('/leave/requests'),
  ]);

  const balances = Array.isArray(balancesResponse.data) ? balancesResponse.data : [];
  const requests = Array.isArray(requestsResponse.data) ? requestsResponse.data : [];
  const history = sortHistoryNewestFirst(
    requests.map(mapRequest).filter((row) => row.id.length > 0),
  );

  return {
    balances,
    history,
    upcomingRequests: toUpcomingRequests(history),
  };
}
