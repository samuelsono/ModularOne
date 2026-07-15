using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Leave accrual / half-day columns are owned by CarTrack.Modules.Leave /
    /// LeaveDbContext. Up/Down intentionally empty.
    /// </remarks>
    public partial class AddLeaveAccrualAndHalfDayLeave : Migration
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
