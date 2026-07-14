import { Button, Spinner, Subtitle2, Text } from '@fluentui/react-components';
import { EditRegular } from '@fluentui/react-icons';
import { useEffect, useMemo, useState } from 'react';

import type { DashboardRender } from '../../types/dashboard';
import { usePageSearchQuery } from '../../context/PageSearchContext';
import { getDashboardRender } from '../../services/dashboardService';
import { getSelectedDashboardId, setSelectedDashboardId, DASHBOARD_CHANGED_EVENT } from '../../utils/dashboardStorage';
import { filterDashboardRender } from '../../utils/pageSearch';
import { DashboardLayoutEditor } from './DashboardLayoutEditor';
import { DashboardPicker } from './DashboardPicker';
import { DashboardSectionView } from './DashboardSectionView';

export function DashboardRenderer() {
  const searchQuery = usePageSearchQuery();
  const [dashboardId, setDashboardId] = useState<string | null>(getSelectedDashboardId());
  const [dashboard, setDashboard] = useState<DashboardRender | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editingLayout, setEditingLayout] = useState(false);
  const [refreshToken, setRefreshToken] = useState(0);

  useEffect(() => {
    if (!dashboardId) {
      return;
    }

    let cancelled = false;

    const load = async () => {
      setLoading(true);
      setError(null);

      try {
        const result = await getDashboardRender(dashboardId);
        if (!cancelled) {
          setDashboard(result);
          setSelectedDashboardId(dashboardId);
        }
      } catch {
        if (!cancelled) {
          setError('Unable to load dashboard.');
          setDashboard(null);
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
  }, [dashboardId, refreshToken]);

  useEffect(() => {
    const handleDashboardChanged = () => setRefreshToken((value) => value + 1);

    window.addEventListener(DASHBOARD_CHANGED_EVENT, handleDashboardChanged);
    return () => window.removeEventListener(DASHBOARD_CHANGED_EVENT, handleDashboardChanged);
  }, []);

  const handleDashboardChange = (id: string) => {
    setEditingLayout(false);
    setDashboardId(id);
  };

  const handleLayoutSaved = () => {
    setEditingLayout(false);
    setRefreshToken((value) => value + 1);
  };

  const visibleDashboard = useMemo(
    () => (dashboard ? filterDashboardRender(dashboard, searchQuery) : null),
    [dashboard, searchQuery],
  );

  if (editingLayout && dashboardId) {
    return (
      <DashboardLayoutEditor
        dashboardId={dashboardId}
        onSaved={handleLayoutSaved}
        onCancel={() => setEditingLayout(false)}
      />
    );
  }

  return (
    <div className="flex flex-col gap-4 w-full h-full px-16 py-5 overflow-y-auto">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-3 min-w-0">
          <Subtitle2 className="mb-0">{dashboard?.name ?? 'Dashboard'}</Subtitle2>
          <DashboardPicker value={dashboardId} onChange={handleDashboardChange} className="min-w-[220px]" />
        </div>
        {dashboardId && (
          <Button
            appearance="secondary"
            icon={<EditRegular />}
            onClick={() => setEditingLayout(true)}
            disabled={loading}
          >
            Edit layout
          </Button>
        )}
      </div>

      {loading && (
        <div className="flex w-full h-full flex-col items-center gap-2 px-3">
          <Spinner size="small" />
          <Text>Loading dashboard...</Text>
        </div>
      )}

      {!loading && error && <Text className="px-3">{error}</Text>}

      {!loading && !error && (!visibleDashboard || visibleDashboard.sections.length === 0) && (
        <Text className="px-0">
          {searchQuery.trim()
            ? 'No dashboard content matches your search.'
            : 'No dashboard sections yet. Go to Reports → Dashboards & Sections to add sections and reports.'}
        </Text>
      )}

      {!loading && !error && visibleDashboard && visibleDashboard.sections.length > 0 && (
        <div className="grid grid-cols-12 items-stretch gap-3">
          {visibleDashboard.sections
            .sort((a, b) => a.sortOrder - b.sortOrder)
            .map((section) => (
              <DashboardSectionView
                key={section.id}
                section={section}
                onReportUpdated={() => setRefreshToken((value) => value + 1)}
              />
            ))}
        </div>
      )}
    </div>
  );
}
