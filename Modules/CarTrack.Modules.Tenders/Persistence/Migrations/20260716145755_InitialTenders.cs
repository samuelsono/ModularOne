using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Modules.Tenders.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialTenders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenderMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CanonicalUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ClosingDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PortalStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MatchedKeywords = table.Column<string[]>(type: "text[]", nullable: false),
                    DocumentUrls = table.Column<string[]>(type: "text[]", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CollectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderMatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenderQueries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Keywords = table.Column<string[]>(type: "text[]", nullable: false),
                    MatchMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SourceIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderQueries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenderScrapeRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Trigger = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SourcesAttempted = table.Column<int>(type: "integer", nullable: false),
                    SourcesUnchanged = table.Column<int>(type: "integer", nullable: false),
                    MatchesNew = table.Column<int>(type: "integer", nullable: false),
                    ItemsSkippedKnown = table.Column<int>(type: "integer", nullable: false),
                    ItemsSkippedExpired = table.Column<int>(type: "integer", nullable: false),
                    ErrorSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    SourceIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderScrapeRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenderSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ParserKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ScrapeIntervalMinutes = table.Column<int>(type: "integer", nullable: true),
                    NextDueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastFetchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSuccessAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ETag = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    LastModifiedHeader = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MaxDetailPagesPerRun = table.Column<int>(type: "integer", nullable: false),
                    RobotsRespect = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenderScrapeRunSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    HttpStatus = table.Column<int>(type: "integer", nullable: true),
                    BytesFetched = table.Column<long>(type: "bigint", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false),
                    ItemsSeen = table.Column<int>(type: "integer", nullable: false),
                    ItemsMatched = table.Column<int>(type: "integer", nullable: false),
                    ItemsSkippedKnown = table.Column<int>(type: "integer", nullable: false),
                    ItemsSkippedExpired = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderScrapeRunSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderScrapeRunSources_TenderScrapeRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "TenderScrapeRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenderMatches_ClosingDate",
                table: "TenderMatches",
                column: "ClosingDate");

            migrationBuilder.CreateIndex(
                name: "IX_TenderMatches_FirstSeenAt",
                table: "TenderMatches",
                column: "FirstSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenderMatches_SourceId_ExternalKey",
                table: "TenderMatches",
                columns: new[] { "SourceId", "ExternalKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenderMatches_Status",
                table: "TenderMatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TenderQueries_IsEnabled",
                table: "TenderQueries",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_TenderScrapeRuns_Status_CreatedAt",
                table: "TenderScrapeRuns",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenderScrapeRunSources_RunId",
                table: "TenderScrapeRunSources",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderScrapeRunSources_SourceId",
                table: "TenderScrapeRunSources",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderSources_IsEnabled",
                table: "TenderSources",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_TenderSources_NextDueAt",
                table: "TenderSources",
                column: "NextDueAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenderMatches");

            migrationBuilder.DropTable(
                name: "TenderQueries");

            migrationBuilder.DropTable(
                name: "TenderScrapeRunSources");

            migrationBuilder.DropTable(
                name: "TenderSources");

            migrationBuilder.DropTable(
                name: "TenderScrapeRuns");
        }
    }
}
