import { Combobox, Option, Spinner, Text } from '@fluentui/react-components';
import { useEffect, useState } from 'react';

import type { DashboardSummary } from '../../types/dashboard';
import { getDashboards } from '../../services/dashboardService';
import { getSelectedDashboardId, setSelectedDashboardId } from '../../utils/dashboardStorage';

interface DashboardPickerProps {
  value: string | null;
  onChange: (dashboardId: string) => void;
  className?: string;
  refreshToken?: number;
}

export function DashboardPicker({ value, onChange, className, refreshToken = 0 }: DashboardPickerProps) {
  const [dashboards, setDashboards] = useState<DashboardSummary[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      setLoading(true);
      try {
        const response = await getDashboards();
        if (cancelled) {
          return;
        }

        setDashboards(response.items);

        if (response.items.length === 0) {
          return;
        }

        const storedId = getSelectedDashboardId();
        const stored = storedId ? response.items.find((item) => item.id === storedId) : null;
        const defaultDashboard = response.items.find((item) => item.isDefault);
        const selected = stored ?? defaultDashboard ?? response.items[0];

        if (!value || !response.items.some((item) => item.id === value)) {
          onChange(selected.id);
          setSelectedDashboardId(selected.id);
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [onChange, refreshToken, value]);

  if (loading) {
    return (
      <div className={`flex items-center gap-2 ${className ?? ''}`}>
        <Spinner size="tiny" />
        <Text size={200}>Loading dashboards...</Text>
      </div>
    );
  }

  if (dashboards.length === 0) {
    return <Text size={200}>No dashboards available</Text>;
  }

  const selected = dashboards.find((item) => item.id === value) ?? dashboards[0];

  return (
    <Combobox
      className={className}
      value={selected.name}
      onOptionSelect={(_, data) => {
        const id = data.optionValue;
        if (!id) {
          return;
        }

        onChange(id);
        setSelectedDashboardId(id);
      }}
      aria-label="Select dashboard"
    >
      {dashboards.map((dashboard) => (
        <Option key={dashboard.id} value={dashboard.id} text={dashboard.name}>
          {dashboard.name}{dashboard.isDefault ? ' (Default)' : ''}
        </Option>
      ))}
    </Combobox>
  );
}
