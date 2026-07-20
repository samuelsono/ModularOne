using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Modules.Expense.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpensePolicyAndSettingsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "KilometersTravelled",
                table: "ExpenseClaims",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MileageRatePerKilometer",
                table: "ExpenseClaims",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptContentType",
                table: "ExpenseClaims",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptFileName",
                table: "ExpenseClaims",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptStoredPath",
                table: "ExpenseClaims",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReceiptUploadedAt",
                table: "ExpenseClaims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TravelDestination",
                table: "ExpenseClaims",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TravelStartPoint",
                table: "ExpenseClaims",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TravelWaypointsJson",
                table: "ExpenseClaims",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PaysByKilometer",
                table: "ExpenseCategories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresReceipt",
                table: "ExpenseCategories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresTravelDetails",
                table: "ExpenseCategories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ExpenseSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    KilometerRate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseSettings");

            migrationBuilder.DropColumn(
                name: "KilometersTravelled",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "MileageRatePerKilometer",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "ReceiptContentType",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "ReceiptFileName",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "ReceiptStoredPath",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "ReceiptUploadedAt",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "TravelDestination",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "TravelStartPoint",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "TravelWaypointsJson",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "PaysByKilometer",
                table: "ExpenseCategories");

            migrationBuilder.DropColumn(
                name: "RequiresReceipt",
                table: "ExpenseCategories");

            migrationBuilder.DropColumn(
                name: "RequiresTravelDetails",
                table: "ExpenseCategories");
        }
    }
}
