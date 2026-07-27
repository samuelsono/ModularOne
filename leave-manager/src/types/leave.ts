export type LeaveRequestStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled' | string;

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

export interface LeaveRequest {
  id: string;
  leaveType: string;
  leaveTypeColor: string | null;
  startDate: string;
  endDate: string;
  workingDays: number;
  status: LeaveRequestStatus;
  notes: string | null;
  createdAt: string;
}

export interface LeaveHistoryItem {
  id: string;
  type: string;
  status: LeaveRequestStatus;
  startDate: string;
  endDate: string;
  workingDays: number;
  notes: string | null;
  createdAt: string | null;
}

export interface LeaveDashboardData {
  balances: LeaveBalance[];
  history: LeaveHistoryItem[];
  upcomingRequests: LeaveHistoryItem[];
}
