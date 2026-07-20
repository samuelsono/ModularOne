using System.Collections.Concurrent;

namespace CarTrack.Modules.Tenders;

/// <summary>
/// Limits global and per-host scrape concurrency so periodic jobs stay polite.
/// </summary>
public sealed class TenderHostConcurrencyGate
{
    private readonly SemaphoreSlim _global;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _perHost = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _perHostLimit;

    public TenderHostConcurrencyGate(Microsoft.Extensions.Options.IOptions<TenderScrapeOptions> options)
    {
        var value = options.Value;
        _global = new SemaphoreSlim(Math.Max(1, value.MaxGlobalConcurrency));
        _perHostLimit = Math.Max(1, value.MaxPerHostConcurrency);
    }

    public async Task<IAsyncDisposable> AcquireAsync(Uri uri, CancellationToken cancellationToken)
    {
        var host = uri.Host;
        var hostGate = _perHost.GetOrAdd(host, _ => new SemaphoreSlim(_perHostLimit));

        await _global.WaitAsync(cancellationToken);
        try
        {
            await hostGate.WaitAsync(cancellationToken);
        }
        catch
        {
            _global.Release();
            throw;
        }

        return new Releaser(_global, hostGate);
    }

    private sealed class Releaser(SemaphoreSlim global, SemaphoreSlim host) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            host.Release();
            global.Release();
            return ValueTask.CompletedTask;
        }
    }
}
