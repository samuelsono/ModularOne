using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Modules.Tenders.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenderSourceCircuitBreaker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CircuitOpenedUntil",
                table: "TenderSources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveFailures",
                table: "TenderSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TenderSources_CircuitOpenedUntil",
                table: "TenderSources",
                column: "CircuitOpenedUntil");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenderSources_CircuitOpenedUntil",
                table: "TenderSources");

            migrationBuilder.DropColumn(
                name: "CircuitOpenedUntil",
                table: "TenderSources");

            migrationBuilder.DropColumn(
                name: "ConsecutiveFailures",
                table: "TenderSources");
        }
    }
}
