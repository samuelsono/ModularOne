namespace CarTrack.Server.Auth;

public record LoginRequest(string Username, string Password, bool RememberMe = false);

public record MfaLoginRequest(string MfaToken, string Code, bool RememberMe = false);

public record RefreshTokenRequest(string RefreshToken);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string NewPassword);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record MfaVerifyRequest(string Code);

public record MfaDisableRequest(string Password, string Code);

public record AuthUserDto(
    string Id,
    string Username,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> Modules,
    bool MustChangePassword,
    bool TwoFactorEnabled);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    AuthUserDto User);

public record LoginResponse(
    bool RequiresTwoFactor,
    string? MfaToken,
    AuthResponse? Session);

public record MfaSetupResponse(string SharedKey, string AuthenticatorUri);

public record MessageResponse(string Message);
