import { useEffect, useState } from 'react';
import {
  Badge,
  Button,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Spinner,
  Subtitle1,
  Text,
} from '@fluentui/react-components';
import { ApiError } from '@platform/api/apiClient';
import type { ExternalAuthSettings } from '@modules/settings/types/settings';

interface ExternalAuthCredentialsFormProps {
  providerLabel: string;
  providerDescription: string;
  showTenantId?: boolean;
  defaultTenantId?: string;
  load: () => Promise<ExternalAuthSettings>;
  save: (request: {
    clientId: string;
    clientSecret?: string;
    tenantId?: string | null;
  }) => Promise<ExternalAuthSettings>;
  callbackPathHint: string;
}

export function ExternalAuthCredentialsForm({
  providerLabel,
  providerDescription,
  showTenantId = false,
  defaultTenantId = 'common',
  load,
  save,
  callbackPathHint,
}: ExternalAuthCredentialsFormProps) {
  const [clientId, setClientId] = useState('');
  const [clientSecret, setClientSecret] = useState('');
  const [tenantId, setTenantId] = useState(defaultTenantId);
  const [hasClientSecret, setHasClientSecret] = useState(false);
  const [isActivated, setIsActivated] = useState(false);
  const [updatedAt, setUpdatedAt] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadSettings() {
      setIsLoading(true);
      setError(null);
      try {
        const settings = await load();
        if (!cancelled) {
          applySettings(settings);
        }
      } catch (loadError) {
        if (!cancelled) {
          setError(loadError instanceof ApiError ? loadError.message : `Failed to load ${providerLabel} settings.`);
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadSettings();
    return () => {
      cancelled = true;
    };
  }, [load, providerLabel]);

  function applySettings(settings: ExternalAuthSettings) {
    setClientId(settings.clientId);
    setTenantId(settings.tenantId || defaultTenantId);
    setHasClientSecret(settings.hasClientSecret);
    setIsActivated(settings.isActivated);
    setUpdatedAt(settings.updatedAt);
    setClientSecret('');
  }

  async function handleSave(event: React.FormEvent) {
    event.preventDefault();
    setIsSaving(true);
    setError(null);
    setSuccess(null);

    try {
      const updated = await save({
        clientId: clientId.trim(),
        clientSecret: clientSecret.trim() || undefined,
        tenantId: showTenantId ? tenantId.trim() || defaultTenantId : null,
      });
      applySettings(updated);
      setSuccess(
        updated.isActivated
          ? `${providerLabel} credentials saved. Sign-in with ${providerLabel} is now active on the login page.`
          : `${providerLabel} credentials saved.`,
      );
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : `Failed to save ${providerLabel} credentials.`);
    } finally {
      setIsSaving(false);
    }
  }

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner size="medium" label={`Loading ${providerLabel} settings...`} />
      </div>
    );
  }

  return (
    <form className="flex flex-col gap-4 max-w-xl" onSubmit={(event) => void handleSave(event)}>
      <div className="flex flex-col mt-5 gap-2">
        <div className="flex items-center gap-2">
          <Subtitle1>{providerLabel} authentication</Subtitle1>
          <Badge appearance="outline" color={isActivated ? 'success' : 'warning'}>
            {isActivated ? 'Active' : 'Inactive'}
          </Badge>
        </div>
        <Text className="text-sm text-neutral-foreground-3">
          {providerDescription}
        </Text>
        <Text className="text-xs text-neutral-foreground-3">
          Register this redirect URI with the provider: <code>{callbackPathHint}</code>
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

      {!isActivated && clientId && !hasClientSecret && (
        <MessageBar intent="warning">
          <MessageBarBody>
            Client ID is saved but the secret must be entered again before {providerLabel} login can activate.
          </MessageBarBody>
        </MessageBar>
      )}

      <Field label="Client ID" required>
        <Input
          value={clientId}
          onChange={(_, data) => setClientId(data.value)}
          placeholder={`${providerLabel} OAuth client ID`}
          autoComplete="off"
        />
      </Field>

      {showTenantId && (
        <Field
          label="Tenant ID"
          hint="Use `common` for personal + work accounts, `organizations` for work only, or a specific tenant GUID."
        >
          <Input
            value={tenantId}
            onChange={(_, data) => setTenantId(data.value)}
            placeholder="common"
            autoComplete="off"
          />
        </Field>
      )}

      <Field
        label="Client secret"
        required={!hasClientSecret}
        hint={hasClientSecret ? 'Leave blank to keep the current secret.' : 'Required to activate sign-in.'}
      >
        <Input
          type="password"
          value={clientSecret}
          onChange={(_, data) => setClientSecret(data.value)}
          placeholder={hasClientSecret ? '••••••••' : `${providerLabel} client secret`}
          autoComplete="new-password"
        />
      </Field>

      {updatedAt && (
        <Text className="text-xs text-neutral-foreground-3">
          Last updated {new Date(updatedAt).toLocaleString('en-ZA')}
        </Text>
      )}

      <div className="flex gap-2 pt-2">
        <Button appearance="primary" type="submit" disabled={isSaving || !clientId.trim()}>
          {isSaving ? 'Saving...' : 'Save credentials'}
        </Button>
      </div>
    </form>
  );
}
