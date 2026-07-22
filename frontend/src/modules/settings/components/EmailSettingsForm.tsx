import { useEffect, useState } from 'react';
import {
  Button,
  Dropdown,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Option,
  Spinner,
  Subtitle1,
  Switch,
  Text,
} from '@fluentui/react-components';
import { ApiError } from '@platform/api/apiClient';
import {
  getEmailSettings,
  testEmailSettings,
  updateEmailSettings,
} from '@modules/settings/services/settingsService';
import type { EmailProvider, EmailSettings } from '@modules/settings/types/settings';

const PROVIDERS: { value: EmailProvider; label: string }[] = [
  { value: 'Smtp', label: 'SMTP' },
  { value: 'SendGrid', label: 'SendGrid' },
  { value: 'Mailgun', label: 'Mailgun' },
];

interface EmailSettingsFormProps {
  onSaved?: () => void;
}

export function EmailSettingsForm({ onSaved }: EmailSettingsFormProps) {
  const [enabled, setEnabled] = useState(false);
  const [provider, setProvider] = useState<EmailProvider>('Smtp');
  const [fromAddress, setFromAddress] = useState('noreply@cartrack.local');
  const [fromName, setFromName] = useState('TalisTrack');
  const [smtpHost, setSmtpHost] = useState('');
  const [smtpPort, setSmtpPort] = useState('587');
  const [useSsl, setUseSsl] = useState(true);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [apiKey, setApiKey] = useState('');
  const [mailgunDomain, setMailgunDomain] = useState('');
  const [hasPassword, setHasPassword] = useState(false);
  const [hasApiKey, setHasApiKey] = useState(false);
  const [updatedAt, setUpdatedAt] = useState<string | null>(null);
  const [testToAddress, setTestToAddress] = useState('');

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
        const settings = await getEmailSettings();
        if (!cancelled) {
          applySettings(settings);
        }
      } catch (loadError) {
        if (!cancelled) {
          setError(
            loadError instanceof ApiError
              ? loadError.message
              : 'Failed to load email settings.',
          );
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

  function applySettings(settings: EmailSettings) {
    setEnabled(settings.enabled);
    setProvider(settings.provider);
    setFromAddress(settings.fromAddress);
    setFromName(settings.fromName);
    setSmtpHost(settings.smtpHost ?? '');
    setSmtpPort(String(settings.smtpPort || 587));
    setUseSsl(settings.useSsl);
    setUsername(settings.username ?? '');
    setMailgunDomain(settings.mailgunDomain ?? '');
    setHasPassword(settings.hasPassword);
    setHasApiKey(settings.hasApiKey);
    setUpdatedAt(settings.updatedAt);
    setPassword('');
    setApiKey('');
  }

  async function handleSave(event: React.FormEvent) {
    event.preventDefault();
    setIsSaving(true);
    setError(null);
    setSuccess(null);
    setTestMessage(null);
    setTestSuccess(null);

    try {
      const port = Number.parseInt(smtpPort, 10);
      const updated = await updateEmailSettings({
        enabled,
        provider,
        fromAddress: fromAddress.trim(),
        fromName: fromName.trim(),
        smtpHost: smtpHost.trim() || null,
        smtpPort: Number.isFinite(port) && port > 0 ? port : 587,
        useSsl,
        username: username.trim() || null,
        password: password.trim() || undefined,
        apiKey: apiKey.trim() || undefined,
        mailgunDomain: mailgunDomain.trim() || null,
      });
      applySettings(updated);
      setSuccess('Email settings saved.');
      onSaved?.();
    } catch (saveError) {
      setError(
        saveError instanceof ApiError
          ? saveError.message
          : 'Failed to save email settings.',
      );
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
      const result = await testEmailSettings({ toAddress: testToAddress.trim() });
      setTestSuccess(result.success);
      setTestMessage(result.message);
    } catch (testError) {
      setTestSuccess(false);
      setTestMessage(
        testError instanceof ApiError
          ? testError.message
          : 'Failed to send test email.',
      );
    } finally {
      setIsTesting(false);
    }
  }

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner size="medium" label="Loading email settings..." />
      </div>
    );
  }

  return (
    <form className="flex flex-col gap-4 max-w-xl" onSubmit={(event) => void handleSave(event)}>
      <div className="flex flex-col mt-5">
        <Subtitle1>Email</Subtitle1>
        <Text className="text-sm text-neutral-foreground-3 mt-1 block">
          Configure how TalisTrack sends invite and password-reset emails. Choose SendGrid, Mailgun,
          or a standard SMTP server. Secrets are stored encrypted on the server.
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

      <Field label="Enable outbound email">
        <Switch
          checked={enabled}
          onChange={(_, data) => setEnabled(data.checked)}
          label={enabled ? 'Enabled' : 'Disabled'}
        />
      </Field>

      <Field label="Provider" required>
        <Dropdown
          value={PROVIDERS.find((item) => item.value === provider)?.label ?? provider}
          selectedOptions={[provider]}
          onOptionSelect={(_, data) => {
            const next = data.optionValue as EmailProvider | undefined;
            if (next) {
              setProvider(next);
            }
          }}
        >
          {PROVIDERS.map((item) => (
            <Option key={item.value} value={item.value} text={item.label}>
              {item.label}
            </Option>
          ))}
        </Dropdown>
      </Field>

      <Field label="From name" required>
        <Input value={fromName} onChange={(_, data) => setFromName(data.value)} />
      </Field>

      <Field label="From address" required>
        <Input
          type="email"
          value={fromAddress}
          onChange={(_, data) => setFromAddress(data.value)}
          placeholder="noreply@example.com"
        />
      </Field>

      {provider === 'Smtp' && (
        <>
          <Field label="SMTP host" required={enabled}>
            <Input
              value={smtpHost}
              onChange={(_, data) => setSmtpHost(data.value)}
              placeholder="smtp.example.com"
            />
          </Field>
          <Field label="SMTP port">
            <Input
              type="number"
              value={smtpPort}
              onChange={(_, data) => setSmtpPort(data.value)}
            />
          </Field>
          <Field label="Use SSL / TLS">
            <Switch
              checked={useSsl}
              onChange={(_, data) => setUseSsl(data.checked)}
              label={useSsl ? 'On' : 'Off'}
            />
          </Field>
          <Field label="Username" hint="Optional for open relays.">
            <Input
              value={username}
              onChange={(_, data) => setUsername(data.value)}
              autoComplete="username"
            />
          </Field>
          <Field
            label="Password"
            hint={hasPassword ? 'Leave blank to keep the current password.' : undefined}
          >
            <Input
              type="password"
              value={password}
              onChange={(_, data) => setPassword(data.value)}
              placeholder={hasPassword ? '••••••••' : 'SMTP password'}
              autoComplete="current-password"
            />
          </Field>
        </>
      )}

      {provider === 'SendGrid' && (
        <Field
          label="API key"
          required={enabled && !hasApiKey}
          hint={hasApiKey ? 'Leave blank to keep the current API key.' : 'Required when email is enabled.'}
        >
          <Input
            type="password"
            value={apiKey}
            onChange={(_, data) => setApiKey(data.value)}
            placeholder={hasApiKey ? '••••••••' : 'SG....'}
            autoComplete="off"
          />
        </Field>
      )}

      {provider === 'Mailgun' && (
        <>
          <Field label="Domain" required={enabled}>
            <Input
              value={mailgunDomain}
              onChange={(_, data) => setMailgunDomain(data.value)}
              placeholder="mg.example.com"
            />
          </Field>
          <Field
            label="API key"
            required={enabled && !hasApiKey}
            hint={hasApiKey ? 'Leave blank to keep the current API key.' : 'Required when email is enabled.'}
          >
            <Input
              type="password"
              value={apiKey}
              onChange={(_, data) => setApiKey(data.value)}
              placeholder={hasApiKey ? '••••••••' : 'key-...'}
              autoComplete="off"
            />
          </Field>
        </>
      )}

      {updatedAt && (
        <Text className="text-xs text-neutral-foreground-3">
          Last updated {new Date(updatedAt).toLocaleString('en-ZA')}
        </Text>
      )}

      <div className="flex gap-2 pt-2">
        <Button appearance="primary" type="submit" disabled={isSaving}>
          {isSaving ? 'Saving...' : 'Save settings'}
        </Button>
      </div>

      <div className="flex flex-col gap-3 border-t border-neutral-stroke-3 pt-4 mt-2">
        <Text weight="semibold">Send test email</Text>
        <Field label="Recipient">
          <Input
            type="email"
            value={testToAddress}
            onChange={(_, data) => setTestToAddress(data.value)}
            placeholder="you@example.com"
          />
        </Field>
        <Button
          appearance="secondary"
          type="button"
          disabled={isTesting || isSaving || !testToAddress.trim()}
          onClick={() => void handleTest()}
        >
          {isTesting ? 'Sending...' : 'Send test email'}
        </Button>
      </div>
    </form>
  );
}
