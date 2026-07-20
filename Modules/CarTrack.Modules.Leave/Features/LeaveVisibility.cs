using CarTrack.Identity.Contracts;
using CarTrack.Server.Users;

namespace CarTrack.Modules.Leave;

internal static class LeaveVisibility
{
    /// <summary>
    /// Admin / HR / Manager retain team and department visibility on dashboards.
    /// Staff and other day-to-day roles are self-scoped on report home pages.
    /// </summary>
    public static bool IsElevatedLeaveViewer(UserDataScope scope) =>
        scope.BypassRowLevelSecurity
        || scope.IsManagerApprover
        || scope.Roles.Any(role => role.Equals(AppRoles.Hr, StringComparison.OrdinalIgnoreCase));

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

    /// <summary>
    /// Dashboard / report visibility: elevated roles keep <see cref="CanViewRequest"/>;
    /// everyone else only sees their own rows (no department peers).
    /// </summary>
    public static bool CanViewRequestInReports(
        UserDataScope scope,
        string viewerUserId,
        StaffOrgInfo? viewerProfile,
        string requesterUserId,
        StaffOrgInfo? requesterProfile)
    {
        if (!CanViewRequest(scope, viewerUserId, viewerProfile, requesterUserId, requesterProfile))
        {
            return false;
        }

        if (IsElevatedLeaveViewer(scope))
        {
            return true;
        }

        return string.Equals(viewerUserId, requesterUserId, StringComparison.Ordinal);
    }

    public static bool CanViewUser(
        UserDataScope scope,
        string viewerUserId,
        StaffOrgInfo? viewerProfile,
        string targetUserId,
        StaffOrgInfo? targetProfile) =>
        CanViewRequest(scope, viewerUserId, viewerProfile, targetUserId, targetProfile);

    public static bool CanViewUserInReports(
        UserDataScope scope,
        string viewerUserId,
        StaffOrgInfo? viewerProfile,
        string targetUserId,
        StaffOrgInfo? targetProfile) =>
        CanViewRequestInReports(scope, viewerUserId, viewerProfile, targetUserId, targetProfile);
}
