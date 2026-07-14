using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddDriversTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drivers", x => x.Id);
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Drivers");
        }
    }
}
