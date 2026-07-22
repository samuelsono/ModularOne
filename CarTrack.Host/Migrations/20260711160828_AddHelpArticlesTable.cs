using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Ownership of HelpArticles moved to CarTrack.Modules.Help / HelpDbContext.
    /// Up/Down are intentionally empty so fresh installs do not create this table
    /// in the host migration pipeline (Help migrates first and owns the table).
    /// Databases that already applied this migration keep their existing table.
    /// </remarks>
    public partial class AddHelpArticlesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
