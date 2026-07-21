import { useCallback, useEffect, useMemo, useState } from "react";
import { tokens, Button, MessageBar, MessageBarBody, Subtitle2 } from '@fluentui/react-components';
import { DeleteRegular, LocationRegular } from "@fluentui/react-icons";
import { VehicleTable } from '@modules/fleet/components/VehicleTable';
import { FlashSettingsRegular, MapPinRegular, VehicleCarRegular } from "@fluentui/react-icons";
import AppFilters from '@platform/ui/AppFilters';
import { ConfirmAction } from '@platform/ui/ConfirmAction';
import { CreateVehicle } from '@modules/fleet/components/vehicles/CreateVehicle';
import { VehicleDetailsDialog } from '@modules/fleet/components/vehicles/VehicleDetailsDialog';
import { VehicleFormDialog } from '@modules/fleet/components/vehicles/VehicleFormDialog';
import { VehicleLiveTrackDialog } from '@modules/fleet/components/vehicles/VehicleLiveTrackDialog';
import { VehicleTripsDrawer } from '@modules/fleet/components/vehicles/VehicleTripsDrawer';
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import { ApiError } from '@platform/api/apiClient';
import { deleteVehicles, getVehicles } from '@modules/fleet/services/vehicleService';
import type { Vehicle } from '@modules/fleet/types/vehicle';
import { filterVehicles } from '@modules/fleet/search/filters';

const filters = [
  { name: "status", label: "Status", value: "active", icon: VehicleCarRegular },
  { name: "type", label: "Type", value: "truck", icon: FlashSettingsRegular },
  { name: "location", label: "Location", value: "city", icon: MapPinRegular },
];

