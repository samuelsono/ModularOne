using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveTypesAndPublicHolidays : Migration
    {
        private static readonly Guid AnnualTypeId = Guid.Parse("11111111-1111-1111-1111-111111111101");
        private static readonly Guid SickTypeId = Guid.Parse("11111111-1111-1111-1111-111111111102");
        private static readonly Guid FamilyTypeId = Guid.Parse("11111111-1111-1111-1111-111111111103");
        private static readonly Guid UnpaidTypeId = Guid.Parse("11111111-1111-1111-1111-111111111104");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaveTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    DeductsBalance = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresDocument = table.Column<bool>(type: "boolean", nullable: false),
                    AllowHalfDay = table.Column<bool>(type: "boolean", nullable: false),
                    MaxConsecutiveDays = table.Column<int>(type: "integer", nullable: true),
                    MinNoticeDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicHolidays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    Branch = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicHolidays", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "LeaveTypes",
                columns: new[] { "Id", "Name", "Code", "Color", "IsPaid", "DeductsBalance", "RequiresDocument", "AllowHalfDay", "MaxConsecutiveDays", "MinNoticeDays", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { AnnualTypeId, "Annual", "ANNUAL", "#0078D4", true, true, false, true, null, 7, true, 1 },
                    { SickTypeId, "Sick", "SICK", "#D13438", true, true, true, true, null, 0, true, 2 },
                    { FamilyTypeId, "Family", "FAMILY", "#8764B8", true, true, false, false, 3, 14, true, 3 },
                    { UnpaidTypeId, "Unpaid", "UNPAID", "#605E5C", false, false, false, true, null, 7, true, 4 },
                });

            migrationBuilder.AddColumn<Guid>(
                name: "LeaveTypeId",
                table: "LeaveRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "LeaveRequests" AS lr
                SET "LeaveTypeId" = lt."Id"
                FROM "LeaveTypes" AS lt
                WHERE lr."LeaveType" = lt."Name"
                   OR UPPER(lr."LeaveType") = lt."Code";
                """);

            migrationBuilder.Sql(
                $"""
                UPDATE "LeaveRequests"
                SET "LeaveTypeId" = '{AnnualTypeId}'
                WHERE "LeaveTypeId" IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "LeaveType",
                table: "LeaveRequests");

            migrationBuilder.AlterColumn<Guid>(
                name: "LeaveTypeId",
                table: "LeaveRequests",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_Code",
                table: "LeaveTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidays_Date",
                table: "PublicHolidays",
                column: "Date");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId",
                principalTable: "LeaveTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                table: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "LeaveTypes");

            migrationBuilder.DropTable(
                name: "PublicHolidays");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "LeaveTypeId",
                table: "LeaveRequests");

            migrationBuilder.AddColumn<string>(
                name: "LeaveType",
                table: "LeaveRequests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Annual");
        }
    }
}
