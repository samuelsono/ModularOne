using CarTrack.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Settings;

public interface IPlatformSettingsService
{
    Task<PlatformSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<PlatformSettingsDto> UpdateAsync(
        UpdatePlatformSettingsRequest request,
        CancellationToken cancellationToken = default);
}

public class PlatformSettingsService(ApplicationDbContext dbContext) : IPlatformSettingsService
{
    private static readonly HashSet<string> ValidModuleSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "accounting",
        "leave",
        "expense",
        "payroll",
        "performance",
        "recruitment",
        "fleet",
    };

    public async Task<PlatformSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        return ToDto(settings);
    }

    public async Task<PlatformSettingsDto> UpdateAsync(
        UpdatePlatformSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var slug = request.DefaultModuleSlug.Trim();
        if (!ValidModuleSlugs.Contains(slug))
        {
            throw new ArgumentException(
                $"Invalid default module slug '{slug}'.",
                nameof(request));
        }

        var settings = await GetOrCreateAsync(cancellationToken);
        settings.DefaultModuleSlugValue = slug.ToLowerInvariant();
        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(settings);
    }

    private async Task<PlatformSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.PlatformSettings
            .FirstOrDefaultAsync(item => item.Id == PlatformSettings.SingletonId, cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = new PlatformSettings
        {
            Id = PlatformSettings.SingletonId,
            DefaultModuleSlugValue = PlatformSettings.DefaultModuleSlug,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.PlatformSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);

        return settings;
    }

    private static PlatformSettingsDto ToDto(PlatformSettings settings) => new(
        settings.DefaultModuleSlugValue,
        settings.UpdatedAt.ToString("O"));
}
