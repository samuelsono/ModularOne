import { useCallback, useState } from 'react';
import type { PendingLeaveAction } from '@modules/leave/components/LeaveActionConfirmDialog';
import { ApiError } from '@platform/api/apiClient';
import {
  cancelLeaveRequest,
  decideLeaveApproval,
  uploadLeaveDocument,
} from '@modules/leave/services/leaveService';
import {
  validateLeaveDocumentFile,
  type LeaveConfirmActionKind,
} from '@modules/leave/utils/leaveActionUtils';

export function useLeaveActions(onCompleted?: () => void | Promise<void>) {
  const [actingId, setActingId] = useState<string | null>(null);
  const [bulkActing, setBulkActing] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [pendingAction, setPendingAction] = useState<PendingLeaveAction | null>(null);

  const refresh = useCallback(async () => {
    if (onCompleted) {
      await onCompleted();
    }
  }, [onCompleted]);

  const requestAction = useCallback((kind: LeaveConfirmActionKind, ids: string[]) => {
    const uniqueIds = [...new Set(ids.filter(Boolean))];
    if (uniqueIds.length === 0) {
      return;
    }

    setActionError(null);
    setPendingAction({ kind, ids: uniqueIds });
  }, []);

  const dismissPendingAction = useCallback(() => {
    if (bulkActing || actingId) {
      return;
    }

    setPendingAction(null);
  }, [actingId, bulkActing]);

  const confirmPendingAction = useCallback(async (reason: string) => {
    if (!pendingAction) {
      return;
    }

    const { kind, ids } = pendingAction;
    const trimmedReason = reason.trim();

    if (kind !== 'approve' && !trimmedReason) {
      setActionError('A reason is required.');
      return;
    }

    const notes = trimmedReason || null;
    const isBulk = ids.length > 1;

    if (isBulk) {
      setBulkActing(true);
    } else {
      setActingId(ids[0] ?? null);
    }

    setActionError(null);

    try {
      for (const id of ids) {
        if (kind === 'cancel') {
          await cancelLeaveRequest(id, trimmedReason);
        } else {
          await decideLeaveApproval(id, {
            approve: kind === 'approve',
            notes,
          });
        }
      }

      setPendingAction(null);
      await refresh();
    } catch (error) {
      setActionError(
        error instanceof ApiError
          ? error.message
          : 'Failed to process leave action.',
      );
    } finally {
      setActingId(null);
      setBulkActing(false);
    }
  }, [pendingAction, refresh]);

  const uploadDocument = useCallback(async (id: string, file: File) => {
    const validationError = validateLeaveDocumentFile(file);
    if (validationError) {
      setActionError(validationError);
      return;
    }

    setActingId(id);
    setActionError(null);

    try {
      await uploadLeaveDocument(id, file);
      await refresh();
    } catch (error) {
      setActionError(
        error instanceof ApiError
          ? error.message
          : 'Failed to upload supporting document.',
      );
    } finally {
      setActingId(null);
    }
  }, [refresh]);

  return {
    actingId,
    bulkActing,
    actionError,
    setActionError,
    pendingAction,
    requestAction,
    confirmPendingAction,
    dismissPendingAction,
    uploadDocument,
    isWorking: bulkActing || actingId !== null,
  };
}
