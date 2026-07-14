import {
  DataGrid,
  DataGridBody,
  DataGridCell,
  DataGridHeader,
  DataGridHeaderCell,
  DataGridRow,
  createTableColumn,
  Text,
} from '@fluentui/react-components';
import type { TableColumnDefinition } from '@fluentui/react-components';

import type { ReportExecutionResult } from '../../types/report';

interface ApiTableWidgetProps {
  data: ReportExecutionResult;
}

type TableRow = {
  id: string;
  values: Record<string, string>;
};

function formatCellValue(value: string | number | null | undefined): string {
  if (value == null) {
    return '—';
  }

  return String(value);
}

export function ApiTableWidget({ data }: ApiTableWidgetProps) {
  const columns = data.columns;
  const rows: TableRow[] = data.rows.map((row, index) => ({
    id: String(index),
    values: Object.fromEntries(columns.map((column, columnIndex) => [column, formatCellValue(row[columnIndex])])),
  }));

  const tableColumns: TableColumnDefinition<TableRow>[] = columns.map((column) =>
    createTableColumn<TableRow>({
      columnId: column,
      renderHeaderCell: () => <Text weight="semibold">{column}</Text>,
      renderCell: (item) => <Text>{item.values[column] ?? '—'}</Text>,
    }),
  );

  if (columns.length === 0) {
    return <Text className="px-2 py-3 text-neutral-600">No columns configured for this API table.</Text>;
  }

  return (
    <DataGrid
      items={rows}
      columns={tableColumns}
      getRowId={(item) => item.id}
      size="small"
    >
      <DataGridHeader>
        <DataGridRow>
          {({ renderHeaderCell }) => (
            <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
          )}
        </DataGridRow>
      </DataGridHeader>
      <DataGridBody<TableRow>>
        {({ item, rowId }) => (
          <DataGridRow<TableRow> key={rowId}>
            {({ renderCell }) => (
              <DataGridCell>{renderCell(item)}</DataGridCell>
            )}
          </DataGridRow>
        )}
      </DataGridBody>
    </DataGrid>
  );
}
