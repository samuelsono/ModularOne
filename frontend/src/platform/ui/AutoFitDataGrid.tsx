import { useCallback, useMemo, useState } from 'react';
import type { TableColumnDefinition, TableColumnSizingOptions } from '@fluentui/react-components';
import {
  DataGrid,
  DataGridBody,
  DataGridCell,
  DataGridHeader,
  DataGridHeaderCell,
  DataGridRow,
} from '@fluentui/react-components';
import { buildColumnSizingOptions } from '@platform/utils/dataGridColumnSizing';
import { mergePersistedSizing, type StoredColumnWidths } from '@platform/utils/usePersistedColumnSizing';
import { usePersistedColumnSizing } from '@platform/utils/usePersistedColumnSizing';
import { stopDataGridRowSelection } from '@platform/utils/dataGrid';

type AutoFitDataGridProps<TItem> = {
  items: TItem[];
  columns: TableColumnDefinition<TItem>[];
  getRowId: (item: TItem) => string;
  columnSizingOptions?: TableColumnSizingOptions;
  enableColumnSizing?: boolean;
  /** Stable id used to persist resized column widths in localStorage. */
  storageKey?: string;
  sortable?: boolean;
  selectionMode?: 'multiselect' | 'single' | undefined;
  selectedIds?: string[];
  onSelectionChange?: (selectedIds: string[]) => void;
  size?: 'small' | 'extra-small' | 'medium';
};

/**
 * IMPORTANT: Do not pass a `style` prop to DataGridHeaderCell / DataGridCell when
 * resizableColumns is enabled. Fluent injects width/minWidth/maxWidth via
 * getTableHeaderCellProps / getTableCellProps, then spreads caller props on top —
 * so a custom `style` replaces those widths and column resizing appears broken.
 * Sticky Actions styling must use className instead.
 */
export function AutoFitDataGrid<TItem>({
  items,
  columns,
  getRowId,
  columnSizingOptions,
  enableColumnSizing = true,
  storageKey,
  sortable,
  selectionMode,
  selectedIds,
  onSelectionChange,
  size = 'medium',
}: AutoFitDataGridProps<TItem>) {
  const orderedColumns = useMemo(() => {
    const nonActionColumns = columns.filter(
      (column) => String(column.columnId) !== 'actions',
    );
    const actionColumns = columns.filter(
      (column) => String(column.columnId) === 'actions',
    );

    return [...nonActionColumns, ...actionColumns];
  }, [columns]);

  const baseOverrides = useMemo(
    () => columnSizingOptions ?? {},
    [columnSizingOptions],
  );

  const [sessionColumnWidths, setSessionColumnWidths] = useState<StoredColumnWidths>({});

  const persisted = usePersistedColumnSizing(
    enableColumnSizing ? storageKey : undefined,
    orderedColumns,
    baseOverrides,
  );

  const transientBaseSizing = useMemo(
    () => buildColumnSizingOptions(orderedColumns, columnSizingOptions),
    [columnSizingOptions, orderedColumns],
  );

  const resolvedColumnSizing = useMemo(() => {
    if (!enableColumnSizing) {
      return transientBaseSizing;
    }

    if (storageKey) {
      return persisted.columnSizingOptions;
    }

    return mergePersistedSizing(transientBaseSizing, sessionColumnWidths);
  }, [enableColumnSizing, persisted.columnSizingOptions, sessionColumnWidths, storageKey, transientBaseSizing]);

  const resolveColumnId = useCallback((columnId: string | number): string => {
    if (typeof columnId === 'number' && Number.isInteger(columnId)) {
      const byIndex = orderedColumns[columnId];
      if (byIndex) {
        return String(byIndex.columnId);
      }
    }
    return String(columnId);
  }, [orderedColumns]);

  const handleTransientResize = useCallback((
    _event: unknown,
    data: { columnId: string | number; width: number },
  ) => {
    const columnId = resolveColumnId(data.columnId);
    if (columnId === 'actions') {
      return;
    }

    setSessionColumnWidths((current) => ({
      ...current,
      [columnId]: Math.round(data.width),
    }));
  }, [resolveColumnId]);

  const handlePersistedResize = useCallback((
    event: unknown,
    data: { columnId: string | number; width: number },
  ) => {
    if (resolveColumnId(data.columnId) === 'actions') {
      return;
    }
    persisted.onColumnResize?.(event, data);
  }, [persisted, resolveColumnId]);

  const selectable = Boolean(selectionMode);

  return (
    <div className="autofit-datagrid w-full min-w-0 overflow-x-auto">
      <DataGrid
        items={items}
        columns={orderedColumns}
        getRowId={getRowId}
        sortable={sortable}
        size={size}
        focusMode="composite"
        selectionMode={selectionMode}
        selectedItems={selectedIds}
        onSelectionChange={(_, data) => {
          onSelectionChange?.(Array.from(data.selectedItems, String));
        }}
        {...(enableColumnSizing
          ? {
            resizableColumns: true,
            columnSizingOptions: resolvedColumnSizing,
            resizableColumnsOptions: { autoFitColumns: false },
            onColumnResize: storageKey ? handlePersistedResize : handleTransientResize,
          }
          : {})}
        style={{ minWidth: 'fit-content', width: '100%' }}
      >
        <DataGridHeader>
          <DataGridRow
            {...(selectable
              ? { selectionCell: { checkboxIndicator: { 'aria-label': 'Select all rows' } } }
              : {})}
          >
            {({ renderHeaderCell, columnId }) => (
              <DataGridHeaderCell
                className={String(columnId) === 'actions' ? 'autofit-datagrid-actions autofit-datagrid-actions--header' : undefined}
              >
                {renderHeaderCell()}
              </DataGridHeaderCell>
            )}
          </DataGridRow>
        </DataGridHeader>
        <DataGridBody<TItem>>
          {({ item, rowId }) => (
            <DataGridRow<TItem>
              key={rowId}
              {...(selectable
                ? { selectionCell: { checkboxIndicator: { 'aria-label': 'Select row' } } }
                : {})}
            >
              {({ renderCell, columnId }) => (
                <DataGridCell
                  onClick={stopDataGridRowSelection}
                  className={String(columnId) === 'actions' ? 'autofit-datagrid-actions' : undefined}
                >
                  {renderCell(item)}
                </DataGridCell>
              )}
            </DataGridRow>
          )}
        </DataGridBody>
      </DataGrid>
    </div>
  );
}
