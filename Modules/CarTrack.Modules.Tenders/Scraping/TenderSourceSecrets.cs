using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace CarTrack.Modules.Tenders;

public record TenderDocumentMeta(
    string Url,
    string? ContentType,
    string? FileName,
    long? ContentLength);

public interface ITenderSourceSecretProtector
{
    string Protect(string plaintext);

    string? Unprotect(string? protectedBase64);
}

public sealed class TenderSourceSecretProtector(
    IDataProtectionProvider dataProtectionProvider,
    ILogger<TenderSourceSecretProtector> logger) : ITenderSourceSecretProtector
{
    public const string ProtectorPurpose = "CarTrack.Tenders.SourceAuthSecret";

    private readonly IDataProtector _protector =
        dataProtectionProvider.CreateProtector(ProtectorPurpose);

    public string Protect(string plaintext) =>
        Convert.ToBase64String(_protector.Protect(Encoding.UTF8.GetBytes(plaintext)));

    public string? Unprotect(string? protectedBase64)
    {
        if (string.IsNullOrWhiteSpace(protectedBase64))
        {
            return null;
        }

        try
        {
            return Encoding.UTF8.GetString(_protector.Unprotect(Convert.FromBase64String(protectedBase64)));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to unprotect tender source auth secret.");
            return null;
        }
    }
}

public static class TenderDocumentMetadata
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string? Serialize(IEnumerable<TenderDocumentMeta> items)
    {
        var list = items.ToList();
        return list.Count == 0 ? null : JsonSerializer.Serialize(list, JsonOptions);
    }

    public static IReadOnlyList<TenderDocumentMeta> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<TenderDocumentMeta>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
