using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseClaimWorkflowStages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaidAt",
                table: "ExpenseClaims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaidByUserId",
                table: "ExpenseClaims",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedAt",
                table: "ExpenseClaims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "ExpenseClaims"
                SET "Status" = 'PendingManager'
                WHERE "Status" = 'Pending';

                UPDATE "ExpenseClaims"
                SET "Status" = 'PendingPayment'
                WHERE "Status" = 'Approved';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "ExpenseClaims"
                SET "Status" = 'Pending'
                WHERE "Status" IN ('PendingManager', 'PendingFinance');

                UPDATE "ExpenseClaims"
                SET "Status" = 'Approved'
                WHERE "Status" IN ('PendingPayment', 'Paid');
                """);

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "PaidByUserId",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "ExpenseClaims");
        }
    }
}
