using System.Text.Encodings.Web;
using CarTrack.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CarTrack.Modules.Users;

public interface IAccountEmailService
{
    Task SendInviteAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}

public class AccountEmailService(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    IOptions<AppUrlOptions> appUrlOptions,
    IOptions<EmailOptions> emailOptions) : IAccountEmailService
{
    private readonly AppUrlOptions _appUrl = appUrlOptions.Value;
    private readonly EmailOptions _email = emailOptions.Value;

    public async Task SendInviteAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var link = BuildLink("/auth/setup-account", user.Email, token);
        var displayName = user.DisplayName ?? user.UserName ?? user.Email ?? "there";

        var body = $"""
            <p>Hi {HtmlEncoder.Default.Encode(displayName)},</p>
            <p>You have been invited to TalisTrack. Set your password using the link below:</p>
            <p><a href="{HtmlEncoder.Default.Encode(link)}">Set up your account</a></p>
            <p>If you did not expect this email, you can ignore it.</p>
            """;

        await emailService.SendAsync(
            user.Email ?? throw new InvalidOperationException("User email is required."),
            "You're invited to TalisTrack",
            body,
            cancellationToken);
    }

    public async Task SendPasswordResetAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var link = BuildLink("/auth/reset-password", user.Email, token);
        var displayName = user.DisplayName ?? user.UserName ?? user.Email ?? "there";

        var body = $"""
            <p>Hi {HtmlEncoder.Default.Encode(displayName)},</p>
            <p>We received a request to reset your TalisTrack password.</p>
            <p><a href="{HtmlEncoder.Default.Encode(link)}">Reset your password</a></p>
            <p>If you did not request this, you can ignore this email.</p>
            """;

        await emailService.SendAsync(
            user.Email ?? throw new InvalidOperationException("User email is required."),
            "Reset your TalisTrack password",
            body,
            cancellationToken);
    }

    private string BuildLink(string path, string? email, string token)
    {
        var baseUrl = _appUrl.FrontendBaseUrl.TrimEnd('/');
        var encodedEmail = Uri.EscapeDataString(email ?? string.Empty);
        var encodedToken = Uri.EscapeDataString(token);
        return $"{baseUrl}{path}?email={encodedEmail}&token={encodedToken}";
    }
}
