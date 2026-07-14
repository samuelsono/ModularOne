using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Altitude",
                table: "Vehicles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Bearing",
                table: "Vehicles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverFirstName",
                table: "Vehicles",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverId",
                table: "Vehicles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverLastName",
                table: "Vehicles",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverLicenseNumber",
                table: "Vehicles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverPhone",
                table: "Vehicles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ElectricBatteryPercentage",
                table: "Vehicles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ElectricBatteryTs",
                table: "Vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ElectricChargingStatus",
                table: "Vehicles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ElectricChargingStatusTs",
                table: "Vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EngineType",
                table: "Vehicles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EventTs",
                table: "Vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FuelLevel",
                table: "Vehicles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FuelPercentageLeft",
                table: "Vehicles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FuelTotalConsumed",
                table: "Vehicles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FuelUpdated",
                table: "Vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GpsFixType",
                table: "Vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Idling",
                table: "Vehicles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Ignition",
                table: "Vehicles",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Vehicles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LocationUpdated",
                table: "Vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Vehicles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Odometer",
                table: "Vehicles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PositionDescription",
                table: "Vehicles",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RoadSpeed",
                table: "Vehicles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Rpm",
                table: "Vehicles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Speed",
                table: "Vehicles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StatusSyncedAt",
                table: "Vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TcuBatteryPercentage",
                table: "Vehicles",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Altitude",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Bearing",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "DriverFirstName",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "DriverLastName",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "DriverLicenseNumber",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "DriverPhone",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ElectricBatteryPercentage",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ElectricBatteryTs",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ElectricChargingStatus",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ElectricChargingStatusTs",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "EngineType",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "EventTs",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "FuelLevel",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "FuelPercentageLeft",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "FuelTotalConsumed",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "FuelUpdated",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "GpsFixType",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Idling",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Ignition",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "LocationUpdated",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Odometer",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "PositionDescription",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "RoadSpeed",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Rpm",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Speed",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "StatusSyncedAt",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "TcuBatteryPercentage",
                table: "Vehicles");
        }
    }
}
