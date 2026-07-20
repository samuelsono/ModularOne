using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Modules.Users.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffGender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "StaffProfiles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Unspecified");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gender",
                table: "StaffProfiles");
        }
    }
}
