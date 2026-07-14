using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CarTrack.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Help;

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

public partial class HelpService(ApplicationDbContext dbContext) : IHelpService
{
    public async Task<IReadOnlyList<HelpArticleSummaryDto>> GetPublishedArticlesAsync(
        string? search,
        string? category,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.HelpArticles
            .AsNoTracking()
            .Where(article => article.IsPublished);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(article => article.CategoryName == category.Trim());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(article =>
                EF.Functions.ILike(article.Title, $"%{term}%")
                || EF.Functions.ILike(article.Body, $"%{term}%"));
        }

        var articles = await query
            .OrderBy(article => article.CategoryName ?? "General")
            .ThenBy(article => article.Title)
            .ToListAsync(cancellationToken);

        return articles.Select(HelpMapper.ToSummaryDto).ToList();
    }

    public async Task<HelpArticleDetailDto?> GetPublishedArticleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var article = await dbContext.HelpArticles
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && item.IsPublished, cancellationToken);

        return article is null ? null : HelpMapper.ToDetailDto(article);
    }

    public async Task<IReadOnlyList<string>> GetPublishedCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.HelpArticles
            .AsNoTracking()
            .Where(article => article.IsPublished && article.CategoryName != null)
            .Select(article => article.CategoryName!)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HelpArticleSummaryDto>> GetAdminArticlesAsync(
        CancellationToken cancellationToken = default)
    {
        var articles = await dbContext.HelpArticles
            .AsNoTracking()
            .OrderByDescending(article => article.UpdatedAt)
            .ToListAsync(cancellationToken);

        return articles.Select(HelpMapper.ToSummaryDto).ToList();
    }

    public async Task<HelpArticleDetailDto?> GetAdminArticleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var article = await dbContext.HelpArticles
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return article is null ? null : HelpMapper.ToDetailDto(article);
    }

    public async Task<HelpArticleDetailDto> CreateArticleAsync(
        string userId,
        SaveHelpArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateSaveRequest(request);

        var now = DateTime.UtcNow;
        var slug = await EnsureUniqueSlugAsync(GenerateSlug(request.Title), null, cancellationToken);

        var article = new HelpArticle
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Slug = slug,
            Body = request.Body.Trim(),
            CategoryName = string.IsNullOrWhiteSpace(request.CategoryName) ? null : request.CategoryName.Trim(),
            Tags = NormalizeTags(request.Tags),
            IsPublished = request.IsPublished,
            CreatedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.HelpArticles.Add(article);
        await dbContext.SaveChangesAsync(cancellationToken);

        return HelpMapper.ToDetailDto(article);
    }

    public async Task<HelpArticleDetailDto?> UpdateArticleAsync(
        Guid id,
        SaveHelpArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateSaveRequest(request);

        var article = await dbContext.HelpArticles
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (article is null)
        {
            return null;
        }

        var slug = await EnsureUniqueSlugAsync(GenerateSlug(request.Title), article.Id, cancellationToken);

        article.Title = request.Title.Trim();
        article.Slug = slug;
        article.Body = request.Body.Trim();
        article.CategoryName = string.IsNullOrWhiteSpace(request.CategoryName) ? null : request.CategoryName.Trim();
        article.Tags = NormalizeTags(request.Tags);
        article.IsPublished = request.IsPublished;
        article.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return HelpMapper.ToDetailDto(article);
    }

    public async Task<HelpArticleDetailDto?> TogglePublishAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var article = await dbContext.HelpArticles
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (article is null)
        {
            return null;
        }

        article.IsPublished = !article.IsPublished;
        article.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return HelpMapper.ToDetailDto(article);
    }

    public async Task<bool> DeleteArticleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var article = await dbContext.HelpArticles
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (article is null)
        {
            return false;
        }

        dbContext.HelpArticles.Remove(article);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateSaveRequest(SaveHelpArticleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new InvalidOperationException("Title is required.");
        }

        if (request.Title.Length > 200)
        {
            throw new InvalidOperationException("Title must be 200 characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            throw new InvalidOperationException("Body is required.");
        }

        if (request.Body.Length > 50000)
        {
            throw new InvalidOperationException("Body must be 50,000 characters or fewer.");
        }
    }

    private static string[] NormalizeTags(string[] tags) =>
        tags
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();

    private async Task<string> EnsureUniqueSlugAsync(
        string baseSlug,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var slug = baseSlug;
        var suffix = 2;

        while (await dbContext.HelpArticles.AnyAsync(
            article => article.Slug == slug && (excludeId == null || article.Id != excludeId),
            cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }

    public static string GenerateSlug(string title)
    {
        var normalized = title.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
            else if (character is ' ' or '-' or '_')
            {
                builder.Append('-');
            }
        }

        var slug = SlugCleanupRegex().Replace(builder.ToString(), "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "article" : slug;
    }

    [GeneratedRegex("-{2,}")]
    private static partial Regex SlugCleanupRegex();
}

internal static class HelpMapper
{
    public static HelpArticleSummaryDto ToSummaryDto(HelpArticle article) =>
        new(
            article.Id,
            article.Title,
            article.Slug,
            article.CategoryName,
            article.Tags,
            article.IsPublished,
            article.UpdatedAt);

    public static HelpArticleDetailDto ToDetailDto(HelpArticle article) =>
        new(
            article.Id,
            article.Title,
            article.Slug,
            article.Body,
            article.CategoryName,
            article.Tags,
            article.IsPublished,
            article.CreatedAt,
            article.UpdatedAt);
}
