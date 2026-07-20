using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Modules.Settings.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalAuthSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalAuthSettings",
                columns: table => new
                {
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProtectedClientSecret = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuthSettings", x => x.Provider);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalAuthSettings");
        }
    }
}
