export type VehicleDisplayFieldKey =
  | 'speed'
  | 'roadSpeed'
  | 'odometer'
  | 'rpm'
  | 'waterTemp'
  | 'oilTemp'
  | 'currentGeofences'
  | 'actions'
  | 'refrigerator'
  | 'unitClock'
  | 'tcu'
  | 'driverIdTag';

export type VehicleDisplayVisibility = Record<VehicleDisplayFieldKey, boolean>;

export interface VehicleDisplayFieldOption {
  key: VehicleDisplayFieldKey;
  label: string;
  section: 'quickStats' | 'others';
}

export const VEHICLE_DISPLAY_FIELDS: VehicleDisplayFieldOption[] = [
  { key: 'speed', label: 'Speed', section: 'quickStats' },
  { key: 'roadSpeed', label: 'Road Speed', section: 'quickStats' },
  { key: 'odometer', label: 'Odometer', section: 'quickStats' },
  { key: 'rpm', label: 'RPM', section: 'quickStats' },
  { key: 'waterTemp', label: 'Water Temp', section: 'quickStats' },
  { key: 'oilTemp', label: 'Oil Temp', section: 'quickStats' },
  { key: 'currentGeofences', label: 'Current Geofences', section: 'others' },
  { key: 'actions', label: 'Actions', section: 'others' },
  { key: 'refrigerator', label: 'Refrigerator', section: 'others' },
  { key: 'unitClock', label: 'Unit Clock', section: 'others' },
  { key: 'tcu', label: 'Telematic Control Unit (TCU)', section: 'others' },
  { key: 'driverIdTag', label: 'Driver ID Tag', section: 'others' },
];

export const DEFAULT_VEHICLE_DISPLAY_VISIBILITY: VehicleDisplayVisibility = {
  speed: true,
  roadSpeed: true,
  odometer: true,
  rpm: true,
  waterTemp: false,
  oilTemp: false,
  currentGeofences: false,
  actions: false,
  refrigerator: false,
  unitClock: false,
  tcu: true,
  driverIdTag: true,
};

const STORAGE_KEY = 'cartrack.vehicleDisplayVisibility';

export function loadVehicleDisplayVisibility(): VehicleDisplayVisibility {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) {
      return { ...DEFAULT_VEHICLE_DISPLAY_VISIBILITY };
    }

    const parsed = JSON.parse(stored) as Partial<VehicleDisplayVisibility>;
    return { ...DEFAULT_VEHICLE_DISPLAY_VISIBILITY, ...parsed };
  } catch {
    return { ...DEFAULT_VEHICLE_DISPLAY_VISIBILITY };
  }
}

export function saveVehicleDisplayVisibility(visibility: VehicleDisplayVisibility): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(visibility));
}
