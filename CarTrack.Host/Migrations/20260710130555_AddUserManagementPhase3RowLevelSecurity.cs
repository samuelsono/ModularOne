using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Ownership of Vehicles / Drivers / CarTrackPageCache moved to
    /// CarTrack.Modules.Fleet / FleetDbContext. Up/Down are intentionally empty
    /// so fresh installs do not create these tables in the host pipeline.
    /// </remarks>
    public partial class AddUserManagementPhase3RowLevelSecurity : Migration
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
