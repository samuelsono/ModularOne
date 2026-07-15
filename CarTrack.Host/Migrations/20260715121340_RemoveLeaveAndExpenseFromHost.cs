using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Ownership of LeaveTypes / PublicHolidays / LeaveBalances / LeaveRequests moved to
    /// CarTrack.Modules.Leave / LeaveDbContext; ExpenseCategories / ExpenseClaims moved to
    /// CarTrack.Modules.Expense / ExpenseDbContext.
    /// Up/Down are intentionally empty: removing Leave/Expense entities from the host
    /// model must not DropTable — the physical tables are now owned by the module
    /// DbContext migrations (and Notifications tables, if still present in the prior
    /// host snapshot, remain owned by NotificationsDbContext).
    /// </remarks>
    public partial class RemoveLeaveAndExpenseFromHost : Migration
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
