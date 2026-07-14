using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddVehiclesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CarTrackVehicleId",
                table: "Vehicles",
                column: "CarTrackVehicleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_RegistrationNumber",
                table: "Vehicles",
                column: "RegistrationNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Vehicles");
        }
    }
}
