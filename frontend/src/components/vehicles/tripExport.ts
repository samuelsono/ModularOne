import type { Trip, VehicleEvent } from '../../types/vehicle';
import { getTripSummaryMetrics, humanizeEventDescription } from './tripEventUtils';
import type { PathPoint } from './tripEventUtils';

function escapeCsvValue(value: string | number | boolean | null | undefined): string {
  if (value == null) {
    return '';
  }

  const stringValue = String(value);
  if (stringValue.includes(',') || stringValue.includes('"') || stringValue.includes('\n')) {
    return `"${stringValue.replace(/"/g, '""')}"`;
  }

  return stringValue;
}

function downloadBlob(content: string, filename: string, mimeType: string): void {
  const blob = new Blob([content], { type: mimeType });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  link.click();
  URL.revokeObjectURL(url);
}

function buildTripFilename(registration: string, trip: Trip, extension: string): string {
  const tripId = trip.tripId ?? 'trip';
  const safeRegistration = registration.replace(/[^\w-]+/g, '_');
  return `${safeRegistration}_trip_${tripId}.${extension}`;
}

export function downloadTripCsv(
  registration: string,
  trip: Trip,
  events: VehicleEvent[],
): void {
  const summary = getTripSummaryMetrics(trip);
  const rows: string[] = [];

  rows.push('Section,Field,Value');
  rows.push(`Summary,Registration,${escapeCsvValue(registration)}`);
  rows.push(`Summary,Trip ID,${escapeCsvValue(trip.tripId)}`);
  rows.push(`Summary,Title,${escapeCsvValue(trip.tripTitle ?? trip.tripType)}`);
  rows.push(`Summary,Start,${escapeCsvValue(trip.startTimestamp)}`);
  rows.push(`Summary,End,${escapeCsvValue(trip.endTimestamp)}`);
  rows.push(`Summary,Start Location,${escapeCsvValue(trip.startLocation)}`);
  rows.push(`Summary,End Location,${escapeCsvValue(trip.endLocation)}`);
  rows.push(`Summary,Stop,${escapeCsvValue(summary.stop)}`);
  rows.push(`Summary,Kilometers,${escapeCsvValue(summary.kilometers)}`);
  rows.push(`Summary,Driving,${escapeCsvValue(summary.driving)}`);
  rows.push(`Summary,Idling,${escapeCsvValue(summary.idling)}`);
  rows.push(`Summary,Ignition,${escapeCsvValue(summary.ignition)}`);
  rows.push(`Summary,Driver,${escapeCsvValue(trip.driverName)}`);
  rows.push('');

  rows.push([
    'Event ID',
    'Timestamp',
    'Description',
    'Latitude',
    'Longitude',
    'Speed (km/h)',
    'Bearing',
    'Altitude (m)',
    'Position',
    'Ignition',
    'Road Speeding',
  ].map(escapeCsvValue).join(','));

  for (const event of events) {
    rows.push([
      event.eventId,
      event.eventTs,
      humanizeEventDescription(event.eventDescription),
      event.latitude,
      event.longitude,
      event.speed,
      event.bearing,
      event.altitude,
      event.positionDescription,
      event.ignition,
      event.roadSpeeding,
    ].map(escapeCsvValue).join(','));
  }

  downloadBlob(
    rows.join('\n'),
    buildTripFilename(registration, trip, 'csv'),
    'text/csv;charset=utf-8',
  );
}

function escapeXml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
}

export function downloadTripKml(
  registration: string,
  trip: Trip,
  pathPoints: PathPoint[],
  events: VehicleEvent[],
): void {
  const summary = getTripSummaryMetrics(trip);
  const tripName = trip.tripTitle || trip.tripType || `Trip ${trip.tripId}`;
  const coordinates = pathPoints
    .map((point) => `${point.longitude},${point.latitude},0`)
    .join(' ');

  const eventPlacemarks = events
    .filter((event) => event.latitude != null && event.longitude != null)
    .map((event) => {
      const name = humanizeEventDescription(event.eventDescription);
      const description = [
        event.eventTs ? `Time: ${event.eventTs}` : null,
        event.speed != null ? `Speed: ${event.speed} km/h` : null,
        event.bearing != null ? `Bearing: ${event.bearing}°` : null,
        event.positionDescription,
      ].filter(Boolean).join('\n');

      return `
    <Placemark>
      <name>${escapeXml(name)}</name>
      <description>${escapeXml(description)}</description>
      <Point>
        <coordinates>${event.longitude},${event.latitude},0</coordinates>
      </Point>
    </Placemark>`;
    })
    .join('');

  const kml = `<?xml version="1.0" encoding="UTF-8"?>
<kml xmlns="http://www.opengis.net/kml/2.2">
  <Document>
    <name>${escapeXml(`${registration} — ${tripName}`)}</name>
    <description>${escapeXml(
      `Stop: ${summary.stop}\nKilometers: ${summary.kilometers}\nDriving: ${summary.driving}\nIdling: ${summary.idling}\nIgnition: ${summary.ignition}`,
    )}</description>
    ${coordinates ? `
    <Placemark>
      <name>${escapeXml('Trip route')}</name>
      <Style>
        <LineStyle>
          <color>ff0000ff</color>
          <width>4</width>
        </LineStyle>
      </Style>
      <LineString>
        <tessellate>1</tessellate>
        <coordinates>${coordinates}</coordinates>
      </LineString>
    </Placemark>` : ''}
    ${pathPoints[0] ? `
    <Placemark>
      <name>Start</name>
      <description>${escapeXml(trip.startLocation ?? '')}</description>
      <Point>
        <coordinates>${pathPoints[0].longitude},${pathPoints[0].latitude},0</coordinates>
      </Point>
    </Placemark>` : ''}
    ${pathPoints.length > 1 ? `
    <Placemark>
      <name>End</name>
      <description>${escapeXml(trip.endLocation ?? '')}</description>
      <Point>
        <coordinates>${pathPoints[pathPoints.length - 1].longitude},${pathPoints[pathPoints.length - 1].latitude},0</coordinates>
      </Point>
    </Placemark>` : ''}
    ${eventPlacemarks}
  </Document>
</kml>`;

  downloadBlob(
    kml,
    buildTripFilename(registration, trip, 'kml'),
    'application/vnd.google-earth.kml+xml;charset=utf-8',
  );
}
