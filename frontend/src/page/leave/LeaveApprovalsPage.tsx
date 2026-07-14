import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Button,
  MessageBar,
  MessageBarBody,
  Spinner,
  Text,
} from '@fluentui/react-components';
import { LeaveActionConfirmDialog } from '../../components/leave/LeaveActionConfirmDialog';
import { LeaveBulkActionBar } from '../../components/leave/LeaveBulkActionBar';
import { LeaveRowActions } from '../../components/leave/LeaveRowActions';
import { LeaveSelectableDataGrid } from '../../components/leave/LeaveSelectableDataGrid';
import {
  createLeaveActionsColumn,
  leaveApprovalColumns,
} from '../../components/leave/leaveTableUtils';
import AppTitle from '../../components/common/AppTitle';
import { useLeaveActions } from '../../hooks/useLeaveActions';
import { usePermissions } from '../../hooks/usePermissions';
import { ApiError } from '../../services/apiClient';
import { getPendingLeaveApprovals } from '../../services/leaveService';
import type { LeaveRequest } from '../../types/leave';
import { UpcomingHolidaysList } from '../../components/leave/UpcomingHolidaysList';
import { ArrowCounterclockwiseRegular, CalendarCheckmarkRegular } from '@fluentui/react-icons';
import EmptyListOrTable from '../../components/common/EmptyLIstOrTable';
import { usePageSearchQuery } from '../../context/PageSearchContext';
import { filterLeaveRequests } from '../../utils/pageSearch';

export default function LeaveApprovalsPage() {
  const searchQuery = usePageSearchQuery();
  const { user, hasPermission } = usePermissions();
  const leavePermissions = useMemo(() => ({
    canWriteRequests: hasPermission('leave.requests.write'),
    canWriteApprovals: hasPermission('leave.approvals.write'),
  }), [hasPermission]);

  const [requests, setRequests] = useState<LeaveRequest[]>([]);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadRequests = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const items = await getPendingLeaveApprovals();
      setRequests(items);
      setSelectedIds((current) => current.filter((id) => items.some((item) => item.id === id)));
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load pending approvals.');
      setRequests([]);
      setSelectedIds([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const {
    actingId,
    bulkActing,
    actionError,
    pendingAction,
    requestAction,
    confirmPendingAction,
    dismissPendingAction,
    isWorking,
  } = useLeaveActions(async () => {
    await loadRequests();
    setSelectedIds([]);
  });

  useEffect(() => {
    void loadRequests();
  }, [loadRequests]);

  const selectedItems = useMemo(
    () => requests.filter((item) => selectedIds.includes(item.id)),
    [requests, selectedIds],
  );

  const filteredRequests = useMemo(
    () => filterLeaveRequests(requests, searchQuery),
    [requests, searchQuery],
  );

  const showLeaveActions = leavePermissions.canWriteRequests || leavePermissions.canWriteApprovals;

  const columns = useMemo(() => {
    const base = [...leaveApprovalColumns];
    if (showLeaveActions) {
      base.push(createLeaveActionsColumn((item) => (
        <LeaveRowActions
          item={item}
          user={user}
          permissions={leavePermissions}
          actingId={actingId}
          disabled={isWorking}
          onAction={(kind, id) => requestAction(kind, [id])}
        />
      )));
    }
    return base;
  }, [actingId, isWorking, leavePermissions, requestAction, showLeaveActions, user]);

  return (
    <div className="flex flex-col gap-4 h-full min-h-0">
      <div className="flex items-start justify-between gap-4 px-3">
        <AppTitle title="Leave Approvals" subtitle="Review and approve leave requests from your team." />
           <Button icon={<ArrowCounterclockwiseRegular />} appearance="secondary" onClick={() => void loadRequests()}>
        </Button>
      </div>

      {error ? (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}

      {actionError ? (
        <MessageBar intent="error">
          <MessageBarBody>{actionError}</MessageBarBody>
        </MessageBar>
      ) : null}

      <div className='flex gap-2'>


      <div className="flex-1 min-h-0 overflow-auto flex flex-col gap-5">
        {isLoading ? (
          <Spinner label="Loading approvals..." />
        ) : requests.length === 0 ? (
          
          <EmptyListOrTable isLoading={isLoading} isEmpty message='No pending leave requests.' icon={CalendarCheckmarkRegular} >
             <></>
          </EmptyListOrTable>
        ) : (
          <>
            <LeaveBulkActionBar
              selectedItems={selectedItems}
              user={user}
              permissions={leavePermissions}
              disabled={isWorking}
              onBulkAction={requestAction}
            />
            <LeaveSelectableDataGrid
              items={filteredRequests}
              columns={columns}
              selectedIds={selectedIds}
              onSelectionChange={setSelectedIds}
              getRowId={(item) => item.id}
            />
          </>
        )}
      </div>

     
      </div>


      <LeaveActionConfirmDialog
        pending={pendingAction}
        isWorking={isWorking}
        onConfirm={(reason) => void confirmPendingAction(reason)}
        onCancel={dismissPendingAction}
      />
    </div>
  );
}
