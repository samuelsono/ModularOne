using CarTrack.Server.Data;
using Microsoft.AspNetCore.Identity;

namespace CarTrack.Modules.Expense;

public class ExpenseApprovalService(
    ExpenseDbContext dbContext,
    ICurrentUserScope currentUserScope,
    IOrgDirectory orgDirectory,
    UserManager<ApplicationUser> userManager) : IExpenseApprovalService
{
    public async Task<ExpenseClaimDto> CreateClaimAsync(
        string requesterUserId,
        CreateExpenseClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateClaimFields(request);
        await EnsureActiveCategoryAsync(request.CategoryId, cancellationToken);

        var requesterBranch = await orgDirectory.ResolveRequesterBranchAsync(
            requesterUserId,
            cancellationToken);

        var managerUserId = await orgDirectory.ResolveManagerUserIdAsync(
            requesterUserId,
            cancellationToken)
            ?? requesterUserId;

        var entity = new ExpenseClaim
        {
            Id = Guid.NewGuid(),
            RequesterUserId = requesterUserId,
            ManagerUserId = managerUserId,
            CategoryId = request.CategoryId,
            ExpenseDate = ParseExpenseDate(request.ExpenseDate),
            Description = request.Description.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Amount = request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "ZAR" : request.Currency.Trim(),
            Status = ExpenseClaimStatuses.Draft,
            RequesterBranch = requesterBranch,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.ExpenseClaims.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await MapAsync(entity.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to load created expense claim.");
    }

    public async Task<ExpenseClaimDto?> UpdateClaimAsync(
        Guid claimId,
        string requesterUserId,
        UpdateExpenseClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateClaimFields(request);
        await EnsureActiveCategoryAsync(request.CategoryId, cancellationToken);

        var entity = await dbContext.ExpenseClaims
            .SingleOrDefaultAsync(item => item.Id == claimId, cancellationToken);

        if (entity is null || entity.RequesterUserId != requesterUserId)
        {
            return null;
        }

        if (!ExpenseClaimStatuses.IsEditable(entity.Status))
        {
            throw new InvalidOperationException("Only draft expense claims can be edited.");
        }

        entity.CategoryId = request.CategoryId;
        entity.ExpenseDate = ParseExpenseDate(request.ExpenseDate);
        entity.Description = request.Description.Trim();
        entity.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        entity.Amount = request.Amount;
        entity.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "ZAR" : request.Currency.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(entity.Id, cancellationToken);
    }

    public async Task<ExpenseClaimDto?> SubmitClaimAsync(
        Guid claimId,
        string requesterUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ExpenseClaims
            .SingleOrDefaultAsync(item => item.Id == claimId, cancellationToken);

        if (entity is null || entity.RequesterUserId != requesterUserId)
        {
            return null;
        }

        if (!ExpenseClaimStatuses.IsEditable(entity.Status))
        {
            throw new InvalidOperationException("Only draft expense claims can be submitted.");
        }

        var managerUserId = await orgDirectory.ResolveManagerUserIdAsync(
            requesterUserId,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(managerUserId))
        {
            throw new InvalidOperationException("No manager is assigned on your staff profile.");
        }

        entity.ManagerUserId = managerUserId;
        entity.Status = ExpenseClaimStatuses.PendingManager;
        entity.SubmittedAt = DateTimeOffset.UtcNow;
        entity.DecidedAt = null;
        entity.DecidedByUserId = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(entity.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ExpenseClaimDto>> GetMyClaimsAsync(
        string requesterUserId,
        string? status,
        int? year,
        CancellationToken cancellationToken = default)
    {
        var targetYear = year ?? DateOnly.FromDateTime(DateTime.UtcNow).Year;
        var query = dbContext.ExpenseClaims
            .AsNoTracking()
            .Include(item => item.Category)
            .Where(item => item.RequesterUserId == requesterUserId && item.ExpenseDate.Year == targetYear);

        if (!string.IsNullOrWhiteSpace(status)
            && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(item => item.Status == status);
        }

        var claims = await query
            .OrderByDescending(item => item.ExpenseDate)
            .ThenByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return await MapManyAsync(claims, cancellationToken);
    }

    public async Task<ExpenseClaimDto?> GetClaimAsync(
        Guid claimId,
        string viewerUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var entity = await dbContext.ExpenseClaims
            .AsNoTracking()
            .Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.Id == claimId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        if (entity.RequesterUserId != viewerUserId
            && !scope.CanViewExpenseApproval(
                entity.RequesterUserId,
                entity.RequesterBranch,
                entity.ManagerUserId,
                entity.Status))
        {
            return null;
        }

        return await MapAsync(entity, cancellationToken);
    }

    public async Task<ExpenseClaimDto?> CancelClaimAsync(
        Guid claimId,
        string requesterUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ExpenseClaims
            .SingleOrDefaultAsync(item => item.Id == claimId, cancellationToken);

        if (entity is null || entity.RequesterUserId != requesterUserId)
        {
            return null;
        }

        if (!ExpenseClaimStatuses.IsCancellable(entity.Status))
        {
            throw new InvalidOperationException("This expense claim cannot be cancelled.");
        }

        entity.Status = ExpenseClaimStatuses.Cancelled;
        entity.DecidedAt = DateTimeOffset.UtcNow;
        entity.DecidedByUserId = requesterUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(entity.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ExpenseClaimDto>> GetPendingApprovalsAsync(
        string actingUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var claims = await dbContext.ExpenseClaims
            .AsNoTracking()
            .Include(item => item.Category)
            .Where(item =>
                item.Status == ExpenseClaimStatuses.PendingManager
                || item.Status == ExpenseClaimStatuses.PendingFinance)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        var visible = claims.Where(item => CanViewApprovalItem(scope, item)).ToList();
        return await MapManyAsync(visible, cancellationToken);
    }

    public async Task<IReadOnlyList<ExpenseClaimDto>> GetPendingPaymentsAsync(
        string actingUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var claims = await dbContext.ExpenseClaims
            .AsNoTracking()
            .Include(item => item.Category)
            .Where(item => item.Status == ExpenseClaimStatuses.PendingPayment)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        var visible = claims.Where(item => scope.CanMarkExpensePaid(item.RequesterBranch)).ToList();
        return await MapManyAsync(visible, cancellationToken);
    }

    public async Task<ExpenseClaimDto?> DecideAsync(
        Guid claimId,
        string actingUserId,
        ApprovalDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var entity = await dbContext.ExpenseClaims
            .SingleOrDefaultAsync(item => item.Id == claimId, cancellationToken);

        if (entity is null || !CanDecide(scope, entity))
        {
            return null;
        }

        if (request.Approve)
        {
            if (string.Equals(entity.Status, ExpenseClaimStatuses.PendingManager, StringComparison.OrdinalIgnoreCase))
            {
                entity.Status = ExpenseClaimStatuses.PendingFinance;
            }
            else if (string.Equals(entity.Status, ExpenseClaimStatuses.PendingFinance, StringComparison.OrdinalIgnoreCase))
            {
                entity.Status = ExpenseClaimStatuses.PendingPayment;
            }
            else
            {
                throw new InvalidOperationException("Expense claim is not awaiting approval.");
            }
        }
        else
        {
            entity.Status = ExpenseClaimStatuses.Rejected;
        }

        entity.DecidedAt = DateTimeOffset.UtcNow;
        entity.DecidedByUserId = actingUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(entity.Id, cancellationToken);
    }

    public async Task<ExpenseClaimDto?> MarkPaidAsync(
        Guid claimId,
        string actingUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var entity = await dbContext.ExpenseClaims
            .SingleOrDefaultAsync(item => item.Id == claimId, cancellationToken);

        if (entity is null || !scope.CanMarkExpensePaid(entity.RequesterBranch))
        {
            return null;
        }

        if (!string.Equals(entity.Status, ExpenseClaimStatuses.PendingPayment, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only claims awaiting payment can be marked as paid.");
        }

        entity.Status = ExpenseClaimStatuses.Paid;
        entity.PaidAt = DateTimeOffset.UtcNow;
        entity.PaidByUserId = actingUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(entity.Id, cancellationToken);
    }

    private static bool CanViewApprovalItem(UserDataScope scope, ExpenseClaim item) =>
        scope.CanViewExpenseApproval(
            item.RequesterUserId,
            item.RequesterBranch,
            item.ManagerUserId,
            item.Status);

    private static bool CanDecide(UserDataScope scope, ExpenseClaim entity)
    {
        if (string.Equals(entity.Status, ExpenseClaimStatuses.PendingManager, StringComparison.OrdinalIgnoreCase))
        {
            return scope.CanApproveExpenseManagerStage(entity.RequesterUserId, entity.ManagerUserId);
        }

        if (string.Equals(entity.Status, ExpenseClaimStatuses.PendingFinance, StringComparison.OrdinalIgnoreCase))
        {
            return scope.CanApproveExpenseFinanceStage(entity.RequesterBranch);
        }

        return false;
    }

    private static void ValidateClaimFields(CreateExpenseClaimRequest request) =>
        ValidateClaimFields(
            request.CategoryId,
            request.ExpenseDate,
            request.Description,
            request.Amount);

    private static void ValidateClaimFields(UpdateExpenseClaimRequest request) =>
        ValidateClaimFields(
            request.CategoryId,
            request.ExpenseDate,
            request.Description,
            request.Amount);

    private static void ValidateClaimFields(
        Guid categoryId,
        string expenseDate,
        string description,
        decimal amount)
    {
        if (categoryId == Guid.Empty)
        {
            throw new InvalidOperationException("Expense category is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new InvalidOperationException("Description is required.");
        }

        if (amount <= 0)
        {
            throw new InvalidOperationException("Amount must be greater than zero.");
        }

        _ = ParseExpenseDate(expenseDate);
    }

    private static DateOnly ParseExpenseDate(string value)
    {
        if (!DateOnly.TryParse(value, out var expenseDate))
        {
            throw new InvalidOperationException("Expense date is invalid.");
        }

        return expenseDate;
    }

    private async Task<ExpenseClaimDto?> MapAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ExpenseClaims
            .AsNoTracking()
            .Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return entity is null ? null : await MapAsync(entity, cancellationToken);
    }

    private async Task<IReadOnlyList<ExpenseClaimDto>> MapManyAsync(
        IEnumerable<ExpenseClaim> entities,
        CancellationToken cancellationToken)
    {
        var list = entities.ToList();
        var names = await ResolveDisplayNamesAsync(
            list.SelectMany(e => new[] { e.RequesterUserId, e.ManagerUserId, e.CreatedByUserId, e.UpdatedByUserId }),
            cancellationToken);
        return list.Select(e => Map(e, names)).ToList();
    }

    private async Task<ExpenseClaimDto> MapAsync(ExpenseClaim entity, CancellationToken cancellationToken)
    {
        var names = await ResolveDisplayNamesAsync(
            [entity.RequesterUserId, entity.ManagerUserId, entity.CreatedByUserId, entity.UpdatedByUserId],
            cancellationToken);
        return Map(entity, names);
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveDisplayNamesAsync(
        IEnumerable<string?> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return await userManager.Users
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .ToDictionaryAsync(
                user => user.Id,
                user => user.DisplayName ?? user.UserName ?? user.Email ?? user.Id,
                StringComparer.Ordinal,
                cancellationToken);
    }

    private static string? NameOrNull(IReadOnlyDictionary<string, string> names, string? userId) =>
        string.IsNullOrWhiteSpace(userId) ? null : names.GetValueOrDefault(userId);

    private async Task EnsureActiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var categoryExists = await dbContext.ExpenseCategories
            .AnyAsync(
                category => category.Id == categoryId && category.IsActive,
                cancellationToken);

        if (!categoryExists)
        {
            throw new InvalidOperationException("Selected expense category is not available.");
        }
    }

    private static ExpenseClaimDto Map(ExpenseClaim entity, IReadOnlyDictionary<string, string> names) =>
        new(
            entity.Id,
            entity.RequesterUserId,
            names.GetValueOrDefault(entity.RequesterUserId) ?? entity.RequesterUserId,
            entity.ManagerUserId,
            NameOrNull(names, entity.ManagerUserId),
            entity.CategoryId,
            entity.Category.Name,
            entity.Category.Code,
            entity.ExpenseDate.ToString("yyyy-MM-dd"),
            entity.Description,
            entity.Notes,
            entity.Amount,
            entity.Currency,
            entity.Status,
            AuditableMapping.FormatTimestamp(entity.CreatedAt),
            entity.CreatedByUserId,
            NameOrNull(names, entity.CreatedByUserId),
            AuditableMapping.FormatTimestamp(entity.UpdatedAt == default ? entity.CreatedAt : entity.UpdatedAt),
            entity.UpdatedByUserId,
            NameOrNull(names, entity.UpdatedByUserId),
            AuditableMapping.FormatTimestamp(entity.SubmittedAt),
            AuditableMapping.FormatTimestamp(entity.DecidedAt),
            AuditableMapping.FormatTimestamp(entity.PaidAt));
}
