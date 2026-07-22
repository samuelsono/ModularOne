using CarTrack.Identity.Contracts;
using CarTrack.Server.Users;

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

    /// <summary>
    /// Requester can manage their own supporting document; Admin / SystemAdmin / HR can do so on behalf.
    /// </summary>
    public static bool CanUploadLeaveDocument(
        this UserDataScope scope,
        string actingUserId,
        string requesterUserId)
    {
        if (string.Equals(actingUserId, requesterUserId, StringComparison.Ordinal))
        {
            return true;
        }

        return scope.BypassRowLevelSecurity
            || scope.Roles.Any(role => role.Equals(AppRoles.Hr, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Self, Admin/SystemAdmin (bypass), HR, or managers for their reports may manage schedule/attendance.
    /// </summary>
    public static bool CanManageUserAttendance(
        this UserDataScope scope,
        string actingUserId,
        string targetUserId)
    {
        if (string.Equals(actingUserId, targetUserId, StringComparison.Ordinal))
        {
            return true;
        }

        if (scope.BypassRowLevelSecurity
            || scope.Roles.Any(role => role.Equals(AppRoles.Hr, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return scope.ReportUserIds.Contains(targetUserId);
    }
}
