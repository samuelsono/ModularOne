using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Settings;

public class PlatformSettingsService(SettingsDbContext dbContext) : IPlatformSettingsService
{
    private static readonly HashSet<string> ValidModuleSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "accounting",
        "leave",
        "expense",
        "payroll",
        "performance",
        "recruitment",
        "tenders",
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
        var settings = await GetOrCreateAsync(cancellationToken);
        bool updated = false;

        if (!string.IsNullOrWhiteSpace(request.DefaultModuleSlug))
        {
            var slug = request.DefaultModuleSlug.Trim();
            if (!ValidModuleSlugs.Contains(slug))
            {
                throw new ArgumentException(
                    $"Invalid default module slug '{slug}'.",
                    nameof(request));
            }

            settings.DefaultModuleSlugValue = slug.ToLowerInvariant();
            updated = true;
        }

        if (request.InstalledAppSlugs is not null && request.InstalledAppSlugs.Length > 0)
        {
            // Validate all installed app slugs
            foreach (var slug in request.InstalledAppSlugs)
            {
                if (!ValidModuleSlugs.Contains(slug))
                {
                    throw new ArgumentException(
                        $"Invalid app slug '{slug}' in InstalledAppSlugs.",
                        nameof(request));
                }
            }

            // Ensure default module is in installed apps
            var normalizedSlugs = request.InstalledAppSlugs
                .Select(s => s.ToLowerInvariant())
                .Distinct()
                .OrderBy(s => s)
                .ToArray();

            if (!normalizedSlugs.Contains(settings.DefaultModuleSlugValue))
            {
                throw new ArgumentException(
                    $"Default module slug '{settings.DefaultModuleSlugValue}' must be in InstalledAppSlugs.",
                    nameof(request));
            }

            settings.InstalledAppSlugs = string.Join(",", normalizedSlugs);
            updated = true;
        }

        if (updated)
        {
            settings.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

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
            InstalledAppSlugs = PlatformSettings.DefaultInstalledAppSlugs,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.PlatformSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);

        return settings;
    }

    private static PlatformSettingsDto ToDto(PlatformSettings settings)
    {
        var installedSlugs = settings.InstalledAppSlugs
            .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim());

        return new(
            settings.DefaultModuleSlugValue,
            installedSlugs.ToArray(),
            settings.UpdatedAt.ToString("O"));
    }
}
