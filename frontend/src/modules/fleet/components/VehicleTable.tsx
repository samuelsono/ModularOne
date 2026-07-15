import * as React from "react";

import {
  VehicleCarRegular,
  EditRegular,
  LocationRegular,
  MoreHorizontalRegular,
  EyeRegular,
  PersonAddRegular,
  DocumentBulletListRegular,
  DeleteRegular,
} from "@fluentui/react-icons";
import {
  Avatar,
  Badge,
  Button,
  Menu,
  MenuDivider,
  MenuItem,
  MenuList,
  MenuPopover,
  MenuTrigger,
  MessageBar,
  MessageBarBody,
  Spinner,
  TableCellLayout,
  Tooltip,
  createTableColumn,
} from "@fluentui/react-components";
import type {
  JSXElement,
  TableColumnDefinition,
  TableRowId,
} from "@fluentui/react-components";
import type { Vehicle } from '@modules/fleet/types/vehicle';
import {
  formatDateOnlyDisplay,
  isDateOnlyExpired,
  isDateOnlyExpiringSoon,
} from '@platform/utils/dateOnly';
import { CreateVehicle } from "./vehicles/CreateVehicle";
import { stopDataGridRowSelection } from '@platform/utils/dataGrid';
import { AutoFitDataGrid } from '@platform/ui/AutoFitDataGrid';
import { withAuditableColumns } from '@platform/ui/auditTableColumns';
import { trackingTableColumnSizing } from '@platform/utils/dataGridColumnSizing';

interface VehicleTableProps {
  items: Vehicle[];
  isLoading?: boolean;
  error?: string | null;
  selectedIds?: Iterable<TableRowId>;
  onSelectionChange?: (selectedIds: string[]) => void;
  onViewDetails?: (vehicle: Vehicle) => void;
  onEdit?: (vehicle: Vehicle) => void;
  onViewReports?: (vehicle: Vehicle) => void;
  onTrackLive?: (vehicle: Vehicle) => void;
  onDelete?: (vehicle: Vehicle) => void;
}

