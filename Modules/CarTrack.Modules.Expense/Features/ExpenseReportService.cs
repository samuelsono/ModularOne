using CarTrack.Server.Data;
using Microsoft.AspNetCore.Identity;

namespace CarTrack.Modules.Expense;

public class ExpenseReportService(
    ExpenseDbContext dbContext,
    ICurrentUserScope currentUserScope,
    UserManager<ApplicationUser> userManager) : IExpenseReportService
{
    public async Task<ExpenseReportSummaryDto> GetSummaryAsync(
        string viewerUserId,
        int? year,
        CancellationToken cancellationToken = default)
    {
        var targetYear = year ?? DateOnly.FromDateTime(DateTime.UtcNow).Year;
        var claims = await LoadVisibleClaimsAsync(viewerUserId, null, targetYear, cancellationToken);

        var pending = claims.Where(claim => ExpenseClaimStatuses.InApprovalPipeline.Contains(claim.Status)).ToList();
        var pendingPayment = claims
            .Where(claim => claim.Status == ExpenseClaimStatuses.PendingPayment)
            .ToList();
        var paidYtd = claims
            .Where(claim => claim.Status == ExpenseClaimStatuses.Paid)
            .Sum(claim => claim.Amount);
        var approvedYtd = pendingPayment.Sum(claim => claim.Amount) + paidYtd;

        return new ExpenseReportSummaryDto(
            pending.Count,
            pending.Sum(claim => claim.Amount),
            approvedYtd,
            pendingPayment.Sum(claim => claim.Amount),
            paidYtd,
            claims.Where(claim => claim.Status != ExpenseClaimStatuses.Draft).Sum(claim => claim.Amount),
            GroupByCategory(claims),
            GroupByStatus(claims));
    }

    public async Task<IReadOnlyList<ExpenseCategoryBalanceDto>> GetMyBalancesAsync(
        string userId,
        int? year,
        CancellationToken cancellationToken = default)
    {
        var targetYear = year ?? DateOnly.FromDateTime(DateTime.UtcNow).Year;
        var categories = await dbContext.ExpenseCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder)
            .ToListAsync(cancellationToken);

        var claims = await LoadOwnClaimsAsync(userId, null, targetYear, cancellationToken);

        return categories.Select(category =>
        {
            var categoryClaims = claims.Where(claim => claim.CategoryId == category.Id).ToList();
            return new ExpenseCategoryBalanceDto(
                category.Id,
                category.Name,
                category.Code,
                categoryClaims.Where(claim => ExpenseClaimStatuses.InApprovalPipeline.Contains(claim.Status)).Sum(claim => claim.Amount),
                categoryClaims.Where(claim => claim.Status == ExpenseClaimStatuses.PendingPayment).Sum(claim => claim.Amount),
                categoryClaims.Where(claim => claim.Status == ExpenseClaimStatuses.Paid).Sum(claim => claim.Amount),
                categoryClaims.Where(claim => claim.Status == ExpenseClaimStatuses.Rejected).Sum(claim => claim.Amount),
                categoryClaims.Where(claim => claim.Status == ExpenseClaimStatuses.Cancelled).Sum(claim => claim.Amount),
                categoryClaims.Where(claim => claim.Status == ExpenseClaimStatuses.Draft).Sum(claim => claim.Amount),
                categoryClaims.Where(claim => claim.Status != ExpenseClaimStatuses.Draft).Sum(claim => claim.Amount));
        }).ToList();
    }

    public async Task<IReadOnlyList<ExpenseHistoryRowDto>> GetMyHistoryAsync(
        string userId,
        string? status,
        int? year,
        CancellationToken cancellationToken = default)
    {
        var targetYear = year ?? DateOnly.FromDateTime(DateTime.UtcNow).Year;
        var claims = await LoadVisibleClaimsAsync(userId, status, targetYear, cancellationToken);
        return claims
            .OrderByDescending(claim => claim.ExpenseDate)
            .ThenByDescending(claim => claim.CreatedAt)
            .ToList();
    }

    private async Task<List<ExpenseHistoryRowDto>> LoadOwnClaimsAsync(
        string userId,
        string? status,
        int year,
        CancellationToken cancellationToken)
    {
        var query = BuildClaimQuery(status, year)
            .Where(claim => claim.RequesterUserId == userId);

        var entities = await query.ToListAsync(cancellationToken);
        var names = await ResolveDisplayNamesAsync(
            entities.SelectMany(e => new[] { e.RequesterUserId, e.ManagerUserId, e.CreatedByUserId, e.UpdatedByUserId }),
            cancellationToken);
        return entities.Select(e => MapHistoryRow(e, names)).ToList();
    }

    private async Task<List<ExpenseHistoryRowDto>> LoadVisibleClaimsAsync(
        string viewerUserId,
        string? status,
        int year,
        CancellationToken cancellationToken)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var query = BuildClaimQuery(status, year);
        var entities = await query.ToListAsync(cancellationToken);
        var visible = entities
            .Where(claim => ExpenseVisibility.CanViewClaim(
                scope,
                viewerUserId,
                claim.RequesterUserId,
                claim.ManagerUserId))
            .ToList();
        var names = await ResolveDisplayNamesAsync(
            visible.SelectMany(e => new[] { e.RequesterUserId, e.ManagerUserId, e.CreatedByUserId, e.UpdatedByUserId }),
            cancellationToken);
        return visible.Select(e => MapHistoryRow(e, names)).ToList();
    }

    private IQueryable<ExpenseClaim> BuildClaimQuery(string? status, int year)
    {
        var query = dbContext.ExpenseClaims
            .AsNoTracking()
            .Include(claim => claim.Category)
            .Where(claim => claim.ExpenseDate.Year == year);

        if (!string.IsNullOrWhiteSpace(status)
            && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(claim => claim.Status == status);
        }

        return query;
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

    private static ExpenseHistoryRowDto MapHistoryRow(
        ExpenseClaim entity,
        IReadOnlyDictionary<string, string> names) =>
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

    private static IReadOnlyList<ExpenseReportAmountDto> GroupByCategory(IReadOnlyList<ExpenseHistoryRowDto> claims) =>
        claims
            .GroupBy(claim => claim.CategoryName)
            .OrderBy(group => group.Key)
            .Select(group => new ExpenseReportAmountDto(
                group.Key,
                group.Count(),
                group.Sum(claim => claim.Amount)))
            .ToList();

    private static IReadOnlyList<ExpenseReportCountDto> GroupByStatus(IReadOnlyList<ExpenseHistoryRowDto> claims) =>
        claims
            .GroupBy(claim => claim.Status)
            .OrderBy(group => group.Key)
            .Select(group => new ExpenseReportCountDto(group.Key, group.Count()))
            .ToList();
}
