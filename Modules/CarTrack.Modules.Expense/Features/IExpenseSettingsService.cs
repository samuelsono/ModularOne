namespace CarTrack.Modules.Expense;

public interface IExpenseSettingsService
{
    Task<ExpenseSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<ExpenseSettingsDto> UpdateAsync(
        UpdateExpenseSettingsRequest request,
        CancellationToken cancellationToken = default);
}