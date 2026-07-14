using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    public partial class CompleteIAuditableColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Vehicles",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "Vehicles",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "StaffProfiles",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "StaffProfiles",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Positions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Positions",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Positions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "Positions",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "LeaveTypes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "LeaveTypes",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "LeaveTypes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "LeaveTypes",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "LeaveRequests",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "LeaveRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "LeaveRequests",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "ExpenseClaims",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "ExpenseClaims",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "ExpenseClaims",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "ExpenseCategories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "ExpenseCategories",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "ExpenseCategories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "ExpenseCategories",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Drivers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "Drivers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Departments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Departments",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Departments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "Departments",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Companies",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "Companies",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CreatedByUserId",
                table: "Vehicles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_UpdatedByUserId",
                table: "Vehicles",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffProfiles_CreatedByUserId",
                table: "StaffProfiles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffProfiles_UpdatedByUserId",
                table: "StaffProfiles",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_CreatedByUserId",
                table: "Positions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_UpdatedByUserId",
                table: "Positions",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_CreatedByUserId",
                table: "LeaveTypes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_UpdatedByUserId",
                table: "LeaveTypes",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_CreatedByUserId",
                table: "LeaveRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_UpdatedByUserId",
                table: "LeaveRequests",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseClaims_CreatedByUserId",
                table: "ExpenseClaims",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseClaims_UpdatedByUserId",
                table: "ExpenseClaims",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_CreatedByUserId",
                table: "ExpenseCategories",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_UpdatedByUserId",
                table: "ExpenseCategories",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_CreatedByUserId",
                table: "Drivers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_UpdatedByUserId",
                table: "Drivers",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CreatedByUserId",
                table: "Departments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_UpdatedByUserId",
                table: "Departments",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CreatedByUserId",
                table: "Companies",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_UpdatedByUserId",
                table: "Companies",
                column: "UpdatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_AspNetUsers_CreatedByUserId",
                table: "Companies",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_AspNetUsers_UpdatedByUserId",
                table: "Companies",
                column: "UpdatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_AspNetUsers_CreatedByUserId",
                table: "Departments",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_AspNetUsers_UpdatedByUserId",
                table: "Departments",
                column: "UpdatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_AspNetUsers_CreatedByUserId",
                table: "Drivers",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_AspNetUsers_UpdatedByUserId",
                table: "Drivers",
                column: "UpdatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseCategories_AspNetUsers_CreatedByUserId",
                table: "ExpenseCategories",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseCategories_AspNetUsers_UpdatedByUserId",
                table: "ExpenseCategories",
                column: "UpdatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseClaims_AspNetUsers_CreatedByUserId",
                table: "ExpenseClaims",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseClaims_AspNetUsers_UpdatedByUserId",
                table: "ExpenseClaims",
                column: "UpdatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_CreatedByUserId",
                table: "LeaveRequests",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_UpdatedByUserId",
                table: "LeaveRequests",
                column: "UpdatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveTypes_AspNetUsers_CreatedByUserId",
                table: "LeaveTypes",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveTypes_AspNetUsers_UpdatedByUserId",
                table: "LeaveTypes",
                column: "UpdatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Positions_AspNetUsers_CreatedByUserId",
                table: "Positions",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Positions_AspNetUsers_UpdatedByUserId",
                table: "Positions",
                column: "UpdatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffProfiles_AspNetUsers_CreatedByUserId",
                table: "StaffProfiles",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffProfiles_AspNetUsers_UpdatedByUserId",
                table: "StaffProfiles",
                column: "UpdatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_AspNetUsers_CreatedByUserId",
                table: "Vehicles",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_AspNetUsers_UpdatedByUserId",
                table: "Vehicles",
                column: "UpdatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_AspNetUsers_CreatedByUserId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_AspNetUsers_UpdatedByUserId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_AspNetUsers_CreatedByUserId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_AspNetUsers_UpdatedByUserId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_AspNetUsers_CreatedByUserId",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_AspNetUsers_UpdatedByUserId",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseCategories_AspNetUsers_CreatedByUserId",
                table: "ExpenseCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseCategories_AspNetUsers_UpdatedByUserId",
                table: "ExpenseCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseClaims_AspNetUsers_CreatedByUserId",
                table: "ExpenseClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseClaims_AspNetUsers_UpdatedByUserId",
                table: "ExpenseClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_CreatedByUserId",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_UpdatedByUserId",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveTypes_AspNetUsers_CreatedByUserId",
                table: "LeaveTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveTypes_AspNetUsers_UpdatedByUserId",
                table: "LeaveTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_Positions_AspNetUsers_CreatedByUserId",
                table: "Positions");

            migrationBuilder.DropForeignKey(
                name: "FK_Positions_AspNetUsers_UpdatedByUserId",
                table: "Positions");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffProfiles_AspNetUsers_CreatedByUserId",
                table: "StaffProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffProfiles_AspNetUsers_UpdatedByUserId",
                table: "StaffProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_AspNetUsers_CreatedByUserId",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_AspNetUsers_UpdatedByUserId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_CreatedByUserId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_UpdatedByUserId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_StaffProfiles_CreatedByUserId",
                table: "StaffProfiles");

            migrationBuilder.DropIndex(
                name: "IX_StaffProfiles_UpdatedByUserId",
                table: "StaffProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Positions_CreatedByUserId",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Positions_UpdatedByUserId",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_LeaveTypes_CreatedByUserId",
                table: "LeaveTypes");

            migrationBuilder.DropIndex(
                name: "IX_LeaveTypes_UpdatedByUserId",
                table: "LeaveTypes");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_CreatedByUserId",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_UpdatedByUserId",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseClaims_CreatedByUserId",
                table: "ExpenseClaims");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseClaims_UpdatedByUserId",
                table: "ExpenseClaims");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseCategories_CreatedByUserId",
                table: "ExpenseCategories");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseCategories_UpdatedByUserId",
                table: "ExpenseCategories");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_CreatedByUserId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_UpdatedByUserId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Departments_CreatedByUserId",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_UpdatedByUserId",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Companies_CreatedByUserId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_UpdatedByUserId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "ExpenseClaims");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ExpenseCategories");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ExpenseCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ExpenseCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "ExpenseCategories");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Companies");
        }
    }
}
