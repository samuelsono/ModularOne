using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveAccrualAndHalfDayLeave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccrualMethod",
                table: "LeaveTypes",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualEntitlement",
                table: "LeaveTypes",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndDayPortion",
                table: "LeaveRequests",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StartDayPortion",
                table: "LeaveRequests",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "LastAccruedYearMonth",
                table: "LeaveBalances",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccrualMethod",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "AnnualEntitlement",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "EndDayPortion",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "StartDayPortion",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "LastAccruedYearMonth",
                table: "LeaveBalances");
        }
    }
}
