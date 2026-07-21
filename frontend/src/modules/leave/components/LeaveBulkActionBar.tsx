import { Button, Text } from '@fluentui/react-components';
import type { AuthUser } from '@platform/auth/types';
import {
  rowSupportsAction,
  type LeaveActionableRow,
  type LeaveActionKind,
  type LeaveActionPermissions,
} from '@modules/leave/utils/leaveActionUtils';

interface LeaveBulkActionBarProps {
  selectedItems: LeaveActionableRow[];
  user: AuthUser | null | undefined;
  permissions: LeaveActionPermissions;
  disabled?: boolean;
  onBulkAction?: (kind: LeaveActionKind, ids: string[]) => void;
}

export function LeaveBulkActionBar({
  selectedItems,
  user,
  permissions,
  disabled = false,
  onBulkAction,
}: LeaveBulkActionBarProps) {
  if (selectedItems.length === 0) {
    return null;
  }

  const approvableIds = selectedItems
    .filter((item) => rowSupportsAction(user, item, permissions, 'approve'))
    .map((item) => item.id);
  const cancellableIds = selectedItems
    .filter((item) => rowSupportsAction(user, item, permissions, 'cancel'))
    .map((item) => item.id);

  return (
    <div className="flex flex-wrap items-center gap-2 rounded border border-neutral-stroke-2 bg-neutral-background-2 px-4 py-3 mx-6">
      <Text size={200} weight="semibold">
        {selectedItems.length} selected
      </Text>

      {approvableIds.length > 0 ? (
        <>
          <Button
            size="small"
            appearance="primary"
            className="!bg-green-700 hover:!bg-green-800 !text-white !border-green-700"
            disabled={disabled}
            onClick={() => onBulkAction?.('approve', approvableIds)}
          >
            Approve ({approvableIds.length})
          </Button>
          <Button
            size="small"
            appearance="primary"
            className="!bg-red-600 hover:!bg-red-700 !text-white !border-red-600"
            disabled={disabled}
            onClick={() => onBulkAction?.('reject', approvableIds)}
          >
            Reject ({approvableIds.length})
          </Button>
        </>
      ) : null}

      {cancellableIds.length > 0 ? (
        <Button
          size="small"
          appearance="primary"
          className="!bg-amber-600 hover:!bg-amber-700 !text-white !border-amber-600"
          disabled={disabled}
          onClick={() => onBulkAction?.('cancel', cancellableIds)}
        >
          Cancel ({cancellableIds.length})
        </Button>
      ) : null}
    </div>
  );
}
