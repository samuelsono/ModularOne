using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CarTrack.Server.Data;
using CarTrack.Server.Users;
using CarTrack.Server.Users.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CarTrack.Server.Auth;

public interface ITokenService
{
    Task<AuthResponse> CreateTokenPairAsync(ApplicationUser user, bool rememberMe, CancellationToken cancellationToken);

    Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken);

    Task RevokeAllRefreshTokensAsync(string userId, CancellationToken cancellationToken);

    string CreateMfaChallengeToken(ApplicationUser user);

    bool TryValidateMfaChallengeToken(string token, out string userId);
}

public class TokenService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IPermissionService permissionService,
    IOptions<JwtOptions> jwtOptions) : ITokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthResponse> CreateTokenPairAsync(
        ApplicationUser user,
        bool rememberMe,
        CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var permissions = await permissionService.GetPermissionsForRolesAsync(roles, cancellationToken);
        var modules = permissionService.GetModulesFromPermissions(permissions);
        var accessToken = CreateAccessToken(user, roles, permissions, modules);
        var refreshToken = await CreateRefreshTokenAsync(user, rememberMe, cancellationToken);

        return new AuthResponse(
            accessToken.Token,
            refreshToken.PlainTextToken,
            accessToken.ExpiresInSeconds,
            await MapUserAsync(user, roles, permissions, modules));
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            return null;
        }

        if (!storedToken.User.IsActive)
        {
            return null;
        }

        storedToken.RevokedAt = DateTimeOffset.UtcNow;

        var user = storedToken.User;
        var roles = await userManager.GetRolesAsync(user);
        var permissions = await permissionService.GetPermissionsForRolesAsync(roles, cancellationToken);
        var modules = permissionService.GetModulesFromPermissions(permissions);
        var accessToken = CreateAccessToken(user, roles, permissions, modules);
        var replacement = await CreateRefreshTokenAsync(user, rememberMe: false, cancellationToken);
        storedToken.ReplacedByTokenHash = replacement.TokenHash;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken.Token,
            replacement.PlainTextToken,
            accessToken.ExpiresInSeconds,
            await MapUserAsync(user, roles, permissions, modules));
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || storedToken.IsRevoked)
        {
            return;
        }

        storedToken.RevokedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllRefreshTokensAsync(string userId, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            return;
        }

        var revokedAt = DateTimeOffset.UtcNow;
        foreach (var token in tokens)
        {
            token.RevokedAt = revokedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public string CreateMfaChallengeToken(ApplicationUser user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.MfaChallengeMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(AuthClaimTypes.TokenPurpose, AuthClaimTypes.MfaChallenge),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool TryValidateMfaChallengeToken(string token, out string userId)
    {
        userId = string.Empty;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var validation = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidAudience = _jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret)),
                ClockSkew = TimeSpan.FromMinutes(1),
            };

            var principal = handler.ValidateToken(token, validation, out _);
            var purpose = principal.FindFirstValue(AuthClaimTypes.TokenPurpose);
            if (!string.Equals(purpose, AuthClaimTypes.MfaChallenge, StringComparison.Ordinal))
            {
                return false;
            }

            userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? string.Empty;

            return !string.IsNullOrWhiteSpace(userId);
        }
        catch
        {
            return false;
        }
    }

    private (string Token, int ExpiresInSeconds) CreateAccessToken(
        ApplicationUser user,
        IList<string> roles,
        IReadOnlyList<string> permissions,
        IReadOnlyList<string> modules)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? user.Email ?? user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in permissions)
        {
            claims.Add(new Claim(AuthClaimTypes.Permission, permission));
        }

        foreach (var module in modules)
        {
            claims.Add(new Claim(AuthClaimTypes.Module, module));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), _jwtOptions.AccessTokenMinutes * 60);
    }

    private async Task<(string PlainTextToken, string TokenHash)> CreateRefreshTokenAsync(
        ApplicationUser user,
        bool rememberMe,
        CancellationToken cancellationToken)
    {
        var plainTextToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var tokenHash = HashToken(plainTextToken);
        var lifetimeDays = rememberMe
            ? _jwtOptions.RefreshTokenRememberMeDays
            : _jwtOptions.RefreshTokenDays;

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(lifetimeDays),
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return (plainTextToken, tokenHash);
    }

    private static string HashToken(string token)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes);
    }

    private async Task<AuthUserDto> MapUserAsync(
        ApplicationUser user,
        IList<string> roles,
        IReadOnlyList<string> permissions,
        IReadOnlyList<string> modules) =>
        new(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.DisplayName,
            roles.ToList(),
            permissions.ToList(),
            modules.ToList(),
            user.MustChangePassword,
            await userManager.GetTwoFactorEnabledAsync(user));
}