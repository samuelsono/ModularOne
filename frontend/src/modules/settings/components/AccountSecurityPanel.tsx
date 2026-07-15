import { useState } from 'react';
import {
  Button,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Spinner,
  Subtitle2,
  Text,
} from '@fluentui/react-components';
import { ApiError } from '@platform/api/apiClient';
import * as authService from '@platform/api/authService';
import { useAuth } from '@platform/auth/AuthContext';

export function AccountSecurityPanel() {
  const { user, refreshUser, logout } = useAuth();
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [passwordMessage, setPasswordMessage] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [isSavingPassword, setIsSavingPassword] = useState(false);

  const [mfaSetup, setMfaSetup] = useState<{ sharedKey: string; authenticatorUri: string } | null>(null);
  const [mfaCode, setMfaCode] = useState('');
  const [disablePassword, setDisablePassword] = useState('');
  const [disableCode, setDisableCode] = useState('');
  const [mfaMessage, setMfaMessage] = useState<string | null>(null);
  const [mfaError, setMfaError] = useState<string | null>(null);
  const [isWorkingMfa, setIsWorkingMfa] = useState(false);

  async function handleChangePassword(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPasswordError(null);
    setPasswordMessage(null);

    if (newPassword !== confirmPassword) {
      setPasswordError('Passwords do not match.');
      return;
    }

    setIsSavingPassword(true);

    try {
      const response = await authService.changePassword({
        currentPassword,
        newPassword,
      });
      setPasswordMessage(response.message);
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
      await logout();
    } catch (error) {
      setPasswordError(error instanceof ApiError ? error.message : 'Unable to change password.');
    } finally {
      setIsSavingPassword(false);
    }
  }

  async function handleBeginMfaSetup() {
    setMfaError(null);
    setMfaMessage(null);
    setIsWorkingMfa(true);

    try {
      const setup = await authService.beginMfaSetup();
      setMfaSetup(setup);
    } catch (error) {
      setMfaError(error instanceof ApiError ? error.message : 'Unable to start MFA setup.');
    } finally {
      setIsWorkingMfa(false);
    }
  }

  async function handleEnableMfa(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMfaError(null);
    setMfaMessage(null);
    setIsWorkingMfa(true);

    try {
      const response = await authService.enableMfa({ code: mfaCode.trim() });
      setMfaMessage(response.message);
      setMfaSetup(null);
      setMfaCode('');
      await refreshUser();
    } catch (error) {
      setMfaError(error instanceof ApiError ? error.message : 'Unable to enable MFA.');
    } finally {
      setIsWorkingMfa(false);
    }
  }

  async function handleDisableMfa(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMfaError(null);
    setMfaMessage(null);
    setIsWorkingMfa(true);

    try {
      const response = await authService.disableMfa({
        password: disablePassword,
        code: disableCode.trim(),
      });
      setMfaMessage(response.message);
      setDisablePassword('');
      setDisableCode('');
      await refreshUser();
    } catch (error) {
      setMfaError(error instanceof ApiError ? error.message : 'Unable to disable MFA.');
    } finally {
      setIsWorkingMfa(false);
    }
  }

  return (
    <div className="max-w-xl flex flex-col gap-6">
      {user?.mustChangePassword && (
        <MessageBar intent="warning">
          <MessageBarBody>
            You must change your password before continuing to use sensitive features.
          </MessageBarBody>
        </MessageBar>
      )}

      <form className="flex flex-col gap-3" onSubmit={handleChangePassword}>
        <Subtitle2>Change password</Subtitle2>
        <Text className="text-sm text-neutral-foreground-3">
          You will be signed out after changing your password.
        </Text>

        {passwordError && (
          <MessageBar intent="error">
            <MessageBarBody>{passwordError}</MessageBarBody>
          </MessageBar>
        )}

        {passwordMessage && (
          <MessageBar intent="success">
            <MessageBarBody>{passwordMessage}</MessageBarBody>
          </MessageBar>
        )}

        <Field label="Current password" required>
          <Input
            type="password"
            value={currentPassword}
            onChange={(_, data) => setCurrentPassword(data.value)}
            disabled={isSavingPassword}
            autoComplete="current-password"
          />
        </Field>

        <Field label="New password" required>
          <Input
            type="password"
            value={newPassword}
            onChange={(_, data) => setNewPassword(data.value)}
            disabled={isSavingPassword}
            autoComplete="new-password"
          />
        </Field>

        <Field label="Confirm new password" required>
          <Input
            type="password"
            value={confirmPassword}
            onChange={(_, data) => setConfirmPassword(data.value)}
            disabled={isSavingPassword}
            autoComplete="new-password"
          />
        </Field>

        <Button
          appearance="primary"
          type="submit"
          disabled={isSavingPassword || !currentPassword || !newPassword}
          icon={isSavingPassword ? <Spinner size="tiny" /> : undefined}
        >
          {isSavingPassword ? 'Saving...' : 'Update password'}
        </Button>
      </form>

      <div className="flex flex-col gap-3 border-t border-[#e3e5e7] pt-4">
        <Subtitle2>Two-factor authentication</Subtitle2>
        <Text className="text-sm text-neutral-foreground-3">
          {user?.twoFactorEnabled
            ? 'Authenticator app verification is enabled for your account.'
            : 'Add an extra layer of security with an authenticator app.'}
        </Text>

        {mfaError && (
          <MessageBar intent="error">
            <MessageBarBody>{mfaError}</MessageBarBody>
          </MessageBar>
        )}

        {mfaMessage && (
          <MessageBar intent="success">
            <MessageBarBody>{mfaMessage}</MessageBarBody>
          </MessageBar>
        )}

        {!user?.twoFactorEnabled && !mfaSetup && (
          <Button appearance="secondary" onClick={() => void handleBeginMfaSetup()} disabled={isWorkingMfa}>
            Set up authenticator
          </Button>
        )}

        {mfaSetup && (
          <form className="flex flex-col gap-3" onSubmit={handleEnableMfa}>
            <Text className="text-sm">
              Add this key to your authenticator app:
              <code className="block mt-2 p-2 bg-neutral-background-2 rounded text-left break-all">
                {mfaSetup.sharedKey}
              </code>
            </Text>
            <Field label="Verification code" required>
              <Input
                value={mfaCode}
                onChange={(_, data) => setMfaCode(data.value)}
                disabled={isWorkingMfa}
                placeholder="6-digit code"
              />
            </Field>
            <Button appearance="primary" type="submit" disabled={isWorkingMfa || !mfaCode.trim()}>
              Enable two-factor authentication
            </Button>
          </form>
        )}

        {user?.twoFactorEnabled && (
          <form className="flex flex-col gap-3" onSubmit={handleDisableMfa}>
            <Field label="Password" required>
              <Input
                type="password"
                value={disablePassword}
                onChange={(_, data) => setDisablePassword(data.value)}
                disabled={isWorkingMfa}
              />
            </Field>
            <Field label="Authenticator code" required>
              <Input
                value={disableCode}
                onChange={(_, data) => setDisableCode(data.value)}
                disabled={isWorkingMfa}
              />
            </Field>
            <Button appearance="secondary" type="submit" disabled={isWorkingMfa || !disablePassword || !disableCode.trim()}>
              Disable two-factor authentication
            </Button>
          </form>
        )}
      </div>
    </div>
  );
}
