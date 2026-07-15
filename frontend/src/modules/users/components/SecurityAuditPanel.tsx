import { useCallback, useEffect, useState } from 'react';
import type { TableColumnDefinition } from '@fluentui/react-components';
import {
  DataGrid,
  DataGridBody,
  DataGridCell,
  DataGridHeader,
  DataGridHeaderCell,
  DataGridRow,
  Spinner,
  Subtitle2,
  TableCellLayout,
  Text,
  createTableColumn,
} from '@fluentui/react-components';
import { ApiError } from '@platform/api/apiClient';
import { getSecurityAuditLog } from '@modules/users/services/userService';
import type { SecurityAuditLogEntry } from '@modules/users/types/audit';
import { stopDataGridRowSelection } from '@platform/utils/dataGrid';
import AppTitle from '@platform/ui/AppTitle';

function formatAction(action: string): string {
  return action.split('.').join(' · ').split('_').join(' ');
}

function formatDateTime(value: string): string {
  return new Date(value).toLocaleString('en-ZA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function SecurityAuditPanel() {
  const [items, setItems] = useState<SecurityAuditLogEntry[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadAuditLog = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await getSecurityAuditLog(100);
      setItems(response.items);
    } catch (loadError) {
      const message = loadError instanceof ApiError
        ? loadError.message
        : 'Failed to load security audit log.';
      setError(message);
      setItems([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadAuditLog();
  }, [loadAuditLog]);

  const columns: TableColumnDefinition<SecurityAuditLogEntry>[] = [
    createTableColumn<SecurityAuditLogEntry>({
      columnId: 'when',
      renderHeaderCell: () => 'When',
      renderCell: (item) => formatDateTime(item.createdAt),
    }),
    createTableColumn<SecurityAuditLogEntry>({
      columnId: 'action',
      renderHeaderCell: () => 'Action',
      renderCell: (item) => formatAction(item.action),
    }),
    createTableColumn<SecurityAuditLogEntry>({
      columnId: 'actor',
      renderHeaderCell: () => 'Actor',
      renderCell: (item) => (
        <TableCellLayout>
          <Text>{item.actorDisplayName ?? item.actorUserId}</Text>
        </TableCellLayout>
      ),
    }),
    createTableColumn<SecurityAuditLogEntry>({
      columnId: 'target',
      renderHeaderCell: () => 'Target',
      renderCell: (item) => item.targetDisplayName ?? item.targetUserId ?? '—',
    }),
    createTableColumn<SecurityAuditLogEntry>({
      columnId: 'details',
      renderHeaderCell: () => 'Details',
      renderCell: (item) => item.details ?? '—',
    }),
  ];

  return (
    <div className="flex flex-col gap-4 max-w-8xl">
      <div className="flex flex-col gap-1">
        <AppTitle title="Security Audit Log" subtitle="Recent role, access, password, and MFA changes." />
      </div>

      {isLoading && <Spinner label="Loading audit log..." />}
      {error && <Text className="text-sm text-red-600">{error}</Text>}

      {!isLoading && !error && (
        <DataGrid
          items={items}
          columns={columns}
          getRowId={(item) => item.id}
          onSelectionChange={stopDataGridRowSelection}
          {...{
             resizableColumns: true,
             resizableColumnsOptions: { autoFitColumns: true },
          }}
        >
          <DataGridHeader>
            <DataGridRow>
              {({ renderHeaderCell }) => (
                <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
              )}
            </DataGridRow>
          </DataGridHeader>
          <DataGridBody<SecurityAuditLogEntry>>
            {({ item, rowId }) => (
              <DataGridRow<SecurityAuditLogEntry> key={rowId}>
                {({ renderCell }) => (
                  <DataGridCell>{renderCell(item)}</DataGridCell>
                )}
              </DataGridRow>
            )}
          </DataGridBody>
        </DataGrid>
      )}
    </div>
  );
}
