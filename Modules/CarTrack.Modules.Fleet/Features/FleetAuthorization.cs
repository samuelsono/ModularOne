using CarTrack.Identity.Contracts;

namespace CarTrack.Modules.Fleet;

/// <summary>Fleet-domain authorization predicates (moved out of UserDataScope).</summary>
public static class FleetAuthorization
{
    public static bool CanAccessVehicle(this UserDataScope scope, Vehicle vehicle)
    {
        if (scope.BypassRowLevelSecurity || scope.HasFullFleetAccess)
        {
            return true;
        }

        if (!scope.IsDriverRestricted || scope.LinkedDriverId is null)
        {
            return !scope.IsDriverRestricted;
        }

        if (vehicle.AssignedDriverId == scope.LinkedDriverId)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(scope.LinkedDriverLicenceNumber)
            && string.Equals(vehicle.DriverLicenseNumber, scope.LinkedDriverLicenceNumber, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(scope.LinkedCarTrackDriverId)
            && string.Equals(vehicle.DriverId, scope.LinkedCarTrackDriverId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(scope.LinkedDriverCode)
            && string.Equals(vehicle.DriverId, scope.LinkedDriverCode, StringComparison.OrdinalIgnoreCase);
    }

    public static bool CanAccessDriver(this UserDataScope scope, Driver driver)
    {
        if (scope.BypassRowLevelSecurity || scope.HasFullFleetAccess)
        {
            return true;
        }

        if (scope.IsDriverRestricted && scope.LinkedDriverId is not null)
        {
            return driver.Id == scope.LinkedDriverId;
        }

        if (scope.IsFinanceBranchScoped)
        {
            return string.Equals(driver.Branch, scope.StaffBranch, StringComparison.OrdinalIgnoreCase);
        }

        if (scope.IsManagerApprover && scope.ReportLinkedDriverIds.Contains(driver.Id))
        {
            return true;
        }

        return !scope.IsDriverRestricted && !scope.IsFinanceBranchScoped;
    }
}
