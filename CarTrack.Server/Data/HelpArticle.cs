namespace CarTrack.Server.Data;

public class HelpArticle
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string? CategoryName { get; set; }

    public string[] Tags { get; set; } = [];

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public ApplicationUser CreatedBy { get; set; } = null!;
}
