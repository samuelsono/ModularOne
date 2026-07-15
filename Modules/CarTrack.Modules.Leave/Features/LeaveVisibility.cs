namespace CarTrack.Modules.Leave;

internal static class LeaveVisibility
{
    public static bool CanViewRequest(
        UserDataScope scope,
        string viewerUserId,
        StaffOrgInfo? viewerProfile,
        string requesterUserId,
        StaffOrgInfo? requesterProfile)
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
        StaffOrgInfo? viewerProfile,
        string targetUserId,
        StaffOrgInfo? targetProfile) =>
        CanViewRequest(scope, viewerUserId, viewerProfile, targetUserId, targetProfile);
}
