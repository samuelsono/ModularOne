namespace CarTrack.Modules.Users;

public interface IUserService
{
    Task<UserListResponse> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<UserDetailDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<UserDetailDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserDetailDto?> UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserDetailDto?> SetRolesAsync(string id, SetUserRolesRequest request, CancellationToken cancellationToken = default);

    Task<UserDetailDto?> SetActiveAsync(string id, SetUserActiveRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ManagerOptionDto>> GetManagerOptionsAsync(
        string? excludeUserId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleSummaryDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DirectReportDto>> GetDirectReportsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<UserOrgDto?> GetOrgAsync(string userId, CancellationToken cancellationToken = default);

    Task<UserDetailDto> CreateFromDriverAsync(
        Guid driverId,
        CreateUserFromDriverRequest request,
        CancellationToken cancellationToken = default);

    Task<UserDetailDto?> SendInviteAsync(string id, CancellationToken cancellationToken = default);
}
