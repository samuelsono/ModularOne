namespace CarTrack.Modules.CoreHr;

public interface ICoreHrService
{
    Task<IReadOnlyList<CompanyDto>> GetCompaniesAsync(CancellationToken cancellationToken = default);

    Task<CompanyDto> CreateCompanyAsync(SaveCompanyRequest request, CancellationToken cancellationToken = default);

    Task<CompanyDto?> UpdateCompanyAsync(Guid id, SaveCompanyRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);

    Task<DepartmentDto> CreateDepartmentAsync(SaveDepartmentRequest request, CancellationToken cancellationToken = default);

    Task<DepartmentDto?> UpdateDepartmentAsync(Guid id, SaveDepartmentRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PositionDto>> GetPositionsAsync(CancellationToken cancellationToken = default);

    Task<PositionDto> CreatePositionAsync(SavePositionRequest request, CancellationToken cancellationToken = default);

    Task<PositionDto?> UpdatePositionAsync(Guid id, SavePositionRequest request, CancellationToken cancellationToken = default);
}
