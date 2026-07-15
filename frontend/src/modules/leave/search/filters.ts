import type {
  LeaveBalance,
  LeaveHistoryRow,
  LeaveLiabilityRow,
  LeaveRequest,
  LeaveType,
  PublicHoliday,
} from '@modules/leave/types/leave';
import { matchesSearchQuery } from '@platform/search/searchText';

export function filterLeaveRequests(requests: LeaveRequest[], query: string): LeaveRequest[] {
  const normalized = query.trim();
  if (!normalized) return requests;
  return requests.filter((request) => matchesSearchQuery(normalized, [
    request.requesterDisplayName, request.leaveType, request.status, request.notes,
    request.documentFileName, request.startDate, request.endDate, request.workingDays,
    request.startDayPortion, request.endDayPortion,
  ]));
}

export function filterLeaveHistoryRows(rows: LeaveHistoryRow[], query: string): LeaveHistoryRow[] {
  const normalized = query.trim();
  if (!normalized) return rows;
  return rows.filter((row) => matchesSearchQuery(normalized, [
    row.requesterDisplayName, row.department, row.branch, row.leaveType, row.status,
    row.notes, row.startDate, row.endDate, row.workingDays,
  ]));
}

export function filterLeaveLiabilityRows(rows: LeaveLiabilityRow[], query: string): LeaveLiabilityRow[] {
  const normalized = query.trim();
  if (!normalized) return rows;
  return rows.filter((row) => matchesSearchQuery(normalized, [
    row.displayName, row.department, row.leaveTypeName, row.leaveTypeCode,
    row.allocated, row.used, row.pending, row.remaining,
  ]));
}

export function filterLeaveBalances(balances: LeaveBalance[], query: string): LeaveBalance[] {
  const normalized = query.trim();
  if (!normalized) return balances;
  return balances.filter((balance) => matchesSearchQuery(normalized, [
    balance.leaveTypeName, balance.cycleStart, balance.cycleEnd,
    balance.remaining, balance.allocated, balance.used, balance.pending, balance.adjusted,
  ]));
}

export function filterLeaveTypes(types: LeaveType[], query: string): LeaveType[] {
  const normalized = query.trim();
  if (!normalized) return types;
  return types.filter((type) => matchesSearchQuery(normalized, [
    type.name, type.code, type.accrualMethod,
    type.isPaid ? 'paid' : 'unpaid',
    type.deductsBalance ? 'deducts balance' : '',
    type.requiresDocument ? 'document' : '',
    type.allowHalfDay ? 'half day' : '',
  ]));
}

export function filterPublicHolidays(holidays: PublicHoliday[], query: string): PublicHoliday[] {
  const normalized = query.trim();
  if (!normalized) return holidays;
  return holidays.filter((holiday) => matchesSearchQuery(normalized, [
    holiday.name, holiday.date, holiday.branch,
    holiday.isRecurring ? 'recurring' : 'once-off',
  ]));
}

export function filterLeaveCalendarEntries<T extends {
  displayName: string;
  department: string | null;
  branch: string | null;
  leaveTypeName: string;
  status: string;
  startDate: string;
  endDate: string;
}>(entries: T[], query: string): T[] {
  const normalized = query.trim();
  if (!normalized) return entries;
  return entries.filter((entry) => matchesSearchQuery(normalized, [
    entry.displayName, entry.department, entry.branch, entry.leaveTypeName,
    entry.status, entry.startDate, entry.endDate,
  ]));
}
