using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Modules.Settings.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformThemes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppThemeNamesByModuleSlug",
                table: "PlatformSettings",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DefaultThemeName",
                table: "PlatformSettings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "talisLightTheme");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppThemeNamesByModuleSlug",
                table: "PlatformSettings");

            migrationBuilder.DropColumn(
                name: "DefaultThemeName",
                table: "PlatformSettings");
        }
    }
}
