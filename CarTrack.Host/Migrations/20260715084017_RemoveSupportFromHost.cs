using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Ownership of SupportTickets / TicketCategories moved to
    /// CarTrack.Modules.Support / SupportDbContext.
    /// Up/Down are intentionally empty: removing Support entities from the host
    /// model must not DropTable — the physical tables are now owned by
    /// SupportDbContext migrations. (Scaffold also listed unrelated CoreHr table
    /// drops already owned elsewhere; those must not run either.)
    /// </remarks>
    public partial class RemoveSupportFromHost : Migration
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
