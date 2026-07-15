using CarTrack.Server.Data;

namespace CarTrack.Modules.Expense;

public class ExpenseCategoryService(ExpenseDbContext dbContext) : IExpenseCategoryService
{
    public async Task<IReadOnlyList<ExpenseCategoryDto>> GetActiveCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await dbContext.ExpenseCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(MapCategory).ToList();
    }

    public async Task<IReadOnlyList<ExpenseCategoryDto>> GetAdminCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await dbContext.ExpenseCategories
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(MapCategory).ToList();
    }

    public async Task<ExpenseCategoryDto> CreateCategoryAsync(
        SaveExpenseCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCategoryRequest(request);

        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.ExpenseCategories.AnyAsync(category => category.Code == code, cancellationToken))
        {
            throw new InvalidOperationException($"Expense category code '{code}' already exists.");
        }

        var entity = new ExpenseCategory
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = code,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
        };

        dbContext.ExpenseCategories.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapCategory(entity);
    }

    public async Task<ExpenseCategoryDto?> UpdateCategoryAsync(
        Guid id,
        SaveExpenseCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCategoryRequest(request);

        var entity = await dbContext.ExpenseCategories
            .FirstOrDefaultAsync(category => category.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.ExpenseCategories.AnyAsync(
                category => category.Id != id && category.Code == code,
                cancellationToken))
        {
            throw new InvalidOperationException($"Expense category code '{code}' already exists.");
        }

        entity.Name = request.Name.Trim();
        entity.Code = code;
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapCategory(entity);
    }

    private static void ValidateCategoryRequest(SaveExpenseCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Category name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new InvalidOperationException("Category code is required.");
        }
    }

    private static ExpenseCategoryDto MapCategory(ExpenseCategory category) =>
        new(
            category.Id,
            category.Name,
            category.Code,
            category.Description,
            category.IsActive,
            category.SortOrder,
            AuditableMapping.FormatTimestamp(category.CreatedAt),
            category.CreatedByUserId,
            null,
            AuditableMapping.FormatTimestamp(category.UpdatedAt == default ? category.CreatedAt : category.UpdatedAt),
            category.UpdatedByUserId,
            null);
}
