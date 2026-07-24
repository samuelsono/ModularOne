using System;
using CarTrack.Modules.Tenders;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Modules.Tenders.Persistence.Migrations
{
    [DbContext(typeof(TendersDbContext))]
    [Migration("20260723090000_AddETendersSourceScope")]
    public partial class AddETendersSourceScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ETendersDateFrom",
                table: "TenderSources",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ETendersDateTo",
                table: "TenderSources",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ETendersPageSize",
                table: "TenderSources",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ETendersDateFrom",
                table: "TenderSources");

            migrationBuilder.DropColumn(
                name: "ETendersDateTo",
                table: "TenderSources");

            migrationBuilder.DropColumn(
                name: "ETendersPageSize",
                table: "TenderSources");
        }
    }
}
