using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddReportPlacements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportPlacements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportPlacements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportPlacements_DashboardSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "DashboardSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportPlacements_ReportDefinitions_ReportId",
                        column: x => x.ReportId,
                        principalTable: "ReportDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO "ReportPlacements" ("Id", "ReportId", "SectionId", "SortOrder", "IsVisible", "CreatedAt", "UpdatedAt")
                SELECT gen_random_uuid(), "Id", "SectionId", "SortOrder", "IsVisible", "CreatedAt", "UpdatedAt"
                FROM "ReportDefinitions"
                WHERE "SectionId" IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_ReportDefinitions_DashboardSections_SectionId",
                table: "ReportDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ReportDefinitions_SectionId_SortOrder",
                table: "ReportDefinitions");

            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "ReportDefinitions");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "ReportDefinitions");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ReportDefinitions");

            migrationBuilder.CreateIndex(
                name: "IX_ReportPlacements_ReportId_SectionId",
                table: "ReportPlacements",
                columns: new[] { "ReportId", "SectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportPlacements_SectionId_SortOrder",
                table: "ReportPlacements",
                columns: new[] { "SectionId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                table: "ReportDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SectionId",
                table: "ReportDefinitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ReportDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE "ReportDefinitions" AS report
                SET "SectionId" = placement."SectionId",
                    "SortOrder" = placement."SortOrder",
                    "IsVisible" = placement."IsVisible"
                FROM (
                    SELECT DISTINCT ON ("ReportId")
                        "ReportId",
                        "SectionId",
                        "SortOrder",
                        "IsVisible"
                    FROM "ReportPlacements"
                    ORDER BY "ReportId", "SortOrder"
                ) AS placement
                WHERE report."Id" = placement."ReportId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_SectionId_SortOrder",
                table: "ReportDefinitions",
                columns: new[] { "SectionId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_ReportDefinitions_DashboardSections_SectionId",
                table: "ReportDefinitions",
                column: "SectionId",
                principalTable: "DashboardSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropTable(
                name: "ReportPlacements");
        }
    }
}
