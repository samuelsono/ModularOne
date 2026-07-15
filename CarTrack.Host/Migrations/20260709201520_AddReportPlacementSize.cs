using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddReportPlacementSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "ReportPlacements",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Medium");

            migrationBuilder.Sql("""
                UPDATE "ReportPlacements" AS placement
                SET "Size" = report."Size"
                FROM "ReportDefinitions" AS report
                WHERE placement."ReportId" = report."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Size",
                table: "ReportPlacements");
        }
    }
}
