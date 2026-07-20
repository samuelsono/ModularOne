using CarTrack.Identity.Contracts;
using CarTrack.Server.Users;

namespace CarTrack.Modules.Expense;

internal static class ExpenseVisibility
{
    public static bool CanViewClaim(
        UserDataScope scope,
        string viewerUserId,
        string requesterUserId,
        string? managerUserId)
    {
        if (scope.BypassRowLevelSecurity)
        {
            return true;
        }

        // HR sees organisation-wide claim history on the expense home dashboard.
        if (scope.Roles.Any(role => role.Equals(AppRoles.Hr, StringComparison.OrdinalIgnoreCase)))
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

        if (!string.IsNullOrWhiteSpace(managerUserId)
            && string.Equals(viewerUserId, managerUserId, StringComparison.Ordinal))
        {
            return true;
        }

        if (scope.IsFinanceBranchScoped || scope.IsFinance)
        {
            return true;
        }

        return false;
    }
}
