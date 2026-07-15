namespace CarTrack.Modules.Help;

public interface IHelpService
{
    Task<IReadOnlyList<HelpArticleSummaryDto>> GetPublishedArticlesAsync(
        string? search,
        string? category,
        CancellationToken cancellationToken = default);

    Task<HelpArticleDetailDto?> GetPublishedArticleAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetPublishedCategoriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HelpArticleSummaryDto>> GetAdminArticlesAsync(CancellationToken cancellationToken = default);

    Task<HelpArticleDetailDto?> GetAdminArticleAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<HelpArticleDetailDto> CreateArticleAsync(
        string userId,
        SaveHelpArticleRequest request,
        CancellationToken cancellationToken = default);

    Task<HelpArticleDetailDto?> UpdateArticleAsync(
        Guid id,
        SaveHelpArticleRequest request,
        CancellationToken cancellationToken = default);

    Task<HelpArticleDetailDto?> TogglePublishAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteArticleAsync(Guid id, CancellationToken cancellationToken = default);
}
