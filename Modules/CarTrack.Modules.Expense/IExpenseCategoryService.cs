namespace CarTrack.Modules.Expense;

public interface IExpenseCategoryService
{
    Task<IReadOnlyList<ExpenseCategoryDto>> GetActiveCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpenseCategoryDto>> GetAdminCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<ExpenseCategoryDto> CreateCategoryAsync(
        SaveExpenseCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<ExpenseCategoryDto?> UpdateCategoryAsync(
        Guid id,
        SaveExpenseCategoryRequest request,
        CancellationToken cancellationToken = default);
}
