namespace CarTrack.Infrastructure.Auth;

public static class ExternalAuthProviders
{
    public const string Google = "Google";
    public const string Microsoft = "Microsoft";
}

public sealed record ExternalOAuthCredentials(
    string Provider,
    string ClientId,
    string ClientSecret,
    string? TenantId);

public sealed record ExternalAuthProviderInfo(
    string Provider,
    bool IsActivated,
    string? ClientId);

public interface IExternalAuthCredentialProvider
{
    Task<IReadOnlyList<ExternalAuthProviderInfo>> GetProviderStatusesAsync(
        CancellationToken cancellationToken = default);

    Task<ExternalOAuthCredentials?> GetCredentialsAsync(
        string provider,
        CancellationToken cancellationToken = default);
}
