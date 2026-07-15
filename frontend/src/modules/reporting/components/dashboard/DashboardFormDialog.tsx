import * as React from 'react';
import type { JSXElement } from '@fluentui/react-components';
import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  DialogTrigger,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Switch,
  Textarea,
} from '@fluentui/react-components';
import { AddRegular, EditRegular } from '@fluentui/react-icons';

import { ApiError } from '@platform/api/apiClient';
import { createDashboard, updateDashboard } from '@modules/reporting/services/dashboardService';
import type { Dashboard, DashboardSummary, SaveDashboardRequest } from '@modules/reporting/types/dashboard';

interface DashboardFormDialogProps {
  dashboard?: DashboardSummary;
  nextSortOrder?: number;
  onSaved?: (dashboard: Dashboard) => void;
  trigger?: React.ReactElement;
}

export function DashboardFormDialog({
  dashboard,
  nextSortOrder = 1,
  onSaved,
  trigger,
}: DashboardFormDialogProps): JSXElement {
  const isEdit = Boolean(dashboard);
  const [open, setOpen] = React.useState(false);
  const [saving, setSaving] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [name, setName] = React.useState(dashboard?.name ?? '');
  const [description, setDescription] = React.useState(dashboard?.description ?? '');
  const [isDefault, setIsDefault] = React.useState(dashboard?.isDefault ?? false);
  const [sortOrder, setSortOrder] = React.useState(String(dashboard?.sortOrder ?? nextSortOrder));

  React.useEffect(() => {
    if (!open) {
      return;
    }

    setName(dashboard?.name ?? '');
    setDescription(dashboard?.description ?? '');
    setIsDefault(dashboard?.isDefault ?? false);
    setSortOrder(String(dashboard?.sortOrder ?? nextSortOrder));
    setError(null);
  }, [dashboard, nextSortOrder, open]);

  const handleSave = async () => {
    const trimmedName = name.trim();
    if (!trimmedName) {
      setError('Dashboard name is required.');
      return;
    }

    const request: SaveDashboardRequest = {
      name: trimmedName,
      description: description.trim() || null,
      isDefault,
      sortOrder: Number.parseInt(sortOrder, 10) || 1,
    };

    setSaving(true);
    setError(null);

    try {
      const saved = isEdit && dashboard
        ? await updateDashboard(dashboard.id, request)
        : await createDashboard(request);

      setOpen(false);
      onSaved?.(saved);
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : 'Failed to save dashboard.');
    } finally {
      setSaving(false);
    }
  };

  const defaultTrigger = (
    <Button
      appearance={isEdit ? 'subtle' : 'primary'}
      size={isEdit ? 'small' : 'medium'}
      icon={isEdit ? <EditRegular /> : <AddRegular />}
    >
      {isEdit ? 'Edit' : 'New dashboard'}
    </Button>
  );

  return (
    <Dialog open={open} onOpenChange={(_, data) => setOpen(data.open)}>
      <DialogTrigger disableButtonEnhancement>
        {trigger ?? defaultTrigger}
      </DialogTrigger>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>{isEdit ? 'Edit dashboard' : 'Create dashboard'}</DialogTitle>
          <DialogContent className="flex flex-col gap-3 pt-3">
            {error && (
              <MessageBar intent="error">
                <MessageBarBody>{error}</MessageBarBody>
              </MessageBar>
            )}

            <Field label="Name" required>
              <Input
                value={name}
                onChange={(_, data) => setName(data.value)}
                placeholder="e.g. Fleet Overview"
              />
            </Field>

            <Field label="Description">
              <Textarea
                value={description}
                onChange={(_, data) => setDescription(data.value)}
                placeholder="Optional description"
                rows={3}
              />
            </Field>

            <Field label="Sort order">
              <Input
                type="number"
                min={1}
                value={sortOrder}
                onChange={(_, data) => setSortOrder(data.value)}
              />
            </Field>

            <Field label="Default dashboard">
              <Switch checked={isDefault} onChange={(_, data) => setIsDefault(data.checked)} />
            </Field>
          </DialogContent>
          <DialogActions>
            <DialogTrigger disableButtonEnhancement>
              <Button appearance="secondary" disabled={saving}>Cancel</Button>
            </DialogTrigger>
            <Button appearance="primary" onClick={() => void handleSave()} disabled={saving}>
              {saving ? 'Saving...' : isEdit ? 'Save changes' : 'Create dashboard'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
