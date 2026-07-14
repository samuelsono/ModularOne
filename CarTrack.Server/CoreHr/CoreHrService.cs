using CarTrack.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.CoreHr;

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

public class CoreHrService(ApplicationDbContext dbContext) : ICoreHrService
{
    public async Task<IReadOnlyList<CompanyDto>> GetCompaniesAsync(CancellationToken cancellationToken = default)
    {
        var companies = await dbContext.Companies
            .AsNoTracking()
            .Include(company => company.CreatedByUser)
            .Include(company => company.UpdatedByUser)
            .OrderBy(company => company.SortOrder)
            .ThenBy(company => company.Name)
            .ToListAsync(cancellationToken);

        return companies.Select(MapCompany).ToList();
    }

    public async Task<CompanyDto> CreateCompanyAsync(
        SaveCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateNameAndCode(request.Name, request.Code);

        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Companies.AnyAsync(company => company.Code == code, cancellationToken))
        {
            throw new InvalidOperationException($"Company code '{code}' already exists.");
        }

        var entity = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = code,
            Description = NormalizeDescription(request.Description),
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
        };

        dbContext.Companies.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapCompany(entity);
    }

    public async Task<CompanyDto?> UpdateCompanyAsync(
        Guid id,
        SaveCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateNameAndCode(request.Name, request.Code);

        var entity = await dbContext.Companies.FirstOrDefaultAsync(company => company.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Companies.AnyAsync(
                company => company.Id != id && company.Code == code,
                cancellationToken))
        {
            throw new InvalidOperationException($"Company code '{code}' already exists.");
        }

        entity.Name = request.Name.Trim();
        entity.Code = code;
        entity.Description = NormalizeDescription(request.Description);
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapCompany(entity);
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var departments = await dbContext.Departments
            .AsNoTracking()
            .Include(department => department.Company)
            .Include(department => department.CreatedByUser)
            .Include(department => department.UpdatedByUser)
            .OrderBy(department => department.SortOrder)
            .ThenBy(department => department.Name)
            .ToListAsync(cancellationToken);

        return departments.Select(MapDepartment).ToList();
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(
        SaveDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateNameAndCode(request.Name, request.Code);
        await EnsureCompanyExistsAsync(request.CompanyId, cancellationToken);

        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Departments.AnyAsync(
                department => department.CompanyId == request.CompanyId && department.Code == code,
                cancellationToken))
        {
            throw new InvalidOperationException($"Department code '{code}' already exists for this company.");
        }

        var entity = new Department
        {
            Id = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            Name = request.Name.Trim(),
            Code = code,
            Description = NormalizeDescription(request.Description),
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
        };

        dbContext.Departments.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        entity.Company = await dbContext.Companies
            .AsNoTracking()
            .SingleAsync(company => company.Id == entity.CompanyId, cancellationToken);

        return MapDepartment(entity);
    }

    public async Task<DepartmentDto?> UpdateDepartmentAsync(
        Guid id,
        SaveDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateNameAndCode(request.Name, request.Code);
        await EnsureCompanyExistsAsync(request.CompanyId, cancellationToken);

        var entity = await dbContext.Departments
            .Include(department => department.Company)
            .FirstOrDefaultAsync(department => department.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Departments.AnyAsync(
                department => department.Id != id
                    && department.CompanyId == request.CompanyId
                    && department.Code == code,
                cancellationToken))
        {
            throw new InvalidOperationException($"Department code '{code}' already exists for this company.");
        }

        entity.CompanyId = request.CompanyId;
        entity.Name = request.Name.Trim();
        entity.Code = code;
        entity.Description = NormalizeDescription(request.Description);
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;

        await dbContext.SaveChangesAsync(cancellationToken);
        entity.Company = await dbContext.Companies
            .AsNoTracking()
            .SingleAsync(company => company.Id == entity.CompanyId, cancellationToken);

        return MapDepartment(entity);
    }

    public async Task<IReadOnlyList<PositionDto>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        var positions = await dbContext.Positions
            .AsNoTracking()
            .Include(position => position.Department)
            .ThenInclude(department => department.Company)
            .Include(position => position.CreatedByUser)
            .Include(position => position.UpdatedByUser)
            .OrderBy(position => position.SortOrder)
            .ThenBy(position => position.Name)
            .ToListAsync(cancellationToken);

        return positions.Select(MapPosition).ToList();
    }

    public async Task<PositionDto> CreatePositionAsync(
        SavePositionRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateNameAndCode(request.Name, request.Code);
        var department = await EnsureDepartmentExistsAsync(request.DepartmentId, cancellationToken);

        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Positions.AnyAsync(
                position => position.DepartmentId == request.DepartmentId && position.Code == code,
                cancellationToken))
        {
            throw new InvalidOperationException($"Position code '{code}' already exists for this department.");
        }

        var entity = new Position
        {
            Id = Guid.NewGuid(),
            DepartmentId = request.DepartmentId,
            Name = request.Name.Trim(),
            Code = code,
            Description = NormalizeDescription(request.Description),
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
        };

        dbContext.Positions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        entity.Department = department;
        return MapPosition(entity);
    }

    public async Task<PositionDto?> UpdatePositionAsync(
        Guid id,
        SavePositionRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateNameAndCode(request.Name, request.Code);
        var department = await EnsureDepartmentExistsAsync(request.DepartmentId, cancellationToken);

        var entity = await dbContext.Positions
            .Include(position => position.Department)
            .ThenInclude(dept => dept.Company)
            .FirstOrDefaultAsync(position => position.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Positions.AnyAsync(
                position => position.Id != id
                    && position.DepartmentId == request.DepartmentId
                    && position.Code == code,
                cancellationToken))
        {
            throw new InvalidOperationException($"Position code '{code}' already exists for this department.");
        }

        entity.DepartmentId = request.DepartmentId;
        entity.Name = request.Name.Trim();
        entity.Code = code;
        entity.Description = NormalizeDescription(request.Description);
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;

        await dbContext.SaveChangesAsync(cancellationToken);
        entity.Department = department;
        return MapPosition(entity);
    }

    private static void ValidateNameAndCode(string name, string code)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("Code is required.");
        }
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private async Task EnsureCompanyExistsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Companies.AnyAsync(company => company.Id == companyId, cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException("Company not found.");
        }
    }

    private async Task<Department> EnsureDepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        var department = await dbContext.Departments
            .Include(dept => dept.Company)
            .SingleOrDefaultAsync(dept => dept.Id == departmentId, cancellationToken);

        if (department is null)
        {
            throw new InvalidOperationException("Department not found.");
        }

        return department;
    }

    private static CompanyDto MapCompany(Company company) =>
        new(
            company.Id,
            company.Name,
            company.Code,
            company.Description,
            company.IsActive,
            company.SortOrder,
            AuditableMapping.FormatTimestamp(company.CreatedAt),
            company.CreatedByUserId,
            AuditableMapping.UserDisplayName(company.CreatedByUser),
            AuditableMapping.FormatTimestamp(company.UpdatedAt == default ? company.CreatedAt : company.UpdatedAt),
            company.UpdatedByUserId,
            AuditableMapping.UserDisplayName(company.UpdatedByUser));

    private static DepartmentDto MapDepartment(Department department) =>
        new(
            department.Id,
            department.CompanyId,
            department.Company.Name,
            department.Name,
            department.Code,
            department.Description,
            department.IsActive,
            department.SortOrder,
            AuditableMapping.FormatTimestamp(department.CreatedAt),
            department.CreatedByUserId,
            AuditableMapping.UserDisplayName(department.CreatedByUser),
            AuditableMapping.FormatTimestamp(department.UpdatedAt == default ? department.CreatedAt : department.UpdatedAt),
            department.UpdatedByUserId,
            AuditableMapping.UserDisplayName(department.UpdatedByUser));

    private static PositionDto MapPosition(Position position) =>
        new(
            position.Id,
            position.DepartmentId,
            position.Department.Name,
            position.Department.CompanyId,
            position.Department.Company.Name,
            position.Name,
            position.Code,
            position.Description,
            position.IsActive,
            position.SortOrder,
            AuditableMapping.FormatTimestamp(position.CreatedAt),
            position.CreatedByUserId,
            AuditableMapping.UserDisplayName(position.CreatedByUser),
            AuditableMapping.FormatTimestamp(position.UpdatedAt == default ? position.CreatedAt : position.UpdatedAt),
            position.UpdatedByUserId,
            AuditableMapping.UserDisplayName(position.UpdatedByUser));
}
