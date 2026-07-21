const ACCESS_TOKEN_KEY = 'cartrack.accessToken';
const REFRESH_TOKEN_KEY = 'cartrack.refreshToken';
const REMEMBER_ME_KEY = 'cartrack.rememberMe';
const ACCESS_EXPIRES_AT_KEY = 'cartrack.accessTokenExpiresAt';

type StorageKind = 'local' | 'session';

function getStorage(kind: StorageKind): Storage {
  return kind === 'local' ? localStorage : sessionStorage;
}

function getActiveStorage(): Storage {
  const rememberMe = localStorage.getItem(REMEMBER_ME_KEY) === 'true';
  return getStorage(rememberMe ? 'local' : 'session');
}

export function setRememberMe(rememberMe: boolean): void {
  if (rememberMe) {
    localStorage.setItem(REMEMBER_ME_KEY, 'true');
    return;
  }

  localStorage.removeItem(REMEMBER_ME_KEY);
}

export function getRememberMe(): boolean {
  return localStorage.getItem(REMEMBER_ME_KEY) === 'true';
}

export function getAccessToken(): string | null {
  return sessionStorage.getItem(ACCESS_TOKEN_KEY)
    ?? localStorage.getItem(ACCESS_TOKEN_KEY);
}

export function getRefreshToken(): string | null {
  return getActiveStorage().getItem(REFRESH_TOKEN_KEY);
}

export function getAccessTokenExpiresAt(): number | null {
  const raw = sessionStorage.getItem(ACCESS_EXPIRES_AT_KEY)
    ?? localStorage.getItem(ACCESS_EXPIRES_AT_KEY);
  if (!raw) {
    return null;
  }

  const value = Number.parseInt(raw, 10);
  return Number.isFinite(value) ? value : null;
}

export function setTokens(
  accessToken: string,
  refreshToken: string,
  rememberMe: boolean,
  expiresInSeconds?: number,
): void {
  setRememberMe(rememberMe);

  sessionStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
  localStorage.removeItem(ACCESS_TOKEN_KEY);

  const refreshStorage = getStorage(rememberMe ? 'local' : 'session');
  const otherStorage = getStorage(rememberMe ? 'session' : 'local');

  refreshStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  otherStorage.removeItem(REFRESH_TOKEN_KEY);

  const expiresAt = Date.now() + Math.max(30, expiresInSeconds ?? 15 * 60) * 1000;
  sessionStorage.setItem(ACCESS_EXPIRES_AT_KEY, String(expiresAt));
  localStorage.removeItem(ACCESS_EXPIRES_AT_KEY);
}

export function clearTokens(): void {
  sessionStorage.removeItem(ACCESS_TOKEN_KEY);
  localStorage.removeItem(ACCESS_TOKEN_KEY);
  sessionStorage.removeItem(REFRESH_TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
  sessionStorage.removeItem(ACCESS_EXPIRES_AT_KEY);
  localStorage.removeItem(ACCESS_EXPIRES_AT_KEY);
  localStorage.removeItem(REMEMBER_ME_KEY);
}

export function hasStoredSession(): boolean {
  return Boolean(getAccessToken() || getRefreshToken());
}

/** True when the access token is missing, expired, or within `skewMs` of expiry. */
export function isAccessTokenExpiringSoon(skewMs = 120_000): boolean {
  const expiresAt = getAccessTokenExpiresAt();
  if (!getAccessToken()) {
    return true;
  }

  if (expiresAt == null) {
    return true;
  }

  return Date.now() >= expiresAt - skewMs;
}
