import { useState } from 'react';
import type { SelectTabData, SelectTabEvent, TabValue } from '@fluentui/react-components';
import {
  Avatar,
  Badge,
  Button,
  Dialog,
  DialogSurface,
  Tab,
  TabList,
} from '@fluentui/react-components';
import {
  BuildingRegular,
  CalendarRegular,
  DismissRegular,
  DocumentBulletListRegular,
  EditRegular,
  GasPumpRegular,
  LocationRegular,
  MailRegular,
  PersonRegular,
  VehicleCarRegular,
  WrenchRegular,
} from '@fluentui/react-icons';
import type { Vehicle } from '@modules/fleet/types/vehicle';
import { formatLastSyncedAt, getVehicleDisplayName } from '@modules/fleet/services/vehicleService';
import { formatDateOnlyDisplay } from '@platform/utils/dateOnly';

interface VehicleDetailsDialogProps {
  vehicle: Vehicle | null;
  open: boolean;
  onClose: () => void;
  onEdit?: (vehicle: Vehicle) => void;
  onViewReports?: (vehicle: Vehicle) => void;
}

function formatDate(value: string | null | undefined): string {
  return formatDateOnlyDisplay(value);
}

function formatIgnitionLabel(status: Vehicle['ignitionStatus']): string {
  switch (status) {
    case 'moving':
      return 'Moving';
    case 'idling':
      return 'Idling';
    default:
      return 'Ignition off';
  }
}

function ignitionBadgeColor(status: Vehicle['ignitionStatus']): 'success' | 'warning' | 'informative' {
  if (status === 'moving') {
    return 'success';
  }
  if (status === 'idling') {
    return 'warning';
  }
  return 'informative';
}

function DetailRow({
  icon,
  label,
  value,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
}) {
  return (
    <div className="grid grid-cols-[20px_140px_1fr] items-center gap-3 border-b border-neutral-stroke-1 py-2.5 last:border-b-0">
      <span className="text-neutral-foreground-3">{icon}</span>
      <span className="text-sm text-neutral-foreground-3">{label}</span>
      <span className="text-sm text-neutral-foreground-1">{value}</span>
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="py-4">
      <h3 className="mb-2 text-base font-semibold text-neutral-foreground-1">{title}</h3>
      <div>{children}</div>
    </section>
  );
}

