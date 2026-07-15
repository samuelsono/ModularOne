using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Ownership of HelpArticles moved to CarTrack.Modules.Help / HelpDbContext.
    /// Up/Down are intentionally empty: removing HelpArticle from the host model must
    /// not DropTable — the physical table is now owned by HelpDbContext migrations.
    /// </remarks>
    public partial class RemoveHelpArticleFromHost : Migration
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
