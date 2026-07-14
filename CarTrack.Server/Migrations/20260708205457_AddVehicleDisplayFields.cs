using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleDisplayFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CentralLockingStatus",
                table: "Vehicles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeofenceIdsJson",
                table: "Vehicles",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LvBatteryVoltage",
                table: "Vehicles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OilTemp",
                table: "Vehicles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "UnitClock",
                table: "Vehicles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WaterTemp",
                table: "Vehicles",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CentralLockingStatus",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "GeofenceIdsJson",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "LvBatteryVoltage",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "OilTemp",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "UnitClock",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "WaterTemp",
                table: "Vehicles");
        }
    }
}
