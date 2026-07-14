using CarTrack.Server.Data;
using CarTrack.Server.Leave;
using CarTrack.Server.Users;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Expense;

public record ExpenseClaimDto(
    Guid Id,
    string RequesterUserId,
    string RequesterDisplayName,
    string ManagerUserId,
    string? ManagerDisplayName,
    Guid CategoryId,
    string CategoryName,
    string CategoryCode,
    string ExpenseDate,
    string Description,
    string? Notes,
    decimal Amount,
    string Currency,
    string Status,
    string CreatedAt,
    string? CreatedByUserId,
    string? CreatedByDisplayName,
    string UpdatedAt,
    string? UpdatedByUserId,
    string? UpdatedByDisplayName,
    string? SubmittedAt,
    string? DecidedAt,
    string? PaidAt);

public record CreateExpenseClaimRequest(
    Guid CategoryId,
    string ExpenseDate,
    string Description,
    string? Notes,
    decimal Amount,
    string? Currency);

public record UpdateExpenseClaimRequest(
    Guid CategoryId,
    string ExpenseDate,
    string Description,
    string? Notes,
    decimal Amount,
    string? Currency);

public interface IExpenseApprovalService
{
    Task<ExpenseClaimDto> CreateClaimAsync(
        string requesterUserId,
        CreateExpenseClaimRequest request,
        CancellationToken cancellationToken = default);

    Task<ExpenseClaimDto?> UpdateClaimAsync(
        Guid claimId,
        string requesterUserId,
        UpdateExpenseClaimRequest request,
        CancellationToken cancellationToken = default);

    Task<ExpenseClaimDto?> SubmitClaimAsync(
        Guid claimId,
        string requesterUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpenseClaimDto>> GetMyClaimsAsync(
        string requesterUserId,
        string? status,
        int? year,
        CancellationToken cancellationToken = default);

    Task<ExpenseClaimDto?> GetClaimAsync(
        Guid claimId,
        string viewerUserId,
        CancellationToken cancellationToken = default);

    Task<ExpenseClaimDto?> CancelClaimAsync(
        Guid claimId,
        string requesterUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpenseClaimDto>> GetPendingApprovalsAsync(
        string actingUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpenseClaimDto>> GetPendingPaymentsAsync(
        string actingUserId,
        CancellationToken cancellationToken = default);

    Task<ExpenseClaimDto?> DecideAsync(
        Guid claimId,
        string actingUserId,
        ApprovalDecisionRequest request,
        CancellationToken cancellationToken = default);

    Task<ExpenseClaimDto?> MarkPaidAsync(
        Guid claimId,
        string actingUserId,
        CancellationToken cancellationToken = default);
}

public class ExpenseApprovalService(
    ApplicationDbContext dbContext,
    ICurrentUserScope currentUserScope) : IExpenseApprovalService
{
    public async Task<ExpenseClaimDto> CreateClaimAsync(
        string requesterUserId,
        CreateExpenseClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateClaimFields(request);
        await EnsureActiveCategoryAsync(request.CategoryId, cancellationToken);

        var requesterBranch = await ManagerHierarchy.ResolveRequesterBranchAsync(
            dbContext,
            requesterUserId,
            cancellationToken);

        var managerUserId = await ManagerHierarchy.ResolveManagerUserIdAsync(
            dbContext,
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

        var managerUserId = await ManagerHierarchy.ResolveManagerUserIdAsync(
            dbContext,
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
            .Include(item => item.Requester)
            .Include(item => item.Manager)
            .Include(item => item.Category)
            .Include(item => item.CreatedByUser)
            .Include(item => item.UpdatedByUser)
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

        return claims.Select(Map).ToList();
    }

    public async Task<ExpenseClaimDto?> GetClaimAsync(
        Guid claimId,
        string viewerUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var entity = await dbContext.ExpenseClaims
            .AsNoTracking()
            .Include(item => item.Requester)
            .Include(item => item.Manager)
            .Include(item => item.Category)
            .Include(item => item.CreatedByUser)
            .Include(item => item.UpdatedByUser)
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

        return Map(entity);
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
            .Include(item => item.Requester)
            .Include(item => item.Manager)
            .Include(item => item.Category)
            .Include(item => item.CreatedByUser)
            .Include(item => item.UpdatedByUser)
            .Where(item =>
                item.Status == ExpenseClaimStatuses.PendingManager
                || item.Status == ExpenseClaimStatuses.PendingFinance)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return claims
            .Where(item => CanViewApprovalItem(scope, item))
            .Select(Map)
            .ToList();
    }

    public async Task<IReadOnlyList<ExpenseClaimDto>> GetPendingPaymentsAsync(
        string actingUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var claims = await dbContext.ExpenseClaims
            .AsNoTracking()
            .Include(item => item.Requester)
            .Include(item => item.Manager)
            .Include(item => item.Category)
            .Include(item => item.CreatedByUser)
            .Include(item => item.UpdatedByUser)
            .Where(item => item.Status == ExpenseClaimStatuses.PendingPayment)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return claims
            .Where(item => scope.CanMarkExpensePaid(item.RequesterBranch))
            .Select(Map)
            .ToList();
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
            .Include(item => item.Requester)
            .Include(item => item.Manager)
            .Include(item => item.Category)
            .Include(item => item.CreatedByUser)
            .Include(item => item.UpdatedByUser)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return entity is null ? null : Map(entity);
    }

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

    private static ExpenseClaimDto Map(ExpenseClaim entity) =>
        new(
            entity.Id,
            entity.RequesterUserId,
            entity.Requester.DisplayName ?? entity.Requester.UserName ?? entity.Requester.Email ?? entity.RequesterUserId,
            entity.ManagerUserId,
            AuditableMapping.UserDisplayName(entity.Manager),
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
            AuditableMapping.UserDisplayName(entity.CreatedByUser),
            AuditableMapping.FormatTimestamp(entity.UpdatedAt == default ? entity.CreatedAt : entity.UpdatedAt),
            entity.UpdatedByUserId,
            AuditableMapping.UserDisplayName(entity.UpdatedByUser),
            AuditableMapping.FormatTimestamp(entity.SubmittedAt),
            AuditableMapping.FormatTimestamp(entity.DecidedAt),
            AuditableMapping.FormatTimestamp(entity.PaidAt));
}
