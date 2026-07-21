import { useCallback, useEffect, useState } from 'react';
import {
  Badge,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Dropdown,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Option,
  Subtitle2,
  Text,
} from '@fluentui/react-components';
import { PersonKeyRegular } from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import {
  createUserFromDriver,
  getManagerOptions,
  type DirectoryManagerOption,
} from '@platform/org/directoryApi';
import type { Driver } from '@modules/fleet/types/driver';
import { usePermissions } from '@platform/permissions/usePermissions';

interface DriverCreateLoginDialogProps {
  driver: Driver;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated?: () => void;
}

export function DriverCreateLoginDialog({
  driver,
  open,
  onOpenChange,
  onCreated,
}: DriverCreateLoginDialogProps) {
  const { canEditUsers } = usePermissions();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [sendInvite, setSendInvite] = useState(true);
  const [managerUserId, setManagerUserId] = useState('');
  const [managers, setManagers] = useState<DirectoryManagerOption[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const reset = useCallback(() => {
    const defaultUsername = driver.workEmail.includes('@')
      ? driver.workEmail.split('@')[0]
      : driver.driverId.replace(/-/g, '').toLowerCase();
    setUsername(defaultUsername);
    setPassword('');
    setSendInvite(true);
    setManagerUserId('');
    setError(null);
  }, [driver.driverId, driver.workEmail]);

  useEffect(() => {
    if (!open) {
      return;
    }

    reset();

    void getManagerOptions()
      .then(setManagers)
      .catch(() => setManagers([]));
  }, [open, reset]);

  async function handleCreate() {
    if (!canEditUsers) {
      return;
    }

    setIsSaving(true);
    setError(null);

    try {
      await createUserFromDriver(driver.id, {
        username: username.trim() || null,
        password: sendInvite ? null : (password.trim() || null),
        roles: ['Driver'],
        managerUserId: managerUserId || null,
        sendInvite,
      });
      onOpenChange(false);
      onCreated?.();
    } catch (createError) {
      const message = createError instanceof ApiError
        ? createError.message
        : 'Failed to create login account.';
      setError(message);
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(_, data) => onOpenChange(data.open)}>
      <DialogSurface className="max-w-md">
        <DialogBody>
          <DialogTitle>Create login for {driver.name}</DialogTitle>
          <DialogContent className="flex flex-col gap-3 pt-2">
            <Text className="text-sm text-neutral-foreground-3">
              A platform user will be created and linked to this driver record with the Driver role.
            </Text>

            <Field label="Username">
              <Input value={username} onChange={(_, data) => setUsername(data.value)} />
            </Field>
            <Checkbox
              checked={sendInvite}
              onChange={(_, data) => setSendInvite(Boolean(data.checked))}
              label="Send email invite (recommended)"
            />
            {!sendInvite && (
              <Field label="Temporary password" hint="Leave blank to auto-generate">
                <Input
                  type="password"
                  value={password}
                  onChange={(_, data) => setPassword(data.value)}
                />
              </Field>
            )}
            <Field label="Manager">
              <Dropdown
                placeholder="Select a manager"
                selectedOptions={managerUserId ? [managerUserId] : []}
                value={
                  managers.find((manager) => manager.id === managerUserId)?.displayName
                  || managers.find((manager) => manager.id === managerUserId)?.email
                  || ''
                }
                onOptionSelect={(_, data) => setManagerUserId(data.optionValue ?? '')}
              >
                <Option value="" text="No manager">No manager</Option>
                {managers.map((manager) => {
                  const label = manager.displayName || manager.email || manager.username;
                  return (
                    <Option key={manager.id} value={manager.id} text={label}>
                      {label}
                    </Option>
                  );
                })}
              </Dropdown>
            </Field>

            {error && (
              <MessageBar intent="error">
                <MessageBarBody>{error}</MessageBarBody>
              </MessageBar>
            )}
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={() => onOpenChange(false)} disabled={isSaving}>
              Cancel
            </Button>
            <Button appearance="primary" onClick={() => void handleCreate()} disabled={isSaving}>
              {isSaving ? 'Creating...' : 'Create login'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}

interface DriverLoginSectionProps {
  driver: Driver;
  onChanged?: () => void;
}

export function DriverLoginSection({ driver, onChanged }: DriverLoginSectionProps) {
  const { canEditUsers } = usePermissions();
  const [createOpen, setCreateOpen] = useState(false);
  const linkedUser = driver.linkedUser;

  return (
    <div className="col-span-2 rounded border border-neutral-stroke-2 p-4 flex flex-col gap-3 bg-neutral-background-2">
      <div className="flex items-center justify-between gap-3">
        <Subtitle2>Platform login</Subtitle2>
        {canEditUsers && !linkedUser && (
          <Button
            appearance="primary"
            size="small"
            icon={<PersonKeyRegular />}
            onClick={() => setCreateOpen(true)}
          >
            Create login
          </Button>
        )}
      </div>

      {linkedUser ? (
        <div className="grid grid-cols-2 gap-3 text-sm">
          <div>
            <Text className="text-neutral-foreground-3 block">Display name</Text>
            <Text weight="semibold">{linkedUser.displayName ?? linkedUser.username}</Text>
          </div>
          <div>
            <Text className="text-neutral-foreground-3 block">Username</Text>
            <Text weight="semibold">{linkedUser.username}</Text>
          </div>
          <div>
            <Text className="text-neutral-foreground-3 block">Email</Text>
            <Text weight="semibold">{linkedUser.email}</Text>
          </div>
          <div>
            <Text className="text-neutral-foreground-3 block">Status</Text>
            <Badge appearance={linkedUser.isActive ? 'filled' : 'outline'} color={linkedUser.isActive ? 'success' : 'subtle'}>
              {linkedUser.isActive ? 'Active' : 'Inactive'}
            </Badge>
          </div>
        </div>
      ) : (
        <Text className="text-sm text-neutral-foreground-3">
          No login account is linked to this driver yet.
        </Text>
      )}

      <DriverCreateLoginDialog
        driver={driver}
        open={createOpen}
        onOpenChange={setCreateOpen}
        onCreated={onChanged}
      />
    </div>
  );
}
