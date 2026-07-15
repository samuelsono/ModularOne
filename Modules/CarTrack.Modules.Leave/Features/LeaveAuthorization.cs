using CarTrack.Identity.Contracts;

namespace CarTrack.Modules.Leave;

/// <summary>Leave-domain authorization predicates (moved out of UserDataScope).</summary>
public static class LeaveAuthorization
{
    public static bool CanViewLeaveApproval(
        this UserDataScope scope,
        string requesterUserId,
        string? requesterBranch)
    {
        if (scope.BypassRowLevelSecurity)
        {
            return true;
        }

        if (scope.IsManagerApprover)
        {
            return scope.ReportUserIds.Contains(requesterUserId);
        }

        return false;
    }

    public static bool CanApproveLeave(
        this UserDataScope scope,
        string requesterUserId,
        string assignedManagerUserId)
    {
        if (scope.BypassRowLevelSecurity)
        {
            return true;
        }

        if (string.Equals(scope.UserId, assignedManagerUserId, StringComparison.Ordinal))
        {
            return true;
        }

        return scope.ReportUserIds.Contains(requesterUserId);
    }
}
