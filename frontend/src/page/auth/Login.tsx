import React, { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
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
import { getAuthErrorMessage, useAuth } from '../../context/AuthContext';

const MicButton: React.FC<ButtonProps> = (props) => (
  <Button {...props} appearance="transparent" size="small" />
);

function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { login, completeMfaLogin } = useAuth();

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [mfaToken, setMfaToken] = useState<string | null>(null);
  const [mfaCode, setMfaCode] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const redirectTo = (location.state as { from?: string } | null)?.from ?? '/';

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

      <Divider className="my-3">Or Login With</Divider>
      <div className="flex justify-between gap-2">
        <Button className="w-full" disabled>Google</Button>
        <Button className="w-full" disabled>Microsoft</Button>
      </div>
    </form>
  );
}

export default LoginPage;
