using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCarTrackPageCache : Migration
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

            migrationBuilder.CreateIndex(
                name: "IX_CarTrackPageCache_Resource_Registration_RangeKey_Page_PerPa~",
                table: "CarTrackPageCache",
                columns: new[] { "Resource", "Registration", "RangeKey", "Page", "PerPage" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarTrackPageCache");
        }
    }
}