export function VehicleDetailsDialog({
  vehicle,
  open,
  onClose,
  onEdit,
  onViewReports,
}: VehicleDetailsDialogProps) {
  const [selectedTab, setSelectedTab] = useState<TabValue>('overview');

  if (!vehicle) {
    return null;
  }

  const driver = vehicle.status?.driver;
  const driverName = driver
    ? [driver.firstName, driver.lastName].filter(Boolean).join(' ') || '—'
    : '—';
  const location = vehicle.status?.location?.positionDescription ?? '—';
  const odometer = vehicle.status?.telemetry?.odometer;
  const odometerLabel = odometer != null
    ? `${(odometer / 1000).toFixed(1)} km`
    : '—';

  return (
    <Dialog
      open={open}
      onOpenChange={(_, data) => {
        if (!data.open) {
          onClose();
        }
      }}
    >
      <DialogSurface className="flex! max-h-[90vh]! w-[640px]! max-w-[95vw]! flex-col! p-0!">
        <div className="border-b border-neutral-stroke-2 px-6 pb-4 pt-6">
          <div className="flex items-start gap-4">
            <Avatar
              icon={<VehicleCarRegular />}
              name={vehicle.registrationNumber}
              color="brand"
              size={72}
            />
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <h2 className="text-2xl font-semibold text-neutral-foreground-1">
                  {vehicle.registrationNumber}
                </h2>
                <Badge color={ignitionBadgeColor(vehicle.ignitionStatus)} appearance="filled">
                  {formatIgnitionLabel(vehicle.ignitionStatus)}
                </Badge>
                {vehicle.isLocalOnly && (
                  <Badge appearance="outline">Local only</Badge>
                )}
              </div>
              <p className="mt-1 text-sm text-neutral-foreground-2">
                {getVehicleDisplayName(vehicle)} • {vehicle.year || '—'}
              </p>
              <div className="mt-3 flex flex-wrap gap-2">
                <Button appearance="primary" size="small" icon={<LocationRegular />}>
                  Track live
                </Button>
                <Button
                  appearance="secondary"
                  size="small"
                  icon={<DocumentBulletListRegular />}
                  onClick={() => onViewReports?.(vehicle)}
                >
                  View reports
                </Button>
              </div>
            </div>
            <Button
              appearance="subtle"
              aria-label="Close"
              icon={<DismissRegular />}
              onClick={onClose}
            />
          </div>

          <TabList
            className="mt-5"
            selectedValue={selectedTab}
            onTabSelect={(_event: SelectTabEvent, data: SelectTabData) => setSelectedTab(data.value)}
          >
            <Tab value="overview">Overview</Tab>
            <Tab value="technical">Technical</Tab>
            <Tab value="registration">Registration</Tab>
            <Tab value="telematics">Telematics</Tab>
          </TabList>
        </div>

        <div className="flex-1 overflow-y-auto px-6">
          {selectedTab === 'overview' && (
            <>
              <Section title="Vehicle information">
                <DetailRow icon={<VehicleCarRegular fontSize={16} />} label="Make" value={vehicle.make || '—'} />
                <DetailRow icon={<VehicleCarRegular fontSize={16} />} label="Model" value={vehicle.model || '—'} />
                <DetailRow icon={<WrenchRegular fontSize={16} />} label="Vehicle type" value={vehicle.vehicleType || '—'} />
                <DetailRow icon={<GasPumpRegular fontSize={16} />} label="Fuel type" value={vehicle.fuelType || '—'} />
                <DetailRow icon={<VehicleCarRegular fontSize={16} />} label="Colour" value={vehicle.colour || '—'} />
              </Section>

              <Section title="Assignment & location">
                <DetailRow icon={<PersonRegular fontSize={16} />} label="Assigned driver" value={driverName} />
                <DetailRow icon={<LocationRegular fontSize={16} />} label="Last location" value={location} />
                <DetailRow icon={<WrenchRegular fontSize={16} />} label="Odometer" value={odometerLabel} />
              </Section>
            </>
          )}

          {selectedTab === 'technical' && (
            <Section title="Technical specifications">
              <DetailRow icon={<WrenchRegular fontSize={16} />} label="VIN" value={vehicle.vin || '—'} />
              <DetailRow icon={<WrenchRegular fontSize={16} />} label="Engine number" value={vehicle.engineNumber || '—'} />
              <DetailRow icon={<WrenchRegular fontSize={16} />} label="Tare" value={vehicle.tare ? `${vehicle.tare.toLocaleString()} kg` : '—'} />
              <DetailRow icon={<WrenchRegular fontSize={16} />} label="GVM" value={vehicle.gvm ? `${vehicle.gvm.toLocaleString()} kg` : '—'} />
            </Section>
          )}

          {selectedTab === 'registration' && (
            <Section title="Registration & ownership">
              <DetailRow icon={<BuildingRegular fontSize={16} />} label="Registered owner" value={vehicle.registeredOwner || '—'} />
              <DetailRow icon={<CalendarRegular fontSize={16} />} label="Licence disc expiry" value={formatDate(vehicle.licenceDiscExpiry)} />
            </Section>
          )}

          {selectedTab === 'telematics' && (
            <Section title="Telematics">
              <DetailRow
                icon={<MailRegular fontSize={16} />}
                label="CarTrack sync"
                value={formatLastSyncedAt(vehicle.carTrackSyncedAt) ?? 'Not synced'}
              />
              <DetailRow
                icon={<LocationRegular fontSize={16} />}
                label="GPS coordinates"
                value={
                  vehicle.status?.location?.latitude != null && vehicle.status.location.longitude != null
                    ? `${vehicle.status.location.latitude.toFixed(5)}, ${vehicle.status.location.longitude.toFixed(5)}`
                    : '—'
                }
              />
              <DetailRow
                icon={<WrenchRegular fontSize={16} />}
                label="Speed"
                value={vehicle.status?.telemetry?.speed != null
                  ? `${Math.round(vehicle.status.telemetry.speed)} km/h`
                  : '—'}
              />
            </Section>
          )}
        </div>

        <div className="border-t border-neutral-stroke-2 px-6 py-4">
          <Button
            appearance="secondary"
            className="w-full"
            icon={<EditRegular />}
            onClick={() => onEdit?.(vehicle)}
          >
            Edit vehicle
          </Button>
        </div>
      </DialogSurface>
    </Dialog>
  );
}
