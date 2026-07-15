using CarTrack.Core;

namespace CarTrack.Modules.Expense;

public class ExpenseClaim : IAuditable
{
    public Guid Id { get; set; }

    public required string RequesterUserId { get; set; }

    public required string ManagerUserId { get; set; }

    public Guid CategoryId { get; set; }

    public ExpenseCategory Category { get; set; } = null!;

    public DateOnly ExpenseDate { get; set; }

    public required string Description { get; set; }

    public string? Notes { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "ZAR";

    public string Status { get; set; } = ExpenseClaimStatuses.Draft;

    public string? RequesterBranch { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string? UpdatedByUserId { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }

    public DateTimeOffset? DecidedAt { get; set; }

    public string? DecidedByUserId { get; set; }

    public DateTimeOffset? PaidAt { get; set; }

    public string? PaidByUserId { get; set; }
}

public static class ExpenseClaimStatuses
{
    public const string Draft = "Draft";

    public const string PendingManager = "PendingManager";

    public const string PendingFinance = "PendingFinance";

    public const string PendingPayment = "PendingPayment";

    public const string Paid = "Paid";

    public const string Rejected = "Rejected";

    public const string Cancelled = "Cancelled";

    public static bool IsEditable(string status) =>
        string.Equals(status, Draft, StringComparison.OrdinalIgnoreCase);

    public static bool IsCancellable(string status) =>
        string.Equals(status, Draft, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, PendingManager, StringComparison.OrdinalIgnoreCase);

    public static readonly string[] InApprovalPipeline =
    [
        PendingManager,
        PendingFinance,
    ];

    public static readonly string[] AwaitingPayment =
    [
        PendingPayment,
    ];
}
