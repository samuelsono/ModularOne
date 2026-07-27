import { createTableColumn, type TableColumnDefinition } from '@fluentui/react-components';

export interface AuditableFields {
  createdAt?: string | null;
  createdByUserId?: string | null;
  createdByDisplayName?: string | null;
  updatedAt?: string | null;
  updatedByUserId?: string | null;
  updatedByDisplayName?: string | null;
}

export function formatAuditDateTime(value: string | null | undefined): string {
  if (!value) {
    return '—';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleString('en-ZA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function formatAuditUser(displayName: string | null | undefined): string {
  return displayName?.trim() || '—';
}

/** Standard IAuditable columns for user-facing data grids. */
export function createAuditableColumns<TItem extends AuditableFields>(): TableColumnDefinition<TItem>[] {
  return [
    createTableColumn<TItem>({
      columnId: 'createdAt',
      renderHeaderCell: () => 'Created',
      renderCell: (item) => (
        <span className="whitespace-nowrap">{formatAuditDateTime(item.createdAt)}</span>
      ),
    }),
    createTableColumn<TItem>({
      columnId: 'createdBy',
      renderHeaderCell: () => 'Created by',
      renderCell: (item) => (
        <span className="whitespace-nowrap">{formatAuditUser(item.createdByDisplayName)}</span>
      ),
    }),
    createTableColumn<TItem>({
      columnId: 'updatedAt',
      renderHeaderCell: () => 'Updated',
      renderCell: (item) => (
        <span className="whitespace-nowrap">{formatAuditDateTime(item.updatedAt)}</span>
      ),
    }),
    createTableColumn<TItem>({
      columnId: 'updatedBy',
      renderHeaderCell: () => 'Updated by',
      renderCell: (item) => (
        <span className="whitespace-nowrap">{formatAuditUser(item.updatedByDisplayName)}</span>
      ),
    }),
  ];
}

export function withAuditableColumns<TItem extends AuditableFields>(
  columns: TableColumnDefinition<TItem>[],
): TableColumnDefinition<TItem>[] {
  const existing = new Set(columns.map((column) => String(column.columnId)));
  const auditColumns = createAuditableColumns<TItem>().filter(
    (column) => !existing.has(String(column.columnId)),
  );

  const nonActionColumns = columns.filter(
    (column) => String(column.columnId) !== 'actions',
  );
  const actionColumns = columns.filter(
    (column) => String(column.columnId) === 'actions',
  );

  return [...nonActionColumns, ...auditColumns, ...actionColumns];
}
