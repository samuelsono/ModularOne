import { useEffect, useState } from 'react';
import {
  Button,
  Checkbox,
  Subtitle2,
} from '@fluentui/react-components';
import {
  BatteryChargeFilled,
  BatteryChargeRegular,
  ContactCardRegular,
  HistoryRegular,
  LineHorizontal1Regular,
  LocationRegular,
  PersonRegular,
  SettingsRegular,
  TimelineRegular,
} from '@fluentui/react-icons';
import type { Vehicle, VehicleDriver } from '@modules/fleet/types/vehicle';
import {
  DEFAULT_VEHICLE_DISPLAY_VISIBILITY,
  loadVehicleDisplayVisibility,
  saveVehicleDisplayVisibility,
  VEHICLE_DISPLAY_FIELDS,
  type VehicleDisplayFieldKey,
  type VehicleDisplayVisibility,
} from './vehicleDisplayOptions';

interface VehicleDetailPanelProps {
  vehicle: Vehicle;
  onViewEvents?: () => void;
  onViewTrips?: () => void;
}

function formatNumberWithSpaces(value: number): string {
  return Math.round(value)
    .toString()
    .replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
}

function formatEventTimestamp(value: string | null | undefined): string {
  if (!value) {
    return '--';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '--';
  }

  const time = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false });
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');

  return `${time} - ${year}/${month}/${day}`;
}

function formatIgnitionLabel(status: Vehicle['ignitionStatus']): string {
  switch (status) {
    case 'moving':
      return 'IGNITION ON';
    case 'idling':
      return 'IGNITION IDLING';
    default:
      return 'IGNITION OFF';
  }
}

function formatSpeed(value: number | null | undefined): string {
  return value == null ? '--' : `${formatNumberWithSpaces(value)} KM/H`;
}

function formatOdometer(value: number | null | undefined): string {
  if (value == null) {
    return '--';
  }

  const kilometers = value / 1000;
  const [integerPart, decimalPart] = kilometers.toFixed(1).split('.');
  const groupedInteger = integerPart.replace(/\B(?=(\d{3})+(?!\d))/g, ' ');

  return `${groupedInteger}.${decimalPart} km`;
}

function formatRpm(value: number | null | undefined): string {
  return value == null ? '--' : formatNumberWithSpaces(value);
}

function formatTemperature(value: number | null | undefined): string {
  if (value == null || value === 0) {
    return '--';
  }

  return `${value.toFixed(1)} °C`;
}

function formatLvBattery(value: number | null | undefined): string {
  if (value == null) {
    return '--';
  }

  return `${value.toFixed(1).replace('.', ',')}V`;
}

function formatUnitClock(value: number | null | undefined): string {
  if (value == null) {
    return '--';
  }

  const totalMinutes = Math.floor(value);
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;

  return `${hours}h ${minutes}m`;
}

function formatDriverName(driver: VehicleDriver | null | undefined): string {
  if (!driver) {
    return 'No linked driver';
  }

  const name = [driver.firstName, driver.lastName].filter(Boolean).join(' ').trim();
  return name.length > 0 ? name : 'No linked driver';
}

function formatTcuBattery(percentage: number | null | undefined): string {
  if (percentage == null) {
    return '--';
  }

  return percentage > 75 ? 'Healthy' : 'Low';
}

function isLvBatteryHealthy(voltage: number | null | undefined): boolean {
  return voltage != null && voltage > 11.6;
}

function isTcuBatteryHealthy(percentage: number | null | undefined): boolean {
  return percentage != null && percentage > 75;
}

function BatteryStatusIcon({ healthy }: { healthy: boolean }) {
  const Icon = healthy ? BatteryChargeFilled : BatteryChargeRegular;

  return (
    <Icon
      className={healthy ? 'text-[#107c10]' : 'text-neutral-foreground-3'}
      fontSize={18}
    />
  );
}

function formatGeofences(ids: string[] | null | undefined): string {
  if (!ids || ids.length === 0) {
    return 'None';
  }

  if (ids.length === 1) {
    return '1 geofence';
  }

  return `${ids.length} geofences`;
}

const MetricCell = ({ label, value }: { label: string; value: string }) => (
  <div className="flex flex-col items-center text-center">
    <span className="text-sm font-semibold text-neutral-foreground-1">{value}</span>
    <span className="text-xs text-neutral-foreground-3">{label}</span>
  </div>
);

