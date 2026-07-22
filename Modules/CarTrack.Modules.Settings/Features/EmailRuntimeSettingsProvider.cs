using CarTrack.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Settings;

public sealed class EmailRuntimeSettingsProvider(
    SettingsDbContext dbContext,
    EmailSettingsService emailSettingsService) : IEmailRuntimeSettingsProvider
{
    public async Task<EmailRuntimeConfig?> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await dbContext.EmailSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == EmailSettings.SingletonId, cancellationToken);

        if (settings is null)
        {
            return null;
        }

        return emailSettingsService.TryBuildRuntimeConfig(settings);
    }
}
