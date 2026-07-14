import type { TableColumnDefinition, TableColumnSizingOptions } from '@fluentui/react-components';
import { AutoFitDataGrid } from '../common/AutoFitDataGrid';
import { leaveTableColumnSizing } from './leaveTableUtils';

interface LeaveSelectableDataGridProps<TItem> {
  items: TItem[];
  columns: TableColumnDefinition<TItem>[];
  selectedIds: string[];
  onSelectionChange: (selectedIds: string[]) => void;
  getRowId: (item: TItem) => string;
  columnSizingOptions?: TableColumnSizingOptions;
  enableColumnSizing?: boolean;
}

export function LeaveSelectableDataGrid<TItem>({
  items,
  columns,
  selectedIds,
  onSelectionChange,
  getRowId,
  columnSizingOptions = leaveTableColumnSizing,
  enableColumnSizing = true,
}: LeaveSelectableDataGridProps<TItem>) {
  return (
    <AutoFitDataGrid
      items={items}
      columns={columns}
      getRowId={getRowId}
      columnSizingOptions={columnSizingOptions}
      enableColumnSizing={enableColumnSizing}
      selectionMode="multiselect"
      selectedIds={selectedIds}
      onSelectionChange={onSelectionChange}
    />
  );
}
