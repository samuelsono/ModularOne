import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Button,
  Dropdown,
  Field,
  MessageBar,
  MessageBarBody,
  Option,
  Spinner,
  Subtitle2,
  Text,
  ToggleButton,
  Tooltip,
  type TableColumnSizingOptions,
} from '@fluentui/react-components';
import { AddRegular, CalendarRegular, PeopleRegular, PersonRegular } from '@fluentui/react-icons';
import { LeaveActionConfirmDialog } from '@modules/leave/components/LeaveActionConfirmDialog';
import { LeaveBulkActionBar } from '@modules/leave/components/LeaveBulkActionBar';
import { LeaveRequestForm } from '@modules/leave/components/LeaveRequestForm';
import { LeaveRowActions } from '@modules/leave/components/LeaveRowActions';
import { LeaveSelectableDataGrid } from '@modules/leave/components/LeaveSelectableDataGrid';
import {
  createLeaveRequestColumnsWithDetailPopover,
} from '@modules/leave/components/leaveTableUtils';
import { useLeaveActions } from '@modules/leave/hooks/useLeaveActions';
import { usePermissions } from '@platform/permissions/usePermissions';
import { ApiError } from '@platform/api/apiClient';
import { getMyLeaveRequests } from '@modules/leave/services/leaveService';
import type { LeaveRequest } from '@modules/leave/types/leave';
import { LEAVE_STATUS_FILTERS } from '@modules/leave/types/leave';
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import { filterLeaveRequests } from '@modules/leave/search/filters';


const leaveTableColumnSizing: TableColumnSizingOptions = {
    department: { minWidth: 320, idealWidth: 400, defaultWidth: 360 },
    dates: { minWidth: 320, idealWidth: 400, defaultWidth: 360 },
    createdAt: { minWidth: 200, idealWidth: 250, defaultWidth: 250 },
    document: { minWidth: 180, idealWidth: 180, defaultWidth: 180 },
    status: { minWidth: 200, idealWidth: 250, defaultWidth: 250 },
    submitted: { minWidth: 200, idealWidth: 250, defaultWidth: 250 },
    workingDays: { minWidth: 60, idealWidth: 80, defaultWidth: 80 },
    leaveType: { minWidth: 100, idealWidth: 120, defaultWidth: 120 },
};

