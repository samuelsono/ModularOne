using CarTrack.Server.Data;
using CarTrack.Server.Users;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Expense;

public record ExpenseReportCountDto(string Label, int Count);

public record ExpenseReportAmountDto(string Label, int Count, decimal Amount);

public record ExpenseReportSummaryDto(
    int PendingCount,
    decimal PendingAmount,
    decimal ApprovedYtdAmount,
    decimal PendingPaymentAmount,
    decimal PaidYtdAmount,
    decimal TotalSubmittedYtd,
    IReadOnlyList<ExpenseReportAmountDto> ByCategory,
    IReadOnlyList<ExpenseReportCountDto> ByStatus);

public record ExpenseCategoryBalanceDto(
    Guid CategoryId,
    string CategoryName,
    string CategoryCode,
    decimal PendingAmount,
    decimal PendingPaymentAmount,
    decimal PaidAmount,
    decimal RejectedAmount,
    decimal CancelledAmount,
    decimal DraftAmount,
    decimal TotalSubmitted);

public record ExpenseHistoryRowDto(
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

public class ExpenseReportService(
    ApplicationDbContext dbContext,
    ICurrentUserScope currentUserScope) : IExpenseReportService
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

        // Balances remain personal (own claims only).
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
        return entities.Select(MapHistoryRow).ToList();
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

        return entities
            .Where(claim => ExpenseVisibility.CanViewClaim(
                scope,
                viewerUserId,
                claim.RequesterUserId,
                claim.ManagerUserId))
            .Select(MapHistoryRow)
            .ToList();
    }

    private IQueryable<ExpenseClaim> BuildClaimQuery(string? status, int year)
    {
        var query = dbContext.ExpenseClaims
            .AsNoTracking()
            .Include(claim => claim.Requester)
            .Include(claim => claim.Manager)
            .Include(claim => claim.Category)
            .Include(claim => claim.CreatedByUser)
            .Include(claim => claim.UpdatedByUser)
            .Where(claim => claim.ExpenseDate.Year == year);

        if (!string.IsNullOrWhiteSpace(status)
            && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(claim => claim.Status == status);
        }

        return query;
    }

    private static ExpenseHistoryRowDto MapHistoryRow(ExpenseClaim entity) =>
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
