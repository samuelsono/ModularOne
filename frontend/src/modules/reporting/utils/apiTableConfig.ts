export const API_RESOURCE_LABELS: Record<string, string> = {
  vehicles: 'Vehicles',
  'vehicle-status': 'Vehicle Status',
  events: 'Vehicle Events',
  trips: 'Trips',
  alerts: 'Alerts',
  'alert-notifications': 'Alert Notifications',
};

export const DEFAULT_COLUMN_MAPPINGS: Record<string, Record<string, string>> = {
  vehicles: {
    registration: 'Registration',
    manufacturer: 'Make',
    model: 'Model',
    vehicle_type: 'Type',
  },
  'vehicle-status': {
    registration: 'Registration',
    speed: 'Speed',
    ignition: 'Ignition',
    odometer: 'Odometer',
    'location.position_description': 'Location',
  },
  events: {
    event_description: 'Event',
    event_ts: 'Time',
    speed: 'Speed',
    position_description: 'Location',
  },
  trips: {
    registration: 'Registration',
    start_timestamp: 'Start',
    end_timestamp: 'End',
    trip_distance: 'Distance',
    driver_name: 'Driver',
  },
  alerts: {
    name: 'Alert Name',
    create_ts: 'Created',
    any_vehicle: 'Any Vehicle',
    inside_geofence: 'Inside Geofence',
    'contact_type.values': 'Contacts',
  },
  'alert-notifications': {
    registration: 'Registration',
    name: 'Alert Name',
    trigger_description: 'Trigger',
    status: 'Status',
    event_ts: 'Time',
    notification_msg: 'Message',
    speed: 'Speed',
  },
};

export interface ApiTableChartOptions {
  columnMapping: Record<string, string>;
  registration?: string;
  startTimestamp?: string;
  endTimestamp?: string;
}

export function buildApiTableChartOptionsJson(options: ApiTableChartOptions): string {
  return JSON.stringify(options, null, 2);
}

export function parseApiTableChartOptionsJson(chartOptionsJson?: string | null): ApiTableChartOptions {
  if (!chartOptionsJson?.trim()) {
    return { columnMapping: {} };
  }

  try {
    const parsed = JSON.parse(chartOptionsJson) as Partial<ApiTableChartOptions>;
    return {
      columnMapping: parsed.columnMapping ?? {},
      registration: parsed.registration,
      startTimestamp: parsed.startTimestamp,
      endTimestamp: parsed.endTimestamp,
    };
  } catch {
    return { columnMapping: {} };
  }
}

export function formatColumnMappingJson(mapping: Record<string, string>): string {
  return JSON.stringify({ columnMapping: mapping }, null, 2);
}

export function parseColumnMappingFromJson(json: string): Record<string, string> {
  const parsed = JSON.parse(json) as { columnMapping?: Record<string, string> };
  if (!parsed.columnMapping || typeof parsed.columnMapping !== 'object') {
    throw new Error('Column mapping must be a JSON object with a "columnMapping" dictionary.');
  }

  return parsed.columnMapping;
}

export function apiResourceRequiresRegistration(resource: string): boolean {
  return resource === 'events' || resource === 'trips';
}

export function apiResourceRequiresDateRange(resource: string): boolean {
  return resource === 'alert-notifications';
}
