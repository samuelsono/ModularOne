export const CURRENT_MODULE_STORAGE_KEY = 'talisTrack.currentModule';
export const USE_DEFAULT_MODULE_ON_LOGIN_KEY = 'talisTrack.useDefaultModuleOnLogin';

export function moduleStorageKey(base: string, userId: string) {
  return `${base}.${userId}`;
}

export function readStoredCurrentModule(userId: string): string | null {
  return localStorage.getItem(moduleStorageKey(CURRENT_MODULE_STORAGE_KEY, userId));
}

export function writeStoredCurrentModule(userId: string, moduleSlug: string) {
  localStorage.setItem(moduleStorageKey(CURRENT_MODULE_STORAGE_KEY, userId), moduleSlug);
}

export function markUseDefaultModuleOnLogin() {
  sessionStorage.setItem(USE_DEFAULT_MODULE_ON_LOGIN_KEY, '1');
}

export function consumeUseDefaultModuleOnLogin(): boolean {
  const shouldUseDefault = sessionStorage.getItem(USE_DEFAULT_MODULE_ON_LOGIN_KEY) === '1';
  if (shouldUseDefault) {
    sessionStorage.removeItem(USE_DEFAULT_MODULE_ON_LOGIN_KEY);
  }
  return shouldUseDefault;
}
