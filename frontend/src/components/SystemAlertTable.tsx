import * as React from "react";

import {
  Avatar,
  Badge,
  DataGridBody,
  DataGridRow,
  DataGrid,
  DataGridHeader,
  DataGridHeaderCell,
  DataGridCell,
  TableCellLayout,
  createTableColumn,
} from "@fluentui/react-components";
import type {
  JSXElement,
  TableColumnDefinition,
} from "@fluentui/react-components";
import {
  formatDateOnlyDisplay,
  isDateOnlyExpired,
  isDateOnlyExpiringSoon,
} from "../utils/dateOnly";
import { stopDataGridRowSelection } from "../utils/dataGrid";

type Driver = {
  id: string;
  driverId: string;
  name: string;
  email: string;
  contactNumber: string;
  gender: "Male" | "Female" | "Other";
  department: string;
  licenceNumber: string;
  licenceIssued: string;   // ISO date string "YYYY-MM-DD"
  licenceExpiry: string;   // ISO date string "YYYY-MM-DD"
};

const items: Driver[] = [
  {
    id: "1",
    driverId: "DRV-001",
    name: "James Carter",
    email: "james.carter@cartrack.co.za",
    contactNumber: "+27 82 341 9870",
    gender: "Male",
    department: "Logistics",
    licenceNumber: "DL-2341987",
    licenceIssued: "2018-03-15",
    licenceExpiry: "2026-03-14",
  },
  {
    id: "2",
    driverId: "DRV-002",
    name: "Aisha Patel",
    email: "aisha.patel@cartrack.co.za",
    contactNumber: "+27 73 550 9234",
    gender: "Female",
    department: "Operations",
    licenceNumber: "DL-5509234",
    licenceIssued: "2020-07-22",
    licenceExpiry: "2028-07-21",
  },
  {
    id: "3",
    driverId: "DRV-003",
    name: "Marcus Osei",
    email: "marcus.osei@cartrack.co.za",
    contactNumber: "+27 61 781 3005",
    gender: "Male",
    department: "Delivery",
    licenceNumber: "DL-7813005",
    licenceIssued: "2015-11-03",
    licenceExpiry: "2025-11-02",
  },
  {
    id: "4",
    driverId: "DRV-004",
    name: "Sophie van der Berg",
    email: "sophie.vdberg@cartrack.co.za",
    contactNumber: "+27 84 102 6478",
    gender: "Female",
    department: "Fleet Management",
    licenceNumber: "DL-1026478",
    licenceIssued: "2021-01-30",
    licenceExpiry: "2029-01-29",
  },
];

const formatDate = (iso: string) => formatDateOnlyDisplay(iso, "en-ZA", "—");

const isExpiringSoon = (iso: string) => isDateOnlyExpiringSoon(iso);

const isExpired = (iso: string) => isDateOnlyExpired(iso);

const columns: TableColumnDefinition<Driver>[] = [
  createTableColumn<Driver>({
    columnId: "driverId",
    compare: (a, b) => a.driverId.localeCompare(b.driverId),
    renderHeaderCell: () => "Driver ID",
    renderCell: (item) => item.driverId,
  }),
  createTableColumn<Driver>({
    columnId: "name",
    compare: (a, b) => a.name.localeCompare(b.name),
    renderHeaderCell: () => "Driver",
    renderCell: (item) => (
      <TableCellLayout
        media={<Avatar aria-label={item.name} name={item.name} />}
      >
        {item.name}
      </TableCellLayout>
    ),
  }),
  createTableColumn<Driver>({
    columnId: "email",
    compare: (a, b) => a.email.localeCompare(b.email),
    renderHeaderCell: () => "Email",
    renderCell: (item) => item.email,
  }),
  createTableColumn<Driver>({
    columnId: "contactNumber",
    compare: (a, b) => a.contactNumber.localeCompare(b.contactNumber),
    renderHeaderCell: () => "Contact Number",
    renderCell: (item) => item.contactNumber,
  }),
  createTableColumn<Driver>({
    columnId: "gender",
    compare: (a, b) => a.gender.localeCompare(b.gender),
    renderHeaderCell: () => "Gender",
    renderCell: (item) => item.gender,
  }),
  createTableColumn<Driver>({
    columnId: "department",
    compare: (a, b) => a.department.localeCompare(b.department),
    renderHeaderCell: () => "Department",
    renderCell: (item) => item.department,
  }),
  createTableColumn<Driver>({
    columnId: "licenceNumber",
    compare: (a, b) => a.licenceNumber.localeCompare(b.licenceNumber),
    renderHeaderCell: () => "Licence Number",
    renderCell: (item) => item.licenceNumber,
  }),
  createTableColumn<Driver>({
    columnId: "licenceIssued",
    compare: (a, b) => a.licenceIssued.localeCompare(b.licenceIssued),
    renderHeaderCell: () => "Licence Issued",
    renderCell: (item) => formatDate(item.licenceIssued),
  }),
  createTableColumn<Driver>({
    columnId: "licenceExpiry",
    compare: (a, b) => a.licenceExpiry.localeCompare(b.licenceExpiry),
    renderHeaderCell: () => "Licence Expiry",
    renderCell: (item) => (
      <TableCellLayout
        media={
          isExpired(item.licenceExpiry) ? (
            <Badge color="danger" appearance="filled">Expired</Badge>
          ) : isExpiringSoon(item.licenceExpiry) ? (
            <Badge color="warning" appearance="filled">Expiring soon</Badge>
          ) : null
        }
      >
        {formatDate(item.licenceExpiry)}
      </TableCellLayout>
    ),
  }),
];

export const SystemAlertTable = (): JSXElement => {
  return (
    <DataGrid
      items={items}
      columns={columns}
      sortable
      selectionMode="multiselect"
      getRowId={(item) => item.id}
      focusMode="composite"
      size="medium"
      style={{ minWidth: "550px" }}
    >
      <DataGridHeader>
        <DataGridRow
          selectionCell={{
            checkboxIndicator: { "aria-label": "Select all rows" },
          }}
        >
          {({ renderHeaderCell }) => (
            <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
          )}
        </DataGridRow>
      </DataGridHeader>
      <DataGridBody<Driver>>
        {({ item, rowId }) => (
          <DataGridRow<Driver>
            key={rowId}
            selectionCell={{
              checkboxIndicator: { "aria-label": "Select row" },
            }}
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
  );
};