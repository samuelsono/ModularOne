using System.Security.Claims;
using System.Text.Encodings.Web;
using CarTrack.Server.Data;
using CarTrack.Server.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Users;

public static class AuthEndpoints
{
    private const string AuthenticatorIssuer = "TalisTrack";

    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/login", LoginAsync)
            .AllowAnonymous();

        group.MapPost("/login/mfa", CompleteMfaLoginAsync)
            .AllowAnonymous();

        group.MapPost("/refresh", RefreshAsync)
            .AllowAnonymous();

        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization();

        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .AllowAnonymous();

        group.MapPost("/reset-password", ResetPasswordAsync)
            .AllowAnonymous();

        group.MapPost("/setup-account", SetupAccountAsync)
            .AllowAnonymous();

        group.MapPost("/change-password", ChangePasswordAsync)
            .RequireAuthorization();

        group.MapGet("/me", GetCurrentUserAsync)
            .RequireAuthorization();

        group.MapGet("/users", GetUsersForBroadcastAsync)
            .RequireAuthorization();

        group.MapPost("/mfa/setup", BeginMfaSetupAsync)
            .RequireAuthorization();

        group.MapPost("/mfa/enable", EnableMfaAsync)
            .RequireAuthorization();

        group.MapPost("/mfa/disable", DisableMfaAsync)
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["username"] = ["Username is required."],
                ["password"] = ["Password is required."],
            });
        }

        var user = await userManager.FindByNameAsync(request.Username)
            ?? await userManager.FindByEmailAsync(request.Username);

        if (user is null)
        {
            return Results.Problem(
                title: "Invalid credentials",
                detail: "The username or password is incorrect.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!user.IsActive)
        {
            return Results.Problem(
                title: "Invalid credentials",
                detail: "The username or password is incorrect.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            return Results.Problem(
                title: "Account locked",
                detail: "Too many failed login attempts. Try again later.",
                statusCode: StatusCodes.Status423Locked);
        }

        if (!signInResult.Succeeded)
        {
            return Results.Problem(
                title: "Invalid credentials",
                detail: "The username or password is incorrect.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (await userManager.GetTwoFactorEnabledAsync(user))
        {
            return Results.Ok(new LoginResponse(
                RequiresTwoFactor: true,
                MfaToken: tokenService.CreateMfaChallengeToken(user),
                Session: null));
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        var response = await tokenService.CreateTokenPairAsync(user, request.RememberMe, cancellationToken);
        return Results.Ok(new LoginResponse(false, null, response));
    }

    private static async Task<IResult> CompleteMfaLoginAsync(
        MfaLoginRequest request,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.MfaToken) || string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["mfaToken"] = ["MFA token is required."],
                ["code"] = ["Authentication code is required."],
            });
        }

        if (!tokenService.TryValidateMfaChallengeToken(request.MfaToken, out var userId))
        {
            return Results.Problem(
                title: "Invalid MFA session",
                detail: "The MFA session has expired. Sign in again.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
        {
            return Results.Unauthorized();
        }

        var isValidCode = await userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultAuthenticatorProvider,
            request.Code.Trim());

        if (!isValidCode)
        {
            return Results.Problem(
                title: "Invalid code",
                detail: "The authentication code is incorrect.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        var response = await tokenService.CreateTokenPairAsync(user, request.RememberMe, cancellationToken);
        return Results.Ok(new LoginResponse(false, null, response));
    }

    private static async Task<IResult> RefreshAsync(
        RefreshTokenRequest request,
        ITokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["refreshToken"] = ["Refresh token is required."],
            });
        }

        var response = await tokenService.RefreshAsync(request.RefreshToken, cancellationToken);
        if (response is null)
        {
            return Results.Problem(
                title: "Invalid refresh token",
                detail: "The refresh token is invalid or expired.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Ok(response);
    }

    private static async Task<IResult> LogoutAsync(
        RefreshTokenRequest request,
        ITokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            await tokenService.RevokeAsync(request.RefreshToken, cancellationToken);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        UserManager<ApplicationUser> userManager,
        IAccountEmailService accountEmailService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["email"] = ["Email is required."],
            });
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is not null && user.IsActive)
        {
            await accountEmailService.SendPasswordResetAsync(user, cancellationToken);
        }

        return Results.Ok(new MessageResponse(
            "If an account exists for that email address, password reset instructions will be sent shortly."));
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ISecurityAuditService auditService,
        CancellationToken cancellationToken)
    {
        var validationError = ValidatePasswordResetRequest(request);
        if (validationError is not null)
        {
            return validationError;
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive)
        {
            return Results.Problem(
                title: "Invalid reset link",
                detail: "This password reset link is invalid or has expired.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return Results.Problem(
                title: "Invalid reset link",
                detail: string.Join(" ", result.Errors.Select(error => error.Description)),
                statusCode: StatusCodes.Status400BadRequest);
        }

        user.MustChangePassword = false;
        user.InvitePendingAt = null;
        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);
        await tokenService.RevokeAllRefreshTokensAsync(user.Id, cancellationToken);

        await auditService.LogAsync(
            SecurityAuditActions.UserPasswordChanged,
            user.Id,
            user.DisplayName ?? user.UserName,
            "Password reset via email link.",
            cancellationToken);

        return Results.Ok(new MessageResponse("Your password has been updated. You can sign in now."));
    }

    private static Task<IResult> SetupAccountAsync(
        ResetPasswordRequest request,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ISecurityAuditService auditService,
        CancellationToken cancellationToken) =>
        ResetPasswordAsync(request, userManager, tokenService, auditService, cancellationToken);

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ISecurityAuditService auditService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["currentPassword"] = ["Current password is required."],
                ["newPassword"] = ["New password is required."],
            });
        }

        var userId = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
        {
            return Results.Unauthorized();
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return Results.Problem(
                title: "Unable to change password",
                detail: string.Join(" ", result.Errors.Select(error => error.Description)),
                statusCode: StatusCodes.Status400BadRequest);
        }

        user.MustChangePassword = false;
        await userManager.UpdateAsync(user);
        await tokenService.RevokeAllRefreshTokensAsync(user.Id, cancellationToken);

        await auditService.LogAsync(
            SecurityAuditActions.UserPasswordChanged,
            user.Id,
            user.DisplayName ?? user.UserName,
            "Password changed by user.",
            cancellationToken);

        return Results.Ok(new MessageResponse("Password updated successfully. Sign in again with your new password."));
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        IPermissionService permissionService)
    {
        var userId = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
        {
            return Results.Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await permissionService.GetPermissionsForRolesAsync(roles);
        var modules = permissionService.GetModulesFromPermissions(permissions);

        return Results.Ok(new AuthUserDto(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.DisplayName,
            roles.ToList(),
            permissions.ToList(),
            modules.ToList(),
            user.MustChangePassword,
            await userManager.GetTwoFactorEnabledAsync(user)));
    }

    private static async Task<IResult> GetUsersForBroadcastAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole(AppRoles.Admin) && !principal.IsInRole(AppRoles.SystemAdmin))
        {
            return Results.Forbid();
        }

        var users = await userManager.Users
            .AsNoTracking()
            .Where(user => user.IsActive)
            .OrderBy(user => user.DisplayName ?? user.Email)
            .Select(user => new AuthUserLookupDto(
                user.Id,
                user.DisplayName ?? user.UserName ?? user.Email ?? user.Id,
                user.Email ?? string.Empty))
            .ToListAsync(cancellationToken);

        return Results.Ok(users);
    }

    private static async Task<IResult> BeginMfaSetupAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        var user = await GetActiveUserAsync(principal, userManager);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        await userManager.ResetAuthenticatorKeyAsync(user);
        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        var email = user.Email ?? user.UserName ?? user.Id;
        var uri = BuildAuthenticatorUri(email, key ?? string.Empty);

        return Results.Ok(new MfaSetupResponse(key ?? string.Empty, uri));
    }

    private static async Task<IResult> EnableMfaAsync(
        MfaVerifyRequest request,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        ISecurityAuditService auditService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["code"] = ["Authentication code is required."],
            });
        }

        var user = await GetActiveUserAsync(principal, userManager);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var isValid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultAuthenticatorProvider,
            request.Code.Trim());

        if (!isValid)
        {
            return Results.Problem(
                title: "Invalid code",
                detail: "The authentication code is incorrect.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);

        await auditService.LogAsync(
            SecurityAuditActions.UserMfaEnabled,
            user.Id,
            user.DisplayName ?? user.UserName,
            cancellationToken: cancellationToken);

        return Results.Ok(new MessageResponse("Two-factor authentication is now enabled."));
    }

    private static async Task<IResult> DisableMfaAsync(
        MfaDisableRequest request,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ISecurityAuditService auditService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["password"] = ["Password is required."],
                ["code"] = ["Authentication code is required."],
            });
        }

        var user = await GetActiveUserAsync(principal, userManager);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var passwordValid = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!passwordValid.Succeeded)
        {
            return Results.Problem(
                title: "Invalid password",
                detail: "Your password is incorrect.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var codeValid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultAuthenticatorProvider,
            request.Code.Trim());

        if (!codeValid)
        {
            return Results.Problem(
                title: "Invalid code",
                detail: "The authentication code is incorrect.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);

        await auditService.LogAsync(
            SecurityAuditActions.UserMfaDisabled,
            user.Id,
            user.DisplayName ?? user.UserName,
            cancellationToken: cancellationToken);

        return Results.Ok(new MessageResponse("Two-factor authentication has been disabled."));
    }

    private static async Task<ApplicationUser?> GetActiveUserAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        var userId = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var user = await userManager.FindByIdAsync(userId);
        return user is not null && user.IsActive ? user : null;
    }

    private static IResult? ValidatePasswordResetRequest(ResetPasswordRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["email"] = ["Email is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            errors["token"] = ["Token is required."];
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            errors["newPassword"] = ["New password is required."];
        }

        return errors.Count > 0 ? Results.ValidationProblem(errors) : null;
    }

    private static string BuildAuthenticatorUri(string email, string unformattedKey)
    {
        var encodedIssuer = UrlEncoder.Default.Encode(AuthenticatorIssuer);
        var encodedEmail = UrlEncoder.Default.Encode(email);
        var encodedKey = UrlEncoder.Default.Encode(unformattedKey);
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={encodedKey}&issuer={encodedIssuer}&digits=6";
    }
}
