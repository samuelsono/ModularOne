using System;
using CarTrack.Modules.Leave;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Modules.Leave.Persistence.Migrations
{
    [DbContext(typeof(LeaveDbContext))]
    [Migration("20260722140000_AddAttendanceAndSchedule")]
    public partial class AddAttendanceAndSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkLocationTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TracksCollaborators = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkLocationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleDayOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    LocationTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleDayOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleDayOverrides_WorkLocationTypes_LocationTypeId",
                        column: x => x.LocationTypeId,
                        principalTable: "WorkLocationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleTemplateDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    LocationTypeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleTemplateDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleTemplateDays_ScheduleTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ScheduleTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduleTemplateDays_WorkLocationTypes_LocationTypeId",
                        column: x => x.LocationTypeId,
                        principalTable: "WorkLocationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    PlannedLocationTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActualLocationTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    RecordedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceDays_WorkLocationTypes_ActualLocationTypeId",
                        column: x => x.ActualLocationTypeId,
                        principalTable: "WorkLocationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceDays_WorkLocationTypes_PlannedLocationTypeId",
                        column: x => x.PlannedLocationTypeId,
                        principalTable: "WorkLocationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceCollaborators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendanceDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    CollaboratorUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ExternalName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceCollaborators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceCollaborators_AttendanceDays_AttendanceDayId",
                        column: x => x.AttendanceDayId,
                        principalTable: "AttendanceDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkLocationTypes_Code",
                table: "WorkLocationTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTemplates_UserId_EffectiveFrom",
                table: "ScheduleTemplates",
                columns: new[] { "UserId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleDayOverrides_LocationTypeId",
                table: "ScheduleDayOverrides",
                column: "LocationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleDayOverrides_UserId_Date",
                table: "ScheduleDayOverrides",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTemplateDays_LocationTypeId",
                table: "ScheduleTemplateDays",
                column: "LocationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTemplateDays_TemplateId_DayOfWeek",
                table: "ScheduleTemplateDays",
                columns: new[] { "TemplateId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDays_ActualLocationTypeId",
                table: "AttendanceDays",
                column: "ActualLocationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDays_PlannedLocationTypeId",
                table: "AttendanceDays",
                column: "PlannedLocationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDays_UserId_Date",
                table: "AttendanceDays",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceCollaborators_AttendanceDayId",
                table: "AttendanceCollaborators",
                column: "AttendanceDayId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AttendanceCollaborators");
            migrationBuilder.DropTable(name: "ScheduleTemplateDays");
            migrationBuilder.DropTable(name: "ScheduleDayOverrides");
            migrationBuilder.DropTable(name: "AttendanceDays");
            migrationBuilder.DropTable(name: "ScheduleTemplates");
            migrationBuilder.DropTable(name: "WorkLocationTypes");
        }
    }
}
