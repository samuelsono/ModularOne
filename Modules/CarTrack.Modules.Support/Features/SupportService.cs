using CarTrack.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Support;

public class SupportService(
    SupportDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ISupportService
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
            .Where(ticket => ticket.SubmittedByUserId == userId)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);

        var names = await ResolveDisplayNamesAsync(tickets, cancellationToken);
        return tickets.Select(ticket => SupportMapper.ToTicketDto(ticket, names)).ToList();
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
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        if (!isAdmin && !string.Equals(ticket.SubmittedByUserId, userId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("You do not have access to this ticket.");
        }

        var names = await ResolveDisplayNamesAsync([ticket], cancellationToken);
        return SupportMapper.ToTicketDto(ticket, names);
    }

    public async Task<IReadOnlyList<SupportTicketDto>> GetAllTicketsAdminAsync(
        SupportTicketFilters filters,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SupportTickets
            .AsNoTracking()
            .Include(ticket => ticket.Category)
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
            var matchingUserIds = await userManager.Users
                .AsNoTracking()
                .Where(user =>
                    EF.Functions.ILike(user.DisplayName ?? user.UserName ?? string.Empty, $"%{search}%"))
                .Select(user => user.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(ticket =>
                EF.Functions.ILike(ticket.Subject, $"%{search}%")
                || EF.Functions.ILike(ticket.Description, $"%{search}%")
                || matchingUserIds.Contains(ticket.SubmittedByUserId));
        }

        var tickets = await query
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);

        var names = await ResolveDisplayNamesAsync(tickets, cancellationToken);
        return tickets.Select(ticket => SupportMapper.ToTicketDto(ticket, names)).ToList();
    }

    public async Task<SupportTicketDto?> UpdateTicketAdminAsync(
        Guid id,
        UpdateTicketAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        var ticket = await dbContext.SupportTickets
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.AssignedToUserId))
        {
            var assignee = await userManager.FindByIdAsync(request.AssignedToUserId);
            if (assignee is null)
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

        var names = await ResolveDisplayNamesAsync([ticket], cancellationToken);
        return SupportMapper.ToTicketDto(ticket, names);
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
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        var names = await ResolveDisplayNamesAsync([ticket], cancellationToken);
        return SupportMapper.ToTicketDto(ticket, names);
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveDisplayNamesAsync(
        IEnumerable<SupportTicket> tickets,
        CancellationToken cancellationToken)
    {
        var userIds = tickets
            .SelectMany(ticket => new[] { ticket.SubmittedByUserId, ticket.AssignedToUserId })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (userIds.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return await userManager.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(
                user => user.Id,
                user => user.DisplayName ?? user.UserName ?? "User",
                StringComparer.Ordinal,
                cancellationToken);
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

    public static SupportTicketDto ToTicketDto(
        SupportTicket ticket,
        IReadOnlyDictionary<string, string> displayNames) =>
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
            displayNames.GetValueOrDefault(ticket.SubmittedByUserId, "User"),
            ticket.AssignedToUserId is null
                ? null
                : displayNames.GetValueOrDefault(ticket.AssignedToUserId),
            ticket.AdminNotes,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.ResolvedAt);
}
