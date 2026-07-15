import { useCallback, useEffect, useMemo, useState } from 'react';
import type { TableColumnDefinition } from '@fluentui/react-components';
import {
  Badge,
  Button,
  Checkbox,
  DataGrid,
  DataGridBody,
  DataGridCell,
  DataGridHeader,
  DataGridHeaderCell,
  DataGridRow,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Dropdown,
  Field,
  Input,
  Option,
  Spinner,
  Subtitle2,
  TableCellLayout,
  Text,
  createTableColumn,
} from '@fluentui/react-components';
import { AddRegular, EditRegular, PersonAddRegular } from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import { getDrivers, type DirectoryDriver } from '@platform/org/directoryApi';
import {
  createUser,
  getManagerOptions,
  getUser,
  getUserOrg,
  getUsers,
  sendUserInvite,
  setUserActive,
  setUserRoles,
  updateUser,
} from '@modules/users/services/userService';
import type {
  CreateUserRequest,
  ManagerOption,
  UpdateUserRequest,
  UserListItem,
  UserOrg,
} from '@modules/users/types/user';
import { APP_ROLES } from '@modules/users/types/user';
import { usePermissions } from '@platform/permissions/usePermissions';
import { UserOrgChart } from './UserOrgChart';
import { stopDataGridRowSelection } from '@platform/utils/dataGrid';
import { matchesSearchQuery } from '@platform/search/searchText';

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

function filterUsers(users: UserListItem[], query: string): UserListItem[] {
  const normalized = query.trim();
  if (!normalized) {
    return users;
  }

  return users.filter((user) => matchesSearchQuery(normalized, [
    user.displayName,
    user.username,
    user.email,
    user.managerDisplayName,
    ...user.roles,
  ]));
}

interface UserFormState {
  username: string;
  email: string;
  password: string;
  sendInvite: boolean;
  displayName: string;
  firstName: string;
  lastName: string;
  roles: string[];
  isActive: boolean;
  employeeNumber: string;
  jobTitle: string;
  department: string;
  branch: string;
  managerUserId: string;
  driverId: string;
}

const emptyFormState = (): UserFormState => ({
  username: '',
  email: '',
  password: '',
  sendInvite: false,
  displayName: '',
  firstName: '',
  lastName: '',
  roles: ['Staff'],
  isActive: true,
  employeeNumber: '',
  jobTitle: '',
  department: '',
  branch: '',
  managerUserId: '',
  driverId: '',
});

