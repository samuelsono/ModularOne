import React, { useEffect, useState } from 'react';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import {
  Button,
  Checkbox,
  Divider,
  Field,
  Input,
  Link,
  MessageBar,
  MessageBarBody,
  Spinner,
  type ButtonProps,
} from '@fluentui/react-components';
import { EyeOffRegular, EyeRegular } from '@fluentui/react-icons';
import { apiFetch } from '@platform/api/apiClient';
import { getAuthErrorMessage, useAuth } from '@platform/auth/AuthContext';
import type { ExternalAuthProviderStatus } from '@modules/settings/types/settings';

const MicButton: React.FC<ButtonProps> = (props) => (
  <Button {...props} appearance="transparent" size="small" />
);

function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const { login, completeMfaLogin } = useAuth();

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [mfaToken, setMfaToken] = useState<string | null>(null);
  const [mfaCode, setMfaCode] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [googleActive, setGoogleActive] = useState(false);
  const [microsoftActive, setMicrosoftActive] = useState(false);

  const redirectTo = (location.state as { from?: string } | null)?.from
    ?? searchParams.get('returnUrl')
    ?? '/';

  useEffect(() => {
    const queryError = searchParams.get('error');
    if (queryError) {
      setError(queryError);
    }

    const queryMfa = searchParams.get('mfaToken');
    if (queryMfa) {
      setMfaToken(queryMfa);
    }
  }, [searchParams]);

  useEffect(() => {
    let cancelled = false;

    async function loadProviders() {
      try {
        const providers = await apiFetch<ExternalAuthProviderStatus[]>('/api/auth/external-providers');
        if (cancelled) {
          return;
        }

        setGoogleActive(providers.some((p) => p.provider === 'Google' && p.isActivated));
        setMicrosoftActive(providers.some((p) => p.provider === 'Microsoft' && p.isActivated));
      } catch {
        if (!cancelled) {
          setGoogleActive(false);
          setMicrosoftActive(false);
        }
      }
    }

    void loadProviders();
    return () => {
      cancelled = true;
    };
  }, []);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      if (mfaToken) {
        await completeMfaLogin(mfaToken, mfaCode.trim(), rememberMe);
        navigate(redirectTo, { replace: true });
        return;
      }

      const result = await login(username.trim(), password, rememberMe);
      if (result.status === 'mfa') {
        setMfaToken(result.mfaToken);
        return;
      }

      navigate(redirectTo, { replace: true });
    } catch (submitError) {
      setError(getAuthErrorMessage(submitError, 'Unable to sign in. Please try again.'));
    } finally {
      setIsSubmitting(false);
    }
  }

  function startExternalLogin(provider: 'Google' | 'Microsoft') {
    const returnUrl = encodeURIComponent(redirectTo.startsWith('/') ? redirectTo : '/');
    window.location.href = `/api/auth/external/${provider}?returnUrl=${returnUrl}`;
  }

  return (
    <form className="flex flex-col text-center gap-3" onSubmit={handleSubmit}>
      <div className="mb-3">
        <p className="text-lg font-bold">{mfaToken ? 'Two-factor authentication' : 'Login page'}</p>
        <p>{mfaToken ? 'Enter the code from your authenticator app' : 'Enter your credentials to login'}</p>
      </div>

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      {!mfaToken && (
        <>
          <Field label="Username" required>
            <Input
              type="text"
              placeholder="Enter email or username"
              value={username}
              onChange={(_, data) => setUsername(data.value)}
              disabled={isSubmitting}
              autoComplete="username"
            />
          </Field>

          <Field label="Password" required>
            <Input
              type={showPassword ? 'text' : 'password'}
              placeholder="Enter your password"
              value={password}
              onChange={(_, data) => setPassword(data.value)}
              disabled={isSubmitting}
              autoComplete="current-password"
              contentAfter={(
                <MicButton
                  icon={showPassword ? <EyeRegular /> : <EyeOffRegular />}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                  onClick={() => setShowPassword(!showPassword)}
                />
              )}
            />
          </Field>

          <div className="flex justify-between items-center gap-2">
            <Checkbox
              label="Remember me"
              checked={rememberMe}
              onChange={(_, data) => setRememberMe(Boolean(data.checked))}
              disabled={isSubmitting}
            />
            <Link href="/auth/forgot-password" className="text-sm">Forgot password?</Link>
          </div>
        </>
      )}

      {mfaToken && (
        <Field label="Authentication code" required>
          <Input
            type="text"
            inputMode="numeric"
            placeholder="6-digit code"
            value={mfaCode}
            onChange={(_, data) => setMfaCode(data.value)}
            disabled={isSubmitting}
            autoComplete="one-time-code"
          />
        </Field>
      )}

      <div className="flex justify-between gap-2">
        <Button
          appearance="primary"
          className="w-full"
          type="submit"
          disabled={isSubmitting || (mfaToken ? !mfaCode.trim() : !username.trim() || !password)}
          icon={isSubmitting ? <Spinner size="tiny" /> : undefined}
        >
          {isSubmitting ? 'Signing in...' : mfaToken ? 'Verify code' : 'Login'}
        </Button>
      </div>

      {mfaToken && (
        <Button
          appearance="subtle"
          onClick={() => {
            setMfaToken(null);
            setMfaCode('');
            setError(null);
          }}
        >
          Back to login
        </Button>
      )}

      {!mfaToken && (
        <>
          <Divider className="my-3">Or Login With</Divider>
          <div className="flex justify-between gap-2">
            <Button
              className="w-full"
              type="button"
              disabled={!googleActive || isSubmitting}
              onClick={() => startExternalLogin('Google')}
            >
              Google
            </Button>
            <Button
              className="w-full"
              type="button"
              disabled={!microsoftActive || isSubmitting}
              onClick={() => startExternalLogin('Microsoft')}
            >
              Microsoft
            </Button>
          </div>
          {!googleActive && !microsoftActive && (
            <TextHint>
              Google and Microsoft sign-in activate after an admin saves Client ID and secret under
              Settings → Integrations.
            </TextHint>
          )}
        </>
      )}
    </form>
  );
}

function TextHint({ children }: { children: React.ReactNode }) {
  return <p className="text-xs text-neutral-foreground-3 text-left">{children}</p>;
}

export default LoginPage;
