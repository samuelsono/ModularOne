import * as React from 'react';
import {
  Avatar,
  Badge,
  Button,
  DataGrid,
  DataGridBody,
  DataGridCell,
  DataGridHeader,
  DataGridHeaderCell,
  DataGridRow,
  Menu,
  MenuDivider,
  MenuItem,
  MenuList,
  MenuPopover,
  MenuTrigger,
  MessageBar,
  MessageBarBody,
  Spinner,
  TableCellLayout,
  Tooltip,
  createTableColumn,
} from '@fluentui/react-components';
import type { JSXElement, TableColumnDefinition, TableRowId } from '@fluentui/react-components';
import {
  CalendarCheckmarkRegular,
  EditRegular,
  EyeRegular,
  KeyRegular,
  MailCheckmarkRegular,
  MailDismissRegular,
  MailRegular,
  MoreHorizontalRegular,
  PersonAccountsRegular,
  PersonProhibitedRegular,
  PersonRegular,
} from '@fluentui/react-icons';
import type { UserListItem } from '@modules/users/types/user';
import { stopDataGridRowSelection } from '@platform/utils/dataGrid';
import { runAfterMenuDismiss } from '@platform/utils/runAfterMenuDismiss';
import { usePersistedColumnSizing } from '@platform/utils/usePersistedColumnSizing';
import { usePermissions } from '@platform/permissions/usePermissions';
import { withAuditableColumns } from '@platform/ui/auditTableColumns';
import { useActiveApp } from '@platform/shell/ActiveAppContext';

interface EmployeesTableProps {
  items: UserListItem[];
  isLoading?: boolean;
  error?: string | null;
  selectedIds?: Iterable<TableRowId>;
  onSelectionChange?: (selectedIds: string[]) => void;
  onViewDetails?: (employee: UserListItem) => void;
  onEdit?: (employee: UserListItem) => void;
  onActivate?: (employee: UserListItem) => void;
  onDeactivate?: (employee: UserListItem) => void;
  onSendInvite?: (employee: UserListItem) => void;
  onResetPassword?: (employee: UserListItem) => void;
  onClearInvitePending?: (employee: UserListItem) => void;
  onMarkInvitePending?: (employee: UserListItem) => void;
  onStartLeaveAdjustment?: (employee: UserListItem) => void;
}

const clickableName: React.CSSProperties = {
  cursor: 'pointer',
  color: 'var(--colorBrandForeground3)',
  fontWeight: 500,
  border: 'none',
  background: 'transparent',
  padding: 0,
  font: 'inherit',
  textAlign: 'left',
};

