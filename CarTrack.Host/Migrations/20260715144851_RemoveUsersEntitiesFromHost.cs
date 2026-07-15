using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarTrack.Server.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Ownership of StaffProfiles / Permissions / RefreshTokens / SecurityAuditLogs /
    /// DriverProfileLinks / RolePermissions moved to CarTrack.Modules.Users / UsersDbContext.
    /// Up/Down are intentionally empty: removing Users entities from the host model must
    /// not DropTable — physical tables are owned by the module DbContext migrations.
    /// </remarks>
    public partial class RemoveUsersEntitiesFromHost : Migration
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
