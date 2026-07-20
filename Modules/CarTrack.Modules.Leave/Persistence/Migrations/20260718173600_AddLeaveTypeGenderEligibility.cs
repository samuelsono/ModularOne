using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Modules.Leave.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveTypeGenderEligibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EligibleGender",
                table: "LeaveTypes",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Any");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EligibleGender",
                table: "LeaveTypes");
        }
    }
}
