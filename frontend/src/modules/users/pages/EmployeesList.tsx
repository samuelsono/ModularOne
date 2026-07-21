import { useCallback, useEffect, useMemo, useState } from 'react';
import { tokens, Button, MessageBar, MessageBarBody, Subtitle2 } from '@fluentui/react-components';
import {
  MailRegular,
  MapPinRegular,
  PersonAccountsRegular,
  PersonProhibitedRegular,
  PersonRegular,
  PersonSwapRegular,
  VehicleCarRegular,
} from '@fluentui/react-icons';
import AppFilters from '@platform/ui/AppFilters';
import { ConfirmAction } from '@platform/ui/ConfirmAction';
import { EmployeesTable } from '@modules/users/components/EmployeesTable';
import { EmployeeDetailsDialog } from '@modules/users/components/EmployeeDetailsDialog';
import { CreateEmployee } from '@modules/users/components/CreateEmployee';
import { EmployeeFormDialog } from '@modules/users/components/EmployeeFormDialog';
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import { usePermissions } from '@platform/permissions/usePermissions';
import { ApiError } from '@platform/api/apiClient';
import { getUsers, sendUserInvite, setUserActive, setUserInvitePending } from '@modules/users/services/userService';
import type { UserListItem } from '@modules/users/types/user';
import { filterEmployees } from '@modules/users/search/filters';
import { AdjustUserLeaveBalancesDialog } from '@modules/leave/components/AdjustUserLeaveBalancesDialog';
import { AdminResetPasswordDialog } from '@modules/users/components/AdminResetPasswordDialog';
import { useActiveApp } from '@platform/shell/ActiveAppContext';

const filters = [
  { name: 'status', label: 'Status', value: 'active', icon: PersonAccountsRegular },
  { name: 'gender', label: 'Gender', value: 'male', icon: PersonSwapRegular },
  { name: 'vehicle', label: 'Vehicle', value: 'truck', icon: VehicleCarRegular },
  { name: 'address', label: 'Address', value: 'city', icon: MapPinRegular },
];

