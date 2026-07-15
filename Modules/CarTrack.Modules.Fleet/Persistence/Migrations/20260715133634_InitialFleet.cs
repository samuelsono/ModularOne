using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Modules.Fleet.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialFleet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarTrackPageCache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Resource = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Registration = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Page = table.Column<int>(type: "integer", nullable: false),
                    PerPage = table.Column<int>(type: "integer", nullable: false),
                    RangeKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    Total = table.Column<int>(type: "integer", nullable: false),
                    LastPage = table.Column<int>(type: "integer", nullable: false),
                    CurrentPage = table.Column<int>(type: "integer", nullable: false),
                    FetchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarTrackPageCache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Drivers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LastName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    WorkEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    WorkPhone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WhatsappNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Gender = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LicenceNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IdNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LicenceIssued = table.Column<DateOnly>(type: "date", nullable: true),
                    LicenceExpiry = table.Column<DateOnly>(type: "date", nullable: true),
                    LicenceCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    HasPdp = table.Column<bool>(type: "boolean", nullable: false),
                    UnitName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    StreetNumber = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    StreetName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Suburb = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    City = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Province = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    EmployeeNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    JobTitle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Department = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Branch = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EmploymentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EmploymentStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WorkStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Manager = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CarTrackDriverId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarTrackVehicleId = table.Column<long>(type: "bigint", nullable: true),
                    RegistrationNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Make = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Vin = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EngineNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Colour = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VehicleType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FuelType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Tare = table.Column<int>(type: "integer", nullable: false),
                    Gvm = table.Column<int>(type: "integer", nullable: false),
                    RegisteredOwner = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LicenceDiscExpiry = table.Column<DateOnly>(type: "date", nullable: true),
                    IgnitionStatus = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CarTrackSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StatusSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EngineType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    EventTs = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Bearing = table.Column<double>(type: "double precision", nullable: true),
                    Speed = table.Column<double>(type: "double precision", nullable: true),
                    RoadSpeed = table.Column<double>(type: "double precision", nullable: true),
                    Ignition = table.Column<bool>(type: "boolean", nullable: true),
                    Idling = table.Column<bool>(type: "boolean", nullable: true),
                    Odometer = table.Column<double>(type: "double precision", nullable: true),
                    Altitude = table.Column<double>(type: "double precision", nullable: true),
                    Rpm = table.Column<double>(type: "double precision", nullable: true),
                    TcuBatteryPercentage = table.Column<double>(type: "double precision", nullable: true),
                    LvBatteryVoltage = table.Column<double>(type: "double precision", nullable: true),
                    UnitClock = table.Column<double>(type: "double precision", nullable: true),
                    WaterTemp = table.Column<double>(type: "double precision", nullable: true),
                    OilTemp = table.Column<double>(type: "double precision", nullable: true),
                    CentralLockingStatus = table.Column<bool>(type: "boolean", nullable: true),
                    GeofenceIdsJson = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    LocationUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PositionDescription = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    GpsFixType = table.Column<int>(type: "integer", nullable: true),
                    FuelLevel = table.Column<double>(type: "double precision", nullable: true),
                    FuelPercentageLeft = table.Column<double>(type: "double precision", nullable: true),
                    FuelTotalConsumed = table.Column<double>(type: "double precision", nullable: true),
                    FuelUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ElectricBatteryPercentage = table.Column<double>(type: "double precision", nullable: true),
                    ElectricChargingStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ElectricBatteryTs = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ElectricChargingStatusTs = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DriverId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DriverFirstName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DriverLastName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DriverPhone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    DriverLicenseNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AssignedDriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_Drivers_AssignedDriverId",
                        column: x => x.AssignedDriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarTrackPageCache_Resource_Registration_RangeKey_Page_PerPa~",
                table: "CarTrackPageCache",
                columns: new[] { "Resource", "Registration", "RangeKey", "Page", "PerPage" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_CarTrackDriverId",
                table: "Drivers",
                column: "CarTrackDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_DriverCode",
                table: "Drivers",
                column: "DriverCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_LicenceNumber",
                table: "Drivers",
                column: "LicenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_WorkEmail",
                table: "Drivers",
                column: "WorkEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_AssignedDriverId",
                table: "Vehicles",
                column: "AssignedDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CarTrackVehicleId",
                table: "Vehicles",
                column: "CarTrackVehicleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_RegistrationNumber",
                table: "Vehicles",
                column: "RegistrationNumber",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarTrackPageCache");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "Drivers");
        }
    }
}
