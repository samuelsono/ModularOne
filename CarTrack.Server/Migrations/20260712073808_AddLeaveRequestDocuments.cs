using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveRequestDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentContentType",
                table: "LeaveRequests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentFileName",
                table: "LeaveRequests",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentPath",
                table: "LeaveRequests",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentContentType",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "DocumentFileName",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "DocumentPath",
                table: "LeaveRequests");
        }
    }
}
