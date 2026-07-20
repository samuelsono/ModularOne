import { useEffect, useState } from 'react';
import {
  Button,
  Checkbox,
  Dropdown,
  Field,
  Option,
  Subtitle2,
  Text,
} from '@fluentui/react-components';
import { APP_MODULES } from '@platform/permissions/apps';
import { getPlatformSettings, updatePlatformSettings } from '@modules/settings/services/settingsService';
import { useActiveApp } from '@platform/shell/ActiveAppContext';
import { getAuthErrorMessage } from '@platform/auth/AuthContext';
import {
  DEFAULT_THEME_NAME,
  availableThemeNames,
  resolveThemeByName,
  type ThemeName,
} from '../../../theme';

function normalizeThemeName(themeName?: string | null): ThemeName {
  if (!themeName) {
    return DEFAULT_THEME_NAME;
  }

  const fallbackTheme = resolveThemeByName(DEFAULT_THEME_NAME);
  const selectedTheme = resolveThemeByName(themeName);
  if (themeName !== DEFAULT_THEME_NAME && selectedTheme === fallbackTheme) {
    return DEFAULT_THEME_NAME;
  }

  return themeName as ThemeName;
}

function normalizeAppThemeMap(
  appThemeMap?: Record<string, string> | null,
): Record<string, ThemeName> {
  if (!appThemeMap) {
    return {};
  }

  const normalized: Record<string, ThemeName> = {};
  for (const [slug, themeName] of Object.entries(appThemeMap)) {
    if (!slug || !themeName) {
      continue;
    }

    normalized[slug.toLowerCase()] = normalizeThemeName(themeName);
  }

  return normalized;
}

export function InstalledAppsPanel() {
  const { refreshPlatformSettings } = useActiveApp();
  const [selectedSlugs, setSelectedSlugs] = useState<Set<string>>(new Set());
  const [defaultThemeName, setDefaultThemeName] = useState<ThemeName>(DEFAULT_THEME_NAME);
  const [appThemeNamesBySlug, setAppThemeNamesBySlug] = useState<Record<string, ThemeName>>({});
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    void getPlatformSettings()
      .then((settings) => {
        setSelectedSlugs(new Set(settings.installedAppSlugs));
        setDefaultThemeName(normalizeThemeName(settings.defaultThemeName));
        setAppThemeNamesBySlug(normalizeAppThemeMap(settings.appThemeNamesByModuleSlug));
      })
      .catch(() => {
        // Default to all apps if fetch fails
        setSelectedSlugs(new Set(APP_MODULES.map((m) => m.slug)));
        setDefaultThemeName(DEFAULT_THEME_NAME);
        setAppThemeNamesBySlug({});
      });
  }, []);

  async function handleSave() {
    if (selectedSlugs.size === 0) {
      setError('At least one application must be installed.');
      return;
    }

    setIsSaving(true);
    setError(null);
    setSuccess(null);

    try {
      const orderedInstalledSlugs = APP_MODULES
        .map((module) => module.slug)
        .filter((slug) => selectedSlugs.has(slug));

      const cleanedAppThemeMap: Record<string, string> = {};
      for (const slug of orderedInstalledSlugs) {
        const appTheme = appThemeNamesBySlug[slug];
        if (appTheme && appTheme !== defaultThemeName) {
          cleanedAppThemeMap[slug] = appTheme;
        }
      }

      await updatePlatformSettings({
        installedAppSlugs: orderedInstalledSlugs,
        defaultThemeName,
        appThemeNamesByModuleSlug: cleanedAppThemeMap,
      });
      await refreshPlatformSettings();
      setSuccess('Installed applications and themes updated successfully.');
    } catch (saveError) {
      setError(getAuthErrorMessage(saveError, 'Could not update installed applications.'));
    } finally {
      setIsSaving(false);
    }
  }

  function toggleApp(slug: string) {
    const newSlugs = new Set(selectedSlugs);
    if (newSlugs.has(slug)) {
      newSlugs.delete(slug);
    } else {
      newSlugs.add(slug);
    }
    setSelectedSlugs(newSlugs);
  }

  function setAppTheme(slug: string, themeName: ThemeName) {
    setAppThemeNamesBySlug((current) => ({
      ...current,
      [slug]: themeName,
    }));
  }

  return (
    <div className="max-w-xl flex flex-col gap-4">
      <div className="flex flex-col">
        <Subtitle2>Installed applications</Subtitle2>
        <Text className="text-sm text-neutral-foreground-3 block mt-1">
          Select which applications are available for users. Users will only be able to access and switch between these installed applications.
        </Text>
      </div>

      <Field label="Default theme">
        <Dropdown
          value={defaultThemeName}
          selectedOptions={[defaultThemeName]}
          onOptionSelect={(_event, data) => {
            if (data.optionValue) {
              setDefaultThemeName(data.optionValue as ThemeName);
            }
          }}
        >
          {availableThemeNames.map((themeName) => (
            <Option key={themeName} value={themeName} text={themeName}>
              {themeName}
            </Option>
          ))}
        </Dropdown>
      </Field>

      <Text className="text-xs text-neutral-foreground-3 block -mt-2">
        This theme is used whenever an app has no specific theme configured.
      </Text>

      <div className="flex flex-col gap-3">
        {APP_MODULES.map((module) => (
          <div key={module.slug} className="rounded border border-neutral-stroke-2 p-3 flex flex-col gap-2">
            <div className="flex flex-col items-center gap-2">
              <Checkbox
                checked={selectedSlugs.has(module.slug)}
                onChange={() => toggleApp(module.slug)}
                label={module.name}
              />
              <Text className="text-sm text-neutral-foreground-3">
                {module.description}
              </Text>
            </div>

            {selectedSlugs.has(module.slug) && (
              <Field label="Theme for this app">
                <Dropdown
                  value={appThemeNamesBySlug[module.slug] ?? defaultThemeName}
                  selectedOptions={[appThemeNamesBySlug[module.slug] ?? defaultThemeName]}
                  onOptionSelect={(_event, data) => {
                    if (data.optionValue) {
                      setAppTheme(module.slug, data.optionValue as ThemeName);
                    }
                  }}
                >
                  {availableThemeNames.map((themeName) => (
                    <Option key={`${module.slug}-${themeName}`} value={themeName} text={themeName}>
                      {themeName}
                    </Option>
                  ))}
                </Dropdown>
              </Field>
            )}
          </div>
        ))}
      </div>

      {error && (
        <Text className="text-sm text-palette-red-foreground-1">{error}</Text>
      )}
      {success && (
        <Text className="text-sm text-palette-green-foreground-1">{success}</Text>
      )}

      <div>
        <Button appearance="primary" onClick={() => void handleSave()} disabled={isSaving}>
          {isSaving ? 'Saving...' : 'Save installed applications'}
        </Button>
      </div>
    </div>
  );
}
