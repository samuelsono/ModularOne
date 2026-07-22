import { useRef } from 'react';
import { Button, makeStyles, tokens, type JSXElement } from '@fluentui/react-components';
import type { AuthUser } from '@platform/auth/types';
import { stopDataGridRowSelection } from '@platform/utils/dataGrid';
import {
  getAttachDocumentLabel,
  getLeaveRowActions,
  LEAVE_DOCUMENT_ACCEPT,
  type LeaveActionableRow,
  type LeaveActionKind,
  type LeaveActionPermissions,
  type LeaveConfirmActionKind,
} from '@modules/leave/utils/leaveActionUtils';
import { AttachRegular } from '@fluentui/react-icons';

const styles = makeStyles({
  documentButton: {
    backgroundColor: tokens.colorStatusSuccessBackground1,
    ":hover": {
      backgroundColor: tokens.colorStatusSuccessBackground2,
    },
    color: tokens.colorBrandBackground,
  },
  approveButton: {
    backgroundColor: tokens.colorStatusSuccessBackground3,
    ":hover": {
      backgroundColor: tokens.colorStatusSuccessForeground2,
    },
    color: tokens.colorNeutralCardBackground,
  },
  cancelButton: {
    backgroundColor: tokens.colorStatusWarningBackground3,
    ":hover": {
      backgroundColor: tokens.colorStatusWarningForeground2,
    },
    color: tokens.colorNeutralCardBackground,
  },
  rejectButton: {
    backgroundColor: tokens.colorStatusDangerBackground3,
    ":hover": {
      backgroundColor: tokens.colorStatusDangerForeground2,
    },
    color: tokens.colorNeutralCardBackground,
  }

});



const actionLabels: Record<LeaveConfirmActionKind, string> = {
  approve: 'Approve',
  reject: 'Reject',
  cancel: 'Cancel',
};

interface LeaveRowActionsProps {
  item: LeaveActionableRow;
  user: AuthUser | null | undefined;
  permissions: LeaveActionPermissions;
  actingId?: string | null;
  disabled?: boolean;
  onAction?: (kind: LeaveConfirmActionKind, id: string) => void;
  onUploadDocument?: (id: string, file: File) => void;
}

const actionIcons : Record<LeaveActionKind, JSXElement | null> = {
  attachDocument: <AttachRegular />,
  approve: null,
  reject: null,
  cancel: null,
}

export function LeaveRowActions({
  item,
  user,
  permissions,
  actingId = null,
  disabled = false,
  onAction,
  onUploadDocument,
}: LeaveRowActionsProps) {
  const classes = styles();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const actions = getLeaveRowActions(user, item, permissions);

  const actionButtonClass: Record<LeaveActionKind, string> = {
  approve: classes.approveButton,
  reject: classes.rejectButton,
  cancel: classes.cancelButton,
  attachDocument: classes.documentButton,
};

  if (actions.length === 0) {
    return <>—</>;
  }

  const isActing = actingId === item.id;

  return (
    <div
      className="flex flex-wrap items-center gap-1"
      onClick={stopDataGridRowSelection}
      onKeyDown={stopDataGridRowSelection}
    >
      <input
        ref={fileInputRef}
        type="file"
        className="hidden"
        accept={LEAVE_DOCUMENT_ACCEPT}
        onChange={(event) => {
          const file = event.target.files?.[0];
          event.target.value = '';
          if (file) {
            onUploadDocument?.(item.id, file);
          }
        }}
      />
      {actions.map((action) => (
        <Button
          key={action}
          size="small"
          appearance="primary"
          icon={actionIcons[action]}
          className={actionButtonClass[action]}
          disabled={disabled || isActing}
          onClick={() => {
            if (action === 'attachDocument') {
              fileInputRef.current?.click();
              return;
            }

            onAction?.(action, item.id);
          }}
        >
          {isActing
            ? 'Working...'
            : action === 'attachDocument'
              ? getAttachDocumentLabel(item)
              : actionLabels[action]}
        </Button>
      ))}
    </div>
  );
}
