export interface VehicleLocation {
  latitude: number | null;
  longitude: number | null;
  positionDescription: string | null;
  updated: string | null;
  gpsFixType: number | null;
  geofenceIds: string[] | null;
}

export interface VehicleDriver {
  driverId: string | null;
  firstName: string | null;
  lastName: string | null;
  phoneNumber: string | null;
  licenseNumber: string | null;
}

export interface VehicleFuel {
  level: number | null;
  percentageLeft: number | null;
  totalConsumed: number | null;
  updated: string | null;
}

export interface VehicleElectric {
  batteryPercentageLeft: number | null;
  chargingStatus: string | null;
  batteryTs: string | null;
  chargingStatusTs: string | null;
}

export interface VehicleTelemetry {
  engineType: string | null;
  eventTs: string | null;
  bearing: number | null;
  speed: number | null;
  roadSpeed: number | null;
  odometer: number | null;
  altitude: number | null;
  rpm: number | null;
  ignition: boolean | null;
  idling: boolean | null;
  tcuBatteryPercentage: number | null;
  lvBatteryVoltage: number | null;
  unitClock: number | null;
  waterTemp: number | null;
  oilTemp: number | null;
  centralLockingStatus: boolean | null;
}

export interface VehicleStatus {
  location: VehicleLocation | null;
  driver: VehicleDriver | null;
  fuel: VehicleFuel | null;
  electric: VehicleElectric | null;
  telemetry: VehicleTelemetry | null;
  statusSyncedAt: string | null;
}

export interface Vehicle {
  id: string;
  registrationNumber: string;
  make: string;
  model: string;
  year: number;
  vin: string;
  engineNumber: string;
  colour: string;
  vehicleType: string;
  fuelType: 'Petrol' | 'Diesel' | 'Electric' | 'Hybrid';
  tare: number;
  gvm: number;
  registeredOwner: string;
  licenceDiscExpiry: string | null;
  ignitionStatus: 'moving' | 'idling' | 'off';
  isLocalOnly: boolean;
  carTrackSyncedAt: string | null;
  status: VehicleStatus | null;
  assignedDriverId?: string | null;
  assignedDriverName?: string | null;
  createdAt?: string;
  createdByUserId?: string | null;
  createdByDisplayName?: string | null;
  updatedAt?: string | null;
  updatedByUserId?: string | null;
  updatedByDisplayName?: string | null;
}

export interface VehiclesResponse {
  items: Vehicle[];
  total: number;
  lastSyncedAt: string | null;
  isCacheFresh: boolean;
  cacheTtlMinutes: number;
}

export interface CreateVehicleRequest {
  registrationNumber: string;
  make: string;
  model: string;
  year: number;
  colour: string;
  vehicleType: string;
  fuelType: string;
  vin?: string;
  engineNumber?: string;
  tare?: number;
  gvm?: number;
  registeredOwner?: string;
  licenceDiscExpiry?: string | null;
  assignedDriverId?: string | null;
}

export type UpdateVehicleRequest = CreateVehicleRequest;

export interface VehicleSyncResponse {
  created: number;
  updated: number;
  items: Vehicle[];
  total: number;
  lastSyncedAt: string | null;
  isCacheFresh: boolean;
  cacheTtlMinutes: number;
}

export interface VehicleEvent {
  eventId: number;
  eventDescription: string | null;
  terminalEventTypeId: number | null;
  eventTs: string | null;
  receivedTs: string | null;
  latitude: number | null;
  longitude: number | null;
  altitude: number | null;
  odometer: number | null;
  bearing: number | null;
  speed: number | null;
  roadSpeed: number | null;
  rpm: number | null;
  ignition: boolean | null;
  roadSpeeding: boolean | null;
  positionDescription: string | null;
  vext: number | null;
  batteryPercentageLeft: number | null;
  waterTemp: number | null;
  oilTemp: number | null;
  gpsFixType: number | null;
  driverId: string | null;
}

export interface Pagination {
  page: number;
  perPage: number;
  lastPage: number;
  total: number;
}

export interface VehicleEventsResponse {
  registration: string;
  start: string;
  end: string;
  items: VehicleEvent[];
  pagination: Pagination;
  fromCache: boolean;
}

export interface TripCoordinates {
  latitude: number | null;
  longitude: number | null;
}

export interface Trip {
  tripId: number;
  startTimestamp: string | null;
  endTimestamp: string | null;
  tripDuration: string | null;
  tripDurationSeconds: number | null;
  startLocation: string | null;
  startCoordinates: TripCoordinates | null;
  endLocation: string | null;
  endCoordinates: TripCoordinates | null;
  startOdometer: number | null;
  endOdometer: number | null;
  tripDistance: number | null;
  startGeofenceName: string | null;
  endGeofenceName: string | null;
  thresholdsSpeedingEvents: number | null;
  roadSpeedingEvents: number | null;
  maxSpeed: number | null;
  harshBrakingEvents: number | null;
  harshCorneringEvents: number | null;
  harshAccelerationEvents: number | null;
  idleTime: string | null;
  idleTimeSeconds: number | null;
  driverId: string | null;
  driverName: string | null;
  tripTitle: string | null;
  tripType: string | null;
  isPrivate: boolean | null;
}

export interface TripsResponse {
  registration: string;
  start: string;
  end: string;
  items: Trip[];
  pagination: Pagination;
  fromCache: boolean;
}

export function vehicleHasLocation(vehicle: Vehicle): boolean {
  const location = vehicle.status?.location;
  return location?.latitude != null && location?.longitude != null;
}

export function getVehicleCoordinates(vehicle: Vehicle): [number, number] | null {
  const location = vehicle.status?.location;
  if (location?.latitude == null || location?.longitude == null) {
    return null;
  }

  return [location.longitude, location.latitude];
}
