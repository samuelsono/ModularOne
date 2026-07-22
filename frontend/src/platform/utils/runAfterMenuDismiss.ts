/**
 * Fluent Menu applies aria-hidden / modal focus locks while open.
 * Opening a Dialog in the same tick leaves inputs unfocusable.
 * Defer until the menu has fully dismissed and Tabster has cleaned up.
 */
export function runAfterMenuDismiss(action: () => void): void {
  window.requestAnimationFrame(() => {
    window.setTimeout(action, 0);
  });
}
