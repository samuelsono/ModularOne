import type { AuthUser } from '@platform/auth/types';

export type LeaveActionKind = 'approve' | 'reject' | 'cancel';

export interface LeaveActionableRow {
  id: string;
  status: string;
  requesterUserId: string;
  managerUserId?: string | null;
  startDate?: string;
}

export interface LeaveActionPermissions {
  canWriteRequests: boolean;
  canWriteApprovals: boolean;
}

import { getLocalDateKey } from '@modules/leave/components/leaveTableUtils';

export function hasLeaveStarted(startDate: string): boolean {
  return getLocalDateKey() >= startDate;
}

export function getLeaveRowActions(
  user: AuthUser | null | undefined,
  item: LeaveActionableRow,
  permissions: LeaveActionPermissions,
): LeaveActionKind[] {
  if (!user) {
    return [];
  }

  const isRequester = user.id === item.requesterUserId;
  const actions: LeaveActionKind[] = [];

  if (item.status === 'Pending') {
    if (isRequester && permissions.canWriteRequests) {
      actions.push('cancel');
    }

    if (!isRequester && permissions.canWriteApprovals) {
      actions.push('approve', 'reject');
    }

    return actions;
  }

  if (
    item.status === 'Approved'
    && item.startDate
    && !hasLeaveStarted(item.startDate)
  ) {
    if (isRequester && permissions.canWriteRequests) {
      actions.push('cancel');
    } else if (!isRequester && permissions.canWriteApprovals) {
      actions.push('cancel');
    }
  }

  return actions;
}

export function rowSupportsAction(
  user: AuthUser | null | undefined,
  item: LeaveActionableRow,
  permissions: LeaveActionPermissions,
  action: LeaveActionKind,
): boolean {
  return getLeaveRowActions(user, item, permissions).includes(action);
}
