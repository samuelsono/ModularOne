using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarTrack.Infrastructure.Auth;
using CarTrack.Server.Data;
using CarTrack.Server.Users;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace CarTrack.Modules.Users;

public interface IExternalAuthLoginService
{
    Task<IReadOnlyList<ExternalAuthProviderStatusDto>> GetProviderStatusesAsync(
        CancellationToken cancellationToken = default);

    Task<string?> BuildAuthorizationUrlAsync(
        string provider,
        string returnUrl,
        string redirectUri,
        CancellationToken cancellationToken = default);

    Task<ExternalAuthCompletion> CompleteAsync(
        string provider,
        string code,
        string state,
        string redirectUri,
        CancellationToken cancellationToken = default);

    string BuildFrontendRedirect(ExternalAuthCompletion completion);
}

public sealed record ExternalAuthProviderStatusDto(string Provider, bool IsActivated);

public abstract record ExternalAuthCompletion(string ReturnUrl);

public sealed record ExternalAuthSessionCompletion(AuthResponse Session, string ReturnUrl)
    : ExternalAuthCompletion(ReturnUrl);

public sealed record ExternalAuthMfaCompletion(string MfaToken, string ReturnUrl)
    : ExternalAuthCompletion(ReturnUrl);

public sealed class ExternalAuthLoginService(
    IExternalAuthCredentialProvider credentialProvider,
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<AppUrlOptions> appUrlOptions,
    IHttpClientFactory httpClientFactory,
    ILogger<ExternalAuthLoginService> logger) : IExternalAuthLoginService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IDataProtector _stateProtector =
        dataProtectionProvider.CreateProtector("CarTrack.ExternalAuth.OAuthState");

    private readonly AppUrlOptions _appUrl = appUrlOptions.Value;

    public async Task<IReadOnlyList<ExternalAuthProviderStatusDto>> GetProviderStatusesAsync(
        CancellationToken cancellationToken = default)
    {
        var statuses = await credentialProvider.GetProviderStatusesAsync(cancellationToken);
        return statuses
            .Select(status => new ExternalAuthProviderStatusDto(status.Provider, status.IsActivated))
            .ToArray();
    }

    public async Task<string?> BuildAuthorizationUrlAsync(
        string provider,
        string returnUrl,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        var credentials = await credentialProvider.GetCredentialsAsync(provider, cancellationToken);
        if (credentials is null)
        {
            return null;
        }

        var safeReturnUrl = SanitizeReturnUrl(returnUrl);
        var state = ProtectState(new OAuthState(credentials.Provider, safeReturnUrl, DateTimeOffset.UtcNow.AddMinutes(10)));

        return credentials.Provider switch
        {
            ExternalAuthProviders.Google => BuildGoogleAuthorizeUrl(credentials, redirectUri, state),
            ExternalAuthProviders.Microsoft => BuildMicrosoftAuthorizeUrl(credentials, redirectUri, state),
            _ => null,
        };
    }

    public async Task<ExternalAuthCompletion> CompleteAsync(
        string provider,
        string code,
        string state,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        var oauthState = UnprotectState(state)
            ?? throw new InvalidOperationException("OAuth state is invalid or expired.");

        if (!string.Equals(oauthState.Provider, NormalizeProvider(provider), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("OAuth provider mismatch.");
        }

        if (oauthState.ExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("OAuth state has expired. Please try signing in again.");
        }

        var credentials = await credentialProvider.GetCredentialsAsync(oauthState.Provider, cancellationToken)
            ?? throw new InvalidOperationException($"{oauthState.Provider} authentication is not configured.");

        var profile = credentials.Provider switch
        {
            ExternalAuthProviders.Google => await ExchangeGoogleAsync(credentials, code, redirectUri, cancellationToken),
            ExternalAuthProviders.Microsoft => await ExchangeMicrosoftAsync(credentials, code, redirectUri, cancellationToken),
            _ => throw new InvalidOperationException("Unsupported OAuth provider."),
        };

        var user = await ResolveOrCreateUserAsync(credentials.Provider, profile, cancellationToken);

        if (!user.IsActive)
        {
            throw new InvalidOperationException("This account is inactive.");
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        if (await userManager.GetTwoFactorEnabledAsync(user))
        {
            return new ExternalAuthMfaCompletion(tokenService.CreateMfaChallengeToken(user), oauthState.ReturnUrl);
        }

        var session = await tokenService.CreateTokenPairAsync(user, rememberMe: true, cancellationToken);
        return new ExternalAuthSessionCompletion(session, oauthState.ReturnUrl);
    }

    public string BuildFrontendRedirect(ExternalAuthCompletion completion)
    {
        var frontend = _appUrl.FrontendBaseUrl.TrimEnd('/');
        var returnUrl = SanitizeReturnUrl(completion.ReturnUrl);

        return completion switch
        {
            ExternalAuthSessionCompletion session =>
                QueryHelpers.AddQueryString($"{frontend}/auth/oauth-callback", new Dictionary<string, string?>
                {
                    ["accessToken"] = session.Session.AccessToken,
                    ["refreshToken"] = session.Session.RefreshToken,
                    ["expiresIn"] = session.Session.ExpiresIn.ToString(),
                    ["returnUrl"] = returnUrl,
                }),
            ExternalAuthMfaCompletion mfa =>
                QueryHelpers.AddQueryString($"{frontend}/auth/login", new Dictionary<string, string?>
                {
                    ["mfaToken"] = mfa.MfaToken,
                    ["returnUrl"] = returnUrl,
                }),
            _ => $"{frontend}/auth/login?error=external_auth_failed",
        };
    }

    private async Task<ApplicationUser> ResolveOrCreateUserAsync(
        string provider,
        ExternalUserProfile profile,
        CancellationToken cancellationToken)
    {
        var loginInfo = new UserLoginInfo(provider, profile.Subject, provider);
        var existingByLogin = await userManager.FindByLoginAsync(loginInfo.LoginProvider, loginInfo.ProviderKey);
        if (existingByLogin is not null)
        {
            return existingByLogin;
        }

        ApplicationUser? user = null;
        if (!string.IsNullOrWhiteSpace(profile.Email))
        {
            user = await userManager.FindByEmailAsync(profile.Email);
        }

        if (user is null)
        {
            var userName = await AllocateUserNameAsync(profile, cancellationToken);
            user = new ApplicationUser
            {
                UserName = userName,
                Email = profile.Email,
                EmailConfirmed = !string.IsNullOrWhiteSpace(profile.Email),
                DisplayName = profile.DisplayName,
                FirstName = profile.GivenName,
                LastName = profile.FamilyName,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                MustChangePassword = false,
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(" ", createResult.Errors.Select(error => error.Description)));
            }

            if (!await userManager.IsInRoleAsync(user, AppRoles.Staff))
            {
                var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Staff);
                if (!roleResult.Succeeded)
                {
                    logger.LogWarning(
                        "Created OAuth user {UserId} but could not assign Staff role: {Errors}",
                        user.Id,
                        string.Join(", ", roleResult.Errors.Select(error => error.Description)));
                }
            }
        }

        var addLogin = await userManager.AddLoginAsync(user, loginInfo);
        if (!addLogin.Succeeded && addLogin.Errors.All(error => error.Code != "LoginAlreadyAssociated"))
        {
            logger.LogWarning(
                "Failed to link {Provider} login for user {UserId}: {Errors}",
                provider,
                user.Id,
                string.Join(", ", addLogin.Errors.Select(error => error.Description)));
        }

        return user;
    }

    private async Task<string> AllocateUserNameAsync(ExternalUserProfile profile, CancellationToken cancellationToken)
    {
        var baseName = !string.IsNullOrWhiteSpace(profile.Email)
            ? profile.Email.Split('@')[0]
            : $"user_{profile.Subject[..Math.Min(8, profile.Subject.Length)]}";

        baseName = new string(baseName.Where(ch => char.IsLetterOrDigit(ch) || ch is '.' or '_' or '-').ToArray());
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "user";
        }

        var candidate = baseName;
        var suffix = 1;
        while (await userManager.FindByNameAsync(candidate) is not null)
        {
            candidate = $"{baseName}{suffix}";
            suffix++;
            cancellationToken.ThrowIfCancellationRequested();
        }

        return candidate;
    }

    private static string BuildGoogleAuthorizeUrl(ExternalOAuthCredentials credentials, string redirectUri, string state)
    {
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = credentials.ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid email profile",
            ["state"] = state,
            ["access_type"] = "online",
            ["prompt"] = "select_account",
        };

        return QueryHelpers.AddQueryString("https://accounts.google.com/o/oauth2/v2/auth", query);
    }

    private static string BuildMicrosoftAuthorizeUrl(ExternalOAuthCredentials credentials, string redirectUri, string state)
    {
        var tenant = string.IsNullOrWhiteSpace(credentials.TenantId) ? "common" : credentials.TenantId;
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = credentials.ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["response_mode"] = "query",
            ["scope"] = "openid profile email User.Read",
            ["state"] = state,
        };

        return QueryHelpers.AddQueryString($"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize", query);
    }

    private async Task<ExternalUserProfile> ExchangeGoogleAsync(
        ExternalOAuthCredentials credentials,
        string code,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(nameof(ExternalAuthLoginService));
        using var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = credentials.ClientId,
            ["client_secret"] = credentials.ClientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
        });

        using var tokenResponse = await client.PostAsync("https://oauth2.googleapis.com/token", tokenRequest, cancellationToken);
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            logger.LogWarning("Google token exchange failed: {Body}", tokenBody);
            throw new InvalidOperationException("Google authentication failed during token exchange.");
        }

        var token = JsonSerializer.Deserialize<OAuthTokenResponse>(tokenBody, JsonOptions)
            ?? throw new InvalidOperationException("Google token response was empty.");

        using var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://openidconnect.googleapis.com/v1/userinfo");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var userResponse = await client.SendAsync(userRequest, cancellationToken);
        var userBody = await userResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!userResponse.IsSuccessStatusCode)
        {
            logger.LogWarning("Google userinfo failed: {Body}", userBody);
            throw new InvalidOperationException("Google authentication failed while loading the user profile.");
        }

        var profile = JsonSerializer.Deserialize<GoogleUserInfo>(userBody, JsonOptions)
            ?? throw new InvalidOperationException("Google user profile was empty.");

        if (string.IsNullOrWhiteSpace(profile.Sub))
        {
            throw new InvalidOperationException("Google did not return a user id.");
        }

        return new ExternalUserProfile(
            profile.Sub,
            profile.Email,
            profile.Name,
            profile.GivenName,
            profile.FamilyName);
    }

    private async Task<ExternalUserProfile> ExchangeMicrosoftAsync(
        ExternalOAuthCredentials credentials,
        string code,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        var tenant = string.IsNullOrWhiteSpace(credentials.TenantId) ? "common" : credentials.TenantId;
        var client = httpClientFactory.CreateClient(nameof(ExternalAuthLoginService));
        using var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = credentials.ClientId,
            ["client_secret"] = credentials.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["scope"] = "openid profile email User.Read",
        });

        using var tokenResponse = await client.PostAsync(
            $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token",
            tokenRequest,
            cancellationToken);
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            logger.LogWarning("Microsoft token exchange failed: {Body}", tokenBody);
            throw new InvalidOperationException("Microsoft authentication failed during token exchange.");
        }

        var token = JsonSerializer.Deserialize<OAuthTokenResponse>(tokenBody, JsonOptions)
            ?? throw new InvalidOperationException("Microsoft token response was empty.");

        using var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var userResponse = await client.SendAsync(userRequest, cancellationToken);
        var userBody = await userResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!userResponse.IsSuccessStatusCode)
        {
            logger.LogWarning("Microsoft Graph /me failed: {Body}", userBody);
            throw new InvalidOperationException("Microsoft authentication failed while loading the user profile.");
        }

        var profile = JsonSerializer.Deserialize<MicrosoftUserInfo>(userBody, JsonOptions)
            ?? throw new InvalidOperationException("Microsoft user profile was empty.");

        var subject = profile.Id;
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new InvalidOperationException("Microsoft did not return a user id.");
        }

        var email = profile.Mail ?? profile.UserPrincipalName;
        return new ExternalUserProfile(
            subject,
            email,
            profile.DisplayName,
            profile.GivenName,
            profile.Surname);
    }

    private string ProtectState(OAuthState state)
    {
        var json = JsonSerializer.Serialize(state);
        return WebEncoders.Base64UrlEncode(_stateProtector.Protect(Encoding.UTF8.GetBytes(json)));
    }

    private OAuthState? UnprotectState(string state)
    {
        try
        {
            var bytes = _stateProtector.Unprotect(WebEncoders.Base64UrlDecode(state));
            return JsonSerializer.Deserialize<OAuthState>(Encoding.UTF8.GetString(bytes), JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private string SanitizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/') || returnUrl.StartsWith("//"))
        {
            return "/";
        }

        return returnUrl;
    }

    private static string? NormalizeProvider(string provider) =>
        provider.Trim().ToUpperInvariant() switch
        {
            "GOOGLE" => ExternalAuthProviders.Google,
            "MICROSOFT" => ExternalAuthProviders.Microsoft,
            _ => null,
        };

    private sealed record OAuthState(string Provider, string ReturnUrl, DateTimeOffset ExpiresAt);

    private sealed record ExternalUserProfile(
        string Subject,
        string? Email,
        string? DisplayName,
        string? GivenName,
        string? FamilyName);

    private sealed class OAuthTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class GoogleUserInfo
    {
        public string Sub { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Name { get; set; }
        [JsonPropertyName("given_name")]
        public string? GivenName { get; set; }
        [JsonPropertyName("family_name")]
        public string? FamilyName { get; set; }
    }

    private sealed class MicrosoftUserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? GivenName { get; set; }
        public string? Surname { get; set; }
        public string? Mail { get; set; }
        public string? UserPrincipalName { get; set; }
    }
}