function formatDateTime(value: string | null): string {
  if (!value) {
    return '—';
  }

  return new Date(value).toLocaleString('en-ZA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

const EmployeeActions = ({
  item,
  canEdit,
  canAdjustLeaveBalances,
  onViewDetails,
  onEdit,
  onActivate,
  onDeactivate,
  onSendInvite,
  onResetPassword,
  onClearInvitePending,
  onMarkInvitePending,
  onStartLeaveAdjustment,
}: {
  item: UserListItem;
  canEdit: boolean;
  canAdjustLeaveBalances?: boolean;
  onViewDetails?: (employee: UserListItem) => void;
  onEdit?: (employee: UserListItem) => void;
  onActivate?: (employee: UserListItem) => void;
  onDeactivate?: (employee: UserListItem) => void;
  onSendInvite?: (employee: UserListItem) => void;
  onResetPassword?: (employee: UserListItem) => void;
  onClearInvitePending?: (employee: UserListItem) => void;
  onMarkInvitePending?: (employee: UserListItem) => void;
  onStartLeaveAdjustment?: (employee: UserListItem) => void;
}) => (
  <div
    className="flex items-center gap-0.5"
    onClick={stopDataGridRowSelection}
    onKeyDown={stopDataGridRowSelection}
  >
    {canEdit && (
      <Button
        appearance="subtle"
        size="small"
        icon={<EditRegular />}
        aria-label={`Edit ${item.displayName ?? item.username}`}
        onClick={() => onEdit?.(item)}
      />
    )}
    <Menu>
      <MenuTrigger disableButtonEnhancement>
        <Tooltip content="More actions" relationship="label">
          <Button
            icon={<MoreHorizontalRegular />}
            appearance="subtle"
            size="small"
            aria-label="More actions"
          />
        </Tooltip>
      </MenuTrigger>
      <MenuPopover>
        <MenuList>
          <MenuItem
            icon={<EyeRegular />}
            onClick={() => runAfterMenuDismiss(() => onViewDetails?.(item))}
          >
            View details
          </MenuItem>
          {canEdit && item.isActive && (
            <MenuItem
              icon={<PersonProhibitedRegular />}
              onClick={() => runAfterMenuDismiss(() => onDeactivate?.(item))}
            >
              Deactivate
            </MenuItem>
          )}
          {canEdit && !item.isActive && (
            <MenuItem icon={<PersonRegular />} onClick={() => onActivate?.(item)}>
              Activate
            </MenuItem>
          )}
          {canEdit && (
            <>
              <MenuDivider />
              <MenuItem
                icon={<KeyRegular />}
                onClick={() => runAfterMenuDismiss(() => onResetPassword?.(item))}
              >
                Reset password
              </MenuItem>
              <MenuItem icon={<MailRegular />} onClick={() => onSendInvite?.(item)}>
                Send invite
              </MenuItem>
              {item.invitePendingAt ? (
                <MenuItem icon={<MailDismissRegular />} onClick={() => onClearInvitePending?.(item)}>
                  Clear invite pending
                </MenuItem>
              ) : (
                <MenuItem icon={<MailCheckmarkRegular />} onClick={() => onMarkInvitePending?.(item)}>
                  Mark invite pending
                </MenuItem>
              )}
            </>
          )}
          {canAdjustLeaveBalances && (
            <MenuItem
              icon={<CalendarCheckmarkRegular />}
              onClick={() => runAfterMenuDismiss(() => onStartLeaveAdjustment?.(item))}
            >
              Adjust leave balances
            </MenuItem>
          )}
        </MenuList>
      </MenuPopover>
    </Menu>
  </div>
);

export function EmployeesTable({
  items,
  isLoading = false,
  error = null,
  selectedIds,
  onSelectionChange,
  onViewDetails,
  onEdit,
  onActivate,
  onDeactivate,
  onSendInvite,
  onResetPassword,
  onClearInvitePending,
  onMarkInvitePending,
  onStartLeaveAdjustment,
}: EmployeesTableProps): JSXElement {
  const { canEditUsers, isAdmin, isHr } = usePermissions();
  const { visibleModules } = useActiveApp();
    const canAdjustLeave = (isAdmin || isHr)
        && visibleModules.some((module) => module.slug === 'leave');

  const columns = React.useMemo<TableColumnDefinition<UserListItem>[]>(() => withAuditableColumns([
    createTableColumn<UserListItem>({
      columnId: 'name',
      compare: (a, b) => (a.displayName ?? a.username).localeCompare(b.displayName ?? b.username),
      renderHeaderCell: () => 'Employee',
      renderCell: (item) => (
        <TableCellLayout media={<Avatar color={"colorful"} aria-label={item.displayName ?? item.username} name={item.displayName ?? item.username} />}>
          <button
            type="button"
            onClick={(event) => {
              stopDataGridRowSelection(event);
              onViewDetails?.(item);
            }}
            style={clickableName}
          >
            {item.displayName ?? item.username}
          </button>
        </TableCellLayout>
      ),
    }),
    createTableColumn<UserListItem>({
      columnId: 'email',
      compare: (a, b) => a.email.localeCompare(b.email),
      renderHeaderCell: () => 'Email',
      renderCell: (item) => item.email,
    }),
    createTableColumn<UserListItem>({
      columnId: 'company',
      compare: (a, b) => (a.companyName ?? '').localeCompare(b.companyName ?? ''),
      renderHeaderCell: () => 'Company',
      renderCell: (item) => item.companyName ?? '—',
    }),
    createTableColumn<UserListItem>({
      columnId: 'department',
      compare: (a, b) => (a.departmentName ?? '').localeCompare(b.departmentName ?? ''),
      renderHeaderCell: () => 'Department',
      renderCell: (item) => item.departmentName ?? '—',
    }),
    createTableColumn<UserListItem>({
      columnId: 'position',
      compare: (a, b) => (a.positionName ?? '').localeCompare(b.positionName ?? ''),
      renderHeaderCell: () => 'Position',
      renderCell: (item) => item.positionName ?? '—',
    }),
    createTableColumn<UserListItem>({
      columnId: 'roles',
      renderHeaderCell: () => 'Roles',
      renderCell: (item) => (
        <div className="flex flex-wrap gap-1">
          {item.roles.map((role) => (
            <Badge key={role} appearance={"filled"} size="small">{role}</Badge>
          ))}
        </div>
      ),
    }),
    createTableColumn<UserListItem>({
      columnId: 'manager',
      compare: (a, b) => (a.managerDisplayName ?? '').localeCompare(b.managerDisplayName ?? ''),
      renderHeaderCell: () => 'Manager',
      renderCell: (item) => item.managerDisplayName ?? '—',
    }),
    createTableColumn<UserListItem>({
      columnId: 'status',
      renderHeaderCell: () => 'Status',
      renderCell: (item) => (
        <div className="flex flex-wrap gap-1">
          <Badge appearance={item.isActive ? 'filled' : 'outline'} color={item.isActive ? 'success' : 'subtle'}>
            {item.isActive ? 'Active' : 'Inactive'}
          </Badge>
          {item.invitePendingAt && (
            <Badge appearance="outline" size="small">Invite pending</Badge>
          )}
        </div>
      ),
    }),
    createTableColumn<UserListItem>({
      columnId: 'lastLogin',
      compare: (a, b) => (a.lastLoginAt ?? '').localeCompare(b.lastLoginAt ?? ''),
      renderHeaderCell: () => 'Last login',
      renderCell: (item) => formatDateTime(item.lastLoginAt),
    }),
    createTableColumn<UserListItem>({
      columnId: 'actions',
      renderHeaderCell: () => 'Actions',
      renderCell: (item) => (
        <EmployeeActions
          item={item}
          canEdit={canEditUsers}
          onViewDetails={onViewDetails}
          onEdit={onEdit}
          onActivate={onActivate}
          onDeactivate={onDeactivate}
          onSendInvite={onSendInvite}
          onResetPassword={onResetPassword}
          onClearInvitePending={onClearInvitePending}
          onMarkInvitePending={onMarkInvitePending}
          onStartLeaveAdjustment={onStartLeaveAdjustment}
          canAdjustLeaveBalances={canAdjustLeave}
        />
      ),
    }),
  ]), [canAdjustLeave, canEditUsers, onActivate, onClearInvitePending, onDeactivate, onEdit, onMarkInvitePending, onResetPassword, onSendInvite, onStartLeaveAdjustment, onViewDetails]);

  const { columnSizingOptions, onColumnResize } = usePersistedColumnSizing(
    'users.employees',
    columns,
  );

  if (isLoading) {
    return (
      <div className="flex justify-center p-6">
        <Spinner size="medium" label="Loading employees..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-3">
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="h-full w-full flex flex-col gap-3 items-center justify-center p-6 text-sm text-neutral-foreground-3">
        <PersonAccountsRegular className="size-26 text-gray-300" />
        No employees found matching query.
      </div>
    );
  }

  return (
    <div style={{ overflowX: 'auto', width: '100%' }}>
      <DataGrid
        items={items}
        columns={columns}
        sortable
        selectionMode="multiselect"
        getRowId={(item) => item.id}
        selectedItems={selectedIds}
        onSelectionChange={(_, data) => {
          onSelectionChange?.(Array.from(data.selectedItems, String));
        }}
        focusMode="composite"
        size="medium"
        // style={{ minWidth: '900px' }}
        resizableColumns
        columnSizingOptions={columnSizingOptions}
        onColumnResize={onColumnResize}
        resizableColumnsOptions={{ autoFitColumns: false }}
      >
        <DataGridHeader>
          <DataGridRow
            selectionCell={{
              checkboxIndicator: { 'aria-label': 'Select all rows' },
            }}
          >
            {({ renderHeaderCell }) => (
              <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
            )}
          </DataGridRow>
        </DataGridHeader>
        <DataGridBody<UserListItem>>
          {({ item, rowId }) => (
            <DataGridRow<UserListItem>
              key={rowId}
              selectionCell={{
                checkboxIndicator: { 'aria-label': 'Select row' },
              }}
            >
              {({ renderCell }) => (
                <DataGridCell onClick={stopDataGridRowSelection}>
                  {renderCell(item)}
                </DataGridCell>
              )}
            </DataGridRow>
          )}
        </DataGridBody>
      </DataGrid>
    </div>
  );
}
