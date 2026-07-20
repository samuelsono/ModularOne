namespace CarTrack.Modules.Users;

public record StaffProfileDto(
    string? EmployeeNumber,
    string? JobTitle,
    string? Department,
    string? Branch,
    string Gender,
    string EmploymentStatus,
    DateOnly? WorkStartDate,
    string? ManagerUserId,
    string? ManagerDisplayName,
    Guid? CompanyId,
    string? CompanyName,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? PositionId,
    string? PositionName);

public record DriverLinkDto(Guid DriverId, string DriverCode, string DriverName);

public record UserListItemDto(
    string Id,
    string Username,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles,
    bool IsActive,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset? InvitePendingAt,
    string? ManagerDisplayName,
    Guid? LinkedDriverId,
    string? CompanyName,
    string? DepartmentName,
    string? PositionName,
    string? CreatedAt,
    string? CreatedByUserId,
    string? CreatedByDisplayName,
    string? UpdatedAt,
    string? UpdatedByUserId,
    string? UpdatedByDisplayName);

public record UserDetailDto(
    string Id,
    string Username,
    string Email,
    string? DisplayName,
    string? FirstName,
    string? LastName,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset? InvitePendingAt,
    IReadOnlyList<string> Roles,
    StaffProfileDto? StaffProfile,
    DriverLinkDto? DriverLink);

public record ManagerOptionDto(string Id, string DisplayName, string Email);

public record RoleSummaryDto(string Name, IReadOnlyList<string> PermissionKeys);

public record PermissionDto(
    Guid Id,
    string Key,
    string ModuleSlug,
    string SubmoduleSlug,
    string Action,
    string? Description);

public record StaffProfileRequest(
    string? EmployeeNumber,
    string? JobTitle,
    string? Department,
    string? Branch,
    string? ManagerUserId,
    Guid? CompanyId = null,
    Guid? DepartmentId = null,
    Guid? PositionId = null,
    string? Gender = null);

public record CreateUserRequest(
    string Username,
    string Email,
    string? Password,
    string? DisplayName,
    string? FirstName,
    string? LastName,
    IReadOnlyList<string> Roles,
    bool IsActive = true,
    bool SendInvite = false,
    StaffProfileRequest? Staff = null,
    Guid? DriverId = null);

public record UpdateUserRequest(
    string Email,
    string? DisplayName,
    string? FirstName,
    string? LastName,
    StaffProfileRequest? Staff = null,
    Guid? DriverId = null,
    bool ClearDriverLink = false);

public record SetUserRolesRequest(IReadOnlyList<string> Roles);

public record SetUserActiveRequest(bool IsActive);

public record UserListResponse(IReadOnlyList<UserListItemDto> Items);