export default function LeaveRequestsPage() {
  const searchQuery = usePageSearchQuery();
  const { user, hasPermission, isAdmin, isHr, isManager } = usePermissions();
  const isElevatedViewer = isAdmin || isHr || isManager;
  const [filterCurrentUserOnly, setFilterCurrentUserOnly] = useState(false);
  const currentUserId = user?.id;
  const isCurrentUserOnlyView = isElevatedViewer && filterCurrentUserOnly && Boolean(currentUserId);
  const leavePermissions = useMemo(() => ({
    canWriteRequests: hasPermission('leave.requests.write'),
    canWriteApprovals: hasPermission('leave.approvals.write'),
  }), [hasPermission]);
  const canWrite = leavePermissions.canWriteRequests;

  const [requests, setRequests] = useState<LeaveRequest[]>([]);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [statusFilter, setStatusFilter] = useState<string>('All');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false);

  const loadRequests = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const items = await getMyLeaveRequests(statusFilter);
      setRequests(items);
      setSelectedIds((current) => current.filter((id) => items.some((item) => item.id === id)));
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load leave requests.');
      setRequests([]);
      setSelectedIds([]);
    } finally {
      setIsLoading(false);
    }
  }, [statusFilter]);

  const {
    actingId,
    actionError,
    pendingAction,
    requestAction,
    confirmPendingAction,
    dismissPendingAction,
    uploadDocument,
    isWorking,
  } = useLeaveActions(async () => {
    await loadRequests();
    setSelectedIds([]);
  });

  useEffect(() => {
    void loadRequests();
  }, [loadRequests]);

  const scopedRequests = useMemo(
    () => (isCurrentUserOnlyView && currentUserId
      ? requests.filter((item) => item.requesterUserId === currentUserId)
      : requests),
    [currentUserId, isCurrentUserOnlyView, requests],
  );

  const selectedItems = useMemo(
    () => scopedRequests.filter((item) => selectedIds.includes(item.id)),
    [scopedRequests, selectedIds],
  );

  const filteredRequests = useMemo(
    () => filterLeaveRequests(scopedRequests, searchQuery),
    [scopedRequests, searchQuery],
  );

  useEffect(() => {
    setSelectedIds((current) => current.filter((id) => scopedRequests.some((item) => item.id === id)));
  }, [scopedRequests]);

  const showLeaveActions = leavePermissions.canWriteRequests || leavePermissions.canWriteApprovals;

  const columns = useMemo(() => createLeaveRequestColumnsWithDetailPopover({
    user,
    permissions: leavePermissions,
    onAction: (kind, id) => requestAction(kind, [id]),
    onUploadDocument: uploadDocument,
    renderActions: showLeaveActions
      ? (item) => (
        <LeaveRowActions
          item={item}
          user={user}
          permissions={leavePermissions}
          actingId={actingId}
          disabled={isWorking}
          onAction={(kind, id) => requestAction(kind, [id])}
          onUploadDocument={uploadDocument}
        />
      )
      : undefined,
  }), [actingId, isWorking, leavePermissions, requestAction, showLeaveActions, uploadDocument, user]);

  return (
    <div className="flex flex-col gap-4 h-full min-h-0">
      <div className="flex items-center justify-between gap-4 px-3">
        <Subtitle2>My requests</Subtitle2>

        <div className="flex items-end gap-2">
          {isElevatedViewer ? (
            <Tooltip
              content={filterCurrentUserOnly ? 'Show team requests' : 'Show only your requests'}
              relationship="label"
            >
              <ToggleButton
                appearance="primary"
                checked={filterCurrentUserOnly}
                icon={filterCurrentUserOnly ? <PeopleRegular  /> : <PersonRegular />}
                onClick={() => setFilterCurrentUserOnly((prev) => !prev)}
              />
            </Tooltip>
          ) : null}

          <Field label={"Filter"} className='flex justify-end'>
            <Dropdown
              value={statusFilter}
              selectedOptions={[statusFilter]}
              onOptionSelect={(_, data) => setStatusFilter(data.optionValue ?? 'All')}
            >
              {LEAVE_STATUS_FILTERS.map((status) => (
                <Option key={status} value={status}>{status}</Option>
              ))}
            </Dropdown>
          </Field>

          {canWrite ? (
            <Button appearance="primary" icon={<AddRegular />} onClick={() => setFormOpen(true)}>
              {isAdmin || isHr ? 'Create leave application' : 'Apply for leave'}
            </Button>
          ) : null}
        </div>
      </div>

      {error ? (
        <MessageBar intent="error" className='mx-3'>
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}

      {actionError ? (
        <MessageBar intent="error" className='mx-3'>
          <MessageBarBody>{actionError}</MessageBarBody>
        </MessageBar>
      ) : null}

      <div className="flex-1 min-h-0 overflow-auto flex flex-col gap-3">
        {isLoading ? (
          <Spinner label="Loading requests..." />
        ) : requests.length === 0 ? (
          
          <div className="h-[70vh] max-h-full w-full flex flex-col gap-3 items-center justify-center p-6 text-sm text-neutral-foreground-3">
                    <CalendarRegular className='size-26 text-gray-300' />
                    No leave requests found.
                    {canWrite ? (
                      <Button appearance="primary" icon={<AddRegular />} onClick={() => setFormOpen(true)}>
                        Apply for leave
                      </Button>
                     ) : null}
          </div>
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
              columnSizingOptions={leaveTableColumnSizing}
              storageKey="leave.requests"
            />
          </>
        )}
      </div>

      <LeaveRequestForm
        open={formOpen}
        onClose={() => setFormOpen(false)}
        onSubmitted={() => void loadRequests()}
      />

      <LeaveActionConfirmDialog
        pending={pendingAction}
        isWorking={isWorking}
        onConfirm={(reason) => void confirmPendingAction(reason)}
        onCancel={dismissPendingAction}
      />
    </div>
  );
}
