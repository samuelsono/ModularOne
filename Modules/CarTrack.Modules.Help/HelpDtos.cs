namespace CarTrack.Modules.Help;

public record HelpArticleSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string? CategoryName,
    string[] Tags,
    bool IsPublished,
    DateTime UpdatedAt);

public record HelpArticleDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    string? CategoryName,
    string[] Tags,
    bool IsPublished,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record SaveHelpArticleRequest(
    string Title,
    string Body,
    string? CategoryName,
    string[] Tags,
    bool IsPublished);