const SectionRow = ({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) => (
  <div className="border-t border-[#e3e5e7] px-4 py-3">
    <div className="text-sm font-semibold text-neutral-foreground-1">{title}</div>
    <div className="mt-1">{children}</div>
  </div>
);

function VehicleDisplaySettings({
  visibility,
  onChange,
  onClose,
}: {
  visibility: VehicleDisplayVisibility;
  onChange: (key: VehicleDisplayFieldKey, checked: boolean) => void;
  onClose: () => void;
}) {
  const quickStats = VEHICLE_DISPLAY_FIELDS.filter((field) => field.section === 'quickStats');
  const others = VEHICLE_DISPLAY_FIELDS.filter((field) => field.section === 'others');

  return (
    <div className="w-[320px] text-neutral-foreground-1">
      <div className="px-4 py-3">
        <Subtitle2>Vehicle display options</Subtitle2>
      </div>

      <div className="border-t border-[#e3e5e7] px-4 py-2">
        <div className="text-xs font-semibold uppercase tracking-wide text-neutral-foreground-3">
          Quick stats
        </div>
        <div className="mt-2 flex flex-col gap-1">
          {quickStats.map((field) => (
            <label
              key={field.key}
              className="flex items-center gap-2 py-1 text-sm cursor-pointer"
            >
              <LineHorizontal1Regular className="shrink-0 text-neutral-foreground-3" fontSize={16} />
              <Checkbox
                checked={visibility[field.key]}
                onChange={(_, data) => onChange(field.key, Boolean(data.checked))}
              />
              <span>{field.label}</span>
            </label>
          ))}
        </div>
      </div>

      <div className="border-t border-[#e3e5e7] px-4 py-2">
        <div className="text-xs font-semibold uppercase tracking-wide text-neutral-foreground-3">
          Others
        </div>
        <div className="mt-2 flex flex-col gap-1">
          {others.map((field) => (
            <label
              key={field.key}
              className="flex items-center gap-2 py-1 text-sm cursor-pointer"
            >
              <LineHorizontal1Regular className="shrink-0 text-neutral-foreground-3" fontSize={16} />
              <Checkbox
                checked={visibility[field.key]}
                onChange={(_, data) => onChange(field.key, Boolean(data.checked))}
              />
              <span>{field.label}</span>
            </label>
          ))}
        </div>
      </div>

      <div className="flex justify-end border-t border-[#e3e5e7] px-4 py-3">
        <Button appearance="primary" onClick={onClose}>
          Close
        </Button>
      </div>
    </div>
  );
}

export function VehicleDetailPanel({ vehicle, onViewEvents, onViewTrips }: VehicleDetailPanelProps) {
  const [view, setView] = useState<'details' | 'settings'>('details');
  const [visibility, setVisibility] = useState<VehicleDisplayVisibility>(
    DEFAULT_VEHICLE_DISPLAY_VISIBILITY,
  );

  useEffect(() => {
    setVisibility(loadVehicleDisplayVisibility());
    setView('details');
  }, [vehicle.id]);

  function handleVisibilityChange(key: VehicleDisplayFieldKey, checked: boolean) {
    setVisibility((current) => {
      const next = { ...current, [key]: checked };
      saveVehicleDisplayVisibility(next);
      return next;
    });
  }

  if (view === 'settings') {
    return (
      <VehicleDisplaySettings
        visibility={visibility}
        onChange={handleVisibilityChange}
        onClose={() => setView('details')}
      />
    );
  }

  const status = vehicle.status;
  const telemetry = status?.telemetry;
  const location = status?.location;
  const driver = status?.driver;
  const eventLabel = formatEventTimestamp(telemetry?.eventTs ?? location?.updated);

  const quickStats = [
    visibility.speed ? { key: 'speed', label: 'Speed', value: formatSpeed(telemetry?.speed) } : null,
    visibility.roadSpeed ? { key: 'roadSpeed', label: 'Road Speed', value: formatSpeed(telemetry?.roadSpeed) } : null,
    visibility.odometer ? { key: 'odometer', label: 'Odometer', value: formatOdometer(telemetry?.odometer) } : null,
    visibility.rpm ? { key: 'rpm', label: 'RPM', value: formatRpm(telemetry?.rpm) } : null,
    visibility.waterTemp ? { key: 'waterTemp', label: 'Water Temp', value: formatTemperature(telemetry?.waterTemp) } : null,
    visibility.oilTemp ? { key: 'oilTemp', label: 'Oil Temp', value: formatTemperature(telemetry?.oilTemp) } : null,
  ].filter((item): item is { key: string; label: string; value: string } => item !== null);

  return (
    <div className="w-[320px] text-neutral-foreground-1">
      <div className="flex items-center gap-2 px-4 py-3">
        <span className="rounded-full bg-neutral-background-3 px-3 py-1 text-xs font-semibold uppercase tracking-wide text-neutral-foreground-2">
          {formatIgnitionLabel(vehicle.ignitionStatus)}
        </span>
        <span className="text-sm font-semibold">{eventLabel}</span>
        <button
          type="button"
          className="ml-auto cursor-pointer border-0 bg-transparent p-0 text-neutral-foreground-3"
          aria-label="Vehicle display options"
          onClick={(event) => {
            event.stopPropagation();
            setView('settings');
          }}
        >
          <SettingsRegular fontSize={18} />
        </button>
      </div>

      <div className="flex items-center gap-2 border-t border-[#e3e5e7] px-4 py-3">
        <PersonRegular className="text-neutral-foreground-3" fontSize={18} />
        <span className="text-sm">{formatDriverName(driver)}</span>
      </div>

      <div className="flex items-start gap-2 border-t border-[#e3e5e7] px-4 py-3">
        <LocationRegular className="mt-0.5 shrink-0 text-neutral-foreground-3" fontSize={18} />
        <span className="text-sm">{location?.positionDescription ?? 'Location unavailable'}</span>
      </div>

      {quickStats.length > 0 && (
        <div className="px-0 py-3">
          <div className="grid grid-cols-3 gap-2 rounded bg-neutral-background-3 px-3 py-3">
            {quickStats.map((stat) => (
              <MetricCell key={stat.key} label={stat.label} value={stat.value} />
            ))}
          </div>
        </div>
      )}

      <SectionRow title="LV Battery">
        <div className="flex items-center gap-2 text-sm">
          <BatteryStatusIcon healthy={isLvBatteryHealthy(telemetry?.lvBatteryVoltage)} />
          <span>{formatLvBattery(telemetry?.lvBatteryVoltage)}</span>
        </div>
      </SectionRow>

      {visibility.currentGeofences && (
        <SectionRow title="Current Geofences">
          <span className="text-sm">{formatGeofences(location?.geofenceIds)}</span>
        </SectionRow>
      )}

      {visibility.actions && (
        <SectionRow title="Actions">
          <span className="text-sm">{telemetry?.centralLockingStatus == null ? '--' : telemetry.centralLockingStatus ? 'Central locking on' : 'Central locking off'}</span>
        </SectionRow>
      )}

      {visibility.refrigerator && (
        <SectionRow title="Refrigerator">
          <span className="text-sm">--</span>
        </SectionRow>
      )}

      {visibility.unitClock && (
        <SectionRow title="Unit Clock">
          <span className="text-sm">{formatUnitClock(telemetry?.unitClock)}</span>
        </SectionRow>
      )}

      {visibility.tcu && (
        <SectionRow title="Telematic Control Unit (TCU)">
          <div className="flex items-center justify-between text-sm">
            <span className="text-neutral-foreground-2">TCU Battery</span>
            <span className="flex items-center gap-2">
              {formatTcuBattery(telemetry?.tcuBatteryPercentage)}
              <BatteryStatusIcon healthy={isTcuBatteryHealthy(telemetry?.tcuBatteryPercentage)} />
            </span>
          </div>
        </SectionRow>
      )}

      {visibility.driverIdTag && (
        <SectionRow title="Driver ID Tag">
          <div className="flex items-center gap-2 text-sm">
            <ContactCardRegular className="text-neutral-foreground-3" fontSize={18} />
            <span>{driver?.driverId ?? '--'}</span>
          </div>
        </SectionRow>
      )}

      {(onViewEvents || onViewTrips) && (
        <div className="flex gap-2 border-t border-[#e3e5e7] px-4 py-3">
          {onViewEvents && (
            <Button
              appearance="secondary"
              icon={<HistoryRegular />}
              className="flex-1"
              onClick={onViewEvents}
            >
              Events
            </Button>
          )}
          {onViewTrips && (
            <Button
              appearance="secondary"
              icon={<TimelineRegular />}
              className="flex-1"
              onClick={onViewTrips}
            >
              Trips
            </Button>
          )}
        </div>
      )}
    </div>
  );
}
