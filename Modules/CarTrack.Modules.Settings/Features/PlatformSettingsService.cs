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

    private static readonly HashSet<string> ValidThemeNames = new(StringComparer.Ordinal)
    {
        "nelotecLightTheme",
        "nelotecDarkTheme",
        "bronzeLightTheme",
        "bronzeDarkTheme",
        "talisFleetLight",
        "talisFleetDark",
        "talisLightTheme",
        "talisDarkTheme",
        "lightBlackTheme",
        "darkBlackTheme",
        "lightTeamsTheme",
        "darkTeamsTheme",
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

        if (!string.IsNullOrWhiteSpace(request.DefaultThemeName))
        {
            var themeName = request.DefaultThemeName.Trim();
            if (!ValidThemeNames.Contains(themeName))
            {
                throw new ArgumentException(
                    $"Invalid default theme name '{themeName}'.",
                    nameof(request));
            }

            settings.DefaultThemeNameValue = themeName;
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

        if (request.AppThemeNamesByModuleSlug is not null)
        {
            var themeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in request.AppThemeNamesByModuleSlug)
            {
                var slug = pair.Key?.Trim().ToLowerInvariant() ?? string.Empty;
                var themeName = pair.Value?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(themeName))
                {
                    continue;
                }

                if (!ValidModuleSlugs.Contains(slug))
                {
                    throw new ArgumentException(
                        $"Invalid app slug '{slug}' in AppThemeNamesByModuleSlug.",
                        nameof(request));
                }

                if (!ValidThemeNames.Contains(themeName))
                {
                    throw new ArgumentException(
                        $"Invalid theme name '{themeName}' in AppThemeNamesByModuleSlug.",
                        nameof(request));
                }

                themeMap[slug] = themeName;
            }

            settings.AppThemeNamesByModuleSlug = SerializeAppThemeMap(themeMap);
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
            DefaultThemeNameValue = PlatformSettings.DefaultThemeName,
            InstalledAppSlugs = PlatformSettings.DefaultInstalledAppSlugs,
            AppThemeNamesByModuleSlug = string.Empty,
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

        var appThemeMap = ParseAppThemeMap(settings.AppThemeNamesByModuleSlug);

        return new(
            settings.DefaultModuleSlugValue,
            installedSlugs.ToArray(),
            settings.DefaultThemeNameValue,
            appThemeMap,
            settings.UpdatedAt.ToString("O"));
    }

    private static Dictionary<string, string> ParseAppThemeMap(string serialized)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return result;
        }

        var entries = serialized.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var entry in entries)
        {
            var parts = entry.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                continue;
            }

            var slug = parts[0].ToLowerInvariant();
            var themeName = parts[1];
            if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(themeName))
            {
                continue;
            }

            result[slug] = themeName;
        }

        return result;
    }

    private static string SerializeAppThemeMap(Dictionary<string, string> appThemeMap)
    {
        var pairs = appThemeMap
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}:{pair.Value}");

        return string.Join(",", pairs);
    }
}
