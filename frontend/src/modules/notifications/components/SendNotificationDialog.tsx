import { useEffect, useState, type FormEvent } from 'react';
import type { JSXElement } from '@fluentui/react-components';
import {
  Button,
  Combobox,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Option,
  Spinner,
  Textarea,
} from '@fluentui/react-components';
import { SendRegular } from '@fluentui/react-icons';
import { useAuth } from '@platform/auth/AuthContext';
import { ApiError } from '@platform/api/apiClient';
import {
  broadcastNotification,
  getAuthUsersForBroadcast,
} from '@modules/notifications/services/notificationService';
import type { AuthUserLookup, NotificationTarget } from '@modules/notifications/types/notification';

const TARGET_OPTIONS = ['Everyone', 'Group', 'User'] as const;

export function SendNotificationDialog(): JSXElement | null {
  const { user } = useAuth();
  const isAdmin = user?.roles.some((role) => role === 'Admin' || role === 'SystemAdmin') ?? false;

  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');
  const [targetType, setTargetType] = useState<(typeof TARGET_OPTIONS)[number]>('Everyone');
  const [targetValue, setTargetValue] = useState('');
  const [users, setUsers] = useState<AuthUserLookup[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    if (!isAdmin) {
      return;
    }

    void getAuthUsersForBroadcast()
      .then(setUsers)
      .catch(() => setUsers([]));
  }, [isAdmin]);

  if (!isAdmin) {
    return null;
  }

  function buildTarget(): NotificationTarget {
    if (targetType === 'Group') {
      return { type: 'Group', value: targetValue.trim() };
    }
    if (targetType === 'User') {
      return { type: 'User', value: targetValue };
    }
    return { type: 'Everyone' };
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSuccess(null);
    setIsSubmitting(true);

    try {
      const sent = await broadcastNotification({
        title: title.trim(),
        body: body.trim(),
        target: buildTarget(),
      });
      setTitle('');
      setBody('');
      setTargetValue('');
      setTargetType('Everyone');
      setSuccess(`Notification sent to ${sent.length} recipient${sent.length === 1 ? '' : 's'}.`);
    } catch (submitError) {
      const message = submitError instanceof ApiError
        ? submitError.message
        : 'Failed to send notification.';
      setError(message);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form className="max-w-xl flex flex-col gap-4" onSubmit={(event) => void handleSubmit(event)}>
      <Field label="Title" required>
        <Input
          value={title}
          maxLength={120}
          onChange={(_, data) => setTitle(data.value)}
        />
      </Field>

      <Field label="Message" required>
        <Textarea
          value={body}
          maxLength={1000}
          resize="vertical"
          onChange={(_, data) => setBody(data.value)}
        />
      </Field>

      <Field label="Target audience" required>
        <Combobox
          value={targetType}
          onOptionSelect={(_, data) => {
            const next = (data.optionText ?? targetType) as (typeof TARGET_OPTIONS)[number];
            setTargetType(next);
            setTargetValue('');
          }}
        >
          {TARGET_OPTIONS.map((option) => (
            <Option key={option}>{option}</Option>
          ))}
        </Combobox>
      </Field>

      {targetType === 'Group' && (
        <Field label="Group / role name" required>
          <Input
            value={targetValue}
            placeholder="e.g. Admin, FleetAdmin"
            onChange={(_, data) => setTargetValue(data.value)}
          />
        </Field>
      )}

      {targetType === 'User' && (
        <Field label="User" required>
          <Combobox
            value={users.find((item) => item.id === targetValue)?.displayName ?? ''}
            onOptionSelect={(_, data) => {
              const selected = users.find((item) => item.displayName === data.optionText);
              setTargetValue(selected?.id ?? '');
            }}
          >
            {users.map((item) => (
              <Option key={item.id} text={item.displayName}>
                {item.displayName} ({item.email})
              </Option>
            ))}
          </Combobox>
        </Field>
      )}

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

      <div>
        <Button
          type="submit"
          appearance="primary"
          icon={isSubmitting ? <Spinner size="tiny" /> : <SendRegular />}
          disabled={isSubmitting || !title.trim() || !body.trim()}
        >
          {isSubmitting ? 'Sending...' : 'Send notification'}
        </Button>
      </div>
    </form>
  );
}
