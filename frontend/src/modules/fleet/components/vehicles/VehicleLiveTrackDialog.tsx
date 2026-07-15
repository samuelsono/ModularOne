import { useCallback, useEffect, useRef, useState } from 'react';
import {
  Badge,
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  Label,
  MessageBar,
  MessageBarBody,
  Select,
  Spinner,
  Text,
} from '@fluentui/react-components';
import { DismissRegular } from '@fluentui/react-icons';
import AppMap from '../AppMap';
import { ApiError } from '@platform/api/apiClient';
import {
  formatLastSyncedAt,
  getVehicle,
  getVehicleDisplayName,
  syncVehiclesFromCarTrack,
} from '@modules/fleet/services/vehicleService';
import type { Vehicle } from '@modules/fleet/types/vehicle';
import { vehicleHasLocation } from '@modules/fleet/types/vehicle';

const REFRESH_OPTIONS = [
  { value: '30', label: '30 seconds' },
  { value: '60', label: '60 seconds' },
  { value: '90', label: '90 seconds' },
  { value: '120', label: '120 seconds' },
] as const;

const DEFAULT_REFRESH_SECONDS = 60;

interface VehicleLiveTrackDialogProps {
  vehicle: Vehicle | null;
  open: boolean;
  onClose: () => void;
}

