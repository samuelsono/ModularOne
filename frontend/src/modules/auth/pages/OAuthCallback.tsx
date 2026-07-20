import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { MessageBar, MessageBarBody, Spinner } from '@fluentui/react-components';
import { setTokens } from '@platform/api/tokenStorage';
import { getCurrentUser } from '@platform/api/authService';
import { markUseDefaultModuleOnLogin } from '@platform/utils/appModuleStorage';
import { useAuth } from '@platform/auth/AuthContext';

export default function OAuthCallbackPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const { refreshUser } = useAuth();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function complete() {
      const accessToken = params.get('accessToken');
      const refreshToken = params.get('refreshToken');
      const returnUrl = params.get('returnUrl') || '/';

      if (!accessToken || !refreshToken) {
        setError('Sign-in did not return a session. Please try again.');
        return;
      }

      try {
        setTokens(accessToken, refreshToken, true);
        await getCurrentUser();
        await refreshUser();
        markUseDefaultModuleOnLogin();
        if (!cancelled) {
          navigate(returnUrl.startsWith('/') ? returnUrl : '/', { replace: true });
        }
      } catch {
        if (!cancelled) {
          setError('Unable to complete sign-in. Please try again.');
        }
      }
    }

    void complete();
    return () => {
      cancelled = true;
    };
  }, [navigate, params, refreshUser]);

  if (error) {
    return (
      <div className="flex flex-col gap-3 text-center">
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
        <a href="/auth/login" className="text-sm underline">Back to login</a>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center gap-3 py-8">
      <Spinner size="medium" label="Completing sign-in..." />
    </div>
  );
}
