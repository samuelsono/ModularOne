namespace CarTrack.Modules.Expense;

public interface IExpenseDocumentStorage
{
    Task<ExpenseDocumentMetadata> SaveAsync(
        Guid claimId,
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task<(Stream Stream, string ContentType, string FileName)?> OpenReadAsync(
        string storedPath,
        CancellationToken cancellationToken = default);

    Task DeleteIfExistsAsync(string? storedPath, CancellationToken cancellationToken = default);
}