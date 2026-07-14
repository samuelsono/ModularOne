namespace CarTrack.Server.Users;

public static class SecurityAuditActions
{
    public const string UserCreated = "user.created";
    public const string UserRolesChanged = "user.roles_changed";
    public const string UserActivated = "user.activated";
    public const string UserDeactivated = "user.deactivated";
    public const string UserInviteSent = "user.invite_sent";
    public const string UserPasswordChanged = "user.password_changed";
    public const string UserMfaEnabled = "user.mfa_enabled";
    public const string UserMfaDisabled = "user.mfa_disabled";
}
