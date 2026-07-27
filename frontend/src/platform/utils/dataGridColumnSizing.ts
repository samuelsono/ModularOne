import type { TableColumnDefinition, TableColumnSizingOptions } from '@fluentui/react-components';

const DEFAULT_COLUMN_SIZING = {
  minWidth: 120,
  idealWidth: 160,
  defaultWidth: 140,
} as const;

export function buildColumnSizingOptions<TItem>(
  columns: TableColumnDefinition<TItem>[],
  overrides: TableColumnSizingOptions = {},
): TableColumnSizingOptions {
  const sizing: TableColumnSizingOptions = {};

  for (const column of columns) {
    const columnId = String(column.columnId);
    sizing[columnId] = overrides[columnId] ?? DEFAULT_COLUMN_SIZING;
  }

  return sizing;
}

export const trackingTableColumnSizing: TableColumnSizingOptions = {
  driverId: { minWidth: 110, idealWidth: 140, defaultWidth: 120 },
  registrationNumber: { minWidth: 140, idealWidth: 180, defaultWidth: 160 },
  name: { minWidth: 160, idealWidth: 240, defaultWidth: 200 },
  make: { minWidth: 100, idealWidth: 140, defaultWidth: 120 },
  model: { minWidth: 140, idealWidth: 200, defaultWidth: 170 },
  email: { minWidth: 180, idealWidth: 360, defaultWidth: 260 },
  contactNumber: { minWidth: 130, idealWidth: 180, defaultWidth: 150 },
  gender: { minWidth: 90, idealWidth: 110, defaultWidth: 100 },
  department: { minWidth: 120, idealWidth: 180, defaultWidth: 150 },
  licenceNumber: { minWidth: 140, idealWidth: 200, defaultWidth: 170 },
  licenceIssued: { minWidth: 130, idealWidth: 160, defaultWidth: 145 },
  licenceExpiry: { minWidth: 150, idealWidth: 280, defaultWidth: 210 },
  licenceDiscExpiry: { minWidth: 155, idealWidth: 280, defaultWidth: 210 },
  ignitionStatus: { minWidth: 110, idealWidth: 130, defaultWidth: 120 },
  year: { minWidth: 70, idealWidth: 90, defaultWidth: 80 },
  vin: { minWidth: 160, idealWidth: 220, defaultWidth: 190 },
  engineNumber: { minWidth: 130, idealWidth: 180, defaultWidth: 150 },
  colour: { minWidth: 100, idealWidth: 130, defaultWidth: 110 },
  vehicleType: { minWidth: 130, idealWidth: 180, defaultWidth: 150 },
  fuelType: { minWidth: 90, idealWidth: 120, defaultWidth: 100 },
  tare: { minWidth: 90, idealWidth: 110, defaultWidth: 100 },
  gvm: { minWidth: 90, idealWidth: 110, defaultWidth: 100 },
  registeredOwner: { minWidth: 160, idealWidth: 360, defaultWidth: 260 },
  actions: { minWidth: 140, idealWidth: 160, defaultWidth: 160, autoFitColumns: false },
};
