using CarTrack.Api;
using CarTrack.Infrastructure.Persistence;

namespace CarTrack.Modules.Tenders;

public sealed class TendersModule : IModule
{
    public const string MigrationsHistoryTable = "__EFMigrationsHistory_Tenders";
    public const string ProbeTable = "TenderSources";

    public string Name => "tenders";

    public void AddModule(IHostApplicationBuilder builder)
    {
        builder.AddModuleNpgsqlDbContext<TendersDbContext>("cartrack", MigrationsHistoryTable);
        builder.Services.Configure<TenderScrapeOptions>(
            builder.Configuration.GetSection(TenderScrapeOptions.SectionName));

        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<TenderHostConcurrencyGate>();
        builder.Services.AddSingleton<TenderKnownKeyCache>();
        builder.Services.AddSingleton<ITenderFetchCache, TenderFetchCache>();
        builder.Services.AddSingleton<ITenderSourceSecretProtector, TenderSourceSecretProtector>();
        builder.Services.AddSingleton<ITenderBrowserRenderer, NullTenderBrowserRenderer>();
        builder.Services.AddSingleton<HttpTenderPageFetcher>();
        builder.Services.AddSingleton<PlaywrightTenderPageFetcher>();
        builder.Services.AddSingleton<ITenderPageFetcher, TenderPageFetchRouter>();
        builder.Services.AddSingleton<IETendersOcdsFetcher, ETendersOcdsFetcher>();

        builder.Services.AddScoped<ITenderSourceService, TenderSourceService>();
        builder.Services.AddScoped<ITenderQueryService, TenderQueryService>();
        builder.Services.AddScoped<ITenderMatchService, TenderMatchService>();
        builder.Services.AddScoped<ITenderScrapeService, TenderScrapeService>();
        builder.Services.AddScoped<ITenderWatchSubscriptionService, TenderWatchSubscriptionService>();

        builder.Services.AddSingleton<RssAtomTenderParser>();
        builder.Services.AddSingleton<ETendersTenderParser>();
        builder.Services.AddSingleton<HtmlTenderParser>();
        builder.Services.AddSingleton<TenderParserRegistry>(sp =>
            new TenderParserRegistry(
            [
                sp.GetRequiredService<RssAtomTenderParser>(),
                sp.GetRequiredService<ETendersTenderParser>(),
                sp.GetRequiredService<HtmlTenderParser>(),
            ]));

        builder.Services.AddHttpClient(TenderScrapeService.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "ChronosTenderSearch/1.0 (+https://localhost; contact=admin@cartrack.local)");
        });

        // National Treasury OCDS API presents an incomplete certificate chain on many hosts.
        // Skip Aspire's default resilience pipeline — retries/timeouts make multi-page OCDS scrapes very slow.
        var ocdsHttp = builder.Services.AddHttpClient(ETendersOcdsFetcher.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "ChronosTenderSearch/1.0 (+https://localhost; contact=admin@cartrack.local)");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            });
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is experimental but required to opt out of defaults.
        ocdsHttp.RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001
        ocdsHttp.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = static (message, _, _, errors) =>
                {
                    if (errors == System.Net.Security.SslPolicyErrors.None)
                    {
                        return true;
                    }

                    var host = message.RequestUri?.Host ?? string.Empty;
                    return host.Equals("ocds-api.etenders.gov.za", StringComparison.OrdinalIgnoreCase);
                },
            });


        builder.Services.AddHostedService<TenderScrapeBackgroundService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/tenders").MapTendersEndpoints();

    public async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TendersDbContext>();
        await db.MigrateModuleAsync(MigrationsHistoryTable, ProbeTable, cancellationToken);
    }

    public Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