const nowrap: React.CSSProperties = { whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" };

const clickableCell: React.CSSProperties = {
  ...nowrap,
  cursor: "pointer",
  color: "var(--colorBrandForeground3)",
  fontWeight: 500,
  textDecoration: "none",
};

const VehicleActions = ({
  item,
  onViewDetails,
  onEdit,
  onViewReports,
  onTrackLive,
  onDelete,
}: {
  item: Vehicle;
  onViewDetails?: (vehicle: Vehicle) => void;
  onEdit?: (vehicle: Vehicle) => void;
  onViewReports?: (vehicle: Vehicle) => void;
  onTrackLive?: (vehicle: Vehicle) => void;
  onDelete?: (vehicle: Vehicle) => void;
}) => (
  <div
    style={{ display: "flex", gap: "2px", alignItems: "center" }}
    onClick={stopDataGridRowSelection}
    onKeyDown={stopDataGridRowSelection}
  >
    <Tooltip content="Edit" relationship="label">
      <Button
        icon={<EditRegular />}
        appearance="subtle"
        size="small"
        aria-label="Edit"
        onClick={() => onEdit?.(item)}
      />
    </Tooltip>
    <Tooltip content="Track Live" relationship="label">
      <Button
        icon={<LocationRegular />}
        appearance="subtle"
        size="small"
        aria-label="Track Live"
        onClick={() => onTrackLive?.(item)}
      />
    </Tooltip>
    <Menu>
      <MenuTrigger disableButtonEnhancement>
        <Tooltip content="More actions" relationship="label">
          <Button icon={<MoreHorizontalRegular />} appearance="subtle" size="small" aria-label="More actions" />
        </Tooltip>
      </MenuTrigger>
      <MenuPopover>
        <MenuList>
          <MenuItem icon={<EyeRegular />} onClick={() => onViewDetails?.(item)}>
            View Details
          </MenuItem>
          <MenuItem icon={<PersonAddRegular />}>Assign Driver</MenuItem>
          <MenuItem icon={<DocumentBulletListRegular />} onClick={() => onViewReports?.(item)}>
            View Reports
          </MenuItem>
          <MenuDivider />
          <MenuItem icon={<DeleteRegular />} onClick={() => onDelete?.(item)}>
            Delete Vehicle
          </MenuItem>
        </MenuList>
      </MenuPopover>
    </Menu>
  </div>
);

const IgnitionBadge = ({ status }: { status: Vehicle["ignitionStatus"] }) => {
  if (status === "moving")
    return <Badge color="success" appearance="filled" style={nowrap}>Moving</Badge>;
  if (status === "idling")
    return <Badge color="warning" appearance="filled" style={nowrap}>Idling</Badge>;
  return <Badge color="informative" appearance="outline" style={nowrap}>Ignition Off</Badge>;
};

const formatDate = (iso: string | null) => formatDateOnlyDisplay(iso, "en-ZA", "—");

const isExpiringSoon = (iso: string | null) => isDateOnlyExpiringSoon(iso);

const isExpired = (iso: string | null) => isDateOnlyExpired(iso);

function createColumns(
  onViewDetails?: (vehicle: Vehicle) => void,
  onEdit?: (vehicle: Vehicle) => void,
  onViewReports?: (vehicle: Vehicle) => void,
  onTrackLive?: (vehicle: Vehicle) => void,
  onDelete?: (vehicle: Vehicle) => void,
): TableColumnDefinition<Vehicle>[] {
  return withAuditableColumns([
  createTableColumn<Vehicle>({
    columnId: "registrationNumber",
    compare: (a, b) => a.registrationNumber.localeCompare(b.registrationNumber),
    renderHeaderCell: () => <span style={nowrap}>Reg. Number</span>,
    renderCell: (item) => (
      <TableCellLayout
        media={
          <Avatar
            icon={<VehicleCarRegular />}
            aria-label="Vehicle"
            idForColor={item.vin}
            color={"colorful"}
          />
        }
      >
        <button
          type="button"
          onClick={(event) => {
            stopDataGridRowSelection(event);
            onViewDetails?.(item);
          }}
          style={{
            ...clickableCell,
            border: "none",
            background: "transparent",
            padding: 0,
            font: "inherit",
            textAlign: "left",
          }}
        >
          {item.registrationNumber}
        </button>
      </TableCellLayout>
    ),
  }),
  createTableColumn<Vehicle>({
    columnId: "make",
    compare: (a, b) => a.make.localeCompare(b.make),
    renderHeaderCell: () => <span style={nowrap}>Make</span>,
    renderCell: (item) => (
      <button
        type="button"
        onClick={(event) => {
          stopDataGridRowSelection(event);
          onViewDetails?.(item);
        }}
        style={{
          ...clickableCell,
          border: "none",
          background: "transparent",
          padding: 0,
          font: "inherit",
          textAlign: "left",
        }}
      >
        {item.make}
      </button>
    ),
  }),
  createTableColumn<Vehicle>({
    columnId: "model",
    compare: (a, b) => a.model.localeCompare(b.model),
    renderHeaderCell: () => <span style={nowrap}>Model</span>,
    renderCell: (item) => <span style={nowrap}>{item.model}</span>,
  }),
  createTableColumn<Vehicle>({
    columnId: "ignitionStatus",
    compare: (a, b) => a.ignitionStatus.localeCompare(b.ignitionStatus),
    renderHeaderCell: () => <span style={nowrap}>Status</span>,
    renderCell: (item) => <IgnitionBadge status={item.ignitionStatus} />,
  }),
  createTableColumn<Vehicle>({
    columnId: "year",
    compare: (a, b) => a.year - b.year,
    renderHeaderCell: () => <span style={nowrap}>Year</span>,
    renderCell: (item) => <span style={nowrap}>{item.year}</span>,
  }),
  createTableColumn<Vehicle>({
    columnId: "vin",
    compare: (a, b) => a.vin.localeCompare(b.vin),
    renderHeaderCell: () => <span style={nowrap}>VIN</span>,
    renderCell: (item) => <span style={nowrap}>{item.vin}</span>,
  }),
  createTableColumn<Vehicle>({
    columnId: "engineNumber",
    compare: (a, b) => a.engineNumber.localeCompare(b.engineNumber),
    renderHeaderCell: () => <span style={nowrap}>Terminal Serial</span>,
    renderCell: (item) => <span style={nowrap}>{item.engineNumber}</span>,
  }),
  createTableColumn<Vehicle>({
    columnId: "colour",
    compare: (a, b) => a.colour.localeCompare(b.colour),
    renderHeaderCell: () => <span style={nowrap}>Colour</span>,
    renderCell: (item) => <span style={nowrap}>{item.colour}</span>,
  }),
  createTableColumn<Vehicle>({
    columnId: "vehicleType",
    compare: (a, b) => a.vehicleType.localeCompare(b.vehicleType),
    renderHeaderCell: () => <span style={nowrap}>Vehicle Type</span>,
    renderCell: (item) => <span style={nowrap}>{item.vehicleType}</span>,
  }),
  createTableColumn<Vehicle>({
    columnId: "fuelType",
    compare: (a, b) => a.fuelType.localeCompare(b.fuelType),
    renderHeaderCell: () => <span style={nowrap}>Fuel Type</span>,
    renderCell: (item) => <span style={nowrap}>{item.fuelType}</span>,
  }),
  createTableColumn<Vehicle>({
    columnId: "tare",
    compare: (a, b) => a.tare - b.tare,
    renderHeaderCell: () => <span style={nowrap}>Tare (kg)</span>,
    renderCell: (item) => <span style={nowrap}>{item.tare.toLocaleString()}</span>,
  }),
  createTableColumn<Vehicle>({
    columnId: "gvm",
    compare: (a, b) => a.gvm - b.gvm,
    renderHeaderCell: () => <span style={nowrap}>GVM (kg)</span>,
    renderCell: (item) => <span style={nowrap}>{item.gvm.toLocaleString()}</span>,
  }),
  createTableColumn<Vehicle>({
    columnId: "registeredOwner",
    compare: (a, b) => a.registeredOwner.localeCompare(b.registeredOwner),
    renderHeaderCell: () => <span style={nowrap}>Registered Owner</span>,
    renderCell: (item) => <span style={nowrap}>{item.registeredOwner}</span>,
  }),
  createTableColumn<Vehicle>({
    columnId: "actions",
    renderHeaderCell: () => <span style={nowrap}>Actions</span>,
    renderCell: (item) => (
      <VehicleActions
        item={item}
        onViewDetails={onViewDetails}
        onEdit={onEdit}
        onViewReports={onViewReports}
        onTrackLive={onTrackLive}
        onDelete={onDelete}
      />
    ),
  }),
  createTableColumn<Vehicle>({
    columnId: "licenceDiscExpiry",
    compare: (a, b) => (a.licenceDiscExpiry ?? "").localeCompare(b.licenceDiscExpiry ?? ""),
    renderHeaderCell: () => <span style={nowrap}>Licence Disc Expiry</span>,
    renderCell: (item) => (
      <div style={{ display: "flex", flexDirection: "column", gap: "2px" }}>
        <span style={nowrap}>{formatDate(item.licenceDiscExpiry)}</span>
        {isExpired(item.licenceDiscExpiry) ? (
          <Badge color="danger" appearance="filled" style={{ whiteSpace: "nowrap", width: "fit-content" }}>Expired</Badge>
        ) : isExpiringSoon(item.licenceDiscExpiry) ? (
          <Badge color="warning" appearance="filled" style={{ whiteSpace: "nowrap", width: "fit-content" }}>Expiring soon</Badge>
        ) : null}
      </div>
    ),
  }),
]);
}


export const VehicleTable = ({
  items,
  isLoading = false,
  error = null,
  selectedIds,
  onSelectionChange,
  onViewDetails,
  onEdit,
  onViewReports,
  onTrackLive,
  onDelete,
}: VehicleTableProps): JSXElement => {
  const columns = React.useMemo(
    () => createColumns(onViewDetails, onEdit, onViewReports, onTrackLive, onDelete),
    [onViewDetails, onEdit, onViewReports, onTrackLive, onDelete],
  );

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-8">
        <Spinner size="large" label="Loading vehicles..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-4">
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      </div>
    );
  }

  if (items.length === 0) {
    return (
       <div className="h-full w-full flex flex-col gap-3 items-center justify-center p-6 text-sm text-neutral-foreground-3">
          <VehicleCarRegular className='size-26 text-gray-300' />
          No vehicles found in your CarTrack fleet.
          <CreateVehicle onCreated={() => {}} />
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
        selectedIds={selectedIds ? [...selectedIds].map(String) : undefined}
        onSelectionChange={onSelectionChange}
        getRowId={(item) => item.id}
      />
    </div>
  );
};
