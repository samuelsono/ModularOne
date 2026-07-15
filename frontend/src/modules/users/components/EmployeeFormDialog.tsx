import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Input,
  Option,
  Spinner,
  Subtitle2,
  Text,
  Dropdown,
} from '@fluentui/react-components';
import { ApiError } from '@platform/api/apiClient';
import {
  createUser,
  getManagerOptions,
  getUser,
  sendUserInvite,
  setUserActive,
  setUserRoles,
  updateUser,
} from '@modules/users/services/userService';
import type { CreateUserRequest, ManagerOption, UpdateUserRequest, UserListItem } from '@modules/users/types/user';
import { APP_ROLES } from '@modules/users/types/user';
import { usePermissions } from '@platform/permissions/usePermissions';
import {
  getCompanies,
  getDepartments,
  getPositions,
  type OrgCompany,
  type OrgDepartment,
  type OrgPosition,
} from '@platform/org/orgApi';

interface EmployeeFormState {
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
  companyId: string;
  departmentId: string;
  positionId: string;
}

interface EmployeeFormDialogProps {
  employee?: UserListItem | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSaved?: () => void;
}

const emptyFormState = (): EmployeeFormState => ({
  username: '',
  email: '',
  password: '',
  sendInvite: true,
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
  companyId: '',
  departmentId: '',
  positionId: '',
});

