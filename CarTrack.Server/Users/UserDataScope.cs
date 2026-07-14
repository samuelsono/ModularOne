using CarTrack.Server.Data;

namespace CarTrack.Server.Users;

public sealed class UserDataScope
{
    public required string UserId { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = [];

    public bool BypassRowLevelSecurity { get; init; }

    public bool HasFullFleetAccess { get; init; }

    public Guid? LinkedDriverId { get; init; }

    public string? LinkedDriverLicenceNumber { get; init; }

    public string? LinkedDriverCode { get; init; }

    public string? LinkedCarTrackDriverId { get; init; }

    public string? StaffBranch { get; init; }

    public IReadOnlySet<string> ReportUserIds { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    public IReadOnlySet<Guid> ReportLinkedDriverIds { get; init; } = new HashSet<Guid>();

    public bool IsDriverRestricted =>
        !BypassRowLevelSecurity
        && !HasFullFleetAccess
        && Roles.Any(role => role.Equals(AppRoles.Driver, StringComparison.OrdinalIgnoreCase));

    public bool IsManagerApprover =>
        !BypassRowLevelSecurity
        && Roles.Any(role => role.Equals(AppRoles.Manager, StringComparison.OrdinalIgnoreCase));

    public bool IsFinanceBranchScoped =>
        !BypassRowLevelSecurity
        && Roles.Any(role => role.Equals(AppRoles.Finance, StringComparison.OrdinalIgnoreCase))
        && !string.IsNullOrWhiteSpace(StaffBranch);

    public bool IsFinance =>
        !BypassRowLevelSecurity
        && Roles.Any(role => role.Equals(AppRoles.Finance, StringComparison.OrdinalIgnoreCase));

    public bool CanAccessVehicle(Vehicle vehicle)
    {
        if (BypassRowLevelSecurity || HasFullFleetAccess)
        {
            return true;
        }

        if (!IsDriverRestricted || LinkedDriverId is null)
        {
            return !IsDriverRestricted;
        }

        if (vehicle.AssignedDriverId == LinkedDriverId)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(LinkedDriverLicenceNumber)
            && string.Equals(vehicle.DriverLicenseNumber, LinkedDriverLicenceNumber, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(LinkedCarTrackDriverId)
            && string.Equals(vehicle.DriverId, LinkedCarTrackDriverId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(LinkedDriverCode)
            && string.Equals(vehicle.DriverId, LinkedDriverCode, StringComparison.OrdinalIgnoreCase);
    }

    public bool CanAccessDriver(Driver driver)
    {
        if (BypassRowLevelSecurity || HasFullFleetAccess)
        {
            return true;
        }

        if (IsDriverRestricted && LinkedDriverId is not null)
        {
            return driver.Id == LinkedDriverId;
        }

        if (IsFinanceBranchScoped)
        {
            return string.Equals(driver.Branch, StaffBranch, StringComparison.OrdinalIgnoreCase);
        }

        if (IsManagerApprover && ReportLinkedDriverIds.Contains(driver.Id))
        {
            return true;
        }

        return !IsDriverRestricted && !IsFinanceBranchScoped;
    }

    public bool CanViewLeaveApproval(string requesterUserId, string? requesterBranch)
    {
        if (BypassRowLevelSecurity)
        {
            return true;
        }

        if (IsManagerApprover)
        {
            return ReportUserIds.Contains(requesterUserId);
        }

        return false;
    }

    public bool CanViewExpenseApproval(string requesterUserId, string? requesterBranch, string assignedManagerUserId, string status)
    {
        if (BypassRowLevelSecurity)
        {
            return true;
        }

        if (string.Equals(status, ExpenseClaimStatuses.PendingManager, StringComparison.OrdinalIgnoreCase))
        {
            if (IsFinance && !IsManagerApprover && !string.Equals(UserId, assignedManagerUserId, StringComparison.Ordinal))
            {
                return false;
            }

            if (IsManagerApprover)
            {
                return ReportUserIds.Contains(requesterUserId);
            }

            return string.Equals(UserId, assignedManagerUserId, StringComparison.Ordinal);
        }

        if (string.Equals(status, ExpenseClaimStatuses.PendingFinance, StringComparison.OrdinalIgnoreCase))
        {
            if (IsFinanceBranchScoped)
            {
                return string.Equals(requesterBranch, StaffBranch, StringComparison.OrdinalIgnoreCase);
            }

            return IsFinance;
        }

        if (string.Equals(status, ExpenseClaimStatuses.PendingPayment, StringComparison.OrdinalIgnoreCase))
        {
            if (IsFinanceBranchScoped)
            {
                return string.Equals(requesterBranch, StaffBranch, StringComparison.OrdinalIgnoreCase);
            }

            return IsFinance;
        }

        return CanViewExpenseApproval(requesterUserId, requesterBranch, assignedManagerUserId);
    }

    public bool CanViewExpenseApproval(string requesterUserId, string? requesterBranch, string assignedManagerUserId)
    {
        if (BypassRowLevelSecurity)
        {
            return true;
        }

        if (IsFinanceBranchScoped)
        {
            return string.Equals(requesterBranch, StaffBranch, StringComparison.OrdinalIgnoreCase);
        }

        if (IsManagerApprover)
        {
            return ReportUserIds.Contains(requesterUserId);
        }

        return string.Equals(UserId, assignedManagerUserId, StringComparison.Ordinal);
    }

    public bool CanApproveLeave(string requesterUserId, string assignedManagerUserId)
    {
        if (BypassRowLevelSecurity)
        {
            return true;
        }

        if (string.Equals(UserId, assignedManagerUserId, StringComparison.Ordinal))
        {
            return true;
        }

        return ReportUserIds.Contains(requesterUserId);
    }

    public bool CanApproveExpenseManagerStage(string requesterUserId, string assignedManagerUserId)
    {
        if (BypassRowLevelSecurity)
        {
            return true;
        }

        if (string.Equals(UserId, assignedManagerUserId, StringComparison.Ordinal))
        {
            return true;
        }

        return IsManagerApprover && ReportUserIds.Contains(requesterUserId);
    }

    public bool CanApproveExpenseFinanceStage(string? requesterBranch)
    {
        if (BypassRowLevelSecurity)
        {
            return true;
        }

        if (IsFinanceBranchScoped)
        {
            return string.Equals(requesterBranch, StaffBranch, StringComparison.OrdinalIgnoreCase);
        }

        return IsFinance;
    }

    public bool CanMarkExpensePaid(string? requesterBranch) => CanApproveExpenseFinanceStage(requesterBranch);

    public bool CanApproveExpense(string requesterUserId, string? requesterBranch, string assignedManagerUserId)
    {
        if (BypassRowLevelSecurity)
        {
            return true;
        }

        if (string.Equals(UserId, assignedManagerUserId, StringComparison.Ordinal))
        {
            return true;
        }

        if (ReportUserIds.Contains(requesterUserId))
        {
            return true;
        }

        return IsFinanceBranchScoped
            && string.Equals(requesterBranch, StaffBranch, StringComparison.OrdinalIgnoreCase);
    }
}
