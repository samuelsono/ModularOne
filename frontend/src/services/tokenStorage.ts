const ACCESS_TOKEN_KEY = 'cartrack.accessToken';
const REFRESH_TOKEN_KEY = 'cartrack.refreshToken';
const REMEMBER_ME_KEY = 'cartrack.rememberMe';

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

export function getAccessToken(): string | null {
  return sessionStorage.getItem(ACCESS_TOKEN_KEY)
    ?? localStorage.getItem(ACCESS_TOKEN_KEY);
}

export function getRefreshToken(): string | null {
  return getActiveStorage().getItem(REFRESH_TOKEN_KEY);
}

export function setTokens(accessToken: string, refreshToken: string, rememberMe: boolean): void {
  setRememberMe(rememberMe);

  sessionStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
  localStorage.removeItem(ACCESS_TOKEN_KEY);

  const refreshStorage = getStorage(rememberMe ? 'local' : 'session');
  const otherStorage = getStorage(rememberMe ? 'session' : 'local');

  refreshStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  otherStorage.removeItem(REFRESH_TOKEN_KEY);
}

export function clearTokens(): void {
  sessionStorage.removeItem(ACCESS_TOKEN_KEY);
  localStorage.removeItem(ACCESS_TOKEN_KEY);
  sessionStorage.removeItem(REFRESH_TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
  localStorage.removeItem(REMEMBER_ME_KEY);
}

export function hasStoredSession(): boolean {
  return Boolean(getAccessToken() || getRefreshToken());
}
