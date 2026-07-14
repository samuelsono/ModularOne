import * as React from 'react';
import type { JSXElement } from '@fluentui/react-components';
import {
  Button,
  Combobox,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Option,
  Text,
  Textarea,
} from '@fluentui/react-components';

import { ApiError } from '../../services/apiClient';
import { getReport, updateReport } from '../../services/reportService';
import type { Report, ReportSize, ReportWithData } from '../../types/report';
import { notifyDashboardChanged } from '../../utils/dashboardStorage';

interface ReportCardSettingsDialogProps {
  open: boolean;
  sectionId: string;
  item: ReportWithData;
  onOpenChange: (open: boolean) => void;
  onSaved?: () => void;
}

export function ReportCardSettingsDialog({
  open,
  sectionId,
  item,
  onOpenChange,
  onSaved,
}: ReportCardSettingsDialogProps): JSXElement {
  const { report } = item;
  const [name, setName] = React.useState(report.name);
  const [subtitle, setSubtitle] = React.useState(report.description ?? '');
  const [size, setSize] = React.useState<ReportSize>(report.size);
  const [saving, setSaving] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (!open) {
      return;
    }

    setName(report.name);
    setSubtitle(report.description ?? '');
    setSize(report.size);
    setError(null);
  }, [open, report.description, report.name, report.size]);

  const handleSave = async () => {
    if (!name.trim()) {
      setError('Name is required.');
      return;
    }

    setSaving(true);
    setError(null);

    try {
      const full = await getReport(report.id);
      const request = buildUpdateRequest(full, sectionId, {
        name: name.trim(),
        subtitle: subtitle.trim(),
        size,
      });

      await updateReport(report.id, request);
      notifyDashboardChanged();
      onSaved?.();
      onOpenChange(false);
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : 'Failed to save report settings.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => onOpenChange(data.open)}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Report card settings</DialogTitle>
          <DialogContent className="flex flex-col gap-3 pt-3">
            {error && (
              <MessageBar intent="error">
                <MessageBarBody>{error}</MessageBarBody>
              </MessageBar>
            )}

            <Text size={200} className="text-neutral-600">
              Size applies to this dashboard section only. Name and subtitle update the report everywhere it appears.
            </Text>

            <Field label="Name" required>
              <Input
                value={name}
                onChange={(_, data) => setName(data.value)}
                placeholder="Report title"
              />
            </Field>

            <Field label="Subtitle">
              <Textarea
                value={subtitle}
                onChange={(_, data) => setSubtitle(data.value)}
                placeholder="Optional subtitle shown on the card"
                rows={2}
              />
            </Field>

            <Field label="Size on this dashboard" required>
              <Combobox
                value={size === 'FullWidth' ? 'Full Width' : size}
                onOptionSelect={(_, data) => setSize((data.optionValue as ReportSize) ?? 'Medium')}
              >
                <Option value="Small" text="Small">Small</Option>
                <Option value="Medium" text="Medium">Medium</Option>
                <Option value="Large" text="Large">Large</Option>
                <Option value="FullWidth" text="Full Width">Full Width</Option>
              </Combobox>
            </Field>
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={() => onOpenChange(false)} disabled={saving}>
              Cancel
            </Button>
            <Button appearance="primary" onClick={() => void handleSave()} disabled={saving}>
              {saving ? 'Saving...' : 'Save'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}

function buildUpdateRequest(
  report: Report,
  sectionId: string,
  values: { name: string; subtitle: string; size: ReportSize },
) {
  const placements = report.placements.map((placement) => ({
    sectionId: placement.sectionId,
    sortOrder: placement.sortOrder,
    size: placement.sectionId === sectionId ? values.size : placement.size,
    isVisible: placement.isVisible,
  }));

  return {
    name: values.name,
    description: values.subtitle || null,
    reportType: report.reportType,
    size: report.size,
    sortOrder: report.sortOrder,
    isVisible: report.isVisible,
    targetTable: report.targetTable,
    aggregateFunction: report.aggregateFunction,
    aggregateField: report.aggregateField,
    groupByColumns: report.groupByColumns,
    filters: report.filters,
    comparisonEnabled: report.comparisonEnabled,
    chartOptionsJson: report.chartOptionsJson,
    placements,
  };
}
