using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Modules.Tenders.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenderWatchSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenderWatchSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    QueryId = table.Column<Guid>(type: "uuid", nullable: true),
                    NotifyInApp = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyEmail = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderWatchSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenderWatchSubscriptions_UserId",
                table: "TenderWatchSubscriptions",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenderWatchSubscriptions");
        }
    }
}
