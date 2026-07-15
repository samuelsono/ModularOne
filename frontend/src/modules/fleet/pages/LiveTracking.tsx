import { useCallback, useEffect, useRef, useState } from "react";
import {
  Button,
  List,
  ListItem,
  MessageBar,
  MessageBarBody,
  Persona,
  Popover,
  PopoverSurface,
  PopoverTrigger,
  Spinner,
  Text,
  type PresenceBadgeStatus,
} from "@fluentui/react-components";
import {
  ArrowSyncRegular,
} from "@fluentui/react-icons";
import AppMap from '@modules/fleet/components/AppMap';
import { VehicleDetailPanel } from '@modules/fleet/components/vehicles/VehicleDetailPanel';
import { VehicleEventsDrawer } from '@modules/fleet/components/vehicles/VehicleEventsDrawer';
import { VehicleTripsDrawer } from '@modules/fleet/components/vehicles/VehicleTripsDrawer';
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import { ApiError } from '@platform/api/apiClient';
import {
  formatLastSyncedAt,
  getVehicleDisplayName,
  getVehicles,
  mapIgnitionToPresence,
  syncVehiclesFromCarTrack,
} from '@modules/fleet/services/vehicleService';
import type { Vehicle } from '@modules/fleet/types/vehicle';
import { filterVehicles } from '@modules/fleet/search/filters';

function buildCacheSummary(lastSyncedAt: string | null, isCacheFresh: boolean): string | null {
  const relative = formatLastSyncedAt(lastSyncedAt);
  if (!relative) {
    return null;
  }

  return isCacheFresh
    ? `Using cached fleet data (synced ${relative}).`
    : `Fleet data last synced ${relative}.`;
}

function buildSyncSummary(created: number, updated: number): string {
  return `Synced ${created} new and ${updated} updated vehicles from CarTrack.`;
}

