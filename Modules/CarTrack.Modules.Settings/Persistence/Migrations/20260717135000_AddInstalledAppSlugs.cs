using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Modules.Settings.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstalledAppSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstalledAppSlugs",
                table: "PlatformSettings",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "accounting,leave,expense,payroll,performance,recruitment,tenders,fleet");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstalledAppSlugs",
                table: "PlatformSettings");
        }
    }
}
