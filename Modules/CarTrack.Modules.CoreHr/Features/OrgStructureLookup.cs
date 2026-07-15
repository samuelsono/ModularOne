using CarTrack.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.CoreHr;

public sealed class OrgStructureLookup(CoreHrDbContext dbContext) : IOrgStructureLookup
{
    public async Task<OrgPositionInfo?> GetPositionAsync(Guid positionId, CancellationToken cancellationToken = default)
    {
        var position = await dbContext.Positions
            .AsNoTracking()
            .Include(item => item.Department)
            .SingleOrDefaultAsync(item => item.Id == positionId, cancellationToken);

        return position is null
            ? null
            : new OrgPositionInfo(
                position.Id,
                position.DepartmentId,
                position.Department.CompanyId,
                position.Name,
                position.Department.Name);
    }

    public async Task<OrgDepartmentInfo?> GetDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        var department = await dbContext.Departments
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == departmentId, cancellationToken);

        return department is null
            ? null
            : new OrgDepartmentInfo(department.Id, department.CompanyId, department.Name);
    }

    public async Task<string?> GetCompanyNameAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await dbContext.Companies
            .AsNoTracking()
            .Where(company => company.Id == companyId)
            .Select(company => company.Name)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>> GetCompanyNamesAsync(
        IEnumerable<Guid> companyIds,
        CancellationToken cancellationToken = default)
    {
        var ids = companyIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await dbContext.Companies
            .AsNoTracking()
            .Where(company => ids.Contains(company.Id))
            .ToDictionaryAsync(company => company.Id, company => company.Name, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetDepartmentNamesAsync(
        IEnumerable<Guid> departmentIds,
        CancellationToken cancellationToken = default)
    {
        var ids = departmentIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await dbContext.Departments
            .AsNoTracking()
            .Where(department => ids.Contains(department.Id))
            .ToDictionaryAsync(department => department.Id, department => department.Name, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetPositionNamesAsync(
        IEnumerable<Guid> positionIds,
        CancellationToken cancellationToken = default)
    {
        var ids = positionIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await dbContext.Positions
            .AsNoTracking()
            .Where(position => ids.Contains(position.Id))
            .ToDictionaryAsync(position => position.Id, position => position.Name, cancellationToken);
    }
}
