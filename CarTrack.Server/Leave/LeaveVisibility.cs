using CarTrack.Server.Data;
using CarTrack.Server.Users;

namespace CarTrack.Server.Leave;

internal static class LeaveVisibility
{
    public static bool CanViewRequest(
        UserDataScope scope,
        string viewerUserId,
        StaffProfile? viewerProfile,
        string requesterUserId,
        StaffProfile? requesterProfile)
    {
        if (scope.BypassRowLevelSecurity)
        {
            return true;
        }

        if (string.Equals(viewerUserId, requesterUserId, StringComparison.Ordinal))
        {
            return true;
        }

        if (scope.ReportUserIds.Contains(requesterUserId))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(viewerProfile?.Department)
            && !string.IsNullOrWhiteSpace(requesterProfile?.Department)
            && string.Equals(viewerProfile.Department, requesterProfile.Department, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public static bool CanViewUser(
        UserDataScope scope,
        string viewerUserId,
        StaffProfile? viewerProfile,
        string targetUserId,
        StaffProfile? targetProfile) =>
        CanViewRequest(scope, viewerUserId, viewerProfile, targetUserId, targetProfile);
}
