using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Ownership of Companies / Departments / Positions moved to
    /// CarTrack.Modules.CoreHr / CoreHrDbContext.
    /// Up/Down are intentionally empty: removing CoreHr entities from the host
    /// model must not DropTable — the physical tables are now owned by
    /// CoreHrDbContext migrations. StaffProfiles retains opaque
    /// CompanyId / DepartmentId / PositionId columns without cross-module FKs.
    /// </remarks>
    public partial class RemoveCoreHrFromHost : Migration
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
