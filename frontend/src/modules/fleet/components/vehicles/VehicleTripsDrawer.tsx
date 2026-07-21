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
  TimelineRegular,
} from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import { getVehicleTrips } from '@modules/fleet/services/vehicleService';
import type { Trip } from '@modules/fleet/types/vehicle';
import {
  buildRangeFromDateInputs,
  formatDateRangeLabel,
  getTodayDateInputValue,
} from './historyDateRange';
import { VehicleHistoryDateFilters } from './VehicleHistoryDateFilters';
import { TripDetailsDrawer } from './TripDetailsDrawer';

interface VehicleTripsDrawerProps {
  registration: string;
  open: boolean;
  onClose: () => void;
}

const PER_PAGE = 15;

function formatTime(value: string | null): string {
  if (!value) {
    return '--';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '--';
  }

  return date.toLocaleString([], {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  });
}

function formatDistance(value: number | null): string | null {
  if (value == null) {
    return null;
  }

  if (value <= 0) {
    return '0 km';
  }

  const kilometers = value / 1000;
  const [integerPart, decimalPart] = kilometers.toFixed(1).split('.');
  const groupedInteger = integerPart.replace(/\B(?=(\d{3})+(?!\d))/g, ' ');

  return `${groupedInteger}.${decimalPart} km`;
}

const TripRow = ({ trip, onSelect }: { trip: Trip; onSelect: (trip: Trip) => void }) => {
  const distance = formatDistance(trip.tripDistance);
  const harshTotal =
    (trip.harshBrakingEvents ?? 0) +
    (trip.harshCorneringEvents ?? 0) +
    (trip.harshAccelerationEvents ?? 0);

  return (
    <li>
      <button
        type="button"
        className="flex w-full gap-3 border-b border-neutral-stroke-2 px-1 py-3 text-left transition-colors hover:bg-neutral-background-2"
        onClick={() => onSelect(trip)}
      >
        <div className="mt-1 flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-neutral-background-3 text-neutral-foreground-2">
          <TimelineRegular fontSize={18} />
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex items-center justify-between gap-2">
            <span className="truncate text-sm font-semibold text-neutral-foreground-1">
              {trip.tripTitle || trip.tripType || 'Trip'}
            </span>
            <span className="shrink-0 text-xs text-neutral-foreground-3">
              {formatTime(trip.startTimestamp)}
            </span>
          </div>

          <div className="mt-0.5 truncate text-xs text-neutral-foreground-2">
            {(trip.startLocation ?? 'Unknown')} → {(trip.endLocation ?? 'Unknown')}
          </div>

          <div className="mt-1 flex flex-wrap gap-x-3 gap-y-0.5 text-xs text-neutral-foreground-3">
            {trip.tripDuration && <span>Duration: {trip.tripDuration}</span>}
            {distance && <span>Distance: {distance}</span>}
            {trip.driverName && <span>Driver: {trip.driverName}</span>}
            {harshTotal > 0 && <span className="text-[#b10e1c]">{harshTotal} harsh events</span>}
          </div>
        </div>
      </button>
    </li>
  );
};

export function VehicleTripsDrawer({ registration, open, onClose }: VehicleTripsDrawerProps) {
  const [trips, setTrips] = useState<Trip[]>([]);
  const [selectedTrip, setSelectedTrip] = useState<Trip | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [lastPage, setLastPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [startDate, setStartDate] = useState(getTodayDateInputValue);
  const [endDate, setEndDate] = useState(getTodayDateInputValue);

  const loadTrips = useCallback(async (
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
      const response = await getVehicleTrips(registration, {
        start: range.start,
        end: range.end,
        page: targetPage,
        perPage: PER_PAGE,
      });
      setTrips(response.items);
      setPage(response.pagination.page);
      setLastPage(response.pagination.lastPage);
      setTotal(response.pagination.total);
    } catch (loadError) {
      const message = loadError instanceof ApiError
        ? loadError.message
        : 'Failed to load trips.';
      setError(message);
      setTrips([]);
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
      setSelectedTrip(null);
      void loadTrips(1, today, today);
    }
  }, [open, loadTrips]);

  function handleClose() {
    setSelectedTrip(null);
    onClose();
  }

  function handleApplyRange() {
    setPage(1);
    void loadTrips(1, startDate, endDate);
  }

  return (
    <>
      <OverlayDrawer
        open={open}
        position="end"
        modalType="non-modal"
        size="medium"
        onOpenChange={(_, data) => {
          if (!data.open) {
            handleClose();
          }
        }}
      >
      <DrawerHeader>
        <DrawerHeaderTitle
          action={
            <div className="flex gap-1">
              <Button
                appearance="subtle"
                aria-label="Refresh trips"
                icon={isLoading ? <Spinner size="tiny" /> : <ArrowSyncRegular />}
                disabled={isLoading}
                onClick={() => void loadTrips(page, startDate, endDate)}
              />
              <Button
                appearance="subtle"
                aria-label="Close"
                icon={<DismissRegular />}
                onClick={handleClose}
              />
            </div>
          }
        >
          Trips — {registration}
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
            <Spinner size="medium" label="Loading trips..." />
          </div>
        )}

        {!isLoading && error && (
          <MessageBar intent="error" className="mt-3">
            <MessageBarBody>{error}</MessageBarBody>
          </MessageBar>
        )}

        {!isLoading && !error && trips.length === 0 && (
          <div className="py-6 text-sm text-neutral-foreground-3">
            No trips recorded for the selected date range.
          </div>
        )}

        {!isLoading && !error && trips.length > 0 && (
          <>
            <ul className="mt-2 flex flex-col">
              {trips.map((trip) => (
                <TripRow key={trip.tripId} trip={trip} onSelect={setSelectedTrip} />
              ))}
            </ul>

            <div className="mt-3 flex items-center justify-between">
              <Button
                appearance="secondary"
                icon={<ChevronLeftRegular />}
                disabled={isLoading || page <= 1}
                onClick={() => void loadTrips(page - 1, startDate, endDate)}
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
                onClick={() => void loadTrips(page + 1, startDate, endDate)}
              >
                Next
              </Button>
            </div>
          </>
        )}
      </DrawerBody>
    </OverlayDrawer>

      <TripDetailsDrawer
        registration={registration}
        trip={selectedTrip}
        open={selectedTrip != null}
        onClose={() => setSelectedTrip(null)}
      />
    </>
  );
}
