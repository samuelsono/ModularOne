export type LeaveRequestStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';

export interface LeaveType {
  id: string;
  name: string;
  code: string;
  color: string;
  isPaid: boolean;
  deductsBalance: boolean;
  requiresDocument: boolean;
  allowHalfDay: boolean;
  accrualMethod: string;
  annualEntitlement: number | null;
  maxConsecutiveDays: number | null;
  minNoticeDays: number;
  eligibleGender: 'Any' | 'Male' | 'Female';
  isActive: boolean;
  sortOrder: number;
  createdAt?: string;
  createdByUserId?: string | null;
  createdByDisplayName?: string | null;
  updatedAt?: string | null;
  updatedByUserId?: string | null;
  updatedByDisplayName?: string | null;
}

export interface SaveLeaveTypeRequest {
  name: string;
  code: string;
  color?: string | null;
  isPaid: boolean;
  deductsBalance: boolean;
  requiresDocument: boolean;
  allowHalfDay: boolean;
  accrualMethod: string;
  annualEntitlement?: number | null;
  maxConsecutiveDays?: number | null;
  minNoticeDays: number;
  eligibleGender: 'Any' | 'Male' | 'Female';
  isActive: boolean;
  sortOrder: number;
}

export interface PublicHoliday {
  id: string;
  name: string;
  date: string;
  isRecurring: boolean;
  branch: string | null;
}

export interface SavePublicHolidayRequest {
  name: string;
  date: string;
  isRecurring: boolean;
  branch?: string | null;
}

export interface LeaveBalance {
  id: string;
  userId: string;
  leaveTypeId: string;
  leaveTypeName: string;
  leaveTypeColor: string | null;
  cycleStart: string;
  cycleEnd: string;
  allocated: number;
  used: number;
  pending: number;
  adjusted: number;
  remaining: number;
}

export interface AdjustLeaveBalanceRequest {
  userId: string;
  leaveTypeId: string;
  cycleStart: string;
  cycleEnd: string;
  allocatedDelta: number;
  adjustedDelta: number;
}

export interface WorkingDaysResult {
  startDate: string;
  endDate: string;
  workingDays: number;
  holidayDates: string[];
  startDayPortion: string;
  endDayPortion: string;
}

export interface LeaveCalendarEntry {
  requestId: string;
  userId: string;
  displayName: string;
  department: string | null;
  branch: string | null;
  leaveTypeId: string;
  leaveTypeName: string;
  leaveTypeColor: string;
  startDate: string;
  endDate: string;
  status: string;
  workingDays: number;
}

export interface LeaveCalendarResponse {
  entries: LeaveCalendarEntry[];
  holidays: PublicHoliday[];
  departments: string[];
  branches: string[];
}

export interface LeaveRequest {
  id: string;
  requesterUserId: string;
  requesterDisplayName: string;
  managerUserId: string;
  leaveTypeId: string;
  leaveType: string;
  leaveTypeColor: string | null;
  startDate: string;
  endDate: string;
  startDayPortion: string;
  endDayPortion: string;
  workingDays: number;
  status: LeaveRequestStatus | string;
  notes: string | null;
  hasDocument: boolean;
  documentFileName: string | null;
  createdAt: string;
  createdByUserId?: string | null;
  createdByDisplayName?: string | null;
  updatedAt?: string | null;
  updatedByUserId?: string | null;
  updatedByDisplayName?: string | null;
  decidedAt: string | null;
}

export interface CreateLeaveRequest {
  leaveTypeId: string;
  startDate: string;
  endDate: string;
  notes?: string | null;
  startDayPortion?: string | null;
  endDayPortion?: string | null;
  /** When set by HR/Admin, create the request for this employee. */
  onBehalfOfUserId?: string | null;
}

export interface ApprovalDecisionRequest {
  approve: boolean;
  notes?: string | null;
}

export const LEAVE_STATUS_FILTERS = ['All', 'Pending', 'Approved', 'Rejected', 'Cancelled'] as const;

export const LEAVE_BALANCE_FILTERS = ['All', 'Has remaining', 'Zero remaining', 'Has pending'] as const;

export interface LeaveReportCount {
  label: string;
  count: number;
}

