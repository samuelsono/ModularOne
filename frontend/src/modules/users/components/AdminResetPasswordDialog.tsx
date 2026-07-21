import { useEffect, useState, type FormEvent } from 'react';
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
  MessageBar,
  MessageBarBody,
  Spinner,
} from '@fluentui/react-components';
import { ApiError } from '@platform/api/apiClient';
import { adminSetUserPassword } from '@modules/users/services/userService';
import type { UserListItem } from '@modules/users/types/user';

interface AdminResetPasswordDialogProps {
  user: UserListItem | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSaved?: () => void;
}

export function AdminResetPasswordDialog({
  user,
  open,
  onOpenChange,
  onSaved,
}: AdminResetPasswordDialogProps) {
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [mustChangePassword, setMustChangePassword] = useState(true);
  const [clearInvitePending, setClearInvitePending] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (!open) {
      return;
    }

    setPassword('');
    setConfirmPassword('');
    setMustChangePassword(true);
    setClearInvitePending(Boolean(user?.invitePendingAt));
    setError(null);
  }, [open, user]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!user) {
      return;
    }

    if (password.length < 8) {
      setError('Password must be at least 8 characters.');
      return;
    }

    if (password !== confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    setIsSaving(true);
    setError(null);

    try {
      await adminSetUserPassword(user.id, {
        newPassword: password,
        mustChangePassword,
        clearInvitePending,
      });
      onOpenChange(false);
      onSaved?.();
    } catch (saveError) {
      setError(saveError instanceof ApiError
        ? saveError.message
        : 'Failed to reset password.');
    } finally {
      setIsSaving(false);
    }
  }

  if (!user) {
    return null;
  }

  return (
    <Dialog open={open} onOpenChange={(_, data) => onOpenChange(data.open)}>
      <DialogSurface className="max-w-md">
        <form onSubmit={(event) => void handleSubmit(event)}>
          <DialogBody>
            <DialogTitle>Reset password</DialogTitle>
            <DialogContent className="flex flex-col gap-3 pt-2">
              <p className="text-sm text-neutral-foreground-2 m-0">
                Set a new password for{' '}
                <strong>{user.displayName || user.username}</strong>.
              </p>

              {error ? (
                <MessageBar intent="error">
                  <MessageBarBody>{error}</MessageBarBody>
                </MessageBar>
              ) : null}

              <Field label="New password" required>
                <Input
                  type="password"
                  value={password}
                  autoComplete="new-password"
                  onChange={(_, data) => setPassword(data.value)}
                  disabled={isSaving}
                />
              </Field>

              <Field label="Confirm password" required>
                <Input
                  type="password"
                  value={confirmPassword}
                  autoComplete="new-password"
                  onChange={(_, data) => setConfirmPassword(data.value)}
                  disabled={isSaving}
                />
              </Field>

              <Checkbox
                checked={mustChangePassword}
                label="Require password change on next sign-in"
                onChange={(_, data) => setMustChangePassword(Boolean(data.checked))}
                disabled={isSaving}
              />

              <Checkbox
                checked={clearInvitePending}
                label="Clear invite pending status"
                onChange={(_, data) => setClearInvitePending(Boolean(data.checked))}
                disabled={isSaving}
              />
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => onOpenChange(false)} disabled={isSaving}>
                Cancel
              </Button>
              <Button appearance="primary" type="submit" disabled={isSaving}>
                {isSaving ? <Spinner size="tiny" /> : 'Reset password'}
              </Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  );
}
