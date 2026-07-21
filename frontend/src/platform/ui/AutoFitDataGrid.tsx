import { useMemo } from 'react';
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
  const baseOverrides = useMemo(
    () => columnSizingOptions ?? {},
    [columnSizingOptions],
  );

  const persisted = usePersistedColumnSizing(
    enableColumnSizing ? storageKey : undefined,
    columns,
    baseOverrides,
  );

  const resolvedColumnSizing = useMemo(
    () => (storageKey && enableColumnSizing
      ? persisted.columnSizingOptions
      : buildColumnSizingOptions(columns, columnSizingOptions)),
    [columns, columnSizingOptions, enableColumnSizing, persisted.columnSizingOptions, storageKey],
  );

  const selectable = Boolean(selectionMode);

  return (
    <div className="w-full min-w-0">
      <DataGrid
        items={items}
        columns={columns}
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
            onColumnResize: persisted.onColumnResize,
          }
          : {})}
        style={{ width: '100%' }}
      >
        <DataGridHeader>
          <DataGridRow
            {...(selectable
              ? { selectionCell: { checkboxIndicator: { 'aria-label': 'Select all rows' } } }
              : {})}
          >
            {({ renderHeaderCell }) => (
              <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
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
              {({ renderCell }) => (
                <DataGridCell onClick={stopDataGridRowSelection}>
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
