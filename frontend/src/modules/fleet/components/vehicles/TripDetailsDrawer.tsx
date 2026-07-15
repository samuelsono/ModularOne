import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Badge,
  Button,
  DrawerBody,
  DrawerHeader,
  DrawerHeaderTitle,
  Menu,
  MenuItem,
  MenuList,
  MenuPopover,
  MenuTrigger,
  MessageBar,
  MessageBarBody,
  OverlayDrawer,
  Spinner,
  Tooltip,
} from '@fluentui/react-components';
import {
  ArrowDownRegular,
  ArrowSyncRegular,
  ArrowUpRegular,
  ArrowRotateClockwiseRegular,
  ArrowDownloadRegular,
  DismissRegular,
  LocationRegular,
  VehicleCarProfileLtrRegular,
  WarningRegular,
} from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import { getTripEvents } from '@modules/fleet/services/vehicleService';
import type { Trip, VehicleEvent } from '@modules/fleet/types/vehicle';
import { TripReplayMap } from './TripReplayMap';
import { TripSummaryBar } from './TripSummaryBar';
import { downloadTripCsv, downloadTripKml } from './tripExport';
import {
  buildTripPath,
  classifyHarshEvent,
  countHarshEventsByType,
  formatTripDateTime,
  getHarshEventLabel,
  getTripHarshCounts,
  humanizeEventDescription,
  type HarshEventType,
} from './tripEventUtils';

interface TripDetailsDrawerProps {
  registration: string;
  trip: Trip | null;
  open: boolean;
  onClose: () => void;
}

const HARSH_ICON_BY_TYPE = {
  braking: ArrowDownRegular,
  cornering: ArrowRotateClockwiseRegular,
  acceleration: ArrowUpRegular,
} as const;

function HarshEventBadge({
  type,
  count,
}: {
  type: HarshEventType;
  count: number;
}) {
  if (count <= 0) {
    return null;
  }

  const Icon = HARSH_ICON_BY_TYPE[type];

  return (
    <Tooltip content={getHarshEventLabel(type)} relationship="label">
      <div className="relative flex h-9 w-9 items-center justify-center rounded-full bg-[#fde7e9] text-[#b10e1c]">
        <Icon fontSize={18} />
        <Badge
          appearance="filled"
          color="danger"
          size="small"
          className="absolute -right-1 -top-1 min-w-[28px] justify-center"
        >
          {count}
        </Badge>
      </div>
    </Tooltip>
  );
}

function TripEventRow({ event }: { event: VehicleEvent }) {
  const harshType = classifyHarshEvent(event.eventDescription);
  const Icon = harshType ? HARSH_ICON_BY_TYPE[harshType] : VehicleCarProfileLtrRegular;
  const iconClassName = harshType
    ? 'bg-[#fde7e9] text-[#b10e1c]'
    : 'bg-neutral-background-3 text-neutral-foreground-2';

  return (
    <li className="flex gap-3 border-b border-[#e3e5e7] px-1 py-2.5">
      <Tooltip
        content={harshType ? getHarshEventLabel(harshType) : humanizeEventDescription(event.eventDescription)}
        relationship="label"
      >
        <div className={`mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-full ${iconClassName}`}>
          <Icon fontSize={16} />
        </div>
      </Tooltip>
      <div className="min-w-0 flex-1">
        <div className="flex items-center justify-between gap-2">
          <span className="truncate text-sm font-semibold text-neutral-foreground-1">
            {humanizeEventDescription(event.eventDescription)}
          </span>
          <span className="shrink-0 text-xs text-neutral-foreground-3">
            {formatTripDateTime(event.eventTs)}
          </span>
        </div>
        {event.positionDescription && (
          <div className="mt-0.5 truncate text-xs text-neutral-foreground-2">
            {event.positionDescription}
          </div>
        )}
        <div className="mt-1 flex flex-wrap gap-x-3 gap-y-0.5 text-xs text-neutral-foreground-3">
          {event.speed != null && <span>Speed: {Math.round(event.speed)} km/h</span>}
          {event.bearing != null && <span>Bearing: {Math.round(event.bearing)}°</span>}
          {event.altitude != null && <span>Alt: {Math.round(event.altitude)} m</span>}
          {event.latitude != null && event.longitude != null && (
            <span>
              GPS: {event.latitude.toFixed(5)}, {event.longitude.toFixed(5)}
            </span>
          )}
        </div>
      </div>
    </li>
  );
}

