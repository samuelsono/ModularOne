using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddNestedDashboardSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentSectionId",
                table: "DashboardSections",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardSections_ParentSectionId",
                table: "DashboardSections",
                column: "ParentSectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_DashboardSections_DashboardSections_ParentSectionId",
                table: "DashboardSections",
                column: "ParentSectionId",
                principalTable: "DashboardSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DashboardSections_DashboardSections_ParentSectionId",
                table: "DashboardSections");

            migrationBuilder.DropIndex(
                name: "IX_DashboardSections_ParentSectionId",
                table: "DashboardSections");

            migrationBuilder.DropColumn(
                name: "ParentSectionId",
                table: "DashboardSections");
        }
    }
}
