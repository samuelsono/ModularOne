namespace CarTrack.Modules.Expense;

public record ExpenseClaimDto(
    Guid Id,
    string RequesterUserId,
    string RequesterDisplayName,
    string ManagerUserId,
    string? ManagerDisplayName,
    Guid CategoryId,
    string CategoryName,
    string CategoryCode,
    bool RequiresReceipt,
    bool RequiresTravelDetails,
    bool PaysByKilometer,
    string ExpenseDate,
    string Description,
    string? Notes,
    decimal Amount,
    decimal? KilometersTravelled,
    string? TravelStartPoint,
    string? TravelDestination,
    string[]? TravelWaypoints,
    decimal? MileageRatePerKilometer,
    bool HasReceipt,
    string? ReceiptFileName,
    string? ReceiptUploadedAt,
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
    string? Currency,
    decimal? KilometersTravelled = null,
    string? TravelStartPoint = null,
    string? TravelDestination = null,
    string[]? TravelWaypoints = null);

public record UpdateExpenseClaimRequest(
    Guid CategoryId,
    string ExpenseDate,
    string Description,
    string? Notes,
    decimal Amount,
    string? Currency,
    decimal? KilometersTravelled = null,
    string? TravelStartPoint = null,
    string? TravelDestination = null,
    string[]? TravelWaypoints = null);

public record ExpenseCategoryDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    int SortOrder,
    bool RequiresReceipt,
    bool RequiresTravelDetails,
    bool PaysByKilometer,
    string CreatedAt,
    string? CreatedByUserId,
    string? CreatedByDisplayName,
    string UpdatedAt,
    string? UpdatedByUserId,
    string? UpdatedByDisplayName);

public record SaveExpenseCategoryRequest(
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    int SortOrder,
    bool RequiresReceipt,
    bool RequiresTravelDetails,
    bool PaysByKilometer);

public record ExpenseSettingsDto(
    decimal KilometerRate,
    string? UpdatedAt);

public record UpdateExpenseSettingsRequest(
    decimal KilometerRate);

public record ExpenseReceiptMetadataDto(
    bool HasReceipt,
    string? ReceiptFileName,
    string? ReceiptUploadedAt);

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
    bool RequiresReceipt,
    bool RequiresTravelDetails,
    bool PaysByKilometer,
    string ExpenseDate,
    string Description,
    string? Notes,
    decimal Amount,
    decimal? KilometersTravelled,
    string? TravelStartPoint,
    string? TravelDestination,
    string[]? TravelWaypoints,
    decimal? MileageRatePerKilometer,
    bool HasReceipt,
    string? ReceiptFileName,
    string? ReceiptUploadedAt,
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
