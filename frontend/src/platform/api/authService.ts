import type {
  AuthResponse,
  ChangePasswordRequest,
  ForgotPasswordRequest,
  LoginRequest,
  LoginResponse,
  MessageResponse,
  MfaDisableRequest,
  MfaLoginRequest,
  MfaSetupResponse,
  MfaVerifyRequest,
  RefreshTokenRequest,
  ResetPasswordRequest,
  AuthUser,
} from '@platform/auth/types';
import { ApiError, apiFetch } from '@platform/api/apiClient';
import {
  clearTokens,
  getAccessToken,
  getRefreshToken,
  setTokens,
} from '@platform/api/tokenStorage';

const AUTH_BASE = '/api/auth';

export async function login(request: LoginRequest): Promise<LoginResponse> {
  return apiFetch<LoginResponse>(`${AUTH_BASE}/login`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function completeMfaLogin(request: MfaLoginRequest): Promise<AuthResponse> {
  const response = await apiFetch<LoginResponse>(`${AUTH_BASE}/login/mfa`, {
    method: 'POST',
    body: JSON.stringify(request),
  });

  if (!response.session) {
    throw new Error('MFA login did not return a session.');
  }

  setTokens(response.session.accessToken, response.session.refreshToken, request.rememberMe ?? false);
  return response.session;
}

export async function refreshSession(): Promise<AuthResponse> {
  const refreshToken = getRefreshToken();
  if (!refreshToken) {
    throw new Error('No refresh token available.');
  }

  const response = await apiFetch<AuthResponse>(`${AUTH_BASE}/refresh`, {
    method: 'POST',
    body: JSON.stringify({ refreshToken } satisfies RefreshTokenRequest),
  });

  const rememberMe = localStorage.getItem('cartrack.rememberMe') === 'true';
  setTokens(response.accessToken, response.refreshToken, rememberMe);
  return response;
}

export async function logout(): Promise<void> {
  const refreshToken = getRefreshToken();
  const payload: RefreshTokenRequest = { refreshToken: refreshToken ?? '' };

  try {
    await authorizedFetch(`${AUTH_BASE}/logout`, {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  } finally {
    clearTokens();
  }
}

export async function forgotPassword(request: ForgotPasswordRequest): Promise<MessageResponse> {
  return apiFetch<MessageResponse>(`${AUTH_BASE}/forgot-password`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function resetPassword(request: ResetPasswordRequest): Promise<MessageResponse> {
  return apiFetch<MessageResponse>(`${AUTH_BASE}/reset-password`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function setupAccount(request: ResetPasswordRequest): Promise<MessageResponse> {
  return apiFetch<MessageResponse>(`${AUTH_BASE}/setup-account`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function changePassword(request: ChangePasswordRequest): Promise<MessageResponse> {
  return authorizedFetch<MessageResponse>(`${AUTH_BASE}/change-password`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function beginMfaSetup(): Promise<MfaSetupResponse> {
  return authorizedFetch<MfaSetupResponse>(`${AUTH_BASE}/mfa/setup`, {
    method: 'POST',
  });
}

export async function enableMfa(request: MfaVerifyRequest): Promise<MessageResponse> {
  return authorizedFetch<MessageResponse>(`${AUTH_BASE}/mfa/enable`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function disableMfa(request: MfaDisableRequest): Promise<MessageResponse> {
  return authorizedFetch<MessageResponse>(`${AUTH_BASE}/mfa/disable`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function getCurrentUser(): Promise<AuthUser> {
  return authorizedFetch<AuthUser>(`${AUTH_BASE}/me`);
}

let refreshPromise: Promise<AuthResponse> | null = null;

export async function authorizedFetch<T>(
  input: RequestInfo | URL,
  init: RequestInit = {},
): Promise<T> {
  const attempt = async (accessToken: string | null) => {
    const headers = new Headers(init.headers);
    if (accessToken) {
      headers.set('Authorization', `Bearer ${accessToken}`);
    }

    return apiFetch<T>(input, {
      ...init,
      headers,
    });
  };

  try {
    return await attempt(getAccessToken());
  } catch (error) {
    if (!(error instanceof ApiError) || error.status !== 401) {
      throw error;
    }

    if (!getRefreshToken()) {
      clearTokens();
      throw error;
    }

    refreshPromise ??= refreshSession().finally(() => {
      refreshPromise = null;
    });

    await refreshPromise;
    return attempt(getAccessToken());
  }
}

export async function loginAndStoreSession(
  request: LoginRequest,
): Promise<{ requiresTwoFactor: true; mfaToken: string } | { requiresTwoFactor: false; session: AuthResponse }> {
  const response = await login(request);

  if (response.requiresTwoFactor) {
    if (!response.mfaToken) {
      throw new Error('MFA is required but no challenge token was returned.');
    }

    return { requiresTwoFactor: true, mfaToken: response.mfaToken };
  }

  if (!response.session) {
    throw new Error('Login did not return a session.');
  }

  setTokens(response.session.accessToken, response.session.refreshToken, request.rememberMe ?? false);
  return { requiresTwoFactor: false, session: response.session };
}
