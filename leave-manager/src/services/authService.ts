import { api, setAuthToken } from './api';
import {
  clearStoredSession,
  getStoredAccessToken,
  getStoredRefreshToken,
  getStoredUserJson,
  storeSessionTokens,
} from './tokenStorage';
import type {
  AuthSession,
  AuthUser,
  LoginRequest,
  LoginResponse,
  RefreshTokenRequest,
} from '../types/auth';

export const SESSION_EXPIRED_EVENT = 'leave-manager:session-expired';

let refreshPromise: Promise<string> | null = null;

export function getStoredUser(): AuthUser | null {
  const json = getStoredUserJson();
  if (!json) {
    return null;
  }

  try {
    return JSON.parse(json) as AuthUser;
  } catch {
    return null;
  }
}

export function displayNameForUser(user: AuthUser | null): string {
  if (!user) {
    return 'Employee';
  }
  return user.displayName?.trim() || user.username || user.email || 'Employee';
}

export function clearSession(): void {
  clearStoredSession();
  setAuthToken(null);
}

export function forceLogout(): void {
  clearSession();
  window.dispatchEvent(new CustomEvent(SESSION_EXPIRED_EVENT));
}

export function restoreSession(): string | null {
  const token = getStoredAccessToken();
  if (token) {
    setAuthToken(token);
  }
  return token;
}

function persistSession(session: AuthSession): void {
  storeSessionTokens(session.accessToken, session.refreshToken, JSON.stringify(session.user));
  setAuthToken(session.accessToken);
}

export async function loginAndStoreSession(request: LoginRequest): Promise<AuthSession> {
  const response = await api.post<LoginResponse>(
    '/auth/login',
    {
      username: request.username,
      password: request.password,
      rememberMe: request.rememberMe ?? false,
    },
    { skipAuthRefresh: true },
  );

  const payload = response.data;

  if (payload.requiresTwoFactor) {
    throw new Error(
      'This account requires MFA. Complete sign-in on the web app, or enable MFA support in this mobile app.',
    );
  }

  const session = payload.session;
  if (!session?.accessToken) {
    throw new Error('Login succeeded but no access token was returned.');
  }

  persistSession(session);
  return session;
}

export async function refreshSession(): Promise<string> {
  const refreshToken = getStoredRefreshToken();
  if (!refreshToken) {
    forceLogout();
    throw new Error('No refresh token available.');
  }

  if (!refreshPromise) {
    refreshPromise = (async () => {
      try {
        const body: RefreshTokenRequest = {
          refreshToken,
          rememberMe: false,
        };
        const response = await api.post<AuthSession>('/auth/refresh', body, {
          skipAuthRefresh: true,
        });
        const session = response.data;
        if (!session?.accessToken) {
          throw new Error('Refresh did not return an access token.');
        }

        const existingUser = getStoredUser();
        const user = session.user ?? existingUser;
        if (!user) {
          throw new Error('Refresh did not return user details.');
        }

        persistSession({ ...session, user });
        return session.accessToken;
      } catch (error) {
        forceLogout();
        throw error;
      } finally {
        refreshPromise = null;
      }
    })();
  }

  return refreshPromise;
}
