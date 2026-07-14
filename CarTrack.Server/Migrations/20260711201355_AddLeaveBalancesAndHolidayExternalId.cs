using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveBalancesAndHolidayExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "PublicHolidays",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WorkingDays",
                table: "LeaveRequests",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "LeaveBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LeaveTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleStart = table.Column<DateOnly>(type: "date", nullable: false),
                    CycleEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    Allocated = table.Column<decimal>(type: "numeric", nullable: false),
                    Used = table.Column<decimal>(type: "numeric", nullable: false),
                    Pending = table.Column<decimal>(type: "numeric", nullable: false),
                    Adjusted = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveBalances_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeaveBalances_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidays_ExternalId",
                table: "PublicHolidays",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_LeaveTypeId",
                table: "LeaveBalances",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_UserId_LeaveTypeId_CycleStart_CycleEnd",
                table: "LeaveBalances",
                columns: new[] { "UserId", "LeaveTypeId", "CycleStart", "CycleEnd" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaveBalances");

            migrationBuilder.DropIndex(
                name: "IX_PublicHolidays_ExternalId",
                table: "PublicHolidays");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "PublicHolidays");

            migrationBuilder.DropColumn(
                name: "WorkingDays",
                table: "LeaveRequests");
        }
    }
}
