namespace CarTrack.Modules.Support;

public interface ISupportService
{
    Task<IReadOnlyList<TicketCategoryDto>> GetCategoriesAsync(
        TicketType? type,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<SupportTicketDto> SubmitTicketAsync(
        string userId,
        SubmitTicketRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupportTicketDto>> GetMyTicketsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<SupportTicketDto?> GetTicketAsync(
        Guid id,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupportTicketDto>> GetAllTicketsAdminAsync(
        SupportTicketFilters filters,
        CancellationToken cancellationToken = default);

    Task<SupportTicketDto?> UpdateTicketAdminAsync(
        Guid id,
        UpdateTicketAdminRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteTicketAdminAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TicketCategoryDto> CreateCategoryAsync(
        SaveCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<TicketCategoryDto?> UpdateCategoryAsync(
        string id,
        SaveCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteCategoryAsync(string id, CancellationToken cancellationToken = default);
}
