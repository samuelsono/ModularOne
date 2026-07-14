import type { SyntheticEvent } from 'react';

/** Prevent Fluent DataGrid row click from toggling selection. */
export function stopDataGridRowSelection(event: SyntheticEvent) {
  event.stopPropagation();
}
