using CarTrack.Api;

namespace CarTrack.Modules.Leave;

public interface ILeaveApprovalService
{
    Task<LeaveRequestDto> CreateRequestAsync(
        string requesterUserId,
        CreateLeaveRequest request,
        IFormFile? document = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequestDto>> GetMyRequestsAsync(
        string requesterUserId,
        string? status,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestDto?> GetRequestAsync(
        Guid requestId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestDto?> CancelRequestAsync(
        Guid requestId,
        string actingUserId,
        CancelLeaveRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequestDto>> GetPendingApprovalsAsync(
        string managerUserId,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestDto?> UploadDocumentAsync(
        Guid requestId,
        string actingUserId,
        IFormFile document,
        CancellationToken cancellationToken = default);

    Task<(Stream Stream, string ContentType, string FileName)?> GetDocumentAsync(
        Guid requestId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestDto?> DecideAsync(
        Guid requestId,
        string managerUserId,
        ApprovalDecisionRequest request,
        CancellationToken cancellationToken = default);
}
