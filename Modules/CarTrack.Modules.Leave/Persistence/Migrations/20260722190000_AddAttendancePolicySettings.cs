using System;
using CarTrack.Modules.Leave;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Modules.Leave.Persistence.Migrations
{
    [DbContext(typeof(LeaveDbContext))]
    [Migration("20260722190000_AddAttendancePolicySettings")]
    public partial class AddAttendancePolicySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendancePolicySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    DefaultAssumption = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendancePolicySettings", x => x.Id);
                });

            // InsertData requires the entity in the migration model (no Designer for this chain).
            // Seed via SQL instead; GetOrCreate also creates the row if missing.
            migrationBuilder.Sql(
                """
                INSERT INTO "AttendancePolicySettings" ("Id", "DefaultAssumption", "UpdatedAt")
                VALUES (1, 'Present', TIMESTAMPTZ '2026-07-22 00:00:00+00')
                ON CONFLICT ("Id") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendancePolicySettings");
        }
    }
}
