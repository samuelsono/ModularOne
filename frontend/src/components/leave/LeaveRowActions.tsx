import { Button } from '@fluentui/react-components';
import type { AuthUser } from '../../types/auth';
import { stopDataGridRowSelection } from '../../utils/dataGrid';
import {
  getLeaveRowActions,
  type LeaveActionableRow,
  type LeaveActionKind,
  type LeaveActionPermissions,
} from '../../utils/leaveActionUtils';

const actionButtonClass: Record<LeaveActionKind, string> = {
  approve: '!bg-green-700 hover:!bg-green-800 !text-white !border-green-700',
  reject: '!bg-red-600 hover:!bg-red-700 !text-white !border-red-600',
  cancel: '!bg-amber-600 hover:!bg-amber-700 !text-white !border-amber-600',
};

const actionLabels: Record<LeaveActionKind, string> = {
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
  onAction?: (kind: LeaveActionKind, id: string) => void;
}

export function LeaveRowActions({
  item,
  user,
  permissions,
  actingId = null,
  disabled = false,
  onAction,
}: LeaveRowActionsProps) {
  const actions = getLeaveRowActions(user, item, permissions);

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
      {actions.map((action) => (
        <Button
          key={action}
          size="small"
          appearance="primary"
          className={actionButtonClass[action]}
          disabled={disabled || isActing}
          onClick={() => onAction?.(action, item.id)}
        >
          {isActing ? 'Working...' : actionLabels[action]}
        </Button>
      ))}
    </div>
  );
}