export interface LeaveReportSummary {
  pendingCount: number;
  onLeaveTodayCount: number;
  totalRemainingDays: number;
  remainingAnnualDays: number;
  remainingSickDays: number;
  remainingOtherDays: number;
  byType: LeaveReportCount[];
  byStatus: LeaveReportCount[];
  byDepartment: LeaveReportCount[];
}

export interface LeaveHistoryRow {
  id: string;
  requesterUserId: string;
  managerUserId: string;
  requesterDisplayName: string;
  department: string | null;
  branch: string | null;
  leaveType: string;
  startDate: string;
  endDate: string;
  workingDays: number;
  status: string;
  notes: string | null;
  createdAt: string;
  createdByUserId?: string | null;
  createdByDisplayName?: string | null;
  updatedAt?: string | null;
  updatedByUserId?: string | null;
  updatedByDisplayName?: string | null;
  decidedAt: string | null;
}

export interface LeaveLiabilityRow {
  userId: string;
  displayName: string;
  department: string | null;
  leaveTypeName: string;
  leaveTypeCode: string;
  leaveTypeColor: string | null;
  allocated: number;
  used: number;
  pending: number;
  remaining: number;
}

export interface LeaveHistoryFilters {
  status?: string;
  department?: string;
  branch?: string;
  startDate?: string;
  endDate?: string;
  userId?: string;
  year?: number;
}

export interface WorkLocationType {
  id: string;
  code: string;
  name: string;
  color: string;
  tracksCollaborators: boolean;
  isActive: boolean;
  sortOrder: number;
}

export interface ScheduleTemplateDay {
  dayOfWeek: number;
  locationTypeId: string;
  locationTypeName: string;
  locationTypeColor: string;
}

export interface ScheduleTemplate {
  id: string;
  userId: string;
  userDisplayName: string;
  effectiveFrom: string;
  effectiveTo: string | null;
  notes: string | null;
  days: ScheduleTemplateDay[];
}

export interface UpsertScheduleTemplateRequest {
  userId?: string | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  notes?: string | null;
  days: Array<{ dayOfWeek: number; locationTypeId: string }>;
}

export interface ScheduleDayOverride {
  id: string;
  userId: string;
  date: string;
  locationTypeId: string;
  locationTypeName: string;
  locationTypeColor: string;
  notes: string | null;
}

export interface UpsertScheduleDayOverrideRequest {
  userId?: string | null;
  date: string;
  locationTypeId: string;
  notes?: string | null;
}

export interface ResolvedScheduleDay {
  userId: string;
  userDisplayName: string;
  date: string;
  kind: 'Work' | 'OnLeave' | 'Unscheduled';
  locationTypeId: string | null;
  locationTypeCode: string | null;
  locationTypeName: string | null;
  locationTypeColor: string | null;
  tracksCollaborators: boolean;
  leaveRequestId: string | null;
  leaveTypeName: string | null;
  fromOverride: boolean;
}

export interface AttendanceCollaborator {
  collaboratorUserId: string | null;
  collaboratorDisplayName: string | null;
  externalName: string | null;
}

export interface AttendanceDay {
  id: string;
  userId: string;
  userDisplayName: string;
  date: string;
  plannedLocationTypeId: string | null;
  plannedLocationTypeName: string | null;
  plannedLocationTypeColor: string | null;
  actualLocationTypeId: string;
  actualLocationTypeName: string;
  actualLocationTypeColor: string;
  tracksCollaborators: boolean;
  source: string;
  notes: string | null;
  recordedByUserId: string;
  recordedByDisplayName: string;
  recordedAt: string;
  collaborators: AttendanceCollaborator[];
}

export interface UpsertAttendanceDayRequest {
  userId?: string | null;
  actualLocationTypeId: string;
  notes?: string | null;
  collaborators?: Array<{ collaboratorUserId?: string | null; externalName?: string | null }>;
}

export interface AttendanceCompareRow {
  userId: string;
  userDisplayName: string;
  date: string;
  plannedKind: string;
  plannedLocationTypeId: string | null;
  plannedLocationTypeName: string | null;
  plannedLocationTypeColor: string | null;
  actualLocationTypeId: string | null;
  actualLocationTypeName: string | null;
  actualLocationTypeColor: string | null;
  hasActual: boolean;
  isMatch: boolean;
  isMismatch: boolean;
  collaborators: AttendanceCollaborator[];
}