export function EmployeeFormDialog({
  employee = null,
  open,
  onOpenChange,
  onSaved,
}: EmployeeFormDialogProps) {
  const isCreate = employee === null;
  const { canEditUsers } = usePermissions();
  const [formState, setFormState] = useState<EmployeeFormState>(emptyFormState);
  const [managers, setManagers] = useState<ManagerOption[]>([]);
  const [companies, setCompanies] = useState<OrgCompany[]>([]);
  const [departments, setDepartments] = useState<OrgDepartment[]>([]);
  const [positions, setPositions] = useState<OrgPosition[]>([]);
  const [formError, setFormError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  const loadOrganizationOptions = useCallback(async () => {
    try {
      const [companyItems, departmentItems, positionItems] = await Promise.all([
        getCompanies(),
        getDepartments(),
        getPositions(),
      ]);
      setCompanies(companyItems.filter((item) => item.isActive));
      setDepartments(departmentItems.filter((item) => item.isActive));
      setPositions(positionItems.filter((item) => item.isActive));
    } catch {
      setCompanies([]);
      setDepartments([]);
      setPositions([]);
    }
  }, []);

  const availableDepartments = useMemo(
    () => (formState.companyId
      ? departments.filter((department) => department.companyId === formState.companyId)
      : departments),
    [departments, formState.companyId],
  );

  const availablePositions = useMemo(
    () => (formState.departmentId
      ? positions.filter((position) => position.departmentId === formState.departmentId)
      : positions),
    [formState.departmentId, positions],
  );

  const loadManagers = useCallback(async (excludeUserId?: string) => {
    try {
      const managerOptions = await getManagerOptions(excludeUserId);
      setManagers(managerOptions);
    } catch {
      setManagers([]);
    }
  }, []);

  const loadEditForm = useCallback(async () => {
    if (!employee) {
      return;
    }

    setIsLoading(true);
    setFormError(null);

    try {
      const [detail, managerOptions] = await Promise.all([
        getUser(employee.id),
        getManagerOptions(employee.id),
      ]);
      setManagers(managerOptions);
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
        companyId: detail.staffProfile?.companyId ?? '',
        departmentId: detail.staffProfile?.departmentId ?? '',
        positionId: detail.staffProfile?.positionId ?? '',
      });
    } catch (loadError) {
      const message = loadError instanceof ApiError
        ? loadError.message
        : 'Failed to load employee.';
      setFormError(message);
    } finally {
      setIsLoading(false);
    }
  }, [employee]);

  useEffect(() => {
    if (!open) {
      setFormState(emptyFormState());
      setFormError(null);
      return;
    }

    if (isCreate) {
      setFormState(emptyFormState());
      setFormError(null);
      void loadManagers();
      void loadOrganizationOptions();
      return;
    }

    void loadOrganizationOptions();
    void loadEditForm();
  }, [employee, isCreate, loadEditForm, loadManagers, loadOrganizationOptions, open]);

  async function handleSave(event?: FormEvent) {
    event?.preventDefault();

    if (!canEditUsers) {
      return;
    }

    if (isCreate) {
      if (!formState.username.trim() || !formState.email.trim()) {
        setFormError('Username and email are required.');
        return;
      }

      if (!formState.sendInvite && !formState.password) {
        setFormError('Password is required unless sending an invite.');
        return;
      }
    } else if (!employee) {
      return;
    }

    setFormError(null);
    setIsSaving(true);

    try {
      const staff = {
        employeeNumber: formState.employeeNumber.trim() || null,
        jobTitle: formState.jobTitle.trim() || null,
        department: formState.department.trim() || null,
        branch: formState.branch.trim() || null,
        managerUserId: formState.managerUserId || null,
        companyId: formState.companyId || null,
        departmentId: formState.departmentId || null,
        positionId: formState.positionId || null,
      };

      if (isCreate) {
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
          staff,
        };

        await createUser(request);
      } else {
        const request: UpdateUserRequest = {
          email: formState.email.trim(),
          displayName: formState.displayName.trim() || null,
          firstName: formState.firstName.trim() || null,
          lastName: formState.lastName.trim() || null,
          staff,
        };

        await updateUser(employee.id, request);
        await setUserRoles(employee.id, { roles: formState.roles });
        if (employee.isActive !== formState.isActive) {
          await setUserActive(employee.id, { isActive: formState.isActive });
        }
      }

      onOpenChange(false);
      onSaved?.();
    } catch (saveError) {
      const message = saveError instanceof ApiError
        ? saveError.message
        : isCreate ? 'Failed to create employee.' : 'Failed to save employee.';
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
    <Dialog
      open={open}
      modalType="modal"
      onOpenChange={(_, data) => onOpenChange(data.open)}
    >
      <DialogSurface aria-describedby={undefined} className="max-w-2xl">
        <form onSubmit={(event) => void handleSave(event)}>
          <DialogBody>
            <DialogTitle>{isCreate ? 'Add employee' : 'Edit employee'}</DialogTitle>
            <DialogContent className="flex flex-col gap-3 pt-2">
            {isLoading ? (
              <Spinner label="Loading employee..." />
            ) : (
              <>
                {isCreate && (
                  <Field label="Username" required>
                    <Input
                      value={formState.username}
                      onChange={(_, data) => setFormState((current) => ({ ...current, username: data.value }))}
                      disabled={!canEditUsers || isSaving}
                      autoComplete="off"
                    />
                  </Field>
                )}

                <Field label="Email" required>
                  <Input
                    type="email"
                    value={formState.email}
                    onChange={(_, data) => setFormState((current) => ({ ...current, email: data.value }))}
                    disabled={!canEditUsers || isSaving}
                  />
                </Field>

                {isCreate && (
                  <>
                    <Checkbox
                      checked={formState.sendInvite}
                      onChange={(_, data) => setFormState((current) => ({
                        ...current,
                        sendInvite: Boolean(data.checked),
                        password: data.checked ? '' : current.password,
                      }))}
                      label="Send email invite instead of setting a password"
                      disabled={!canEditUsers || isSaving}
                    />

                    {!formState.sendInvite && (
                      <Field label="Temporary password" required>
                        <Input
                          type="password"
                          value={formState.password}
                          onChange={(_, data) => setFormState((current) => ({ ...current, password: data.value }))}
                          disabled={!canEditUsers || isSaving}
                        />
                      </Field>
                    )}
                  </>
                )}

                <Field label="Display name">
                  <Input
                    value={formState.displayName}
                    onChange={(_, data) => setFormState((current) => ({ ...current, displayName: data.value }))}
                    disabled={!canEditUsers || isSaving}
                    autoComplete="off"
                  />
                </Field>

                <div className="grid grid-cols-2 gap-3">
                  <Field label="First name">
                    <Input
                      value={formState.firstName}
                      onChange={(_, data) => setFormState((current) => ({ ...current, firstName: data.value }))}
                      disabled={!canEditUsers || isSaving}
                      autoComplete="given-name"
                    />
                  </Field>
                  <Field label="Last name">
                    <Input
                      value={formState.lastName}
                      onChange={(_, data) => setFormState((current) => ({ ...current, lastName: data.value }))}
                      disabled={!canEditUsers || isSaving}
                      autoComplete="family-name"
                    />
                  </Field>
                </div>

                <Checkbox
                  checked={formState.isActive}
                  onChange={(_, data) => setFormState((current) => ({ ...current, isActive: Boolean(data.checked) }))}
                  label="User can sign in"
                  disabled={!canEditUsers || isSaving}
                />

                <Field label="Roles">
                  <div className="grid grid-cols-2 gap-2">
                    {APP_ROLES.map((role) => (
                      <Checkbox
                        key={role}
                        checked={formState.roles.includes(role)}
                        onChange={(_, data) => toggleRole(role, Boolean(data.checked))}
                        label={role}
                        disabled={!canEditUsers || isSaving}
                      />
                    ))}
                  </div>
                </Field>

                <Subtitle2 className="mt-2">Staff profile</Subtitle2>
                <div className="grid grid-cols-2 gap-3">
                  <Field label="Company">
                    <Dropdown
                      placeholder="Select company"
                      value={companies.find((company) => company.id === formState.companyId)?.name ?? ''}
                      selectedOptions={formState.companyId ? [formState.companyId] : []}
                      onOptionSelect={(_, data) => setFormState((current) => ({
                        ...current,
                        companyId: data.optionValue ?? '',
                        departmentId: '',
                        positionId: '',
                      }))}
                      disabled={!canEditUsers || isSaving}
                    >
                      <Option value="">No company</Option>
                      {companies.map((company) => (
                        <Option key={company.id} value={company.id} text={company.name}>{company.name}</Option>
                      ))}
                    </Dropdown>
                  </Field>
                  <Field label="Department">
                    <Dropdown
                      placeholder="Select department"
                      value={availableDepartments.find((department) => department.id === formState.departmentId)?.name ?? ''}
                      selectedOptions={formState.departmentId ? [formState.departmentId] : []}
                      onOptionSelect={(_, data) => setFormState((current) => ({
                        ...current,
                        departmentId: data.optionValue ?? '',
                        positionId: '',
                        companyId: availableDepartments.find((department) => department.id === data.optionValue)?.companyId ?? current.companyId,
                      }))}
                      disabled={!canEditUsers || isSaving}
                    >
                      <Option value="">No department</Option>
                      {availableDepartments.map((department) => (
                        <Option key={department.id} value={department.id} text={department.name}>{department.name}</Option>
                      ))}
                    </Dropdown>
                  </Field>
                  <Field label="Position">
                    <Dropdown
                      placeholder="Select position"
                      value={availablePositions.find((position) => position.id === formState.positionId)?.name ?? ''}
                      selectedOptions={formState.positionId ? [formState.positionId] : []}
                      onOptionSelect={(_, data) => {
                        const position = availablePositions.find((item) => item.id === data.optionValue);
                        setFormState((current) => ({
                          ...current,
                          positionId: data.optionValue ?? '',
                          departmentId: position?.departmentId ?? current.departmentId,
                          companyId: position?.companyId ?? current.companyId,
                          jobTitle: position?.name ?? current.jobTitle,
                        }));
                      }}
                      disabled={!canEditUsers || isSaving}
                    >
                      <Option value="">No position</Option>
                      {availablePositions.map((position) => (
                        <Option key={position.id} value={position.id} text={position.name}>{position.name}</Option>
                      ))}
                    </Dropdown>
                  </Field>
                  <Field label="Employee number">
                    <Input
                      value={formState.employeeNumber}
                      onChange={(_, data) => setFormState((current) => ({ ...current, employeeNumber: data.value }))}
                      disabled={!canEditUsers || isSaving}
                    />
                  </Field>
                  <Field label="Job title">
                    <Input
                      value={formState.jobTitle}
                      onChange={(_, data) => setFormState((current) => ({ ...current, jobTitle: data.value }))}
                      disabled={!canEditUsers || isSaving}
                    />
                  </Field>
                  <Field label="Branch">
                    <Input
                      value={formState.branch}
                      onChange={(_, data) => setFormState((current) => ({ ...current, branch: data.value }))}
                      disabled={!canEditUsers || isSaving}
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
                    disabled={!canEditUsers || isSaving}
                  >
                    <Option value="">No manager</Option>
                    {managers.map((manager) => (
                      <Option key={manager.id} value={manager.id} text={manager.displayName}>
                        {manager.displayName}
                      </Option>
                    ))}
                  </Dropdown>
                </Field>

                {!isCreate && employee?.invitePendingAt && canEditUsers && (
                  <Button
                    appearance="secondary"
                    disabled={isSaving}
                    onClick={async () => {
                      setIsSaving(true);
                      setFormError(null);

                      try {
                        await sendUserInvite(employee.id);
                        onSaved?.();
                      } catch (inviteError) {
                        const message = inviteError instanceof ApiError
                          ? inviteError.message
                          : 'Failed to resend invite.';
                        setFormError(message);
                      } finally {
                        setIsSaving(false);
                      }
                    }}
                  >
                    Resend invite email
                  </Button>
                )}
              </>
            )}

            {formError && <Text className="text-sm text-red-600">{formError}</Text>}
          </DialogContent>
            <DialogActions>
              <Button
                type="button"
                appearance="secondary"
                onClick={() => onOpenChange(false)}
                disabled={isSaving}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                appearance="primary"
                disabled={isSaving || isLoading || !canEditUsers}
              >
                {isSaving ? (isCreate ? 'Creating...' : 'Saving...') : (isCreate ? 'Create employee' : 'Save')}
              </Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  );
}
