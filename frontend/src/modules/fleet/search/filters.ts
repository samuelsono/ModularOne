import type { Driver } from '@modules/fleet/types/driver';
import type { Vehicle } from '@modules/fleet/types/vehicle';
import { matchesSearchQuery } from '@platform/search/searchText';

export function filterVehicles(vehicles: Vehicle[], query: string): Vehicle[] {
  const normalized = query.trim();
  if (!normalized) return vehicles;
  return vehicles.filter((vehicle) => matchesSearchQuery(normalized, [
    vehicle.registrationNumber, vehicle.vin, vehicle.make, vehicle.model,
    vehicle.engineNumber, vehicle.colour, vehicle.vehicleType, vehicle.registeredOwner,
    vehicle.status?.location?.positionDescription,
    vehicle.status?.driver?.firstName, vehicle.status?.driver?.lastName,
  ]));
}

export function filterDrivers(drivers: Driver[], query: string): Driver[] {
  const normalized = query.trim();
  if (!normalized) return drivers;
  return drivers.filter((driver) => matchesSearchQuery(normalized, [
    driver.firstName, driver.lastName, driver.name, driver.email, driver.workEmail,
    driver.driverId, driver.employeeNumber, driver.licenceNumber, driver.contactNumber,
    driver.workPhone, driver.department, driver.branch, driver.jobTitle,
  ]));
}
