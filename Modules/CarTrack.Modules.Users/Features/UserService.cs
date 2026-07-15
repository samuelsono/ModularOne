using System.Security.Cryptography;
using CarTrack.Identity.Contracts;
using CarTrack.Server.Data;
using CarTrack.Server.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Users;

public class UserService(
    UsersDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IAccountEmailService accountEmailService,
    ISecurityAuditService auditService,
    ITokenService tokenService,
    IOrgDirectory orgDirectory,
    IOrgStructureLookup orgStructure,
    IDriverDirectory driverDirectory) : IUserService
{
    private static readonly HashSet<string> ManagerEligibleRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        AppRoles.Manager,
        AppRoles.Staff,
        AppRoles.FleetAdmin,
        AppRoles.Hr,
        AppRoles.SystemAdmin,
        AppRoles.Admin,
    };

    public async Task<UserListResponse> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.DisplayName ?? user.UserName)
            .ToListAsync(cancellationToken);

        var staffProfiles = await dbContext.StaffProfiles
            .AsNoTracking()
            .ToDictionaryAsync(profile => profile.UserId, cancellationToken);

        var orgNames = await ResolveOrgNamesAsync(staffProfiles.Values, cancellationToken);

        var driverLinks = await dbContext.DriverProfileLinks
            .AsNoTracking()
            .ToDictionaryAsync(link => link.UserId, cancellationToken);

        var displayNameIds = staffProfiles.Values
            .SelectMany(profile => new[] { profile.ManagerUserId, profile.CreatedByUserId, profile.UpdatedByUserId })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList()!;

        var displayNames = await ResolveUserDisplayNamesAsync(displayNameIds!, cancellationToken);

        var items = new List<UserListItemDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            staffProfiles.TryGetValue(user.Id, out var staffProfile);
            driverLinks.TryGetValue(user.Id, out var driverLink);
            orgNames.TryGetValue(user.Id, out var org);

            string? managerName = null;
            if (staffProfile?.ManagerUserId is not null)
            {
                displayNames.TryGetValue(staffProfile.ManagerUserId, out managerName);
            }

            string? createdByName = null;
            if (staffProfile?.CreatedByUserId is not null)
            {
                displayNames.TryGetValue(staffProfile.CreatedByUserId, out createdByName);
            }

            string? updatedByName = null;
            if (staffProfile?.UpdatedByUserId is not null)
            {
                displayNames.TryGetValue(staffProfile.UpdatedByUserId, out updatedByName);
            }

            items.Add(new UserListItemDto(
                user.Id,
                user.UserName ?? string.Empty,
                user.Email ?? string.Empty,
                user.DisplayName,
                roles.ToList(),
                user.IsActive,
                user.LastLoginAt,
                user.InvitePendingAt,
                managerName,
                driverLink?.DriverId,
                org?.CompanyName,
                org?.DepartmentName ?? staffProfile?.Department,
                org?.PositionName ?? staffProfile?.JobTitle,
                AuditableMapping.FormatTimestamp(staffProfile?.CreatedAt ?? user.CreatedAt),
                staffProfile?.CreatedByUserId,
                createdByName,
                AuditableMapping.FormatTimestamp(
                    staffProfile is null
                        ? user.CreatedAt
                        : (staffProfile.UpdatedAt == default ? staffProfile.CreatedAt : staffProfile.UpdatedAt)),
                staffProfile?.UpdatedByUserId,
                updatedByName));
        }

        return new UserListResponse(items);
    }

    public async Task<UserDetailDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        if (user is null)
        {
            return null;
        }

        return await MapDetailAsync(user, cancellationToken);
    }

    public async Task<UserDetailDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRoles(request.Roles);

        if (request.Staff?.ManagerUserId is not null)
        {
            await ValidateManagerAssignmentAsync(null, request.Staff.ManagerUserId, cancellationToken);
        }

        if (request.DriverId is not null)
        {
            await ValidateDriverLinkAsync(null, request.DriverId.Value, cancellationToken);
        }

        var sendInvite = request.SendInvite;
        var password = sendInvite
            ? GenerateSecurePassword()
            : request.Password?.Trim();

        if (!sendInvite && string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Password is required unless sending an invite.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Username.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = !sendInvite,
            DisplayName = request.DisplayName?.Trim(),
            FirstName = request.FirstName?.Trim(),
            LastName = request.LastName?.Trim(),
            IsActive = request.IsActive,
            MustChangePassword = sendInvite,
            InvitePendingAt = sendInvite ? DateTimeOffset.UtcNow : null,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var createResult = await userManager.CreateAsync(user, password!);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", createResult.Errors.Select(error => error.Description)));
        }

        await userManager.AddToRolesAsync(user, request.Roles);

        if (request.Staff is not null)
        {
            await UpsertStaffProfileAsync(user.Id, request.Staff, cancellationToken);
        }

        if (request.DriverId is not null)
        {
            await UpsertDriverLinkAsync(user.Id, request.DriverId.Value, cancellationToken);
        }

        await auditService.LogAsync(
            SecurityAuditActions.UserCreated,
            user.Id,
            user.DisplayName ?? user.UserName,
            $"Roles: {string.Join(", ", request.Roles)}",
            cancellationToken);

        if (sendInvite)
        {
            await accountEmailService.SendInviteAsync(user, cancellationToken);
            await auditService.LogAsync(
                SecurityAuditActions.UserInviteSent,
                user.Id,
                user.DisplayName ?? user.UserName,
                cancellationToken: cancellationToken);
        }

        return (await GetByIdAsync(user.Id, cancellationToken))!;
    }

    public async Task<UserDetailDto?> UpdateAsync(
        string id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (request.Staff?.ManagerUserId is not null)
        {
            await ValidateManagerAssignmentAsync(id, request.Staff.ManagerUserId, cancellationToken);
        }

        if (request.DriverId is not null)
        {
            await ValidateDriverLinkAsync(id, request.DriverId.Value, cancellationToken);
        }

        user.Email = request.Email.Trim();
        user.DisplayName = request.DisplayName?.Trim();
        user.FirstName = request.FirstName?.Trim();
        user.LastName = request.LastName?.Trim();

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", updateResult.Errors.Select(error => error.Description)));
        }

        if (request.Staff is not null)
        {
            await UpsertStaffProfileAsync(id, request.Staff, cancellationToken);
        }

        if (request.ClearDriverLink)
        {
            var existingLink = await dbContext.DriverProfileLinks
                .SingleOrDefaultAsync(link => link.UserId == id, cancellationToken);
            if (existingLink is not null)
            {
                dbContext.DriverProfileLinks.Remove(existingLink);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        else if (request.DriverId is not null)
        {
            await UpsertDriverLinkAsync(id, request.DriverId.Value, cancellationToken);
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<UserDetailDto?> SetRolesAsync(
        string id,
        SetUserRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return null;
        }

        ValidateRoles(request.Roles);

        var currentRoles = await userManager.GetRolesAsync(user);
        var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", removeResult.Errors.Select(error => error.Description)));
        }

        var addResult = await userManager.AddToRolesAsync(user, request.Roles);
        if (!addResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", addResult.Errors.Select(error => error.Description)));
        }

        if (!currentRoles.OrderBy(role => role).SequenceEqual(request.Roles.OrderBy(role => role), StringComparer.OrdinalIgnoreCase))
        {
            await tokenService.RevokeAllRefreshTokensAsync(id, cancellationToken);
            await auditService.LogAsync(
                SecurityAuditActions.UserRolesChanged,
                user.Id,
                user.DisplayName ?? user.UserName,
                $"From [{string.Join(", ", currentRoles)}] to [{string.Join(", ", request.Roles)}]",
                cancellationToken);
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<UserDetailDto?> SetActiveAsync(
        string id,
        SetUserActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var wasActive = user.IsActive;
        user.IsActive = request.IsActive;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", updateResult.Errors.Select(error => error.Description)));
        }

        if (!request.IsActive)
        {
            var refreshTokens = await dbContext.RefreshTokens
                .Where(token => token.UserId == id && token.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in refreshTokens)
            {
                token.RevokedAt = DateTimeOffset.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (wasActive != request.IsActive)
        {
            await auditService.LogAsync(
                request.IsActive ? SecurityAuditActions.UserActivated : SecurityAuditActions.UserDeactivated,
                user.Id,
                user.DisplayName ?? user.UserName,
                cancellationToken: cancellationToken);
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<ManagerOptionDto>> GetManagerOptionsAsync(
        string? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users
            .AsNoTracking()
            .Where(user => user.IsActive)
            .OrderBy(user => user.DisplayName ?? user.UserName)
            .ToListAsync(cancellationToken);

        var options = new List<ManagerOptionDto>();
        foreach (var user in users)
        {
            if (excludeUserId is not null && user.Id == excludeUserId)
            {
                continue;
            }

            var roles = await userManager.GetRolesAsync(user);
            if (!roles.Any(role => ManagerEligibleRoles.Contains(role)))
            {
                continue;
            }

            options.Add(new ManagerOptionDto(
                user.Id,
                user.DisplayName ?? user.UserName ?? user.Email ?? user.Id,
                user.Email ?? string.Empty));
        }

        return options;
    }

    public async Task<IReadOnlyList<RoleSummaryDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await roleManager.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);

        var rolePermissions = await dbContext.RolePermissions
            .AsNoTracking()
            .Include(mapping => mapping.Permission)
            .ToListAsync(cancellationToken);

        return roles
            .Select(role => new RoleSummaryDto(
                role.Name ?? string.Empty,
                rolePermissions
                    .Where(mapping => mapping.RoleId == role.Id)
                    .Select(mapping => mapping.Permission.Key)
                    .OrderBy(key => key)
                    .ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Permissions
            .AsNoTracking()
            .OrderBy(permission => permission.ModuleSlug)
            .ThenBy(permission => permission.SubmoduleSlug)
            .ThenBy(permission => permission.Action)
            .Select(permission => new PermissionDto(
                permission.Id,
                permission.Key,
                permission.ModuleSlug,
                permission.SubmoduleSlug,
                permission.Action,
                permission.Description))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DirectReportDto>> GetDirectReportsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var profiles = await dbContext.StaffProfiles
            .AsNoTracking()
            .Where(profile => profile.ManagerUserId == userId)
            .ToListAsync(cancellationToken);

        var reportUserIds = profiles.Select(profile => profile.UserId).ToList();
        var reportUsers = reportUserIds.Count == 0
            ? new Dictionary<string, ApplicationUser>()
            : await userManager.Users
                .AsNoTracking()
                .Where(user => reportUserIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id, cancellationToken);

        return profiles
            .Select(profile =>
            {
                reportUsers.TryGetValue(profile.UserId, out var user);
                return new DirectReportDto(
                    profile.UserId,
                    user?.DisplayName ?? user?.UserName ?? user?.Email ?? profile.UserId,
                    user?.Email ?? string.Empty,
                    profile.JobTitle,
                    profile.Department,
                    user?.IsActive ?? false);
            })
            .OrderBy(item => item.DisplayName)
            .ToList();
    }

    public async Task<UserOrgDto?> GetOrgAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var staffProfile = await dbContext.StaffProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

        ManagerOptionDto? manager = null;
        if (staffProfile?.ManagerUserId is not null)
        {
            var managerUser = await userManager.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(entity => entity.Id == staffProfile.ManagerUserId, cancellationToken);

            if (managerUser is not null)
            {
                manager = new ManagerOptionDto(
                    managerUser.Id,
                    managerUser.DisplayName ?? managerUser.UserName ?? managerUser.Email ?? managerUser.Id,
                    managerUser.Email ?? string.Empty);
            }
        }

        var directReports = await GetDirectReportsAsync(userId, cancellationToken);
        var orgTree = await BuildOrgTreeAsync(userId, depth: 0, cancellationToken);

        return new UserOrgDto(
            user.Id,
            user.DisplayName ?? user.UserName ?? user.Email ?? user.Id,
            user.Email ?? string.Empty,
            manager,
            directReports,
            orgTree);
    }

    public async Task<UserDetailDto> CreateFromDriverAsync(
        Guid driverId,
        CreateUserFromDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        var driver = await driverDirectory.GetForUserCreateAsync(driverId, cancellationToken)
            ?? throw new InvalidOperationException("Driver not found.");

        var alreadyLinked = await dbContext.DriverProfileLinks
            .AnyAsync(link => link.DriverId == driverId, cancellationToken);

        if (alreadyLinked)
        {
            throw new InvalidOperationException("This driver already has a linked login account.");
        }

        var username = string.IsNullOrWhiteSpace(request.Username)
            ? DeriveUsername(driver.WorkEmail, driver.DriverCode)
            : request.Username.Trim();

        var existingUsername = await userManager.FindByNameAsync(username);
        if (existingUsername is not null)
        {
            throw new InvalidOperationException($"Username '{username}' is already taken.");
        }

        var password = string.IsNullOrWhiteSpace(request.Password)
            ? null
            : request.Password;

        var sendInvite = request.SendInvite || string.IsNullOrWhiteSpace(password);

        var roles = request.Roles is { Count: > 0 }
            ? request.Roles
            : [AppRoles.Driver];

        var createRequest = new CreateUserRequest(
            username,
            driver.WorkEmail.Trim(),
            password,
            $"{driver.FirstName} {driver.LastName}".Trim(),
            driver.FirstName,
            driver.LastName,
            roles,
            IsActive: true,
            SendInvite: sendInvite,
            Staff: new StaffProfileRequest(
                driver.EmployeeNumber,
                driver.JobTitle,
                driver.Department,
                driver.Branch,
                request.ManagerUserId),
            DriverId: driverId);

        return await CreateAsync(createRequest, cancellationToken);
    }

    public async Task<UserDetailDto?> SendInviteAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        user.InvitePendingAt = DateTimeOffset.UtcNow;
        user.MustChangePassword = true;
        user.EmailConfirmed = false;
        await userManager.UpdateAsync(user);

        await accountEmailService.SendInviteAsync(user, cancellationToken);
        await auditService.LogAsync(
            SecurityAuditActions.UserInviteSent,
            user.Id,
            user.DisplayName ?? user.UserName,
            "Invite resent by administrator.",
            cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task<UserDetailDto> MapDetailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);

        var staffProfile = await dbContext.StaffProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserId == user.Id, cancellationToken);

        var driverLink = await dbContext.DriverProfileLinks
            .AsNoTracking()
            .SingleOrDefaultAsync(link => link.UserId == user.Id, cancellationToken);

        StaffProfileDto? staffDto = staffProfile is null
            ? null
            : await MapStaffProfileDtoAsync(staffProfile, cancellationToken);

        DriverLinkDto? driverDto = null;
        if (driverLink is not null)
        {
            var summaries = await driverDirectory.GetLinkSummariesAsync([driverLink.DriverId], cancellationToken);
            summaries.TryGetValue(driverLink.DriverId, out var summary);
            driverDto = new DriverLinkDto(
                driverLink.DriverId,
                summary?.DriverCode ?? string.Empty,
                summary?.DisplayName ?? string.Empty);
        }

        return new UserDetailDto(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.FirstName,
            user.LastName,
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt,
            user.InvitePendingAt,
            roles.ToList(),
            staffDto,
            driverDto);
    }

    private async Task UpsertStaffProfileAsync(
        string userId,
        StaffProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.StaffProfiles
            .SingleOrDefaultAsync(entity => entity.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = new StaffProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            dbContext.StaffProfiles.Add(profile);
        }

        profile.EmployeeNumber = request.EmployeeNumber?.Trim();
        profile.Branch = request.Branch?.Trim();
        profile.ManagerUserId = string.IsNullOrWhiteSpace(request.ManagerUserId)
            ? null
            : request.ManagerUserId.Trim();

        await ApplyOrganizationAssignmentAsync(profile, request, cancellationToken);

        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyOrganizationAssignmentAsync(
        StaffProfile profile,
        StaffProfileRequest request,
        CancellationToken cancellationToken)
    {
        profile.CompanyId = request.CompanyId;
        profile.DepartmentId = request.DepartmentId;
        profile.PositionId = request.PositionId;

        if (request.PositionId is not null)
        {
            var position = await orgStructure.GetPositionAsync(request.PositionId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Position not found.");

            profile.PositionId = position.Id;
            profile.DepartmentId = position.DepartmentId;
            profile.CompanyId = position.CompanyId;
            profile.JobTitle = position.Name;
            profile.Department = position.DepartmentName;
            return;
        }

        if (request.DepartmentId is not null)
        {
            var department = await orgStructure.GetDepartmentAsync(request.DepartmentId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Department not found.");

            profile.DepartmentId = department.Id;
            profile.CompanyId = department.CompanyId;
            profile.Department = department.Name;
            profile.PositionId = null;
            profile.JobTitle = request.JobTitle?.Trim();
            return;
        }

        if (request.CompanyId is not null)
        {
            var companyName = await orgStructure.GetCompanyNameAsync(request.CompanyId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Company not found.");

            profile.CompanyId = request.CompanyId;
            profile.DepartmentId = null;
            profile.PositionId = null;
            profile.Department = request.Department?.Trim();
            profile.JobTitle = request.JobTitle?.Trim();
            _ = companyName;
            return;
        }

        profile.CompanyId = null;
        profile.DepartmentId = null;
        profile.PositionId = null;
        profile.JobTitle = request.JobTitle?.Trim();
        profile.Department = request.Department?.Trim();
    }

    private async Task<StaffProfileDto> MapStaffProfileDtoAsync(
        StaffProfile staffProfile,
        CancellationToken cancellationToken)
    {
        var org = await ResolveOrgNamesAsync([staffProfile], cancellationToken);
        org.TryGetValue(staffProfile.UserId, out var names);

        string? managerName = null;
        if (staffProfile.ManagerUserId is not null)
        {
            var namesById = await ResolveUserDisplayNamesAsync([staffProfile.ManagerUserId], cancellationToken);
            namesById.TryGetValue(staffProfile.ManagerUserId, out managerName);
        }

        return new(
            staffProfile.EmployeeNumber,
            names?.PositionName ?? staffProfile.JobTitle,
            names?.DepartmentName ?? staffProfile.Department,
            staffProfile.Branch,
            staffProfile.EmploymentStatus,
            staffProfile.WorkStartDate,
            staffProfile.ManagerUserId,
            managerName,
            staffProfile.CompanyId,
            names?.CompanyName,
            staffProfile.DepartmentId,
            names?.DepartmentName ?? staffProfile.Department,
            staffProfile.PositionId,
            names?.PositionName ?? staffProfile.JobTitle);
    }

    private async Task<Dictionary<string, OrgDisplayNames>> ResolveOrgNamesAsync(
        IEnumerable<StaffProfile> profiles,
        CancellationToken cancellationToken)
    {
        var list = profiles.ToList();
        var companyIds = list.Where(p => p.CompanyId.HasValue).Select(p => p.CompanyId!.Value).Distinct().ToList();
        var departmentIds = list.Where(p => p.DepartmentId.HasValue).Select(p => p.DepartmentId!.Value).Distinct().ToList();
        var positionIds = list.Where(p => p.PositionId.HasValue).Select(p => p.PositionId!.Value).Distinct().ToList();

        var companies = companyIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await orgStructure.GetCompanyNamesAsync(companyIds, cancellationToken)).ToDictionary();

        var departments = departmentIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await orgStructure.GetDepartmentNamesAsync(departmentIds, cancellationToken)).ToDictionary();

        var positions = positionIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await orgStructure.GetPositionNamesAsync(positionIds, cancellationToken)).ToDictionary();

        var result = new Dictionary<string, OrgDisplayNames>(StringComparer.Ordinal);
        foreach (var profile in list)
        {
            result[profile.UserId] = new OrgDisplayNames(
                profile.CompanyId is Guid companyId && companies.TryGetValue(companyId, out var companyName)
                    ? companyName
                    : null,
                profile.DepartmentId is Guid departmentId && departments.TryGetValue(departmentId, out var departmentName)
                    ? departmentName
                    : null,
                profile.PositionId is Guid positionId && positions.TryGetValue(positionId, out var positionName)
                    ? positionName
                    : null);
        }

        return result;
    }

    private sealed record OrgDisplayNames(string? CompanyName, string? DepartmentName, string? PositionName);

    private async Task UpsertDriverLinkAsync(
        string userId,
        Guid driverId,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.DriverProfileLinks
            .SingleOrDefaultAsync(link => link.UserId == userId, cancellationToken);

        if (existing is not null)
        {
            existing.DriverId = driverId;
        }
        else
        {
            dbContext.DriverProfileLinks.Add(new DriverProfileLink
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DriverId = driverId,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateDriverLinkAsync(
        string? userId,
        Guid driverId,
        CancellationToken cancellationToken)
    {
        if (!await driverDirectory.ExistsAsync(driverId, cancellationToken))
        {
            throw new InvalidOperationException("Driver not found.");
        }

        var linkedToAnotherUser = await dbContext.DriverProfileLinks
            .AnyAsync(link => link.DriverId == driverId && link.UserId != userId, cancellationToken);

        if (linkedToAnotherUser)
        {
            throw new InvalidOperationException("Driver is already linked to another user.");
        }
    }

    private async Task ValidateManagerAssignmentAsync(
        string? userId,
        string managerUserId,
        CancellationToken cancellationToken)
    {
        if (userId is not null && userId == managerUserId)
        {
            throw new InvalidOperationException("A user cannot be their own manager.");
        }

        var manager = await userManager.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == managerUserId, cancellationToken);

        if (manager is null || !manager.IsActive)
        {
            throw new InvalidOperationException("Manager user not found or inactive.");
        }

        if (userId is null)
        {
            return;
        }

        var visited = new HashSet<string>(StringComparer.Ordinal) { userId };
        var currentManagerId = managerUserId;

        while (!string.IsNullOrWhiteSpace(currentManagerId))
        {
            if (!visited.Add(currentManagerId))
            {
                throw new InvalidOperationException("Manager assignment would create a cycle.");
            }

            var profile = await dbContext.StaffProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(entity => entity.UserId == currentManagerId, cancellationToken);

            currentManagerId = profile?.ManagerUserId ?? string.Empty;
        }
    }

    private async Task<OrgChartNodeDto> BuildOrgTreeAsync(
        string userId,
        int depth,
        CancellationToken cancellationToken)
    {
        if (depth > 12)
        {
            return new OrgChartNodeDto(userId, "…", null, null, []);
        }

        var user = await userManager.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);

        var profile = await dbContext.StaffProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.UserId == userId, cancellationToken);

        var reportIds = await orgDirectory.GetDirectReportUserIdsAsync(userId, cancellationToken);
        var children = new List<OrgChartNodeDto>();
        foreach (var reportId in reportIds)
        {
            children.Add(await BuildOrgTreeAsync(reportId, depth + 1, cancellationToken));
        }

        return new OrgChartNodeDto(
            userId,
            user?.DisplayName ?? user?.UserName ?? userId,
            profile?.JobTitle,
            profile?.Department,
            children);
    }

    private static string GenerateSecurePassword() =>
        $"{Convert.ToBase64String(RandomNumberGenerator.GetBytes(18))}Aa1!";

    private static string DeriveUsername(string workEmail, string driverCode)
    {
        if (!string.IsNullOrWhiteSpace(workEmail) && workEmail.Contains('@'))
        {
            return workEmail.Split('@')[0].Trim().ToLowerInvariant();
        }

        return driverCode.Replace("-", string.Empty).ToLowerInvariant();
    }

    private static void ValidateRoles(IReadOnlyList<string> roles)
    {
        if (roles.Count == 0)
        {
            throw new InvalidOperationException("At least one role is required.");
        }

        foreach (var role in roles)
        {
            if (!AppRoles.All.Contains(role, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Unknown role: {role}");
            }
        }
    }

    private async Task<Dictionary<string, string>> ResolveUserDisplayNamesAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return await userManager.Users
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .ToDictionaryAsync(
                user => user.Id,
                user => user.DisplayName ?? user.UserName ?? user.Email ?? user.Id,
                StringComparer.Ordinal,
                cancellationToken);
    }
}
