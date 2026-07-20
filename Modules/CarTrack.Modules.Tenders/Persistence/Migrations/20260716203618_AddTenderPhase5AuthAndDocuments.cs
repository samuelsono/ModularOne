using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Modules.Tenders.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenderPhase5AuthAndDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthKind",
                table: "TenderSources",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AuthUsername",
                table: "TenderSources",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtectedAuthSecret",
                table: "TenderSources",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentMetadataJson",
                table: "TenderMatches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DocumentsRefreshedAt",
                table: "TenderMatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenderMatches_OwnerUserId",
                table: "TenderMatches",
                column: "OwnerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenderMatches_OwnerUserId",
                table: "TenderMatches");

            migrationBuilder.DropColumn(
                name: "AuthKind",
                table: "TenderSources");

            migrationBuilder.DropColumn(
                name: "AuthUsername",
                table: "TenderSources");

            migrationBuilder.DropColumn(
                name: "ProtectedAuthSecret",
                table: "TenderSources");

            migrationBuilder.DropColumn(
                name: "DocumentMetadataJson",
                table: "TenderMatches");

            migrationBuilder.DropColumn(
                name: "DocumentsRefreshedAt",
                table: "TenderMatches");
        }
    }
}