export function VehicleLiveTrackDialog({
  vehicle,
  open,
  onClose,
}: VehicleLiveTrackDialogProps) {
  const [trackedVehicle, setTrackedVehicle] = useState<Vehicle | null>(null);
  const [refreshSeconds, setRefreshSeconds] = useState(DEFAULT_REFRESH_SECONDS);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [lastRefreshedAt, setLastRefreshedAt] = useState<Date | null>(null);
  const vehicleIdRef = useRef<string | null>(null);

  const refreshLocation = useCallback(async (vehicleId: string, syncFromCarTrack: boolean) => {
    setIsRefreshing(true);
    setError(null);

    try {
      if (syncFromCarTrack) {
        const syncResponse = await syncVehiclesFromCarTrack();
        const synced = syncResponse.items.find((item) => item.id === vehicleId);
        if (synced) {
          setTrackedVehicle(synced);
          setLastRefreshedAt(new Date());
          return;
        }
      }

      const latest = await getVehicle(vehicleId);
      setTrackedVehicle(latest);
      setLastRefreshedAt(new Date());
    } catch (refreshError) {
      const message = refreshError instanceof ApiError
        ? refreshError.message
        : 'Failed to refresh vehicle location.';
      setError(message);
    } finally {
      setIsRefreshing(false);
    }
  }, []);

  useEffect(() => {
    if (!open || !vehicle) {
      setTrackedVehicle(null);
      setError(null);
      setLastRefreshedAt(null);
      vehicleIdRef.current = null;
      return;
    }

    vehicleIdRef.current = vehicle.id;
    setTrackedVehicle(vehicle);
    void refreshLocation(vehicle.id, true);
  }, [open, vehicle, refreshLocation]);

  useEffect(() => {
    if (!open || !vehicleIdRef.current) {
      return;
    }

    const intervalId = window.setInterval(() => {
      const id = vehicleIdRef.current;
      if (id) {
        void refreshLocation(id, true);
      }
    }, refreshSeconds * 1000);

    return () => {
      window.clearInterval(intervalId);
    };
  }, [open, refreshSeconds, refreshLocation]);

  const hasLocation = trackedVehicle ? vehicleHasLocation(trackedVehicle) : false;
  const locationDescription = trackedVehicle?.status?.location?.positionDescription;
  const locationUpdated = trackedVehicle?.status?.location?.updated;
  const speed = trackedVehicle?.status?.telemetry?.speed;
  const bearing = trackedVehicle?.status?.telemetry?.bearing;

  return (
    <Dialog open={open} onOpenChange={(_, data) => { if (!data.open) onClose(); }}>
      <DialogSurface style={{ maxWidth: '960px', width: 'min(96vw, 960px)' }}>
        <DialogBody>
          <DialogTitle
            action={
              <button
                type="button"
                onClick={onClose}
                aria-label="Close"
                className="border-0 bg-transparent cursor-pointer p-1"
              >
                <DismissRegular />
              </button>
            }
          >
            Live track — {vehicle ? getVehicleDisplayName(vehicle) : 'Vehicle'}
          </DialogTitle>
          <DialogContent>
            <div className="flex flex-col gap-3">
              <div className="flex flex-wrap items-end justify-between gap-3">

                <div className="flex  gap-5">
                <div className="flex flex-col gap-1">
                  <Text weight="semibold">{vehicle?.registrationNumber}</Text>
                  {trackedVehicle && (
                    <Badge
                      appearance="filled"
                      color={
                        trackedVehicle.ignitionStatus === 'moving'
                          ? 'success'
                          : trackedVehicle.ignitionStatus === 'idling'
                            ? 'warning'
                            : 'informative'
                      }
                    >
                      {trackedVehicle.ignitionStatus === 'moving'
                        ? 'Moving'
                        : trackedVehicle.ignitionStatus === 'idling'
                          ? 'Idling'
                          : 'Ignition off'}
                    </Badge>
                  )}
                </div>
               
                <div className="flex flex-col gap-1 border-l border-gray-300 px-5">
                  <span className='font-bold '>Speed: </span>
                  <span>{Math.round(speed ?? 0)} km/h</span>
                </div>
                <div className="flex flex-col gap-1 border-l border-gray-300 px-5">
                  <span className='font-bold '>Bearing: </span>
                  <span>{Math.round(bearing ?? 0)}°</span>
                </div>
                </div>


                <div className="flex items-center gap-2">
                  <Label htmlFor="live-track-refresh">Refresh every</Label>
                  <Select
                    id="live-track-refresh"
                    value={String(refreshSeconds)}
                    onChange={(_, data) => {
                      const next = Number.parseInt(data.value, 10);
                      if (!Number.isNaN(next)) {
                        setRefreshSeconds(next);
                      }
                    }}
                    style={{ minWidth: '140px' }}
                  >
                    {REFRESH_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </Select>
                </div>
              </div>

              {error && (
                <MessageBar intent="error">
                  <MessageBarBody>{error}</MessageBarBody>
                </MessageBar>
              )}

              {!hasLocation && !isRefreshing && (
                <MessageBar intent="warning">
                  <MessageBarBody>
                    No GPS location is available for this vehicle yet. Try syncing from CarTrack.
                  </MessageBarBody>
                </MessageBar>
              )}

              <div className="relative rounded border border-neutral-stroke-1 overflow-hidden" style={{ height: '420px' }}>
                {isRefreshing && (
                  <div className="absolute inset-0 z-10 flex items-center justify-center bg-white/60">
                    <Spinner size="small" label="Refreshing location..." />
                  </div>
                )}
                {open && (
                  <AppMap
                    key={vehicle?.id ?? 'live-track'}
                    layoutKey={open}
                    vehicles={trackedVehicle ? [trackedVehicle] : []}
                    style={{ height: '100%' }}
                  />
                )}
              </div>

              <div className="flex flex-wrap gap-4 text-sm text-neutral-foreground-3">
                {speed != null && (
                  <span>Speed: {Math.round(speed)} km/h</span>
                )}
                {bearing != null && (
                  <span>Bearing: {Math.round(bearing)}°</span>
                )}
                {locationDescription && (
                  <span>{locationDescription}</span>
                )}
                {locationUpdated && (
                  <span>Location updated {formatLastSyncedAt(locationUpdated) ?? locationUpdated}</span>
                )}
                {lastRefreshedAt && (
                  <span>Last refreshed {formatLastSyncedAt(lastRefreshedAt.toISOString()) ?? 'just now'}</span>
                )}
              </div>
            </div>
          </DialogContent>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
