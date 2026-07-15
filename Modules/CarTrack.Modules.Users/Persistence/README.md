# Users persistence

`UsersDbContext` owns StaffProfiles, Permissions, RefreshTokens, SecurityAuditLogs,
DriverProfileLinks, RolePermissions (history: `__EFMigrationsHistory_Users`).

Host Identity (`ApplicationDbContext`) keeps AspNet* tables only.
Auth endpoints / TokenService live in this module; JWT bearer composition stays on Host Program.cs.
