import { useEffect, useId, useState } from 'react';
import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Textarea,
} from '@fluentui/react-components';
import type { LeaveActionKind } from '../../utils/leaveActionUtils';

export interface PendingLeaveAction {
  kind: LeaveActionKind;
  ids: string[];
}

const titles: Record<LeaveActionKind, string> = {
  approve: 'Approve leave',
  reject: 'Reject leave',
  cancel: 'Cancel leave',
};

const actionNames: Record<LeaveActionKind, string> = {
  approve: 'Approve',
  reject: 'Reject',
  cancel: 'Cancel request',
};

const messages: Record<LeaveActionKind, (count: number) => string> = {
  approve: (count) =>
    count === 1
      ? 'This will approve the selected leave request'
      : `This will approve ${count} selected leave requests`,
  reject: (count) =>
    count === 1
      ? 'This will reject the selected leave request'
      : `This will reject ${count} selected leave requests`,
  cancel: (count) =>
    count === 1
      ? 'This will cancel the selected leave request'
      : `This will cancel ${count} selected leave requests`,
};

const confirmButtonClass: Record<LeaveActionKind, string> = {
  approve: '!bg-green-700 hover:!bg-green-800 !text-white !border-green-700',
  reject: '!bg-red-600 hover:!bg-red-700 !text-white !border-red-600',
  cancel: '!bg-amber-600 hover:!bg-amber-700 !text-white !border-amber-600',
};

interface LeaveActionConfirmDialogProps {
  pending: PendingLeaveAction | null;
  isWorking?: boolean;
  onConfirm: (reason: string) => void;
  onCancel: () => void;
}

export function LeaveActionConfirmDialog({
  pending,
  isWorking = false,
  onConfirm,
  onCancel,
}: LeaveActionConfirmDialogProps) {
  const dialogId = useId('leave-action-confirm-');
  const [reason, setReason] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);

  useEffect(() => {
    if (pending) {
      setReason('');
      setValidationError(null);
    }
  }, [pending]);

  const open = pending !== null;
  const kind = pending?.kind ?? 'approve';
  const count = pending?.ids.length ?? 0;
  const reasonRequired = kind !== 'approve';
  const trimmedReason = reason.trim();
  const canConfirm = !reasonRequired || trimmedReason.length > 0;

  function handleConfirm() {
    if (reasonRequired && !trimmedReason) {
      setValidationError('A reason is required.');
      return;
    }

    setValidationError(null);
    onConfirm(trimmedReason);
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(_, data) => {
        if (!data.open && !isWorking) {
          onCancel();
        }
      }}
    >
      <DialogSurface
        aria-labelledby={`${dialogId}-title`}
        aria-describedby={`${dialogId}-content`}
      >
        <DialogBody>
          <DialogTitle id={`${dialogId}-title`}>
            {titles[kind]}
          </DialogTitle>
          <DialogContent id={`${dialogId}-content`} className="flex flex-col gap-3">
            <p>
              {messages[kind](count)}. Are you sure you want to continue?
            </p>
            <Field
              label={reasonRequired ? 'Reason' : 'Reason (optional)'}
              required={reasonRequired}
              validationState={validationError ? 'error' : 'none'}
              validationMessage={validationError ?? undefined}
            >
              <Textarea
                value={reason}
                resize="vertical"
                rows={3}
                disabled={isWorking}
                placeholder={
                  reasonRequired
                    ? 'Enter a reason for this action'
                    : 'Optionally add a note for this approval'
                }
                onChange={(_, data) => {
                  setReason(data.value);
                  if (validationError) {
                    setValidationError(null);
                  }
                }}
              />
            </Field>
          </DialogContent>
          <DialogActions>
            <Button
              appearance="primary"
              className={confirmButtonClass[kind]}
              disabled={isWorking || !canConfirm}
              onClick={handleConfirm}
            >
              {isWorking ? 'Working...' : actionNames[kind]}
            </Button>
            <Button appearance="secondary" disabled={isWorking} onClick={onCancel}>
              Cancel
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
