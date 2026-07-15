import * as React from 'react';
import {
  Avatar,
  Badge,
  MessageBar,
  MessageBarBody,
  Spinner,
  TableCellLayout,
  createTableColumn,
} from '@fluentui/react-components';
import type { JSXElement, TableColumnDefinition } from '@fluentui/react-components';
import type { Driver } from '@modules/fleet/types/driver';
import { EditDriver } from './drivers/EditDriver';
import {
  formatDateOnlyDisplay,
  isDateOnlyExpired,
  isDateOnlyExpiringSoon,
} from '@platform/utils/dateOnly';
import { CreateDriver } from './drivers/CreateDriver';
import { PersonKeyRegular } from '@fluentui/react-icons';
import { stopDataGridRowSelection } from '@platform/utils/dataGrid';
import { AutoFitDataGrid } from '@platform/ui/AutoFitDataGrid';
import { withAuditableColumns } from '@platform/ui/auditTableColumns';
import { trackingTableColumnSizing } from '@platform/utils/dataGridColumnSizing';

interface DriversTableProps {
  items: Driver[];
  isLoading?: boolean;
  error?: string | null;
  onUpdated?: () => void;
  onViewDetails?: (driver: Driver) => void;
}

const formatDate = (iso: string | null) => formatDateOnlyDisplay(iso, 'en-ZA', '--');

const isExpiringSoon = (iso: string | null) => isDateOnlyExpiringSoon(iso);

const isExpired = (iso: string | null) => isDateOnlyExpired(iso);

const clickableName: React.CSSProperties = {
  cursor: 'pointer',
  color: 'var(--colorBrandForeground3)',
  fontWeight: 500,
  border: 'none',
  background: 'transparent',
  padding: 0,
  font: 'inherit',
  textAlign: 'left',
};

export const DriversTable = ({
  items,
  isLoading = false,
  error = null,
  onUpdated,
  onViewDetails,
}: DriversTableProps): JSXElement => {
  const columns = React.useMemo<TableColumnDefinition<Driver>[]>(() => withAuditableColumns([
    createTableColumn<Driver>({
      columnId: 'driverId',
      compare: (a, b) => a.driverId.localeCompare(b.driverId),
      renderHeaderCell: () => 'Driver ID',
       renderCell: (item) => (
        <TableCellLayout>
          <button
            type="button"
            onClick={(event) => {
              stopDataGridRowSelection(event);
              onViewDetails?.(item);
            }}
            style={clickableName}
          >
            {item.driverId}
          </button>
        </TableCellLayout>
      ),
    }),
    createTableColumn<Driver>({
      columnId: 'name',
      compare: (a, b) => a.name.localeCompare(b.name),
      renderHeaderCell: () => 'Driver',
      renderCell: (item) => (
        <TableCellLayout media={<Avatar aria-label={item.name} name={item.name} />}>
          <button
            type="button"
            onClick={(event) => {
              stopDataGridRowSelection(event);
              onViewDetails?.(item);
            }}
            style={clickableName}
          >
            {item.name}
          </button>
        </TableCellLayout>
      ),
    }),
    createTableColumn<Driver>({
      columnId: 'email',
      compare: (a, b) => a.email.localeCompare(b.email),
      renderHeaderCell: () => 'Email',
      renderCell: (item) => <span className="whitespace-nowrap">{item.email}</span>,
    }),
    createTableColumn<Driver>({
      columnId: 'contactNumber',
      compare: (a, b) => a.contactNumber.localeCompare(b.contactNumber),
      renderHeaderCell: () => 'Contact Number',
      renderCell: (item) => <span className="whitespace-nowrap">{item.contactNumber}</span>,
    }),
    createTableColumn<Driver>({
      columnId: 'gender',
      compare: (a, b) => a.gender.localeCompare(b.gender),
      renderHeaderCell: () => 'Gender',
      renderCell: (item) => <span className="whitespace-nowrap">{item.gender}</span>,
    }),
    createTableColumn<Driver>({
      columnId: 'department',
      compare: (a, b) => a.department.localeCompare(b.department),
      renderHeaderCell: () => 'Department',
      renderCell: (item) => <span className="whitespace-nowrap">{item.department || '--'}</span>,
    }),
    createTableColumn<Driver>({
      columnId: 'licenceNumber',
      compare: (a, b) => a.licenceNumber.localeCompare(b.licenceNumber),
      renderHeaderCell: () => 'Licence Number',
      renderCell: (item) => <span className="whitespace-nowrap">{item.licenceNumber}</span>,
    }),
    createTableColumn<Driver>({
      columnId: 'licenceIssued',
      compare: (a, b) => (a.licenceIssued ?? '').localeCompare(b.licenceIssued ?? ''),
      renderHeaderCell: () => 'Licence Issued',
      renderCell: (item) => <span className="whitespace-nowrap">{formatDate(item.licenceIssued)}</span>,
    }),
    createTableColumn<Driver>({
      columnId: 'actions',
      renderHeaderCell: () => 'Actions',
      renderCell: (item) => <EditDriver driver={item} onUpdated={onUpdated} />,
    }),
    createTableColumn<Driver>({
      columnId: 'licenceExpiry',
      compare: (a, b) => (a.licenceExpiry ?? '').localeCompare(b.licenceExpiry ?? ''),
      renderHeaderCell: () => 'Licence Expiry',
      renderCell: (item) => (
        <TableCellLayout
          media={
            item.licenceExpiry && isExpired(item.licenceExpiry) ? (
              <Badge color="danger" appearance="filled">Expired</Badge>
            ) : item.licenceExpiry && isExpiringSoon(item.licenceExpiry) ? (
              <Badge color="warning" appearance="filled">Expiring soon</Badge>
            ) : null
          }
        >
          <span className="whitespace-nowrap">{formatDate(item.licenceExpiry)}</span>
        </TableCellLayout>
      ),
    }),
  ]), [onUpdated, onViewDetails]);

  if (isLoading) {
    return (
      <div className="flex justify-center p-6">
        <Spinner size="medium" label="Loading drivers..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-3">
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="h-full w-full flex flex-col gap-3 items-center justify-center p-6 text-sm text-neutral-foreground-3">
        <PersonKeyRegular className='size-26 text-gray-300' />
        No drivers found matching query.
        <CreateDriver onCreated={onUpdated} />
      </div>
    );
  }

  return (
    <div style={{ overflowX: 'auto', width: '100%' }}>
      <AutoFitDataGrid
        items={items}
        columns={columns}
        sortable
        columnSizingOptions={trackingTableColumnSizing}
        selectionMode="multiselect"
        getRowId={(item) => item.id}
      />
    </div>
  );
};
