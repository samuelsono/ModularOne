import { useEffect, useState } from 'react';
import {
  Button,
  Checkbox,
  Subtitle2,
  Text,
} from '@fluentui/react-components';
import { APP_MODULES } from '@platform/permissions/apps';
import { getPlatformSettings, updatePlatformSettings } from '@modules/settings/services/settingsService';
import { useActiveApp } from '@platform/shell/ActiveAppContext';
import { getAuthErrorMessage } from '@platform/auth/AuthContext';

export function InstalledAppsPanel() {
  const { refreshPlatformSettings } = useActiveApp();
  const [selectedSlugs, setSelectedSlugs] = useState<Set<string>>(new Set());
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    void getPlatformSettings()
      .then((settings) => {
        setSelectedSlugs(new Set(settings.installedAppSlugs));
      })
      .catch(() => {
        // Default to all apps if fetch fails
        setSelectedSlugs(new Set(APP_MODULES.map((m) => m.slug)));
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
      await updatePlatformSettings({
        installedAppSlugs: Array.from(selectedSlugs),
      });
      await refreshPlatformSettings();
      setSuccess('Installed applications updated. Users will only see the selected apps.');
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

  return (
    <div className="max-w-xl flex flex-col gap-4">
      <div className="flex flex-col">
        <Subtitle2>Installed applications</Subtitle2>
        <Text className="text-sm text-neutral-foreground-3 block mt-1">
          Select which applications are available for users. Users will only be able to access and switch between these installed applications.
        </Text>
      </div>

      <div className="flex flex-col gap-3">
        {APP_MODULES.map((module) => (
          <div key={module.slug} className="flex items-center gap-2">
            <Checkbox
              checked={selectedSlugs.has(module.slug)}
              onChange={() => toggleApp(module.slug)}
              label={module.name}
            />
            <Text className="text-sm text-neutral-foreground-3">
              {module.description}
            </Text>
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
