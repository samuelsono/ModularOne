using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.CoreHr;

public static class CoreHrSeeder
{
    public static readonly Guid DefaultCompanyId = Guid.Parse("33333333-3333-3333-3333-333333333301");
    public static readonly Guid OperationsDepartmentId = Guid.Parse("33333333-3333-3333-3333-333333333302");
    public static readonly Guid FinanceDepartmentId = Guid.Parse("33333333-3333-3333-3333-333333333303");
    public static readonly Guid FleetManagerPositionId = Guid.Parse("33333333-3333-3333-3333-333333333304");
    public static readonly Guid AccountantPositionId = Guid.Parse("33333333-3333-3333-3333-333333333305");

    public static async Task SeedAsync(CoreHrDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Companies.AnyAsync(cancellationToken))
        {
            dbContext.Companies.Add(new Company
            {
                Id = DefaultCompanyId,
                Name = "CarTrack",
                Code = "CARTRACK",
                Description = "Default company",
                IsActive = true,
                SortOrder = 1,
            });

            dbContext.Departments.AddRange(
                new Department
                {
                    Id = OperationsDepartmentId,
                    CompanyId = DefaultCompanyId,
                    Name = "Operations",
                    Code = "OPS",
                    Description = "Fleet and field operations",
                    IsActive = true,
                    SortOrder = 1,
                },
                new Department
                {
                    Id = FinanceDepartmentId,
                    CompanyId = DefaultCompanyId,
                    Name = "Finance",
                    Code = "FIN",
                    Description = "Finance and payroll",
                    IsActive = true,
                    SortOrder = 2,
                });

            dbContext.Positions.AddRange(
                new Position
                {
                    Id = FleetManagerPositionId,
                    DepartmentId = OperationsDepartmentId,
                    Name = "Fleet Manager",
                    Code = "FLEET_MGR",
                    Description = "Manages fleet operations",
                    IsActive = true,
                    SortOrder = 1,
                },
                new Position
                {
                    Id = AccountantPositionId,
                    DepartmentId = FinanceDepartmentId,
                    Name = "Accountant",
                    Code = "ACCOUNTANT",
                    Description = "Handles accounting workflows",
                    IsActive = true,
                    SortOrder = 1,
                });

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
