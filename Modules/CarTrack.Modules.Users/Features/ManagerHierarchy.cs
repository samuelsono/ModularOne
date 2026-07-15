using CarTrack.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Users;

public static class ManagerHierarchy
{
    public static async Task<string?> ResolveManagerUserIdAsync(
        UsersDbContext dbContext,
        string requesterUserId,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.StaffProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserId == requesterUserId, cancellationToken);

        return profile?.ManagerUserId;
    }

    public static async Task<IReadOnlyList<string>> GetDirectReportUserIdsAsync(
        UsersDbContext dbContext,
        string managerUserId,
        CancellationToken cancellationToken = default) =>
        await dbContext.StaffProfiles
            .AsNoTracking()
            .Where(profile => profile.ManagerUserId == managerUserId)
            .Select(profile => profile.UserId)
            .ToListAsync(cancellationToken);

    public static async Task<HashSet<string>> GetAllReportUserIdsAsync(
        UsersDbContext dbContext,
        string managerUserId,
        CancellationToken cancellationToken = default)
    {
        var allProfiles = await dbContext.StaffProfiles
            .AsNoTracking()
            .Select(profile => new { profile.UserId, profile.ManagerUserId })
            .ToListAsync(cancellationToken);

        var childrenByManager = allProfiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.ManagerUserId))
            .GroupBy(profile => profile.ManagerUserId!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.UserId).ToList(),
                StringComparer.Ordinal);

        var reportIds = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();

        foreach (var directReportId in childrenByManager.GetValueOrDefault(managerUserId) ?? [])
        {
            queue.Enqueue(directReportId);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!reportIds.Add(current))
            {
                continue;
            }

            foreach (var childId in childrenByManager.GetValueOrDefault(current) ?? [])
            {
                queue.Enqueue(childId);
            }
        }

        return reportIds;
    }

    public static async Task<string?> ResolveRequesterBranchAsync(
        UsersDbContext dbContext,
        IDriverDirectory driverDirectory,
        string requesterUserId,
        CancellationToken cancellationToken = default)
    {
        var staffProfile = await dbContext.StaffProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserId == requesterUserId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(staffProfile?.Branch))
        {
            return staffProfile.Branch;
        }

        var driverLink = await dbContext.DriverProfileLinks
            .AsNoTracking()
            .SingleOrDefaultAsync(link => link.UserId == requesterUserId, cancellationToken);

        if (driverLink is null)
        {
            return null;
        }

        var scope = await driverDirectory.GetScopeInfoAsync(driverLink.DriverId, cancellationToken);
        return scope?.Branch;
    }
}
