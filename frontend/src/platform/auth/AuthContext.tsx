import React from 'react';
import type { AuthUser } from '@platform/auth/types';
import { ApiError } from '@platform/api/apiClient';
import * as authService from '@platform/api/authService';
import { clearTokens, hasStoredSession } from '@platform/api/tokenStorage';
import { markUseDefaultModuleOnLogin } from '@platform/utils/appModuleStorage';

interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (username: string, password: string, rememberMe?: boolean) => Promise<{ status: 'mfa'; mfaToken: string } | { status: 'ok' }>;
  completeMfaLogin: (mfaToken: string, code: string, rememberMe?: boolean) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

const AuthContext = React.createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = React.useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = React.useState(true);

  React.useEffect(() => {
    let cancelled = false;

    async function bootstrap() {
      if (!hasStoredSession()) {
        if (!cancelled) {
          setIsLoading(false);
        }
        return;
      }

      try {
        const currentUser = await authService.getCurrentUser();
        if (!cancelled) {
          setUser(currentUser);
        }
      } catch {
        try {
          const refreshed = await authService.refreshSession();
          if (!cancelled) {
            setUser(refreshed.user);
          }
        } catch {
          clearTokens();
          if (!cancelled) {
            setUser(null);
          }
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    void bootstrap();

    return () => {
      cancelled = true;
    };
  }, []);

  const login = React.useCallback(async (username: string, password: string, rememberMe = false) => {
    const result = await authService.loginAndStoreSession({ username, password, rememberMe });

    if (result.requiresTwoFactor) {
      return { status: 'mfa' as const, mfaToken: result.mfaToken };
    }

    setUser(result.session.user);
    markUseDefaultModuleOnLogin();
    return { status: 'ok' as const };
  }, []);

  const completeMfaLogin = React.useCallback(async (mfaToken: string, code: string, rememberMe = false) => {
    const session = await authService.completeMfaLogin({ mfaToken, code, rememberMe });
    setUser(session.user);
    markUseDefaultModuleOnLogin();
  }, []);

  const logout = React.useCallback(async () => {
    try {
      await authService.logout();
    } finally {
      setUser(null);
    }
  }, []);

  const refreshUser = React.useCallback(async () => {
    const currentUser = await authService.getCurrentUser();
    setUser(currentUser);
  }, []);

  const value = React.useMemo<AuthContextValue>(() => ({
    user,
    isAuthenticated: user !== null,
    isLoading,
    login,
    completeMfaLogin,
    logout,
    refreshUser,
  }), [user, isLoading, login, completeMfaLogin, logout, refreshUser]);

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const context = React.useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider.');
  }
  return context;
}

export function getAuthErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}
