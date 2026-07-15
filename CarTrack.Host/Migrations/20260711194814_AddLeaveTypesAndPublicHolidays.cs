using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Ownership of LeaveTypes / PublicHolidays (and LeaveRequests LeaveTypeId)
    /// moved to CarTrack.Modules.Leave / LeaveDbContext. Up/Down are intentionally
    /// empty so fresh installs do not create these tables in the host pipeline.
    /// </remarks>
    public partial class AddLeaveTypesAndPublicHolidays : Migration
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
