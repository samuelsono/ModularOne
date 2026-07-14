namespace CarTrack.Server.CoreHr;

public record CompanyDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    int SortOrder,
    string CreatedAt,
    string? CreatedByUserId,
    string? CreatedByDisplayName,
    string UpdatedAt,
    string? UpdatedByUserId,
    string? UpdatedByDisplayName);

public record DepartmentDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    int SortOrder,
    string CreatedAt,
    string? CreatedByUserId,
    string? CreatedByDisplayName,
    string UpdatedAt,
    string? UpdatedByUserId,
    string? UpdatedByDisplayName);

public record PositionDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    Guid CompanyId,
    string CompanyName,
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    int SortOrder,
    string CreatedAt,
    string? CreatedByUserId,
    string? CreatedByDisplayName,
    string UpdatedAt,
    string? UpdatedByUserId,
    string? UpdatedByDisplayName);

public record SaveCompanyRequest(
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    int SortOrder);

public record SaveDepartmentRequest(
    Guid CompanyId,
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    int SortOrder);

public record SavePositionRequest(
    Guid DepartmentId,
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    int SortOrder);
