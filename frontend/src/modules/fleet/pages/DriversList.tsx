import { useEffect, useMemo, useState } from 'react';
import { Subtitle2 } from '@fluentui/react-components';
import { MapPinRegular, PersonAccountsRegular, PersonSwapRegular, VehicleCarRegular } from '@fluentui/react-icons';
import AppFilters from '@platform/ui/AppFilters';
import { DriversTable } from '@modules/fleet/components/DriversTable';
import { CreateDriver } from '@modules/fleet/components/drivers/CreateDriver';
import { DriverDetailsDialog } from '@modules/fleet/components/drivers/DriverDetailsDialog';
import { DriverFormDialog } from '@modules/fleet/components/drivers/DriverFormDialog';
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import { ApiError } from '@platform/api/apiClient';
import { getDrivers } from '@modules/fleet/services/driverService';
import type { Driver } from '@modules/fleet/types/driver';
import { filterDrivers } from '@modules/fleet/search/filters';

const filters = [
  { name: 'status', label: 'Status', value: 'active', icon: PersonAccountsRegular },
  { name: 'gender', label: 'Gender', value: 'male', icon: PersonSwapRegular },
  { name: 'vehicle', label: 'Vehicle', value: 'truck', icon: VehicleCarRegular },
  { name: 'address', label: 'Address', value: 'city', icon: MapPinRegular },
];

const DriversList = () => {
  const searchQuery = usePageSearchQuery();
  const [drivers, setDrivers] = useState<Driver[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [detailsDriver, setDetailsDriver] = useState<Driver | null>(null);
  const [editDriver, setEditDriver] = useState<Driver | null>(null);

  useEffect(() => {
    async function loadDrivers() {
      setIsLoading(true);
      setError(null);

      try {
        const response = await getDrivers();
        setDrivers(response.items);
      } catch (loadError) {
        const message = loadError instanceof ApiError
          ? loadError.message
          : 'Failed to load drivers.';
        setError(message);
        setDrivers([]);
      } finally {
        setIsLoading(false);
      }
    }

    void loadDrivers();
  }, [reloadKey]);

  const visibleDrivers = useMemo(
    () => filterDrivers(drivers, searchQuery),
    [drivers, searchQuery],
  );

  function handleUpdated() {
    setReloadKey((value) => value + 1);
  }

  return (
    <div className="flex flex-col w-full h-full px-3 pt-3 overflow-y-hidden">
      <div className="flex justify-between mb-0 ">
        <Subtitle2 className="mx-3">Driver management</Subtitle2>
        <div className="flex justify-between mb-3 gap-2">
          <AppFilters filters={filters} onFilterChange={() => {}} />
          <CreateDriver onCreated={handleUpdated} />
        </div>
      </div>

      <div className="flex flex-col w-full h-full bg-white rounded shadow overflow-hidden">
        <div className="p-3 border-b border-[#e3e5e7] flex justify-between items-center">
          <Subtitle2>Your Drivers</Subtitle2>
          {!isLoading && !error && (
            <span className="text-sm text-neutral-foreground-3">{visibleDrivers.length} drivers</span>
          )}
        </div>
        <DriversTable
          items={visibleDrivers}
          isLoading={isLoading}
          error={error}
          onUpdated={handleUpdated}
          onViewDetails={setDetailsDriver}
        />
      </div>

      <DriverDetailsDialog
        driver={detailsDriver}
        open={detailsDriver !== null}
        onClose={() => setDetailsDriver(null)}
        onEdit={(driver) => {
          setDetailsDriver(null);
          setEditDriver(driver);
        }}
      />

      <DriverFormDialog
        mode="edit"
        driver={editDriver ?? undefined}
        open={editDriver !== null}
        onOpenChange={(open) => {
          if (!open) {
            setEditDriver(null);
          }
        }}
        onSaved={handleUpdated}
      />
    </div>
  );
};

export default DriversList;