const VehicleList = () => {
  const searchQuery = usePageSearchQuery();
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [tripsRegistration, setTripsRegistration] = useState<string | null>(null);
  const [detailsVehicle, setDetailsVehicle] = useState<Vehicle | null>(null);
  const [editVehicle, setEditVehicle] = useState<Vehicle | null>(null);
  const [trackVehicle, setTrackVehicle] = useState<Vehicle | null>(null);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [pendingDeleteIds, setPendingDeleteIds] = useState<string[]>([]);
  const [isDeleting, setIsDeleting] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const selectedVehicles = useMemo(
    () => vehicles.filter((vehicle) => selectedIds.includes(vehicle.id)),
    [vehicles, selectedIds],
  );

  const visibleVehicles = useMemo(
    () => filterVehicles(vehicles, searchQuery),
    [searchQuery, vehicles],
  );

  const handleVehicleSaved = () => {
    setReloadKey((value) => value + 1);
    setEditVehicle(null);
  };

  const requestDelete = useCallback((ids: string[]) => {
    if (ids.length === 0) {
      return;
    }
    setPendingDeleteIds(ids);
    setDeleteConfirmOpen(true);
  }, []);

  const handleConfirmDelete = useCallback(async () => {
    if (pendingDeleteIds.length === 0) {
      return;
    }

    setIsDeleting(true);
    setActionError(null);

    try {
      await deleteVehicles(pendingDeleteIds);
      setSelectedIds((current) => current.filter((id) => !pendingDeleteIds.includes(id)));
      setPendingDeleteIds([]);
      setDeleteConfirmOpen(false);
      setReloadKey((value) => value + 1);
    } catch (deleteError) {
      const message = deleteError instanceof ApiError
        ? deleteError.message
        : "Failed to delete selected vehicles.";
      setActionError(message);
    } finally {
      setIsDeleting(false);
    }
  }, [pendingDeleteIds]);

  useEffect(() => {
    let cancelled = false;

    async function loadVehicles() {
      setIsLoading(true);
      setError(null);

      try {
        const response = await getVehicles();
        if (!cancelled) {
          setVehicles(response.items);
          setSelectedIds((current) =>
            current.filter((id) => response.items.some((vehicle) => vehicle.id === id)),
          );
        }
      } catch (loadError) {
        if (!cancelled) {
          const message = loadError instanceof ApiError
            ? loadError.message
            : "Failed to load vehicles.";
          setError(message);
          setVehicles([]);
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadVehicles();

    return () => {
      cancelled = true;
    };
  }, [reloadKey]);

  const deleteMessage = pendingDeleteIds.length === 1
    ? `This will remove ${vehicles.find((vehicle) => vehicle.id === pendingDeleteIds[0])?.registrationNumber ?? "the selected vehicle"} from your fleet list`
    : `This will remove ${pendingDeleteIds.length} vehicles from your fleet list`;

  return (
    <div className="flex flex-col w-full h-full px-3 pt-3 overflow-y-hidden overflow-x-hidden">
      <div className="flex justify-between items-center ">
        <Subtitle2 className="mx-3">Fleet management</Subtitle2>
        <div className="flex justify-between mb-3 gap-2">
          <AppFilters filters={filters} onFilterChange={() => {}} />
          <CreateVehicle onCreated={() => setReloadKey((value) => value + 1)} />
        </div>
      </div>
      <div className="flex flex-col w-full min-w-0 h-full rounded shadow overflow-hidden" style={{ backgroundColor: tokens.colorNeutralBackground1 }}>
        <div className="p-3 border-b flex justify-between items-center gap-3" style={{ borderBottomColor: tokens.colorNeutralStroke3 }}>
          <Subtitle2>All Vehicles</Subtitle2>
          <div className="flex items-center gap-3">
            {selectedIds.length > 0 && (
              <div className="flex items-center gap-2">
                <span className="text-sm text-neutral-foreground-3">
                  {selectedIds.length} selected
                </span>
                {selectedIds.length === 1 && (
                  <Button
                    appearance="secondary"
                    size="small"
                    icon={<LocationRegular />}
                    onClick={() => {
                      const vehicle = selectedVehicles[0];
                      if (vehicle) {
                        setTrackVehicle(vehicle);
                      }
                    }}
                  >
                    Track live
                  </Button>
                )}
                <Button
                  appearance="secondary"
                  size="small"
                  icon={<DeleteRegular />}
                  onClick={() => requestDelete(selectedIds)}
                >
                  Delete
                </Button>
              </div>
            )}
            {!isLoading && !error && (
              <span className="text-sm text-neutral-foreground-3">{visibleVehicles.length} vehicles</span>
            )}
          </div>
        </div>

        {actionError && (
          <div className="px-3 pt-3">
            <MessageBar intent="error">
              <MessageBarBody>{actionError}</MessageBarBody>
            </MessageBar>
          </div>
        )}

        <VehicleTable
          items={visibleVehicles}
          isLoading={isLoading}
          error={error}
          selectedIds={selectedIds}
          onSelectionChange={setSelectedIds}
          onViewDetails={setDetailsVehicle}
          onEdit={setEditVehicle}
          onViewReports={(vehicle) => setTripsRegistration(vehicle.registrationNumber)}
          onTrackLive={setTrackVehicle}
          onDelete={(vehicle) => requestDelete([vehicle.id])}
        />
      </div>

      <VehicleDetailsDialog
        vehicle={detailsVehicle}
        open={detailsVehicle !== null}
        onClose={() => setDetailsVehicle(null)}
        onEdit={(vehicle) => {
          setDetailsVehicle(null);
          setEditVehicle(vehicle);
        }}
        onViewReports={(vehicle) => {
          setDetailsVehicle(null);
          setTripsRegistration(vehicle.registrationNumber);
        }}
      />

      <VehicleLiveTrackDialog
        vehicle={trackVehicle}
        open={trackVehicle !== null}
        onClose={() => setTrackVehicle(null)}
      />

      <VehicleFormDialog
        mode="edit"
        vehicle={editVehicle ?? undefined}
        open={editVehicle !== null}
        onOpenChange={(open) => {
          if (!open) {
            setEditVehicle(null);
          }
        }}
        onSaved={handleVehicleSaved}
      />

      {tripsRegistration && (
        <VehicleTripsDrawer
          registration={tripsRegistration}
          open={tripsRegistration !== null}
          onClose={() => setTripsRegistration(null)}
        />
      )}

      <ConfirmAction
        open={deleteConfirmOpen}
        onOpenChange={(open) => {
          setDeleteConfirmOpen(open);
          if (!open) {
            setPendingDeleteIds([]);
          }
        }}
        title="Delete vehicles"
        message={deleteMessage}
        actionName={isDeleting ? "Deleting..." : "Delete"}
        destructive
        onAction={() => {
          if (!isDeleting) {
            void handleConfirmDelete();
          }
        }}
        onCancel={() => {
          setPendingDeleteIds([]);
        }}
      />
    </div>
  );
};

export default VehicleList;
