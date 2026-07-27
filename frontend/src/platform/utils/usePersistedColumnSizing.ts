import { useCallback, useMemo, useRef, useState } from 'react';
import type { TableColumnDefinition, TableColumnSizingOptions } from '@fluentui/react-components';
import { buildColumnSizingOptions } from '@platform/utils/dataGridColumnSizing';

export type StoredColumnWidths = Record<string, number>;

const STORAGE_PREFIX = 'cartrack.datagrid.columnWidths.';

export function columnWidthStorageKey(tableId: string): string {
  return `${STORAGE_PREFIX}${tableId}`;
}

export function loadColumnWidths(storageKey: string): StoredColumnWidths {
  try {
    const raw = localStorage.getItem(storageKey);
    if (!raw) {
      return {};
    }

    const parsed = JSON.parse(raw) as unknown;
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
      return {};
    }

    const widths: StoredColumnWidths = {};
    for (const [columnId, value] of Object.entries(parsed)) {
      if (typeof value === 'number' && Number.isFinite(value) && value > 0) {
        widths[columnId] = Math.round(value);
      }
    }

    return widths;
  } catch {
    return {};
  }
}

export function saveColumnWidths(storageKey: string, widths: StoredColumnWidths): void {
  try {
    localStorage.setItem(storageKey, JSON.stringify(widths));
  } catch {
    // Ignore quota / private mode failures.
  }
}

export function mergePersistedSizing(
  base: TableColumnSizingOptions,
  persisted: StoredColumnWidths,
): TableColumnSizingOptions {
  const merged: TableColumnSizingOptions = {};

  for (const [columnId, options] of Object.entries(base)) {
    const width = persisted[columnId];
    if (width == null) {
      merged[columnId] = options;
      continue;
    }

    const minWidth = options?.minWidth ?? 80;
    const clamped = Math.max(minWidth, width);
    merged[columnId] = {
      ...options,
      defaultWidth: clamped,
      idealWidth: clamped,
    };
  }

  return merged;
}

export function usePersistedColumnSizing<TItem>(
  storageKey: string | undefined,
  columns: TableColumnDefinition<TItem>[],
  baseOverrides: TableColumnSizingOptions = {},
) {
  const [persistedWidths, setPersistedWidths] = useState<StoredColumnWidths>(() => (
    storageKey ? loadColumnWidths(storageKey) : {}
  ));
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const baseSizing = useMemo(
    () => buildColumnSizingOptions(columns, baseOverrides),
    [columns, baseOverrides],
  );

  const columnSizingOptions = useMemo(
    () => (storageKey ? mergePersistedSizing(baseSizing, persistedWidths) : baseSizing),
    [baseSizing, persistedWidths, storageKey],
  );

  const resolveColumnId = useCallback((columnId: string | number): string => {
    if (typeof columnId === 'number' && Number.isInteger(columnId)) {
      const byIndex = columns[columnId];
      if (byIndex) {
        return String(byIndex.columnId);
      }
    }

    const byExactMatch = columns.find(
      (column) => String(column.columnId) === String(columnId),
    );
    if (byExactMatch) {
      return String(byExactMatch.columnId);
    }

    return String(columnId);
  }, [columns]);

  const onColumnResize = useCallback((
    _event: unknown,
    data: { columnId: string | number; width: number },
  ) => {
    if (!storageKey) {
      return;
    }

    const columnId = resolveColumnId(data.columnId);
    const width = Math.round(data.width);
    setPersistedWidths((current) => {
      const next = { ...current, [columnId]: width };
      if (saveTimerRef.current) {
        clearTimeout(saveTimerRef.current);
      }
      saveTimerRef.current = setTimeout(() => {
        saveColumnWidths(storageKey, next);
      }, 150);
      return next;
    });
  }, [resolveColumnId, storageKey]);

  return {
    columnSizingOptions,
    onColumnResize: storageKey ? onColumnResize : undefined,
  };
}
