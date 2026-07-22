import type { AuthUser } from '@platform/auth/types';
import { getLocalDateKey } from '@modules/leave/components/leaveTableUtils';

export type LeaveConfirmActionKind = 'approve' | 'reject' | 'cancel';
export type LeaveActionKind = LeaveConfirmActionKind | 'attachDocument';

export interface LeaveActionableRow {
  id: string;
  status: string;
  requesterUserId: string;
  managerUserId?: string | null;
  startDate?: string;
  hasDocument?: boolean;
}

export interface LeaveActionPermissions {
  canWriteRequests: boolean;
  canWriteApprovals: boolean;
}

const DOCUMENT_ADMIN_ROLES = new Set(['Admin', 'SystemAdmin', 'HR']);

export function isLeaveDocumentAdministrator(user: AuthUser | null | undefined): boolean {
  return Boolean(user?.roles.some((role) => DOCUMENT_ADMIN_ROLES.has(role)));
}

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
  const canAttachOnBehalf = isLeaveDocumentAdministrator(user);
  const actions: LeaveActionKind[] = [];

  if (item.status === 'Pending') {
    if (permissions.canWriteRequests && (isRequester || canAttachOnBehalf)) {
      actions.push('attachDocument');
    }

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

export function getAttachDocumentLabel(item: LeaveActionableRow): string {
  return item.hasDocument ? '' : '';
}

const LEAVE_DOCUMENT_EXTENSIONS = new Set([
  '.pdf',
  '.jpg',
  '.jpeg',
  '.png',
  '.heic',
  '.doc',
  '.docx',
]);

export const LEAVE_DOCUMENT_ACCEPT = '.pdf,.jpg,.jpeg,.png,.heic,.doc,.docx';
export const LEAVE_DOCUMENT_MAX_BYTES = 5 * 1024 * 1024;

export function validateLeaveDocumentFile(file: File): string | null {
  const extension = file.name.includes('.')
    ? `.${file.name.split('.').pop()!.toLowerCase()}`
    : '';

  if (!LEAVE_DOCUMENT_EXTENSIONS.has(extension)) {
    return 'Unsupported document type. Allowed formats: PDF, JPG, PNG, HEIC, DOC, DOCX.';
  }

  if (file.size > LEAVE_DOCUMENT_MAX_BYTES) {
    return 'Document must be 5 MB or smaller.';
  }

  return null;
}
