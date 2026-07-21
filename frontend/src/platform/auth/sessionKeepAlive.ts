import { refreshSession } from '@platform/api/authService';
import {
  getRefreshToken,
  hasStoredSession,
  isAccessTokenExpiringSoon,
} from '@platform/api/tokenStorage';

const ACTIVITY_EVENTS = [
  'pointerdown',
  'keydown',
  'scroll',
  'touchstart',
  'mousemove',
  'visibilitychange',
] as const;

/** Consider the user "active" if they interacted within this window. */
const ACTIVITY_WINDOW_MS = 5 * 60_000;
/** How often to check whether a proactive refresh is needed. */
const KEEP_ALIVE_INTERVAL_MS = 30_000;
/** Minimum gap between activity-triggered refresh attempts. */
const ACTIVITY_REFRESH_THROTTLE_MS = 60_000;

let started = false;
let lastActivityAt = 0;
let lastRefreshAttemptAt = 0;
let intervalId: ReturnType<typeof setInterval> | null = null;

function markActivity() {
  if (typeof document !== 'undefined' && document.visibilityState === 'hidden') {
    return;
  }

  lastActivityAt = Date.now();
}

function isUserRecentlyActive(): boolean {
  return lastActivityAt > 0 && Date.now() - lastActivityAt <= ACTIVITY_WINDOW_MS;
}

async function refreshIfNeeded(force = false): Promise<void> {
  if (!hasStoredSession() || !getRefreshToken()) {
    return;
  }

  if (!force && !isUserRecentlyActive()) {
    return;
  }

  if (!isAccessTokenExpiringSoon()) {
    return;
  }

  const now = Date.now();
  if (!force && now - lastRefreshAttemptAt < ACTIVITY_REFRESH_THROTTLE_MS) {
    return;
  }

  lastRefreshAttemptAt = now;

  try {
    await refreshSession();
  } catch {
    // refreshSession / authorizedFetch already clear tokens and emit session-expired.
  }
}

function onActivity() {
  markActivity();
  void refreshIfNeeded();
}

/**
 * While the user is interacting with the app, renew access (and refresh) tokens
 * before they expire. Idle users are left alone until the next API call or until
 * the refresh token itself expires.
 */
export function startSessionKeepAlive(): void {
  if (started || typeof window === 'undefined') {
    return;
  }

  started = true;
  markActivity();

  for (const eventName of ACTIVITY_EVENTS) {
    window.addEventListener(eventName, onActivity, { passive: true });
  }

  intervalId = setInterval(() => {
    void refreshIfNeeded();
  }, KEEP_ALIVE_INTERVAL_MS);
}

export function stopSessionKeepAlive(): void {
  if (!started || typeof window === 'undefined') {
    return;
  }

  started = false;

  for (const eventName of ACTIVITY_EVENTS) {
    window.removeEventListener(eventName, onActivity);
  }

  if (intervalId) {
    clearInterval(intervalId);
    intervalId = null;
  }
}
