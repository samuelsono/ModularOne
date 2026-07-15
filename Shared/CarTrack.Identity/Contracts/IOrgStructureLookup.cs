namespace CarTrack.Identity.Contracts;

/// <summary>
/// Cross-module CoreHr org structure resolution without referencing CoreHr types.
/// Implemented by the CoreHr module.
/// </summary>
public interface IOrgStructureLookup
{
    Task<OrgPositionInfo?> GetPositionAsync(Guid positionId, CancellationToken cancellationToken = default);

    Task<OrgDepartmentInfo?> GetDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);

    Task<string?> GetCompanyNameAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, string>> GetCompanyNamesAsync(
        IEnumerable<Guid> companyIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, string>> GetDepartmentNamesAsync(
        IEnumerable<Guid> departmentIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, string>> GetPositionNamesAsync(
        IEnumerable<Guid> positionIds,
        CancellationToken cancellationToken = default);
}

public sealed record OrgPositionInfo(
    Guid Id,
    Guid DepartmentId,
    Guid CompanyId,
    string Name,
    string DepartmentName);

public sealed record OrgDepartmentInfo(
    Guid Id,
    Guid CompanyId,
    string Name);
