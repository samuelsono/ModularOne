import { useEffect, useState } from 'react';
import {
  Button,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Spinner,
  Subtitle1,
  Text,
} from '@fluentui/react-components';
import { ApiError } from '../../services/apiClient';
import {
  getCarTrackSettings,
  testCarTrackConnection,
  updateCarTrackSettings,
} from '../../services/settingsService';
import type { CarTrackSettings } from '../../types/settings';

const DEFAULT_BASE_URL = 'https://fleetapi-za.cartrack.com';

interface CarTrackCredentialsFormProps {
  onSaved?: () => void;
}

export function CarTrackCredentialsForm({ onSaved }: CarTrackCredentialsFormProps) {
  const [baseUrl, setBaseUrl] = useState(DEFAULT_BASE_URL);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [hasStoredPassword, setHasStoredPassword] = useState(false);
  const [updatedAt, setUpdatedAt] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isTesting, setIsTesting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [testMessage, setTestMessage] = useState<string | null>(null);
  const [testSuccess, setTestSuccess] = useState<boolean | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setIsLoading(true);
      setError(null);

      try {
        const settings = await getCarTrackSettings();
        if (!cancelled) {
          applySettings(settings);
        }
      } catch (loadError) {
        if (!cancelled) {
          const message = loadError instanceof ApiError
            ? loadError.message
            : 'Failed to load CarTrack settings.';
          setError(message);
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, []);

  function applySettings(settings: CarTrackSettings) {
    setBaseUrl(settings.baseUrl || DEFAULT_BASE_URL);
    setUsername(settings.username);
    setHasStoredPassword(settings.hasPassword);
    setUpdatedAt(settings.updatedAt);
    setPassword('');
  }

  async function handleSave(event: React.FormEvent) {
    event.preventDefault();
    setIsSaving(true);
    setError(null);
    setSuccess(null);
    setTestMessage(null);
    setTestSuccess(null);

    try {
      const updated = await updateCarTrackSettings({
        baseUrl: baseUrl.trim(),
        username: username.trim(),
        password: password.trim() || undefined,
      });
      applySettings(updated);
      setSuccess('CarTrack credentials saved.');
      onSaved?.();
    } catch (saveError) {
      const message = saveError instanceof ApiError
        ? saveError.message
        : 'Failed to save CarTrack credentials.';
      setError(message);
    } finally {
      setIsSaving(false);
    }
  }

  async function handleTest() {
    setIsTesting(true);
    setTestMessage(null);
    setTestSuccess(null);
    setError(null);

    try {
      const result = await testCarTrackConnection();
      setTestSuccess(result.success);
      setTestMessage(result.message);
    } catch (testError) {
      const message = testError instanceof ApiError
        ? testError.message
        : 'Connection test failed.';
      setTestSuccess(false);
      setTestMessage(message);
    } finally {
      setIsTesting(false);
    }
  }

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner size="medium" label="Loading CarTrack settings..." />
      </div>
    );
  }

  return (
    <form className="flex flex-col gap-4 max-w-xl" onSubmit={(event) => void handleSave(event)}>
      <div>
        <Subtitle1>CarTrack Fleet API</Subtitle1>
        <Text className="text-sm text-neutral-foreground-3 mt-1 block">
          Connect TalisTrack to your CarTrack fleet account. Credentials are stored securely on the server
          and used for vehicle sync, live tracking, and trip history.
        </Text>
      </div>

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      {success && (
        <MessageBar intent="success">
          <MessageBarBody>{success}</MessageBarBody>
        </MessageBar>
      )}

      {testMessage && (
        <MessageBar intent={testSuccess ? 'success' : 'warning'}>
          <MessageBarBody>{testMessage}</MessageBarBody>
        </MessageBar>
      )}

      <Field label="API base URL" required>
        <Input
          value={baseUrl}
          onChange={(_, data) => setBaseUrl(data.value)}
          placeholder={DEFAULT_BASE_URL}
        />
      </Field>

      <Field label="Username" required>
        <Input
          value={username}
          onChange={(_, data) => setUsername(data.value)}
          placeholder="CarTrack API username"
          autoComplete="username"
        />
      </Field>

      <Field
        label="Password"
        required={!hasStoredPassword}
        hint={hasStoredPassword ? 'Leave blank to keep the current password.' : undefined}
      >
        <Input
          type="password"
          value={password}
          onChange={(_, data) => setPassword(data.value)}
          placeholder={hasStoredPassword ? '••••••••' : 'CarTrack API password'}
          autoComplete="current-password"
        />
      </Field>

      {updatedAt && (
        <Text className="text-xs text-neutral-foreground-3">
          Last updated {new Date(updatedAt).toLocaleString('en-ZA')}
        </Text>
      )}

      <div className="flex gap-2 pt-2">
        <Button appearance="primary" type="submit" disabled={isSaving}>
          {isSaving ? 'Saving...' : 'Save credentials'}
        </Button>
        <Button
          appearance="secondary"
          type="button"
          disabled={isTesting || isSaving}
          onClick={() => void handleTest()}
        >
          {isTesting ? 'Testing...' : 'Test connection'}
        </Button>
      </div>
    </form>
  );
}
