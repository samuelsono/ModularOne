import { useCallback, useEffect, useState } from 'react';
import {
  Button,
  Field,
  MessageBar,
  MessageBarBody,
  Radio,
  RadioGroup,
  Spinner,
  Subtitle2,
  Text,
} from '@fluentui/react-components';
import { ApiError } from '@platform/api/apiClient';
import { getAttendancePolicy, updateAttendancePolicy } from '@modules/leave/services/leaveService';
import type { AttendanceDefaultAssumption, AttendancePolicySettings } from '@modules/leave/types/leave';
import AppTitle from '@platform/ui/AppTitle';

interface AttendancePolicyManagerProps {
  canWrite: boolean;
}

export function AttendancePolicyManager({ canWrite }: AttendancePolicyManagerProps) {
  const [policy, setPolicy] = useState<AttendancePolicySettings | null>(null);
  const [assumption, setAssumption] = useState<AttendanceDefaultAssumption>('Present');
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const settings = await getAttendancePolicy();
      setPolicy(settings);
      setAssumption(settings.defaultAssumption === 'Absent' ? 'Absent' : 'Present');
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load attendance policy.');
      setPolicy(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const save = async () => {
    if (!canWrite) {
      return;
    }

    setIsSaving(true);
    setError(null);
    setMessage(null);
    try {
      const updated = await updateAttendancePolicy({ defaultAssumption: assumption });
      setPolicy(updated);
      setAssumption(updated.defaultAssumption === 'Absent' ? 'Absent' : 'Present');
      setMessage(`Attendance policy saved (default ${updated.defaultAssumption}).`);
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : 'Failed to save attendance policy.');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <section className="flex flex-col gap-4 px-3">
      <AppTitle
        title="Attendance policy"
        subtitle="Controls the quick Present / Absent mark on the attendance calendar."
      />

      {error ? (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}
      {message ? (
        <MessageBar intent="success">
          <MessageBarBody>{message}</MessageBarBody>
        </MessageBar>
      ) : null}

      {isLoading ? (
        <Spinner label="Loading attendance policy..." />
      ) : (
        <div className="flex flex-col gap-4 max-w-xl">
          <Subtitle2>Default assumption</Subtitle2>
          <Text size={200} className="text-neutral-foreground-3">
            When managers use the quick mark button, Present uses the scheduled location
            (or Office if unscheduled). Absent records the Absent location.
          </Text>
          <Field label="Quick mark defaults to">
            <RadioGroup
              value={assumption}
              onChange={(_, data) => setAssumption(data.value as AttendanceDefaultAssumption)}
              disabled={!canWrite || isSaving}
            >
              <Radio value="Present" label="Present" />
              <Radio value="Absent" label="Absent" />
            </RadioGroup>
          </Field>
          {canWrite ? (
            <div>
              <Button
                appearance="primary"
                disabled={isSaving || assumption === policy?.defaultAssumption}
                onClick={() => void save()}
              >
                {isSaving ? 'Saving...' : 'Save policy'}
              </Button>
            </div>
          ) : null}
        </div>
      )}
    </section>
  );
}