const EmployeesList = () => {
  const searchQuery = usePageSearchQuery();
  const { canManageUsers, canEditUsers, isAdmin, isHr } = usePermissions();
  const [employees, setEmployees] = useState<UserListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [detailsEmployee, setDetailsEmployee] = useState<UserListItem | null>(null);
  const [editEmployee, setEditEmployee] = useState<UserListItem | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [isBulkWorking, setIsBulkWorking] = useState(false);
  const [deactivateConfirmOpen, setDeactivateConfirmOpen] = useState(false);
  const [pendingDeactivateIds, setPendingDeactivateIds] = useState<string[]>([]);
  const [leaveAdjustmentUser, setLeaveAdjustmentUser] = useState<UserListItem | null>(null);
  const [resetPasswordUser, setResetPasswordUser] = useState<UserListItem | null>(null);

  const loadEmployees = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await getUsers();
      setEmployees(response.items);
      setSelectedIds((current) =>
        current.filter((id) => response.items.some((employee) => employee.id === id)),
      );
    } catch (loadError) {
      const message = loadError instanceof ApiError
        ? loadError.message
        : 'Failed to load employees.';
      setError(message);
      setEmployees([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadEmployees();
  }, [loadEmployees, reloadKey]);

  const visibleEmployees = useMemo(
    () => filterEmployees(employees, searchQuery),
    [employees, searchQuery],
  );

  const selectedEmployees = useMemo(
    () => employees.filter((employee) => selectedIds.includes(employee.id)),
    [employees, selectedIds],
  );

  function handleUpdated() {
    setReloadKey((value) => value + 1);
  }

  async function runBulkAction(
    ids: string[],
    action: (id: string) => Promise<unknown>,
    failureMessage: string,
  ) {
    if (ids.length === 0) {
      return;
    }

    setIsBulkWorking(true);
    setActionError(null);

    try {
      await Promise.all(ids.map((id) => action(id)));
      handleUpdated();
    } catch (bulkError) {
      const message = bulkError instanceof ApiError
        ? bulkError.message
        : failureMessage;
      setActionError(message);
    } finally {
      setIsBulkWorking(false);
    }
  }

  const handleActivateSelected = useCallback(() => {
    void runBulkAction(
      selectedIds,
      (id) => setUserActive(id, { isActive: true }),
      'Failed to activate selected employees.',
    );
  }, [selectedIds]);

  const handleSendInviteSelected = useCallback(() => {
    void runBulkAction(
      selectedIds,
      (id) => sendUserInvite(id),
      'Failed to send invites to selected employees.',
    );
  }, [selectedIds]);

  const requestDeactivate = useCallback((ids: string[]) => {
    if (ids.length === 0) {
      return;
    }
    setPendingDeactivateIds(ids);
    setDeactivateConfirmOpen(true);
  }, []);

  const handleConfirmDeactivate = useCallback(async () => {
    await runBulkAction(
      pendingDeactivateIds,
      (id) => setUserActive(id, { isActive: false }),
      'Failed to deactivate selected employees.',
    );
    setPendingDeactivateIds([]);
    setDeactivateConfirmOpen(false);
  }, [pendingDeactivateIds]);

  const deactivateMessage = pendingDeactivateIds.length === 1
    ? `This will deactivate ${employees.find((employee) => employee.id === pendingDeactivateIds[0])?.displayName ?? 'the selected employee'}`
    : `This will deactivate ${pendingDeactivateIds.length} employees`;

  return (
    <div className="flex flex-col w-full h-full pt-3 overflow-y-hidden">
      <div className="flex justify-between mb-0 px-3">
        <Subtitle2 className="">Employee management</Subtitle2>
        <div className="flex justify-between mb-3 gap-2">
          <AppFilters filters={filters} onFilterChange={() => {}} />
          <CreateEmployee onCreated={handleUpdated} />
        </div>
      </div>

      {!canManageUsers && (
        <div className="px-3 pb-3">
          <MessageBar intent="warning">
            <MessageBarBody>
              You need user read access to view the full employee directory.
            </MessageBarBody>
          </MessageBar>
        </div>
      )}

      <div className="flex flex-col w-full h-full rounded shadow overflow-hidden" style={{ backgroundColor: tokens.colorNeutralBackground1 }}>
        <div
          className="p-3 border-b flex justify-between items-center gap-3"
          style={{ borderBottomColor: tokens.colorNeutralStroke3 }}
        >
          <Subtitle2>Your Employees</Subtitle2>
          <div className="flex items-center gap-3">
            {selectedIds.length > 0 && canEditUsers && (
              <div className="flex items-center gap-2">
                <span className="text-sm text-neutral-foreground-3">
                  {selectedIds.length} selected
                </span>
                {selectedEmployees.some((employee) => !employee.isActive) && (
                  <Button
                    appearance="secondary"
                    size="small"
                    icon={<PersonRegular />}
                    disabled={isBulkWorking}
                    onClick={() => void handleActivateSelected()}
                  >
                    Activate
                  </Button>
                )}
                {selectedEmployees.some((employee) => employee.isActive) && (
                  <Button
                    appearance="secondary"
                    size="small"
                    icon={<PersonProhibitedRegular />}
                    disabled={isBulkWorking}
                    onClick={() => requestDeactivate(selectedIds)}
                  >
                    Deactivate
                  </Button>
                )}
                <Button
                  appearance="secondary"
                  size="small"
                  icon={<MailRegular />}
                  disabled={isBulkWorking}
                  onClick={() => void handleSendInviteSelected()}
                >
                  Send invite
                </Button>
              </div>
            )}
            {!isLoading && !error && (
              <span className="text-sm text-neutral-foreground-3">{visibleEmployees.length} employees</span>
            )}
          </div>
        </div>

        {actionError && (
          <div className="px-3 pt-3">
            <MessageBar intent="error">
              <MessageBarBody>{actionError}</MessageBarBody>
            </MessageBar>
          </div>
        )}

        <EmployeesTable
          items={visibleEmployees}
          isLoading={isLoading}
          error={error}
          selectedIds={selectedIds}
          onSelectionChange={setSelectedIds}
          onViewDetails={setDetailsEmployee}
          onEdit={setEditEmployee}
          onActivate={(employee) => void runBulkAction([employee.id], (id) => setUserActive(id, { isActive: true }), 'Failed to activate employee.')}
          onDeactivate={(employee) => requestDeactivate([employee.id])}
          onSendInvite={(employee) => void runBulkAction([employee.id], (id) => sendUserInvite(id), 'Failed to send invite.')}
          onResetPassword={setResetPasswordUser}
          onClearInvitePending={(employee) => void runBulkAction(
            [employee.id],
            (id) => setUserInvitePending(id, { invitePending: false }),
            'Failed to clear invite pending.',
          )}
          onMarkInvitePending={(employee) => void runBulkAction(
            [employee.id],
            (id) => setUserInvitePending(id, { invitePending: true }),
            'Failed to mark invite pending.',
          )}
          onStartLeaveAdjustment={(employee) => setLeaveAdjustmentUser(employee)}
        />
      </div>

      <EmployeeDetailsDialog
        employee={detailsEmployee}
        open={detailsEmployee !== null}
        onClose={() => setDetailsEmployee(null)}
        onEdit={(employee) => {
          setDetailsEmployee(null);
          setEditEmployee(employee);
        }}
      />

      <AdminResetPasswordDialog
        user={resetPasswordUser}
        open={resetPasswordUser !== null}
        onOpenChange={(open) => {
          if (!open) {
            setResetPasswordUser(null);
          }
        }}
        onSaved={handleUpdated}
      />

      <AdjustUserLeaveBalancesDialog
            open={leaveAdjustmentUser !== null}
            user={leaveAdjustmentUser}
            onClose={() => setLeaveAdjustmentUser(null)}
          />

      <EmployeeFormDialog
        employee={editEmployee}
        open={editEmployee !== null}
        onOpenChange={(open) => {
          if (!open) {
            setEditEmployee(null);
          }
        }}
        onSaved={handleUpdated}
      />

      <ConfirmAction
        open={deactivateConfirmOpen}
        onOpenChange={setDeactivateConfirmOpen}
        title="Deactivate employees"
        message={deactivateMessage}
        actionName="Deactivate"
        destructive
        onAction={() => void handleConfirmDeactivate()}
        onCancel={() => setPendingDeactivateIds([])}
      />
    </div>
  );
};

export default EmployeesList;
