using CarTrack.Server.Data;
using CarTrack.Server.Users;
using Microsoft.AspNetCore.Identity;

namespace CarTrack.Modules.Expense;

public class ExpenseApprovalService(
    ExpenseDbContext dbContext,
    ICurrentUserScope currentUserScope,
    IOrgDirectory orgDirectory,
    UserManager<ApplicationUser> userManager,
    IExpenseSettingsService expenseSettingsService,
    IExpenseDocumentStorage expenseDocumentStorage) : IExpenseApprovalService
{
    public async Task<ExpenseClaimDto> CreateClaimAsync(
        string requesterUserId,
        CreateExpenseClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateClaimFields(request.CategoryId, request.ExpenseDate, request.Description, request.Amount);
        var category = await GetActiveCategoryAsync(request.CategoryId, cancellationToken);
        var travel = ValidateTravelFields(category, request.KilometersTravelled, request.TravelStartPoint, request.TravelDestination, request.TravelWaypoints);
        var amount = await ResolveAmountAsync(category, request.Amount, travel.KilometersTravelled, cancellationToken);

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
            Amount = amount,
            KilometersTravelled = travel.KilometersTravelled,
            TravelStartPoint = travel.TravelStartPoint,
            TravelDestination = travel.TravelDestination,
            TravelWaypointsJson = travel.TravelWaypointsJson,
            MileageRatePerKilometer = travel.MileageRatePerKilometer,
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
        ValidateClaimFields(request.CategoryId, request.ExpenseDate, request.Description, request.Amount);
        var category = await GetActiveCategoryAsync(request.CategoryId, cancellationToken);
        var travel = ValidateTravelFields(category, request.KilometersTravelled, request.TravelStartPoint, request.TravelDestination, request.TravelWaypoints);
        var amount = await ResolveAmountAsync(category, request.Amount, travel.KilometersTravelled, cancellationToken);

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
        entity.Amount = amount;
        entity.KilometersTravelled = travel.KilometersTravelled;
        entity.TravelStartPoint = travel.TravelStartPoint;
        entity.TravelDestination = travel.TravelDestination;
        entity.TravelWaypointsJson = travel.TravelWaypointsJson;
        entity.MileageRatePerKilometer = travel.MileageRatePerKilometer;
        entity.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "ZAR" : request.Currency.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(entity.Id, cancellationToken);
    }

    public async Task<ExpenseClaimDto?> UploadReceiptAsync(
        Guid claimId,
        string requesterUserId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ExpenseClaims
            .SingleOrDefaultAsync(item => item.Id == claimId, cancellationToken);

        if (entity is null || entity.RequesterUserId != requesterUserId)
        {
            return null;
        }

        if (file is null || file.Length == 0)
        {
            throw new InvalidOperationException("A receipt file is required.");
        }

        var metadata = await expenseDocumentStorage.SaveAsync(claimId, file, cancellationToken);
        await expenseDocumentStorage.DeleteIfExistsAsync(entity.ReceiptStoredPath, cancellationToken);

        entity.ReceiptStoredPath = metadata.StoredPath;
        entity.ReceiptFileName = metadata.FileName;
        entity.ReceiptContentType = metadata.ContentType;
        entity.ReceiptUploadedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(entity.Id, cancellationToken);
    }

    public async Task<(Stream Stream, string ContentType, string FileName)?> GetReceiptAsync(
        Guid claimId,
        string viewerUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var entity = await dbContext.ExpenseClaims
            .AsNoTracking()
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

        if (string.IsNullOrWhiteSpace(entity.ReceiptStoredPath))
        {
            return null;
        }

        return await expenseDocumentStorage.OpenReadAsync(entity.ReceiptStoredPath, cancellationToken);
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

        await EnsureSubmissionRequirementsAsync(entity, cancellationToken);

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
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var targetYear = year ?? DateOnly.FromDateTime(DateTime.UtcNow).Year;
        var query = dbContext.ExpenseClaims
            .AsNoTracking()
            .Include(item => item.Category)
            .Where(item => item.ExpenseDate.Year == targetYear);

        if (!scope.BypassRowLevelSecurity
            && !scope.Roles.Any(role => role.Equals(AppRoles.Hr, StringComparison.OrdinalIgnoreCase))
            && !scope.IsFinance)
        {
            var visibleUserIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { requesterUserId };
            foreach (var reportUserId in scope.ReportUserIds)
            {
                visibleUserIds.Add(reportUserId);
            }

            query = query.Where(item => visibleUserIds.Contains(item.RequesterUserId));
        }

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

        if (!request.Approve && string.IsNullOrWhiteSpace(request.Notes))
        {
            throw new InvalidOperationException("A reason is required to reject an expense claim.");
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
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            entity.Notes = request.Notes.Trim();
        }

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

    private async Task EnsureSubmissionRequirementsAsync(ExpenseClaim entity, CancellationToken cancellationToken)
    {
        var category = await GetActiveCategoryAsync(entity.CategoryId, cancellationToken);

        if (category.RequiresReceipt && string.IsNullOrWhiteSpace(entity.ReceiptStoredPath))
        {
            throw new InvalidOperationException("A receipt is required for this expense category.");
        }

        if (category.RequiresTravelDetails)
        {
            _ = ValidateTravelFields(
                category,
                entity.KilometersTravelled,
                entity.TravelStartPoint,
                entity.TravelDestination,
                ParseTravelWaypoints(entity.TravelWaypointsJson));
        }
    }

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

    private async Task<ExpenseCategory> GetActiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await dbContext.ExpenseCategories
            .FirstOrDefaultAsync(
                item => item.Id == categoryId && item.IsActive,
                cancellationToken);

        if (category is null)
        {
            throw new InvalidOperationException("Selected expense category is not available.");
        }

        return category;
    }

    private async Task<decimal> ResolveAmountAsync(
        ExpenseCategory category,
        decimal requestAmount,
        decimal? kilometersTravelled,
        CancellationToken cancellationToken)
    {
        if (!category.PaysByKilometer)
        {
            if (requestAmount <= 0)
            {
                throw new InvalidOperationException("Amount must be greater than zero.");
            }

            return requestAmount;
        }

        if (kilometersTravelled is null || kilometersTravelled <= 0)
        {
            throw new InvalidOperationException("Kilometers travelled are required for travel expenses.");
        }

        var settings = await expenseSettingsService.GetAsync(cancellationToken);
        if (settings.KilometerRate <= 0)
        {
            throw new InvalidOperationException("Kilometer rate has not been configured.");
        }

        return decimal.Round(kilometersTravelled.Value * settings.KilometerRate, 2);
    }

    private static (
        decimal? KilometersTravelled,
        string? TravelStartPoint,
        string? TravelDestination,
        string? TravelWaypointsJson,
        decimal? MileageRatePerKilometer) ValidateTravelFields(
        ExpenseCategory category,
        decimal? kilometersTravelled,
        string? travelStartPoint,
        string? travelDestination,
        string[]? travelWaypoints)
    {
        if (!category.RequiresTravelDetails)
        {
            return (null, null, null, null, null);
        }

        if (kilometersTravelled is null || kilometersTravelled <= 0)
        {
            throw new InvalidOperationException("Kilometers travelled are required for this expense category.");
        }

        if (string.IsNullOrWhiteSpace(travelStartPoint))
        {
            throw new InvalidOperationException("Starting point is required for this expense category.");
        }

        if (string.IsNullOrWhiteSpace(travelDestination))
        {
            throw new InvalidOperationException("Destination is required for this expense category.");
        }

        var normalizedWaypoints = travelWaypoints is { Length: > 0 }
            ? travelWaypoints.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToArray()
            : [];

        var waypointsJson = normalizedWaypoints.Length > 0
            ? System.Text.Json.JsonSerializer.Serialize(normalizedWaypoints)
            : null;

        return (
            decimal.Round(kilometersTravelled.Value, 2),
            travelStartPoint.Trim(),
            travelDestination.Trim(),
            waypointsJson,
            null);
    }

    private static string[]? ParseTravelWaypoints(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<string[]>(json);
        }
        catch
        {
            return null;
        }
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
            entity.Category.RequiresReceipt,
            entity.Category.RequiresTravelDetails,
            entity.Category.PaysByKilometer,
            entity.ExpenseDate.ToString("yyyy-MM-dd"),
            entity.Description,
            entity.Notes,
            entity.Amount,
            entity.KilometersTravelled,
            entity.TravelStartPoint,
            entity.TravelDestination,
            ParseTravelWaypoints(entity.TravelWaypointsJson),
            entity.MileageRatePerKilometer,
            !string.IsNullOrWhiteSpace(entity.ReceiptStoredPath),
            entity.ReceiptFileName,
            AuditableMapping.FormatTimestamp(entity.ReceiptUploadedAt),
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
