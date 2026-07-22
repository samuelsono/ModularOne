using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Ownership of LeaveRequests / ExpenseClaims moved to CarTrack.Modules.Leave /
    /// CarTrack.Modules.Expense. Up/Down are intentionally empty so fresh installs
    /// do not create these tables in the host migration pipeline (modules migrate
    /// first and own the tables). Databases that already applied this migration
    /// keep their existing tables.
    /// </remarks>
    public partial class AddUserManagementPhase2 : Migration
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
