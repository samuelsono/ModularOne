using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Companies/Departments/Positions owned by CoreHrDbContext.
    /// StaffProfiles CompanyId/DepartmentId/PositionId owned by UsersDbContext
    /// (included in InitialUsers). Up/Down emptied for fresh installs.
    /// </remarks>
    public partial class AddCoreHrOrganizationStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
