import { useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  Button,
  Field,
  Input,
  Link,
  MessageBar,
  MessageBarBody,
  Spinner,
} from '@fluentui/react-components';
import { ApiError } from '../../services/apiClient';
import * as authService from '../../services/authService';

interface PasswordSetupFormProps {
  title: string;
  description: string;
  submitLabel: string;
  onSubmit: (request: { email: string; token: string; newPassword: string }) => Promise<void>;
}

function PasswordSetupForm({
  title,
  description,
  submitLabel,
  onSubmit,
}: PasswordSetupFormProps) {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const email = useMemo(() => searchParams.get('email') ?? '', [searchParams]);
  const token = useMemo(() => searchParams.get('token') ?? '', [searchParams]);

  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    if (!email || !token) {
      setError('This link is invalid or incomplete. Request a new email and try again.');
      return;
    }

    if (newPassword !== confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    setIsSubmitting(true);

    try {
      await onSubmit({ email, token, newPassword });
      setSuccess(true);
    } catch (submitError) {
      const message = submitError instanceof ApiError
        ? submitError.message
        : 'Unable to update your password. Please try again.';
      setError(message);
    } finally {
      setIsSubmitting(false);
    }
  }

  if (success) {
    return (
      <div className="flex flex-col text-center gap-3">
        <p className="text-lg font-bold">Password updated</p>
        <p>Your password has been saved. You can sign in with your new credentials.</p>
        <Button appearance="primary" className="w-full" onClick={() => navigate('/auth/login')}>
          Go to login
        </Button>
      </div>
    );
  }

  return (
    <form className="flex flex-col text-center gap-3" onSubmit={handleSubmit}>
      <div className="mb-3">
        <p className="text-lg font-bold">{title}</p>
        <p>{description}</p>
      </div>

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      <Field label="New password" required>
        <Input
          type="password"
          value={newPassword}
          onChange={(_, data) => setNewPassword(data.value)}
          disabled={isSubmitting}
          autoComplete="new-password"
        />
      </Field>

      <Field label="Confirm password" required>
        <Input
          type="password"
          value={confirmPassword}
          onChange={(_, data) => setConfirmPassword(data.value)}
          disabled={isSubmitting}
          autoComplete="new-password"
        />
      </Field>

      <Button
        appearance="primary"
        className="w-full"
        type="submit"
        disabled={isSubmitting || !newPassword || !confirmPassword}
        icon={isSubmitting ? <Spinner size="tiny" /> : undefined}
      >
        {isSubmitting ? 'Saving...' : submitLabel}
      </Button>

      <Link href="/auth/login" className="text-sm">Back to login</Link>
    </form>
  );
}

export function ResetPasswordPage() {
  return (
    <PasswordSetupForm
      title="Reset password"
      description="Choose a new password for your account."
      submitLabel="Reset password"
      onSubmit={async (request) => {
        await authService.resetPassword(request);
      }}
    />
  );
}

export function SetupAccountPage() {
  return (
    <PasswordSetupForm
      title="Set up your account"
      description="Welcome to TalisTrack. Create your password to finish setup."
      submitLabel="Create password"
      onSubmit={async (request) => {
        await authService.setupAccount(request);
      }}
    />
  );
}

export default ResetPasswordPage;
