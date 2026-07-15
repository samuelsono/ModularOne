using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Ownership of Notifications / NotificationRecipients moved to
    /// CarTrack.Modules.Notifications / NotificationsDbContext.
    /// Up/Down are intentionally empty so fresh installs do not create these
    /// tables in the host migration pipeline (Notifications migrates first and
    /// owns the tables). Databases that already applied this migration keep
    /// their existing tables.
    /// </remarks>
    public partial class AddNotificationsTable : Migration
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
