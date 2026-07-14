using CarTrack.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Support;

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

public class SupportService(ApplicationDbContext dbContext) : ISupportService
{
    public async Task<IReadOnlyList<TicketCategoryDto>> GetCategoriesAsync(
        TicketType? type,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.TicketCategories.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(category => category.IsActive);
        }

        var categories = await query
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Label)
            .ToListAsync(cancellationToken);

        return categories
            .Where(category => type is null || category.AppliesTo.Contains(type.Value))
            .Select(SupportMapper.ToCategoryDto)
            .ToList();
    }

    public async Task<SupportTicketDto> SubmitTicketAsync(
        string userId,
        SubmitTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateSubmitRequest(request);

        var category = await dbContext.TicketCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == request.CategoryId && item.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException("The selected category is not valid.");

        if (!category.AppliesTo.Contains(request.Type))
        {
            throw new InvalidOperationException("The selected category does not apply to this ticket type.");
        }

        var now = DateTime.UtcNow;
        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            Status = TicketStatus.Open,
            Priority = request.Priority,
            Subject = request.Subject.Trim(),
            Description = request.Description.Trim(),
            CategoryId = category.Id,
            BugSeverity = request.Type == TicketType.BugReport ? request.BugSeverity?.Trim() : null,
            StepsToReproduce = request.Type == TicketType.BugReport ? request.StepsToReproduce?.Trim() : null,
            ExpectedBehavior = request.Type == TicketType.BugReport ? request.ExpectedBehavior?.Trim() : null,
            ActualBehavior = request.Type == TicketType.BugReport ? request.ActualBehavior?.Trim() : null,
            BrowserOrEnvironment = request.Type == TicketType.BugReport ? request.BrowserOrEnvironment?.Trim() : null,
            SatisfactionRating = request.Type == TicketType.Feedback ? request.SatisfactionRating : null,
            SubmittedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.SupportTickets.Add(ticket);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await LoadTicketDtoAsync(ticket.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to load submitted ticket.");
    }

    public async Task<IReadOnlyList<SupportTicketDto>> GetMyTicketsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var tickets = await dbContext.SupportTickets
            .AsNoTracking()
            .Include(ticket => ticket.Category)
            .Include(ticket => ticket.SubmittedBy)
            .Include(ticket => ticket.AssignedTo)
            .Where(ticket => ticket.SubmittedByUserId == userId)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);

        return tickets.Select(SupportMapper.ToTicketDto).ToList();
    }

    public async Task<SupportTicketDto?> GetTicketAsync(
        Guid id,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var ticket = await dbContext.SupportTickets
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.SubmittedBy)
            .Include(item => item.AssignedTo)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        if (!isAdmin && !string.Equals(ticket.SubmittedByUserId, userId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("You do not have access to this ticket.");
        }

        return SupportMapper.ToTicketDto(ticket);
    }

    public async Task<IReadOnlyList<SupportTicketDto>> GetAllTicketsAdminAsync(
        SupportTicketFilters filters,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SupportTickets
            .AsNoTracking()
            .Include(ticket => ticket.Category)
            .Include(ticket => ticket.SubmittedBy)
            .Include(ticket => ticket.AssignedTo)
            .AsQueryable();

        if (filters.Type is not null)
        {
            query = query.Where(ticket => ticket.Type == filters.Type);
        }

        if (filters.Status is not null)
        {
            query = query.Where(ticket => ticket.Status == filters.Status);
        }

        if (filters.Priority is not null)
        {
            query = query.Where(ticket => ticket.Priority == filters.Priority);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(ticket =>
                EF.Functions.ILike(ticket.Subject, $"%{search}%")
                || EF.Functions.ILike(ticket.Description, $"%{search}%")
                || EF.Functions.ILike(
                    ticket.SubmittedBy.DisplayName ?? ticket.SubmittedBy.UserName ?? string.Empty,
                    $"%{search}%"));
        }

        var tickets = await query
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);

        return tickets.Select(SupportMapper.ToTicketDto).ToList();
    }

    public async Task<SupportTicketDto?> UpdateTicketAdminAsync(
        Guid id,
        UpdateTicketAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        var ticket = await dbContext.SupportTickets
            .Include(item => item.Category)
            .Include(item => item.SubmittedBy)
            .Include(item => item.AssignedTo)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.AssignedToUserId))
        {
            var assigneeExists = await dbContext.Users
                .AnyAsync(user => user.Id == request.AssignedToUserId, cancellationToken);

            if (!assigneeExists)
            {
                throw new InvalidOperationException("The selected assignee was not found.");
            }
        }

        var previousStatus = ticket.Status;
        ticket.Status = request.Status;
        ticket.Priority = request.Priority;
        ticket.AssignedToUserId = string.IsNullOrWhiteSpace(request.AssignedToUserId)
            ? null
            : request.AssignedToUserId;
        ticket.AdminNotes = request.AdminNotes?.Trim();
        ticket.UpdatedAt = DateTime.UtcNow;

        if (request.Status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            ticket.ResolvedAt ??= DateTime.UtcNow;
        }
        else if (previousStatus is TicketStatus.Resolved or TicketStatus.Closed)
        {
            ticket.ResolvedAt = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(ticket).Reference(item => item.AssignedTo).LoadAsync(cancellationToken);

        return SupportMapper.ToTicketDto(ticket);
    }

    public async Task<bool> DeleteTicketAdminAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ticket = await dbContext.SupportTickets.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (ticket is null)
        {
            return false;
        }

        dbContext.SupportTickets.Remove(ticket);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TicketCategoryDto> CreateCategoryAsync(
        SaveCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCategoryRequest(request);

        var id = request.Id.Trim().ToLowerInvariant();
        if (await dbContext.TicketCategories.AnyAsync(category => category.Id == id, cancellationToken))
        {
            throw new InvalidOperationException($"Category '{id}' already exists.");
        }

        var category = new TicketCategory
        {
            Id = id,
            Label = request.Label.Trim(),
            AppliesTo = request.AppliesTo,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
        };

        dbContext.TicketCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return SupportMapper.ToCategoryDto(category);
    }

    public async Task<TicketCategoryDto?> UpdateCategoryAsync(
        string id,
        SaveCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCategoryRequest(request);

        var category = await dbContext.TicketCategories
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (category is null)
        {
            return null;
        }

        category.Label = request.Label.Trim();
        category.AppliesTo = request.AppliesTo;
        category.IsActive = request.IsActive;
        category.SortOrder = request.SortOrder;

        await dbContext.SaveChangesAsync(cancellationToken);

        return SupportMapper.ToCategoryDto(category);
    }

    public async Task<bool> DeleteCategoryAsync(string id, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.TicketCategories
            .Include(item => item.Tickets)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (category is null)
        {
            return false;
        }

        if (category.Tickets.Count > 0)
        {
            throw new InvalidOperationException("Cannot delete a category that has tickets.");
        }

        dbContext.TicketCategories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<SupportTicketDto?> LoadTicketDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await dbContext.SupportTickets
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.SubmittedBy)
            .Include(item => item.AssignedTo)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return ticket is null ? null : SupportMapper.ToTicketDto(ticket);
    }

    private static void ValidateSubmitRequest(SubmitTicketRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new InvalidOperationException("Subject is required.");
        }

        if (request.Subject.Length > 200)
        {
            throw new InvalidOperationException("Subject must be 200 characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new InvalidOperationException("Description is required.");
        }

        if (request.Description.Trim().Length < 20)
        {
            throw new InvalidOperationException("Description must be at least 20 characters.");
        }

        if (request.Description.Length > 4000)
        {
            throw new InvalidOperationException("Description must be 4000 characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(request.CategoryId))
        {
            throw new InvalidOperationException("Category is required.");
        }

        if (request.Type == TicketType.Feedback)
        {
            if (request.SatisfactionRating is not null
                && (request.SatisfactionRating < 1 || request.SatisfactionRating > 5))
            {
                throw new InvalidOperationException("Satisfaction rating must be between 1 and 5.");
            }
        }
        else if (request.SatisfactionRating is not null)
        {
            throw new InvalidOperationException("Satisfaction rating is only allowed for feedback.");
        }

        if (request.Type != TicketType.BugReport)
        {
            if (!string.IsNullOrWhiteSpace(request.BugSeverity)
                || !string.IsNullOrWhiteSpace(request.StepsToReproduce)
                || !string.IsNullOrWhiteSpace(request.ExpectedBehavior)
                || !string.IsNullOrWhiteSpace(request.ActualBehavior)
                || !string.IsNullOrWhiteSpace(request.BrowserOrEnvironment))
            {
                throw new InvalidOperationException("Bug report fields are only allowed for bug reports.");
            }
        }
    }

    private static void ValidateCategoryRequest(SaveCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            throw new InvalidOperationException("Category id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Label))
        {
            throw new InvalidOperationException("Category label is required.");
        }

        if (request.AppliesTo.Length == 0)
        {
            throw new InvalidOperationException("Category must apply to at least one ticket type.");
        }
    }
}

internal static class SupportMapper
{
    public static TicketCategoryDto ToCategoryDto(TicketCategory category) =>
        new(
            category.Id,
            category.Label,
            category.AppliesTo,
            category.IsActive,
            category.SortOrder);

    public static SupportTicketDto ToTicketDto(SupportTicket ticket) =>
        new(
            ticket.Id,
            ticket.Type,
            ticket.Status,
            ticket.Priority,
            ticket.Subject,
            ticket.Description,
            ToCategoryDto(ticket.Category),
            ticket.BugSeverity,
            ticket.StepsToReproduce,
            ticket.ExpectedBehavior,
            ticket.ActualBehavior,
            ticket.BrowserOrEnvironment,
            ticket.SatisfactionRating,
            ticket.SubmittedBy?.DisplayName ?? ticket.SubmittedBy?.UserName ?? "User",
            ticket.AssignedTo?.DisplayName ?? ticket.AssignedTo?.UserName,
            ticket.AdminNotes,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.ResolvedAt);
}
