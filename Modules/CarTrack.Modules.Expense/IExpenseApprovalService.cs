using CarTrack.Api;

namespace CarTrack.Modules.Expense;

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

    Task<ExpenseClaimDto?> UploadReceiptAsync(
        Guid claimId,
        string requesterUserId,
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task<(Stream Stream, string ContentType, string FileName)?> GetReceiptAsync(
        Guid claimId,
        string viewerUserId,
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
