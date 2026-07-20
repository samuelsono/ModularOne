using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CarTrack.Modules.Tenders;

public interface ITenderFetchCache
{
    Task<string?> TryGetAsync(string url, CancellationToken cancellationToken = default);

    Task SetAsync(string url, string body, CancellationToken cancellationToken = default);
}

/// <summary>
/// Memory cache always; uses <see cref="IDistributedCache"/> (Redis) when registered by the Host.
/// </summary>
public sealed class TenderFetchCache(
    IMemoryCache memoryCache,
    IOptions<TenderScrapeOptions> options,
    IServiceProvider services) : ITenderFetchCache
{
    private readonly IDistributedCache? _distributed = services.GetService(typeof(IDistributedCache)) as IDistributedCache;

    public async Task<string?> TryGetAsync(string url, CancellationToken cancellationToken = default)
    {
        var key = CacheKey(url);
        if (memoryCache.TryGetValue(key, out string? memoryHit) && memoryHit is not null)
        {
            return memoryHit;
        }

        if (_distributed is null)
        {
            return null;
        }

        var bytes = await _distributed.GetAsync(key, cancellationToken);
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        var body = System.Text.Encoding.UTF8.GetString(bytes);
        memoryCache.Set(key, body, TimeSpan.FromMinutes(Math.Max(1, options.Value.FetchCacheTtlMinutes)));
        return body;
    }

    public async Task SetAsync(string url, string body, CancellationToken cancellationToken = default)
    {
        var key = CacheKey(url);
        var ttl = TimeSpan.FromMinutes(Math.Max(1, options.Value.FetchCacheTtlMinutes));
        memoryCache.Set(key, body, ttl);

        if (_distributed is not null)
        {
            await _distributed.SetAsync(
                key,
                System.Text.Encoding.UTF8.GetBytes(body),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
                cancellationToken);
        }
    }

    private static string CacheKey(string url) => "tenders:fetch:" + TenderUrlNormalizer.Sha256Hex(url);
}

/// <summary>
/// Warm per-source set of known ExternalKeys to avoid repeated DB round-trips within a process.
/// </summary>
public sealed class TenderKnownKeyCache
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _keys = new();

    public ConcurrentDictionary<string, byte> GetOrCreate(Guid sourceId) =>
        _keys.GetOrAdd(sourceId, _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase));

    public void Remember(Guid sourceId, string externalKey) =>
        GetOrCreate(sourceId).TryAdd(externalKey, 0);

    public void Invalidate(Guid sourceId) => _keys.TryRemove(sourceId, out _);
}
