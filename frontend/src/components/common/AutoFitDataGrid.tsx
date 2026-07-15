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
import { buildColumnSizingOptions } from '../../utils/dataGridColumnSizing';
import { stopDataGridRowSelection } from '../../utils/dataGrid';

type AutoFitDataGridProps<TItem> = {
  items: TItem[];
  columns: TableColumnDefinition<TItem>[];
  getRowId: (item: TItem) => string;
  columnSizingOptions?: TableColumnSizingOptions;
  enableColumnSizing?: boolean;
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
  sortable,
  selectionMode,
  selectedIds,
  onSelectionChange,
  size = 'medium',
}: AutoFitDataGridProps<TItem>) {
  const resolvedColumnSizing = useMemo(
    () => buildColumnSizingOptions(columns, columnSizingOptions),
    [columns, columnSizingOptions],
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
