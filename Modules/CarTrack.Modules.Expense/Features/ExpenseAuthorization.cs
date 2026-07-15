using CarTrack.Identity.Contracts;

namespace CarTrack.Modules.Expense;

/// <summary>Expense-domain authorization predicates (moved out of UserDataScope).</summary>
public static class ExpenseAuthorization
{
    public static bool CanViewExpenseApproval(
        this UserDataScope scope,
        string requesterUserId,
        string? requesterBranch,
        string assignedManagerUserId,
        string status)
    {
        if (scope.BypassRowLevelSecurity)
        {
            return true;
        }

        if (string.Equals(status, ExpenseClaimStatuses.PendingManager, StringComparison.OrdinalIgnoreCase))
        {
            if (scope.IsFinance && !scope.IsManagerApprover && !string.Equals(scope.UserId, assignedManagerUserId, StringComparison.Ordinal))
            {
                return false;
            }

            if (scope.IsManagerApprover)
            {
                return scope.ReportUserIds.Contains(requesterUserId);
            }

            return string.Equals(scope.UserId, assignedManagerUserId, StringComparison.Ordinal);
        }

        if (string.Equals(status, ExpenseClaimStatuses.PendingFinance, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, ExpenseClaimStatuses.PendingPayment, StringComparison.OrdinalIgnoreCase))
        {
            if (scope.IsFinanceBranchScoped)
            {
                return string.Equals(requesterBranch, scope.StaffBranch, StringComparison.OrdinalIgnoreCase);
            }

            return scope.IsFinance;
        }

        return CanViewExpenseApproval(scope, requesterUserId, requesterBranch, assignedManagerUserId);
    }

    public static bool CanViewExpenseApproval(
        this UserDataScope scope,
        string requesterUserId,
        string? requesterBranch,
        string assignedManagerUserId)
    {
        if (scope.BypassRowLevelSecurity)
        {
            return true;
        }

        if (scope.IsFinanceBranchScoped)
        {
            return string.Equals(requesterBranch, scope.StaffBranch, StringComparison.OrdinalIgnoreCase);
        }

        if (scope.IsManagerApprover)
        {
            return scope.ReportUserIds.Contains(requesterUserId);
        }

        return string.Equals(scope.UserId, assignedManagerUserId, StringComparison.Ordinal);
    }

    public static bool CanApproveExpenseManagerStage(
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

        return scope.IsManagerApprover && scope.ReportUserIds.Contains(requesterUserId);
    }

    public static bool CanApproveExpenseFinanceStage(this UserDataScope scope, string? requesterBranch)
    {
        if (scope.BypassRowLevelSecurity)
        {
            return true;
        }

        if (scope.IsFinanceBranchScoped)
        {
            return string.Equals(requesterBranch, scope.StaffBranch, StringComparison.OrdinalIgnoreCase);
        }

        return scope.IsFinance;
    }

    public static bool CanMarkExpensePaid(this UserDataScope scope, string? requesterBranch) =>
        CanApproveExpenseFinanceStage(scope, requesterBranch);
}
