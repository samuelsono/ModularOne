import { useEffect, useState } from 'react';
import {
  Button,
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

export function PlatformApplicationsPanel() {
  const { defaultModuleSlug, refreshPlatformSettings } = useActiveApp();
  const [selectedSlug, setSelectedSlug] = useState(defaultModuleSlug);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    setSelectedSlug(defaultModuleSlug);
  }, [defaultModuleSlug]);

  useEffect(() => {
    void getPlatformSettings()
      .then((settings) => setSelectedSlug(settings.defaultModuleSlug))
      .catch(() => {
        // Active app context already loaded the overview default.
      });
  }, []);

  async function handleSave() {
    setIsSaving(true);
    setError(null);
    setSuccess(null);

    try {
      await updatePlatformSettings({ defaultModuleSlug: selectedSlug });
      await refreshPlatformSettings();
      setSuccess('Default application updated. It will always stay active for all users.');
    } catch (saveError) {
      setError(getAuthErrorMessage(saveError, 'Could not update the default application.'));
    } finally {
      setIsSaving(false);
    }
  }

  const selectedModule = APP_MODULES.find((module) => module.slug === selectedSlug);

  return (
    <div className="max-w-xl flex flex-col gap-4">
      <div>
        <Subtitle2>Default application</Subtitle2>
        <Text className="text-sm text-neutral-foreground-3 block mt-1">
          Choose which application opens by default and remains always active in the app launcher for every user.
        </Text>
      </div>

      <Field label="Default app module">
        <Dropdown
          value={selectedModule?.name ?? selectedSlug}
          selectedOptions={[selectedSlug]}
          onOptionSelect={(_event, data) => {
            if (data.optionValue) {
              setSelectedSlug(data.optionValue);
            }
          }}
        >
          {APP_MODULES.map((module) => (
            <Option key={module.slug} value={module.slug} text={module.name}>
              {module.name}
            </Option>
          ))}
        </Dropdown>
      </Field>

      {error && (
        <Text className="text-sm text-palette-red-foreground-1">{error}</Text>
      )}
      {success && (
        <Text className="text-sm text-palette-green-foreground-1">{success}</Text>
      )}

      <div>
        <Button appearance="primary" onClick={() => void handleSave()} disabled={isSaving}>
          {isSaving ? 'Saving...' : 'Save default application'}
        </Button>
      </div>
    </div>
  );
}
