namespace CarTrack.Modules.Expense;

public interface IExpenseReportService
{
    Task<ExpenseReportSummaryDto> GetSummaryAsync(
        string viewerUserId,
        int? year,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpenseCategoryBalanceDto>> GetMyBalancesAsync(
        string userId,
        int? year,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpenseHistoryRowDto>> GetMyHistoryAsync(
        string userId,
        string? status,
        int? year,
        CancellationToken cancellationToken = default);
}
