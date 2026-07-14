import type { Trip, VehicleEvent } from '../../types/vehicle';

export type HarshEventType = 'braking' | 'cornering' | 'acceleration';

export interface PathPoint {
  longitude: number;
  latitude: number;
  bearing: number | null;
  speed: number | null;
  eventTs: string | null;
  eventDescription: string | null;
  positionDescription: string | null;
}

export function classifyHarshEvent(description: string | null): HarshEventType | null {
  if (!description) {
    return null;
  }

  const upper = description.toUpperCase();
  if (upper.includes('HARSH') && upper.includes('BRAK')) {
    return 'braking';
  }
  if (upper.includes('HARSH') && upper.includes('CORNER')) {
    return 'cornering';
  }
  if (upper.includes('HARSH') && upper.includes('ACCEL')) {
    return 'acceleration';
  }

  return null;
}

export function getHarshEventLabel(type: HarshEventType): string {
  switch (type) {
    case 'braking':
      return 'Harsh braking';
    case 'cornering':
      return 'Harsh cornering';
    case 'acceleration':
      return 'Harsh acceleration';
  }
}

export function countHarshEventsByType(events: VehicleEvent[]): Record<HarshEventType, number> {
  const counts: Record<HarshEventType, number> = {
    braking: 0,
    cornering: 0,
    acceleration: 0,
  };

  for (const event of events) {
    const type = classifyHarshEvent(event.eventDescription);
    if (type) {
      counts[type] += 1;
    }
  }

  return counts;
}

export function getTripHarshCounts(trip: Trip): Record<HarshEventType, number> {
  return {
    braking: trip.harshBrakingEvents ?? 0,
    cornering: trip.harshCorneringEvents ?? 0,
    acceleration: trip.harshAccelerationEvents ?? 0,
  };
}

export function buildTripPath(events: VehicleEvent[], trip: Trip): PathPoint[] {
  const fromEvents = events
    .filter((event) => event.latitude != null && event.longitude != null)
    .sort((left, right) => {
      const leftTime = left.eventTs ? new Date(left.eventTs).getTime() : 0;
      const rightTime = right.eventTs ? new Date(right.eventTs).getTime() : 0;
      return leftTime - rightTime;
    })
    .map((event) => ({
      longitude: event.longitude!,
      latitude: event.latitude!,
      bearing: event.bearing,
      speed: event.speed,
      eventTs: event.eventTs,
      eventDescription: event.eventDescription,
      positionDescription: event.positionDescription,
    }));

  if (fromEvents.length > 0) {
    return fromEvents;
  }

  const fallback: PathPoint[] = [];
  const start = trip.startCoordinates;
  const end = trip.endCoordinates;

  if (start?.latitude != null && start.longitude != null) {
    fallback.push({
      longitude: start.longitude,
      latitude: start.latitude,
      bearing: null,
      speed: null,
      eventTs: trip.startTimestamp,
      eventDescription: 'TRIP_START',
      positionDescription: trip.startLocation,
    });
  }

  if (end?.latitude != null && end.longitude != null) {
    fallback.push({
      longitude: end.longitude,
      latitude: end.latitude,
      bearing: null,
      speed: null,
      eventTs: trip.endTimestamp,
      eventDescription: 'TRIP_END',
      positionDescription: trip.endLocation,
    });
  }

  return fallback;
}

export function humanizeEventDescription(description: string | null): string {
  if (!description) {
    return 'Event';
  }

  return description
    .toLowerCase()
    .split('_')
    .map((word) => (word ? word[0].toUpperCase() + word.slice(1) : word))
    .join(' ');
}

export function formatTripDateTime(value: string | null): string {
  if (!value) {
    return '--';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '--';
  }

  return date.toLocaleString([], {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  });
}

function padTwoDigits(value: number): string {
  return `${value}`.padStart(2, '0');
}

export function formatTimeOfDay(value: string | null): string {
  if (!value) {
    return '--';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '--';
  }

  return `${padTwoDigits(date.getHours())}:${padTwoDigits(date.getMinutes())}:${padTwoDigits(date.getSeconds())}`;
}

export function formatDurationSeconds(seconds: number | null | undefined): string {
  if (seconds == null || seconds < 0 || Number.isNaN(seconds)) {
    return '00:00:00';
  }

  const totalSeconds = Math.floor(seconds);
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const remainingSeconds = totalSeconds % 60;

  return `${padTwoDigits(hours)}:${padTwoDigits(minutes)}:${padTwoDigits(remainingSeconds)}`;
}

function parseDurationToSeconds(duration: string | null | undefined): number | null {
  if (!duration) {
    return null;
  }

  const match = /^(\d+):(\d{2}):(\d{2})$/.exec(duration.trim());
  if (!match) {
    return null;
  }

  return Number(match[1]) * 3600 + Number(match[2]) * 60 + Number(match[3]);
}

export function formatTripKilometers(distance: number | null | undefined): string {
  if (distance == null) {
    return '--';
  }

  return (distance / 1000).toFixed(3);
}

export interface TripSummaryMetrics {
  stop: string;
  kilometers: string;
  driving: string;
  idling: string;
  ignition: string;
}

export function getTripSummaryMetrics(trip: Trip): TripSummaryMetrics {
  const idleSeconds = trip.idleTimeSeconds ?? parseDurationToSeconds(trip.idleTime) ?? 0;
  const ignitionSeconds = trip.tripDurationSeconds ?? parseDurationToSeconds(trip.tripDuration);
  const drivingSeconds = ignitionSeconds != null
    ? Math.max(0, ignitionSeconds - idleSeconds)
    : null;

  return {
    stop: formatTimeOfDay(trip.endTimestamp),
    kilometers: formatTripKilometers(trip.tripDistance),
    driving: drivingSeconds != null
      ? formatDurationSeconds(drivingSeconds)
      : (trip.tripDuration ?? '--'),
    idling: trip.idleTime ?? formatDurationSeconds(idleSeconds),
    ignition: trip.tripDuration ?? (ignitionSeconds != null
      ? formatDurationSeconds(ignitionSeconds)
      : '--'),
  };
}
