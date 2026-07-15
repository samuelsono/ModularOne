namespace CarTrack.Modules.Users;

public record DirectReportDto(
    string Id,
    string DisplayName,
    string Email,
    string? JobTitle,
    string? Department,
    bool IsActive);

public record OrgChartNodeDto(
    string Id,
    string DisplayName,
    string? JobTitle,
    string? Department,
    IReadOnlyList<OrgChartNodeDto> Reports);

public record UserOrgDto(
    string Id,
    string DisplayName,
    string Email,
    ManagerOptionDto? Manager,
    IReadOnlyList<DirectReportDto> DirectReports,
    OrgChartNodeDto OrgTree);

public record CreateUserFromDriverRequest(
    string? Username,
    string? Password,
    IReadOnlyList<string>? Roles,
    string? ManagerUserId,
    bool SendInvite = false);
