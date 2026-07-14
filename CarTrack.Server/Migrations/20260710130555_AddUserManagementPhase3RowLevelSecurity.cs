using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddUserManagementPhase3RowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedDriverId",
                table: "Vehicles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequesterBranch",
                table: "LeaveRequests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequesterBranch",
                table: "ExpenseClaims",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CarTrackDriverId",
                table: "Drivers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_AssignedDriverId",
                table: "Vehicles",
                column: "AssignedDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_CarTrackDriverId",
                table: "Drivers",
                column: "CarTrackDriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Drivers_AssignedDriverId",
                table: "Vehicles",
                column: "AssignedDriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Drivers_AssignedDriverId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_AssignedDriverId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_CarTrackDriverId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "AssignedDriverId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "RequesterBranch",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "RequesterBranch",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "CarTrackDriverId",
                table: "Drivers");
        }
    }
}
