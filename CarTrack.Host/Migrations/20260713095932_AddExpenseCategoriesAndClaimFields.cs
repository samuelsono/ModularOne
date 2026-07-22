using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Ownership of ExpenseCategories / ExpenseClaims fields moved to
    /// CarTrack.Modules.Expense / ExpenseDbContext. Up/Down intentionally empty.
    /// </remarks>
    public partial class AddExpenseCategoriesAndClaimFields : Migration
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
