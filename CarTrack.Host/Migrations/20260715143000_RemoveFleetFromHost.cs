using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Ownership of Vehicles / Drivers / CarTrackPageCache moved to
    /// CarTrack.Modules.Fleet / FleetDbContext.
    /// Up/Down are intentionally empty: removing Fleet entities from the host
    /// model must not DropTable — the physical tables are now owned by the module
    /// DbContext migrations.
    /// </remarks>
    public partial class RemoveFleetFromHost : Migration
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
