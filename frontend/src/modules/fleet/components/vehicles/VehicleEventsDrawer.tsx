import { useCallback, useEffect, useState } from 'react';
import {
  Button,
  DrawerBody,
  DrawerHeader,
  DrawerHeaderTitle,
  MessageBar,
  MessageBarBody,
  OverlayDrawer,
  Spinner,
} from '@fluentui/react-components';
import {
  ArrowSyncRegular,
  ChevronLeftRegular,
  ChevronRightRegular,
  DismissRegular,
  VehicleCarProfileLtrRegular,
} from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import { getVehicleEvents } from '@modules/fleet/services/vehicleService';
import type { VehicleEvent } from '@modules/fleet/types/vehicle';
import {
  buildRangeFromDateInputs,
  formatDateRangeLabel,
  getTodayDateInputValue,
} from './historyDateRange';
import { VehicleHistoryDateFilters } from './VehicleHistoryDateFilters';

interface VehicleEventsDrawerProps {
  registration: string;
  open: boolean;
  onClose: () => void;
}

function formatEventTime(value: string | null): string {
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

function formatSpeed(value: number | null): string | null {
  return value == null ? null : `${Math.round(value)} km/h`;
}

function humanizeDescription(description: string | null): string {
  if (!description) {
    return 'Event';
  }

  return description
    .toLowerCase()
    .split('_')
    .map((word) => (word ? word[0].toUpperCase() + word.slice(1) : word))
    .join(' ');
}

const EventRow = ({ event }: { event: VehicleEvent }) => {
  const speed = formatSpeed(event.speed);

  return (
    <li className="flex gap-3 border-b border-[#e3e5e7] px-1 py-3">
      <div className="mt-1 flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-neutral-background-3 text-neutral-foreground-2">
        <VehicleCarProfileLtrRegular fontSize={18} />
      </div>
      <div className="min-w-0 flex-1">
        <div className="flex items-center justify-between gap-2">
          <span className="truncate text-sm font-semibold text-neutral-foreground-1">
            {humanizeDescription(event.eventDescription)}
          </span>
          <span className="shrink-0 text-xs text-neutral-foreground-3">
            {formatEventTime(event.eventTs)}
          </span>
        </div>
        {event.positionDescription && (
          <div className="mt-0.5 truncate text-xs text-neutral-foreground-2">
            {event.positionDescription}
          </div>
        )}
        <div className="mt-1 flex flex-wrap gap-x-3 gap-y-0.5 text-xs text-neutral-foreground-3">
          {speed && <span>Speed: {speed}</span>}
          {event.ignition != null && <span>Ignition: {event.ignition ? 'On' : 'Off'}</span>}
          {event.roadSpeeding && <span className="text-[#b10e1c]">Speeding</span>}
        </div>
      </div>
    </li>
  );
};

const PER_PAGE = 15;

export function VehicleEventsDrawer({ registration, open, onClose }: VehicleEventsDrawerProps) {
  const [events, setEvents] = useState<VehicleEvent[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [lastPage, setLastPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [startDate, setStartDate] = useState(getTodayDateInputValue);
  const [endDate, setEndDate] = useState(getTodayDateInputValue);

  const loadEvents = useCallback(async (
    targetPage: number,
    rangeStartDate: string,
    rangeEndDate: string,
  ) => {
    const range = buildRangeFromDateInputs(rangeStartDate, rangeEndDate);
    if (!range) {
      setError('Please select a valid start and end date.');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const response = await getVehicleEvents(registration, {
        start: range.start,
        end: range.end,
        page: targetPage,
        perPage: PER_PAGE,
      });
      setEvents(response.items);
      setPage(response.pagination.page);
      setLastPage(response.pagination.lastPage);
      setTotal(response.pagination.total);
    } catch (loadError) {
      const message = loadError instanceof ApiError
        ? loadError.message
        : 'Failed to load events.';
      setError(message);
      setEvents([]);
    } finally {
      setIsLoading(false);
    }
  }, [registration]);

  useEffect(() => {
    if (open) {
      const today = getTodayDateInputValue();
      setStartDate(today);
      setEndDate(today);
      setPage(1);
      void loadEvents(1, today, today);
    }
  }, [open, loadEvents]);

  function handleApplyRange() {
    setPage(1);
    void loadEvents(1, startDate, endDate);
  }

  return (
    <OverlayDrawer
      open={open}
      position="end"
      size="medium"
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
              <Button
                appearance="subtle"
                aria-label="Refresh events"
                icon={isLoading ? <Spinner size="tiny" /> : <ArrowSyncRegular />}
                disabled={isLoading}
                onClick={() => void loadEvents(page, startDate, endDate)}
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
          Events — {registration}
        </DrawerHeaderTitle>
      </DrawerHeader>

      <DrawerBody>
        <VehicleHistoryDateFilters
          startDate={startDate}
          endDate={endDate}
          isLoading={isLoading}
          onStartDateChange={setStartDate}
          onEndDateChange={setEndDate}
          onApply={handleApplyRange}
        />

        <div className="mt-3 flex items-center justify-between text-xs text-neutral-foreground-3">
          <span>{formatDateRangeLabel(startDate, endDate)}</span>
          {!isLoading && !error && total > 0 && <span>{total} total</span>}
        </div>

        {isLoading && (
          <div className="flex justify-center py-6">
            <Spinner size="medium" label="Loading events..." />
          </div>
        )}

        {!isLoading && error && (
          <MessageBar intent="error" className="mt-3">
            <MessageBarBody>{error}</MessageBarBody>
          </MessageBar>
        )}

        {!isLoading && !error && events.length === 0 && (
          <div className="py-6 text-sm text-neutral-foreground-3">
            No events recorded for the selected date range.
          </div>
        )}

        {!isLoading && !error && events.length > 0 && (
          <>
            <ul className="mt-2 flex flex-col">
              {events.map((event) => (
                <EventRow key={event.eventId} event={event} />
              ))}
            </ul>

            <div className="mt-3 flex items-center justify-between">
              <Button
                appearance="secondary"
                icon={<ChevronLeftRegular />}
                disabled={isLoading || page <= 1}
                onClick={() => void loadEvents(page - 1, startDate, endDate)}
              >
                Previous
              </Button>
              <span className="text-xs text-neutral-foreground-3">
                Page {page} of {lastPage}
              </span>
              <Button
                appearance="secondary"
                icon={<ChevronRightRegular />}
                iconPosition="after"
                disabled={isLoading || page >= lastPage}
                onClick={() => void loadEvents(page + 1, startDate, endDate)}
              >
                Next
              </Button>
            </div>
          </>
        )}
      </DrawerBody>
    </OverlayDrawer>
  );
}
