import { useRef } from 'react';
import { Button, Menu, MenuItem, MenuList, MenuPopover, MenuTrigger, type JSXElement } from '@fluentui/react-components';
import type { AuthUser } from '@platform/auth/types';
import { stopDataGridRowSelection } from '@platform/utils/dataGrid';
import { runAfterMenuDismiss } from '@platform/utils/runAfterMenuDismiss';
import {
  getAttachDocumentLabel,
  getLeaveRowActions,
  LEAVE_DOCUMENT_ACCEPT,
  type LeaveActionableRow,
  type LeaveActionKind,
  type LeaveActionPermissions,
  type LeaveConfirmActionKind,
} from '@modules/leave/utils/leaveActionUtils';
import {
  AttachRegular,
  CheckmarkRegular,
  DismissRegular,
  MoreVerticalRegular,
} from '@fluentui/react-icons';



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
  approve: <CheckmarkRegular />,
  reject: <DismissRegular />,
  cancel: <DismissRegular />,
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
  const fileInputRef = useRef<HTMLInputElement>(null);
  const actions = getLeaveRowActions(user, item, permissions);

  if (actions.length === 0) {
    return <>—</>;
  }

  const isActing = actingId === item.id;
  const isDisabled = disabled || isActing;

  const actionLabelsWithDocument: Record<LeaveActionKind, string> = {
    approve: actionLabels.approve,
    reject: actionLabels.reject,
    cancel: actionLabels.cancel,
    attachDocument: getAttachDocumentLabel(item) || 'Attach document',
  };

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
      <Menu>
        <MenuTrigger disableButtonEnhancement>
          <Button
            size="small"
            appearance="subtle"
            icon={<MoreVerticalRegular />}
            aria-label={isActing ? 'Working...' : 'More actions'}
            disabled={isDisabled}
          />
        </MenuTrigger>
        <MenuPopover>
          <MenuList>
            {actions.map((action) => (
              <MenuItem
                key={action}
                icon={actionIcons[action]}
                disabled={isDisabled}
                onClick={() => {
                  if (action === 'attachDocument') {
                    runAfterMenuDismiss(() => {
                      fileInputRef.current?.click();
                    });
                    return;
                  }

                  onAction?.(action, item.id);
                }}
              >
                {actionLabelsWithDocument[action]}
              </MenuItem>
            ))}
          </MenuList>
        </MenuPopover>
      </Menu>
    </div>
  );
}
