using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseCategoriesAndClaimFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "ExpenseClaims",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpenseDate",
                table: "ExpenseClaims",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ExpenseClaims",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExpenseCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseClaims_CategoryId",
                table: "ExpenseClaims",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseClaims_RequesterUserId_ExpenseDate",
                table: "ExpenseClaims",
                columns: new[] { "RequesterUserId", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_Code",
                table: "ExpenseCategories",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseClaims_ExpenseCategories_CategoryId",
                table: "ExpenseClaims",
                column: "CategoryId",
                principalTable: "ExpenseCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseClaims_ExpenseCategories_CategoryId",
                table: "ExpenseClaims");

            migrationBuilder.DropTable(
                name: "ExpenseCategories");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseClaims_CategoryId",
                table: "ExpenseClaims");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseClaims_RequesterUserId_ExpenseDate",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "ExpenseDate",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ExpenseClaims");
        }
    }
}
