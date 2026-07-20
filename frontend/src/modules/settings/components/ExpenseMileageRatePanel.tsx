import { useEffect, useState } from 'react';
import {
  Button,
  Field,
  Input,
  Subtitle2,
  Text,
} from '@fluentui/react-components';
import { getExpenseSettings, updateExpenseSettings } from '@modules/settings/services/settingsService';
import { getAuthErrorMessage } from '@platform/auth/AuthContext';

function parseRate(value: string): number | null {
  const parsed = Number.parseFloat(value.trim());
  if (!Number.isFinite(parsed) || parsed <= 0) {
    return null;
  }

  return Math.round(parsed * 100) / 100;
}

export function ExpenseMileageRatePanel() {
  const [rateInput, setRateInput] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    void getExpenseSettings()
      .then((settings) => {
        setRateInput(settings.kilometerRate > 0 ? settings.kilometerRate.toFixed(2) : '');
      })
      .catch(() => {
        setRateInput('');
      });
  }, []);

  async function handleSave() {
    const kilometerRate = parseRate(rateInput);
    if (kilometerRate === null) {
      setError('Enter a kilometer rate greater than zero.');
      return;
    }

    setIsSaving(true);
    setError(null);
    setSuccess(null);

    try {
      const updated = await updateExpenseSettings({ kilometerRate });
      setRateInput(updated.kilometerRate.toFixed(2));
      setSuccess('Mileage rate updated. Travel claims will use this rate per kilometer.');
    } catch (saveError) {
      setError(getAuthErrorMessage(saveError, 'Could not update mileage rate.'));
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <div className="max-w-xl flex flex-col gap-4">
      <div>
        <Subtitle2>Mileage reimbursement rate</Subtitle2>
        <Text className="text-sm text-neutral-foreground-3 block mt-1">
          This rate is used for categories that reimburse by kilometer.
        </Text>
      </div>

      <Field label="Rate per kilometer (ZAR)" required>
        <Input
          type="number"
          step="0.01"
          min="0"
          value={rateInput}
          onChange={(_, data) => setRateInput(data.value)}
        />
      </Field>

      {error && (
        <Text className="text-sm text-palette-red-foreground-1">{error}</Text>
      )}
      {success && (
        <Text className="text-sm text-palette-green-foreground-1">{success}</Text>
      )}

      <div>
        <Button appearance="primary" onClick={() => void handleSave()} disabled={isSaving}>
          {isSaving ? 'Saving...' : 'Save mileage rate'}
        </Button>
      </div>
    </div>
  );
}