export function TripDetailsDrawer({
  registration,
  trip,
  open,
  onClose,
}: TripDetailsDrawerProps) {
  const [events, setEvents] = useState<VehicleEvent[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isReplayFullscreen, setIsReplayFullscreen] = useState(false);

  const loadEvents = useCallback(async () => {
    if (!trip?.startTimestamp || !trip.endTimestamp) {
      setError('Trip timestamps are missing.');
      setEvents([]);
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const response = await getTripEvents(registration, trip);
      setEvents(response.items);
    } catch (loadError) {
      const message = loadError instanceof ApiError
        ? loadError.message
        : 'Failed to load trip events.';
      setError(message);
      setEvents([]);
    } finally {
      setIsLoading(false);
    }
  }, [registration, trip]);

  useEffect(() => {
    if (open && trip) {
      void loadEvents();
    } else {
      setEvents([]);
      setError(null);
      setIsReplayFullscreen(false);
    }
  }, [open, trip, loadEvents]);

  useEffect(() => {
    if (!isReplayFullscreen) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setIsReplayFullscreen(false);
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isReplayFullscreen]);

  const pathPoints = useMemo(
    () => (trip ? buildTripPath(events, trip) : []),
    [events, trip],
  );

  const harshCounts = useMemo(() => {
    if (!trip) {
      return { braking: 0, cornering: 0, acceleration: 0 };
    }

    const fromEvents = countHarshEventsByType(events);
    const fromTrip = getTripHarshCounts(trip);

    return {
      braking: Math.max(fromEvents.braking, fromTrip.braking),
      cornering: Math.max(fromEvents.cornering, fromTrip.cornering),
      acceleration: Math.max(fromEvents.acceleration, fromTrip.acceleration),
    };
  }, [events, trip]);

  const harshTotal = harshCounts.braking + harshCounts.cornering + harshCounts.acceleration;

  return (
    <OverlayDrawer
      open={open}
      position="bottom"
      size="large"
      modalType="non-modal"
      className="!h-[78vh]"
      onOpenChange={(_, data) => {
        if (!data.open) {
          onClose();
        }
      }}
    >
      <DrawerHeader>
        <DrawerHeaderTitle
          action={
            <div className="flex gap-1">
              <Menu>
                <MenuTrigger disableButtonEnhancement>
                  <Button
                    appearance="subtle"
                    aria-label="Download trip details"
                    icon={<ArrowDownloadRegular />}
                    disabled={!trip || isLoading}
                  />
                </MenuTrigger>
                <MenuPopover>
                  <MenuList>
                    <MenuItem
                      onClick={() => {
                        if (trip) {
                          downloadTripCsv(registration, trip, events);
                        }
                      }}
                    >
                      Download CSV
                    </MenuItem>
                    <MenuItem
                      onClick={() => {
                        if (trip) {
                          downloadTripKml(registration, trip, pathPoints, events);
                        }
                      }}
                    >
                      Download KML
                    </MenuItem>
                  </MenuList>
                </MenuPopover>
              </Menu>
              <Button
                appearance="subtle"
                aria-label="Refresh trip events"
                icon={isLoading ? <Spinner size="tiny" /> : <ArrowSyncRegular />}
                disabled={isLoading || !trip}
                onClick={() => void loadEvents()}
              />
              <Button
                appearance="subtle"
                aria-label="Close"
                icon={<DismissRegular />}
                onClick={onClose}
              />
            </div>
          }
        >
          {trip?.tripTitle || trip?.tripType || 'Trip details'}
          {registration ? ` — ${registration}` : ''}
        </DrawerHeaderTitle>
      </DrawerHeader>

      <DrawerBody className="!overflow-y-auto">
        {trip && (
          <div className="grid gap-4 lg:grid-cols-[minmax(0,1.2fr)_minmax(0,1fr)]">
            <div className="flex flex-col gap-4">
              <div className="grid gap-3 sm:grid-cols-2">
                <div className="rounded-lg border border-[#e3e5e7] p-3">
                  <div className="mb-1 flex items-center gap-1 text-xs font-semibold uppercase tracking-wide text-neutral-foreground-3">
                    <LocationRegular fontSize={14} />
                    Start
                  </div>
                  <div className="text-sm font-medium text-neutral-foreground-1">
                    {formatTripDateTime(trip.startTimestamp)}
                  </div>
                  <div className="mt-1 text-sm text-neutral-foreground-2">
                    {trip.startLocation ?? 'Unknown location'}
                  </div>
                </div>

                <div className="rounded-lg border border-[#e3e5e7] p-3">
                  <div className="mb-1 flex items-center gap-1 text-xs font-semibold uppercase tracking-wide text-neutral-foreground-3">
                    <LocationRegular fontSize={14} />
                    End
                  </div>
                  <div className="text-sm font-medium text-neutral-foreground-1">
                    {formatTripDateTime(trip.endTimestamp)}
                  </div>
                  <div className="mt-1 text-sm text-neutral-foreground-2">
                    {trip.endLocation ?? 'Unknown location'}
                  </div>
                </div>
              </div>

              {harshTotal > 0 && (
                <div className="flex flex-wrap items-center gap-3 rounded-lg border border-[#fde7e9] bg-[#fff5f6] px-3 py-2">
                  <div className="flex items-center gap-1 text-sm font-medium text-[#b10e1c]">
                    <WarningRegular fontSize={16} />
                    Harsh events
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <HarshEventBadge type="braking" count={harshCounts.braking} />
                    <HarshEventBadge type="cornering" count={harshCounts.cornering} />
                    <HarshEventBadge type="acceleration" count={harshCounts.acceleration} />
                  </div>
                </div>
              )}

              {trip && (
                <div
                  className={
                    isReplayFullscreen
                      ? 'fixed inset-0 z-[1200] flex flex-col bg-white'
                      : 'overflow-hidden rounded-lg border border-[#e3e5e7]'
                  }
                >
                  <TripReplayMap
                    pathPoints={pathPoints}
                    isFullscreen={isReplayFullscreen}
                    onToggleFullscreen={() => setIsReplayFullscreen((value) => !value)}
                  />
                  <TripSummaryBar trip={trip} />
                </div>
              )}
            </div>

            <div className="flex min-h-0 flex-col">
              <div className="mb-2 flex items-center justify-between">
                <h3 className="text-sm font-semibold text-neutral-foreground-1">
                  Trip events
                </h3>
                {!isLoading && !error && (
                  <span className="text-xs text-neutral-foreground-3">
                    {events.length} events
                  </span>
                )}
              </div>

              {isLoading && (
                <div className="flex justify-center py-8">
                  <Spinner size="medium" label="Loading trip events..." />
                </div>
              )}

              {!isLoading && error && (
                <MessageBar intent="error">
                  <MessageBarBody>{error}</MessageBarBody>
                </MessageBar>
              )}

              {!isLoading && !error && events.length === 0 && (
                <div className="py-6 text-sm text-neutral-foreground-3">
                  No events recorded for this trip window.
                </div>
              )}

              {!isLoading && !error && events.length > 0 && (
                <ul className="max-h-[65vh] overflow-y-auto rounded-lg border border-[#e3e5e7]">
                  {events.map((event) => (
                    <TripEventRow key={event.eventId} event={event} />
                  ))}
                </ul>
              )}
            </div>
          </div>
        )}
      </DrawerBody>
    </OverlayDrawer>
  );
}
