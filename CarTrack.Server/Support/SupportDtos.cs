using CarTrack.Server.Data;

namespace CarTrack.Server.Support;

public record TicketCategoryDto(
    string Id,
    string Label,
    TicketType[] AppliesTo,
    bool IsActive,
    int SortOrder);

public record SubmitTicketRequest(
    TicketType Type,
    string Subject,
    string Description,
    string CategoryId,
    TicketPriority Priority = TicketPriority.Medium,
    string? BugSeverity = null,
    string? StepsToReproduce = null,
    string? ExpectedBehavior = null,
    string? ActualBehavior = null,
    string? BrowserOrEnvironment = null,
    int? SatisfactionRating = null);

public record SupportTicketDto(
    Guid Id,
    TicketType Type,
    TicketStatus Status,
    TicketPriority Priority,
    string Subject,
    string Description,
    TicketCategoryDto Category,
    string? BugSeverity,
    string? StepsToReproduce,
    string? ExpectedBehavior,
    string? ActualBehavior,
    string? BrowserOrEnvironment,
    int? SatisfactionRating,
    string SubmittedByDisplayName,
    string? AssignedToDisplayName,
    string? AdminNotes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt);

public record UpdateTicketAdminRequest(
    TicketStatus Status,
    TicketPriority Priority,
    string? AssignedToUserId,
    string? AdminNotes);

public record SaveCategoryRequest(
    string Id,
    string Label,
    TicketType[] AppliesTo,
    bool IsActive,
    int SortOrder);

public record SupportTicketFilters(
    TicketType? Type = null,
    TicketStatus? Status = null,
    TicketPriority? Priority = null,
    string? Search = null);
