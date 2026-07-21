/** Dispatched when the refresh token fails and the session must end. */
export const SESSION_EXPIRED_EVENT = 'cartrack:session-expired';

export function dispatchSessionExpired(reason = 'refresh_failed'): void {
  window.dispatchEvent(new CustomEvent(SESSION_EXPIRED_EVENT, { detail: { reason } }));
}
