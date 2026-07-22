using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Vehicles/Drivers auditable columns are owned by FleetDbContext;
    /// StaffProfiles auditable columns by UsersDbContext.
    /// Up/Down intentionally empty so fresh installs do not alter those tables
    /// in the host pipeline.
    /// </remarks>
    public partial class CompleteIAuditableColumns : Migration
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
