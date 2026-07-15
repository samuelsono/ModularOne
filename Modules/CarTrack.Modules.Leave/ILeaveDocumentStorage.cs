namespace CarTrack.Modules.Leave;

public interface ILeaveDocumentStorage
{
    Task<LeaveDocumentMetadata> SaveAsync(
        Guid requestId,
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task<(Stream Stream, string ContentType, string FileName)?> OpenReadAsync(
        string storedPath,
        CancellationToken cancellationToken = default);

    Task DeleteIfExistsAsync(string? storedPath, CancellationToken cancellationToken = default);
}
