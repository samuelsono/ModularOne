using CarTrack.Identity.Contracts;
using CarTrack.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Users;
public sealed class OrgDirectory(
    UsersDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IDriverDirectory driverDirectory) : IOrgDirectory
{
    public Task<string?> ResolveManagerUserIdAsync(
        string requesterUserId,
        CancellationToken cancellationToken = default) =>
        ManagerHierarchy.ResolveManagerUserIdAsync(dbContext, requesterUserId, cancellationToken);

    public Task<IReadOnlyList<string>> GetDirectReportUserIdsAsync(
        string managerUserId,
        CancellationToken cancellationToken = default) =>
        ManagerHierarchy.GetDirectReportUserIdsAsync(dbContext, managerUserId, cancellationToken);

    public async Task<IReadOnlySet<string>> GetAllReportUserIdsAsync(
        string managerUserId,
        CancellationToken cancellationToken = default)
    {
        var reports = await ManagerHierarchy.GetAllReportUserIdsAsync(dbContext, managerUserId, cancellationToken);
        return reports;
    }

    public Task<string?> ResolveRequesterBranchAsync(
        string requesterUserId,
        CancellationToken cancellationToken = default) =>
        ManagerHierarchy.ResolveRequesterBranchAsync(dbContext, driverDirectory, requesterUserId, cancellationToken);

    public async Task<StaffOrgInfo?> GetStaffOrgInfoAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.StaffProfiles
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => new StaffOrgInfo(item.UserId, item.Department, item.Branch))
            .FirstOrDefaultAsync(cancellationToken);

        return profile;
    }

    public async Task<IReadOnlyDictionary<string, StaffOrgInfo>> GetStaffOrgInfoBatchAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<string, StaffOrgInfo>();
        }

        var rows = await dbContext.StaffProfiles
            .AsNoTracking()
            .Where(item => ids.Contains(item.UserId))
            .Select(item => new StaffOrgInfo(item.UserId, item.Department, item.Branch))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(item => item.UserId);
    }

    public async Task<IReadOnlyList<StaffAccrualInfo>> GetActiveStaffForAccrualAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.StaffProfiles
            .AsNoTracking()
            .Where(profile => profile.EmploymentStatus == "Active")
            .Select(profile => new StaffAccrualInfo(profile.UserId, profile.WorkStartDate))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken = default) =>
        userManager.Users.AnyAsync(user => user.Id == userId, cancellationToken);

    public async Task<DriverLinkedUserInfo?> GetLinkedUserForDriverAsync(
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        var link = await dbContext.DriverProfileLinks
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.DriverId == driverId, cancellationToken);

        if (link is null)
        {
            return null;
        }

        var user = await userManager.FindByIdAsync(link.UserId);
        return user is null ? null : ToLinkedUserInfo(user);
    }

    public async Task<IReadOnlyDictionary<Guid, DriverLinkedUserInfo>> GetLinkedUsersByDriverIdAsync(
        CancellationToken cancellationToken = default)
    {
        var links = await dbContext.DriverProfileLinks
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (links.Count == 0)
        {
            return new Dictionary<Guid, DriverLinkedUserInfo>();
        }

        var userIds = links.Select(link => link.UserId).Distinct().ToList();
        var users = await userManager.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        var result = new Dictionary<Guid, DriverLinkedUserInfo>();
        foreach (var link in links)
        {
            if (users.TryGetValue(link.UserId, out var user))
            {
                result[link.DriverId] = ToLinkedUserInfo(user);
            }
        }

        return result;
    }

    private static DriverLinkedUserInfo ToLinkedUserInfo(ApplicationUser user) =>
        new(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.IsActive);
}
