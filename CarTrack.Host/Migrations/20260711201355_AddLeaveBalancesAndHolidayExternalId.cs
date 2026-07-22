using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Ownership of LeaveBalances / PublicHolidays columns moved to
    /// CarTrack.Modules.Leave / LeaveDbContext. Up/Down intentionally empty.
    /// </remarks>
    public partial class AddLeaveBalancesAndHolidayExternalId : Migration
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