export function UsersSettingsPanel({ searchQuery = '' }: { searchQuery?: string }) {
  const { canEditUsers } = usePermissions();
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<UserListItem | null>(null);
  const [formState, setFormState] = useState<UserFormState>(emptyFormState);
  const [formError, setFormError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [managers, setManagers] = useState<ManagerOption[]>([]);
  const [drivers, setDrivers] = useState<DirectoryDriver[]>([]);
  const [userOrg, setUserOrg] = useState<UserOrg | null>(null);

  const loadUsers = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await getUsers();
      setUsers(response.items);
    } catch (loadError) {
      const message = loadError instanceof ApiError
        ? loadError.message
        : 'Failed to load users.';
      setError(message);
      setUsers([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadUsers();
  }, [loadUsers]);

  const visibleUsers = useMemo(
    () => filterUsers(users, searchQuery),
    [users, searchQuery],
  );

  const openCreateDialog = useCallback(async () => {
    setEditingUser(null);
    setFormState(emptyFormState());
    setFormError(null);
    setUserOrg(null);
    setDialogOpen(true);

    try {
      const [managerOptions, driverResponse] = await Promise.all([
        getManagerOptions(),
        getDrivers(),
      ]);
      setManagers(managerOptions);
      setDrivers(driverResponse.items);
    } catch {
      setManagers([]);
      setDrivers([]);
    }
  }, []);

  const openEditDialog = useCallback(async (user: UserListItem) => {
    setEditingUser(user);
    setFormError(null);
    setUserOrg(null);
    setDialogOpen(true);

    try {
      const [managerOptions, driverResponse, detail, org] = await Promise.all([
        getManagerOptions(user.id),
        getDrivers(),
        getUser(user.id),
        getUserOrg(user.id),
      ]);
      setManagers(managerOptions);
      setDrivers(driverResponse.items);
      setUserOrg(org);

      setFormState({
        username: detail.username,
        email: detail.email,
        password: '',
        sendInvite: false,
        displayName: detail.displayName ?? '',
        firstName: detail.firstName ?? '',
        lastName: detail.lastName ?? '',
        roles: [...detail.roles],
        isActive: detail.isActive,
        employeeNumber: detail.staffProfile?.employeeNumber ?? '',
        jobTitle: detail.staffProfile?.jobTitle ?? '',
        department: detail.staffProfile?.department ?? '',
        branch: detail.staffProfile?.branch ?? '',
        managerUserId: detail.staffProfile?.managerUserId ?? '',
        driverId: detail.driverLink?.driverId ?? '',
      });
    } catch {
      setManagers([]);
      setDrivers([]);
    }
  }, []);

  const columns = useMemo<TableColumnDefinition<UserListItem>[]>(() => [
    createTableColumn<UserListItem>({
      columnId: 'name',
      renderHeaderCell: () => 'Name',
      renderCell: (item) => (
        <TableCellLayout>
          <div className='flex flex-col'>
             <Text weight="semibold">{item.displayName ?? item.username}</Text>
             <Text size={200}>{item.email}</Text>
          </div>
        </TableCellLayout>
      ),
    }),
    createTableColumn<UserListItem>({
      columnId: 'roles',
      renderHeaderCell: () => 'Roles',
      renderCell: (item) => (
        <div className="flex flex-wrap gap-1">
          {item.roles.map((role) => (
            <Badge key={role} appearance="outline" size="small">{role}</Badge>
          ))}
        </div>
      ),
    }),
    createTableColumn<UserListItem>({
      columnId: 'manager',
      renderHeaderCell: () => 'Manager',
      renderCell: (item) => item.managerDisplayName ?? '—',
    }),
    createTableColumn<UserListItem>({
      columnId: 'active',
      renderHeaderCell: () => 'Active',
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
      renderHeaderCell: () => 'Last login',
      renderCell: (item) => formatDateTime(item.lastLoginAt),
    }),
    createTableColumn<UserListItem>({
      columnId: 'actions',
      renderHeaderCell: () => '',
      renderCell: (item) => canEditUsers ? (
        <Button
          appearance="subtle"
          icon={<EditRegular />}
          onClick={(event) => {
            event.stopPropagation();
            void openEditDialog(item);
          }}
        >
          Edit
        </Button>
      ) : null,
    }),
  ], [canEditUsers, openEditDialog]);

  async function handleSave() {
    setFormError(null);
    setIsSaving(true);

    try {
      if (editingUser) {
        const request: UpdateUserRequest = {
          email: formState.email.trim(),
          displayName: formState.displayName.trim() || null,
          firstName: formState.firstName.trim() || null,
          lastName: formState.lastName.trim() || null,
          staff: {
            employeeNumber: formState.employeeNumber.trim() || null,
            jobTitle: formState.jobTitle.trim() || null,
            department: formState.department.trim() || null,
            branch: formState.branch.trim() || null,
            managerUserId: formState.managerUserId || null,
          },
          driverId: formState.driverId || null,
          clearDriverLink: !formState.driverId,
        };

        await updateUser(editingUser.id, request);
        await setUserRoles(editingUser.id, { roles: formState.roles });
        if (editingUser.isActive !== formState.isActive) {
          await setUserActive(editingUser.id, { isActive: formState.isActive });
        }
      } else {
        const request: CreateUserRequest = {
          username: formState.username.trim(),
          email: formState.email.trim(),
          password: formState.sendInvite ? null : formState.password,
          displayName: formState.displayName.trim() || null,
          firstName: formState.firstName.trim() || null,
          lastName: formState.lastName.trim() || null,
          roles: formState.roles,
          isActive: formState.isActive,
          sendInvite: formState.sendInvite,
          staff: {
            employeeNumber: formState.employeeNumber.trim() || null,
            jobTitle: formState.jobTitle.trim() || null,
            department: formState.department.trim() || null,
            branch: formState.branch.trim() || null,
            managerUserId: formState.managerUserId || null,
          },
          driverId: formState.driverId || null,
        };

        await createUser(request);
      }

      setDialogOpen(false);
      await loadUsers();
    } catch (saveError) {
      const message = saveError instanceof ApiError
        ? saveError.message
        : 'Failed to save user.';
      setFormError(message);
    } finally {
      setIsSaving(false);
    }
  }

  function toggleRole(role: string, checked: boolean) {
    setFormState((current) => ({
      ...current,
      roles: checked
        ? [...new Set([...current.roles, role])]
        : current.roles.filter((value) => value !== role),
    }));
  }

  return (
    <div className="flex flex-col gap-4 max-w-6xl">
      <div className="flex items-center justify-between gap-3">
        <div className='flex flex-col'>
          <Subtitle2>Users & access</Subtitle2>
          <Text className="text-sm text-neutral-foreground-3 block mt-1">
            Create platform users, assign roles, and link driver profiles.
          </Text>
        </div>
        {canEditUsers && (
          <Button appearance="primary" icon={<PersonAddRegular />} onClick={() => void openCreateDialog()}>
            Add user
          </Button>
        )}
      </div>

      {isLoading && <Spinner label="Loading users..." />}
      {error && <Text className="text-sm text-red-600">{error}</Text>}

      {!isLoading && !error && (
        <DataGrid
          items={visibleUsers}
          columns={columns}
          getRowId={(item) => item.id}
          onSelectionChange={stopDataGridRowSelection}
        >
          <DataGridHeader>
            <DataGridRow>
              {({ renderHeaderCell }) => (
                <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
              )}
            </DataGridRow>
          </DataGridHeader>
          <DataGridBody<UserListItem>>
            {({ item, rowId }) => (
              <DataGridRow<UserListItem> key={rowId}>
                {({ renderCell }) => (
                  <DataGridCell>{renderCell(item)}</DataGridCell>
                )}
              </DataGridRow>
            )}
          </DataGridBody>
        </DataGrid>
      )}

      <Dialog open={dialogOpen} onOpenChange={(_, data) => setDialogOpen(data.open)}>
        <DialogSurface className="max-w-2xl">
          <DialogBody>
            <DialogTitle>{editingUser ? 'Edit user' : 'Create user'}</DialogTitle>
            <DialogContent className="flex flex-col gap-3 pt-2">
              {!editingUser && (
                <Field label="Username" required>
                  <Input
                    value={formState.username}
                    onChange={(_, data) => setFormState((current) => ({ ...current, username: data.value }))}
                  />
                </Field>
              )}

              <Field label="Email" required>
                <Input
                  type="email"
                  value={formState.email}
                  onChange={(_, data) => setFormState((current) => ({ ...current, email: data.value }))}
                />
              </Field>

              {!editingUser && (
                <>
                  <Checkbox
                    checked={formState.sendInvite}
                    onChange={(_, data) => setFormState((current) => ({
                      ...current,
                      sendInvite: Boolean(data.checked),
                      password: data.checked ? '' : current.password,
                    }))}
                    label="Send email invite instead of setting a password"
                  />

                  {!formState.sendInvite && (
                    <Field label="Temporary password" required>
                      <Input
                        type="password"
                        value={formState.password}
                        onChange={(_, data) => setFormState((current) => ({ ...current, password: data.value }))}
                      />
                    </Field>
                  )}
                </>
              )}

              {editingUser?.invitePendingAt && canEditUsers && (
                <Button
                  appearance="secondary"
                  onClick={async () => {
                    if (!editingUser) {
                      return;
                    }

                    setIsSaving(true);
                    setFormError(null);

                    try {
                      await sendUserInvite(editingUser.id);
                      await loadUsers();
                      setFormError(null);
                    } catch (inviteError) {
                      const message = inviteError instanceof ApiError
                        ? inviteError.message
                        : 'Failed to resend invite.';
                      setFormError(message);
                    } finally {
                      setIsSaving(false);
                    }
                  }}
                  disabled={isSaving}
                >
                  Resend invite email
                </Button>
              )}

              <div className="grid grid-cols-2 gap-3">
                <Field label="Display name">
                  <Input
                    value={formState.displayName}
                    onChange={(_, data) => setFormState((current) => ({ ...current, displayName: data.value }))}
                  />
                </Field>
                <Field label="Active">
                  <Checkbox
                    checked={formState.isActive}
                    onChange={(_, data) => setFormState((current) => ({ ...current, isActive: Boolean(data.checked) }))}
                    label="User can sign in"
                  />
                </Field>
              </div>

              <Field label="Roles">
                <div className="grid grid-cols-2 gap-2">
                  {APP_ROLES.map((role) => (
                    <Checkbox
                      key={role}
                      checked={formState.roles.includes(role)}
                      onChange={(_, data) => toggleRole(role, Boolean(data.checked))}
                      label={role}
                    />
                  ))}
                </div>
              </Field>

              <Subtitle2 className="mt-2">Staff profile</Subtitle2>
              <div className="grid grid-cols-2 gap-3">
                <Field label="Employee number">
                  <Input
                    value={formState.employeeNumber}
                    onChange={(_, data) => setFormState((current) => ({ ...current, employeeNumber: data.value }))}
                  />
                </Field>
                <Field label="Job title">
                  <Input
                    value={formState.jobTitle}
                    onChange={(_, data) => setFormState((current) => ({ ...current, jobTitle: data.value }))}
                  />
                </Field>
                <Field label="Department">
                  <Input
                    value={formState.department}
                    onChange={(_, data) => setFormState((current) => ({ ...current, department: data.value }))}
                  />
                </Field>
                <Field label="Branch">
                  <Input
                    value={formState.branch}
                    onChange={(_, data) => setFormState((current) => ({ ...current, branch: data.value }))}
                  />
                </Field>
              </div>

              <Field label="Manager">
                <Dropdown
                  placeholder="Select a manager"
                  value={managers.find((manager) => manager.id === formState.managerUserId)?.displayName ?? ''}
                  selectedOptions={formState.managerUserId ? [formState.managerUserId] : []}
                  onOptionSelect={(_, data) => setFormState((current) => ({
                    ...current,
                    managerUserId: data.optionValue ?? '',
                  }))}
                >
                  <Option value="">No manager</Option>
                  {managers.map((manager) => (
                    <Option key={manager.id} value={manager.id} text={manager.displayName}>
                      {manager.displayName}
                    </Option>
                  ))}
                </Dropdown>
              </Field>

              <Field label="Linked driver">
                <Dropdown
                  placeholder="Link to driver record"
                  value={drivers.find((driver) => driver.id === formState.driverId)?.name ?? ''}
                  selectedOptions={formState.driverId ? [formState.driverId] : []}
                  onOptionSelect={(_, data) => setFormState((current) => ({
                    ...current,
                    driverId: data.optionValue ?? '',
                  }))}
                >
                  <Option value="">No driver link</Option>
                  {drivers.map((driver) => (
                    <Option key={driver.id} value={driver.id} text={driver.name}>
                      {driver.name} ({driver.driverId})
                    </Option>
                  ))}
                </Dropdown>
              </Field>

              {editingUser && userOrg && (
                <div className="rounded border border-[#e3e5e7] p-3 bg-neutral-background-2">
                  {userOrg.manager && (
                    <Text className="text-sm block mb-2">
                      Manager: <strong>{userOrg.manager.displayName}</strong>
                    </Text>
                  )}
                  {userOrg.directReports.length > 0 && (
                    <Text className="text-sm block mb-2">
                      Direct reports: {userOrg.directReports.map((report) => report.displayName).join(', ')}
                    </Text>
                  )}
                  <UserOrgChart tree={userOrg.orgTree} />
                </div>
              )}

              {formError && <Text className="text-sm text-red-600">{formError}</Text>}
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setDialogOpen(false)} disabled={isSaving}>
                Cancel
              </Button>
              <Button appearance="primary" onClick={() => void handleSave()} disabled={isSaving}>
                {isSaving ? 'Saving...' : 'Save'}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
}