const LiveTracking = () => {
  const searchQuery = usePageSearchQuery();
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSyncing, setIsSyncing] = useState(false);
  const [eventsRegistration, setEventsRegistration] = useState<string | null>(null);
  const [tripsRegistration, setTripsRegistration] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const initialLoadStarted = useRef(false);

  const applyVehicleResponse = useCallback((
    items: Vehicle[],
    lastSyncedAt: string | null,
    isCacheFresh: boolean,
    syncMessage?: string,
  ) => {
    setVehicles(items);
    setStatusMessage(syncMessage ?? buildCacheSummary(lastSyncedAt, isCacheFresh));
  }, []);

  const loadFromCache = useCallback(async () => {
    const response = await getVehicles();
    applyVehicleResponse(
      response.items,
      response.lastSyncedAt,
      response.isCacheFresh,
    );
    return response;
  }, [applyVehicleResponse]);

  const syncFromCarTrack = useCallback(async () => {
    const response = await syncVehiclesFromCarTrack();
    applyVehicleResponse(
      response.items,
      response.lastSyncedAt,
      response.isCacheFresh,
      buildSyncSummary(response.created, response.updated),
    );
  }, [applyVehicleResponse]);

  useEffect(() => {
    // Guarded so the initial load runs once even under React StrictMode's
    // double-invoked effects. We intentionally do not cancel in cleanup:
    // the guard already prevents duplicate work, and cancelling would leave
    // the spinner stuck because the first (only) run would skip setIsLoading(false).
    if (initialLoadStarted.current) {
      return;
    }

    initialLoadStarted.current = true;

    async function loadInitialVehicles() {
      setIsLoading(true);
      setError(null);
      setStatusMessage(null);

      try {
        const cached = await loadFromCache();

        if (!cached.isCacheFresh) {
          await syncFromCarTrack();
        }
      } catch (loadError) {
        const message = loadError instanceof ApiError
          ? loadError.message
          : "Failed to load vehicles.";
        setError(message);
        setVehicles([]);
      } finally {
        setIsLoading(false);
      }
    }

    void loadInitialVehicles();
  }, [loadFromCache, syncFromCarTrack]);

  async function handleManualSync() {
    setIsSyncing(true);
    setError(null);

    try {
      await syncFromCarTrack();
    } catch (syncError) {
      const message = syncError instanceof ApiError
        ? syncError.message
        : "Failed to sync vehicles from CarTrack.";
      setError(message);
    } finally {
      setIsSyncing(false);
    }
  }

  const filteredVehicles = filterVehicles(vehicles, searchQuery);

  return (
    <div className="flex flex-col w-full h-full overflow-y-hidden">
      <div className="flex flex-col w-full h-full bg-white">
        <div className="absolute w-[260px] flex flex-col top-14 left-15 z-50 bg-white shadow-md rounded h-[95vh] border-b border-[#e3e5e7]">
          <div className="m-3 flex gap-2 items-center justify-between">
            <Text size={200} className="text-neutral-600">Use the top search bar to filter vehicles.</Text>
            <Button
              appearance="subtle"
              icon={isSyncing ? <Spinner size="tiny" /> : <ArrowSyncRegular />}
              aria-label="Sync from CarTrack"
              title="Sync from CarTrack"
              disabled={isLoading || isSyncing}
              onClick={() => void handleManualSync()}
            />
          </div>

          {isLoading && (
            <div className="flex justify-center p-4">
              <Spinner size="medium" label="Loading vehicles..." />
            </div>
          )}

          {error && (
            <div className="px-3 pb-2">
              <MessageBar intent="error">
                <MessageBarBody>{error}</MessageBarBody>
              </MessageBar>
            </div>
          )}

          {!isLoading && !error && statusMessage && (
            <div className="px-3 pb-2 text-xs text-neutral-foreground-3">
              {statusMessage}
            </div>
          )}

          {!isLoading && !error && filteredVehicles.length === 0 && (
            <div className="px-3 pb-3 text-sm text-neutral-foreground-3">
              No vehicles found.
            </div>
          )}

          <List navigationMode="composite">
            {filteredVehicles.map((vehicle) => (
              <ListItem
                key={vehicle.id}
                className="flex flex-col p-2! hover:bg-slate-100 border-b border-[#e3e5e7]"
              >
                <Popover positioning={"after"} withArrow>
                  <PopoverTrigger disableButtonEnhancement>
                    <button
                      type="button"
                      className="w-full cursor-pointer border-0 bg-transparent p-0 text-left"
                    >
                      <Persona
                        avatar={{ name: getVehicleDisplayName(vehicle) }}
                        presence={{ status: mapIgnitionToPresence(vehicle.ignitionStatus) as PresenceBadgeStatus }}
                        primaryText={vehicle.registrationNumber}
                        secondaryText={getVehicleDisplayName(vehicle)}
                      />
                    </button>
                  </PopoverTrigger>
                  <PopoverSurface tabIndex={-1} className="p-0!">
                    <VehicleDetailPanel
                      vehicle={vehicle}
                      onViewEvents={() => setEventsRegistration(vehicle.registrationNumber)}
                      onViewTrips={() => setTripsRegistration(vehicle.registrationNumber)}
                    />
                  </PopoverSurface>
                </Popover>
              </ListItem>
            ))}
          </List>
        </div>
        <div className="flex-1 min-h-0">
          <AppMap vehicles={vehicles} style={{ height: '100%', minHeight: '100vh' }} />
        </div>
      </div>

      {eventsRegistration && (
        <VehicleEventsDrawer
          registration={eventsRegistration}
          open={eventsRegistration !== null}
          onClose={() => setEventsRegistration(null)}
        />
      )}

      {tripsRegistration && (
        <VehicleTripsDrawer
          registration={tripsRegistration}
          open={tripsRegistration !== null}
          onClose={() => setTripsRegistration(null)}
        />
      )}
    </div>
  );
};

export default LiveTracking;