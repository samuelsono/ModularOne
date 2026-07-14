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
  DialogTrigger,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Option,
  Text,
  Textarea,
} from '@fluentui/react-components';
import { AddRegular, EditRegular } from '@fluentui/react-icons';

import { ApiError } from '../../services/apiClient';
import { createDashboardSection, updateDashboardSection } from '../../services/dashboardService';
import type { DashboardSection, SaveDashboardSectionRequest } from '../../types/dashboard';
import type { ReportSize } from '../../types/report';
import { MAX_SECTION_DEPTH } from '../../types/dashboard';
import { getEligibleParentSections, getSectionLabel } from '../../utils/sectionTree';

interface DashboardSectionFormDialogProps {
  dashboardId: string;
  section?: DashboardSection;
  sections?: DashboardSection[];
  parentSectionId?: string | null;
  nextSortOrder?: number;
  onSaved?: (section: DashboardSection) => void;
  trigger?: React.ReactElement;
}

const ROOT_PARENT_VALUE = '__root__';

export function DashboardSectionFormDialog({
  dashboardId,
  section,
  sections = [],
  parentSectionId = null,
  nextSortOrder = 1,
  onSaved,
  trigger,
}: DashboardSectionFormDialogProps): JSXElement {
  const isEdit = Boolean(section);
  const [open, setOpen] = React.useState(false);
  const [saving, setSaving] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [title, setTitle] = React.useState(section?.title ?? '');
  const [subtitle, setSubtitle] = React.useState(section?.subtitle ?? '');
  const [layoutDirection, setLayoutDirection] = React.useState<'Row' | 'Column'>(section?.layoutDirection ?? 'Row');
  const [size, setSize] = React.useState<ReportSize>(section?.size ?? 'FullWidth');
  const [sortOrder, setSortOrder] = React.useState(String(section?.sortOrder ?? nextSortOrder));
  const [selectedParentId, setSelectedParentId] = React.useState(
    section?.parentSectionId ?? parentSectionId ?? ROOT_PARENT_VALUE,
  );

  const eligibleParents = React.useMemo(
    () => getEligibleParentSections(sections, section?.id),
    [section?.id, sections],
  );

  React.useEffect(() => {
    if (!open) {
      return;
    }

    setTitle(section?.title ?? '');
    setSubtitle(section?.subtitle ?? '');
    setLayoutDirection(section?.layoutDirection ?? 'Row');
    setSize(section?.size ?? 'FullWidth');
    setSortOrder(String(section?.sortOrder ?? nextSortOrder));
    setSelectedParentId(section?.parentSectionId ?? parentSectionId ?? ROOT_PARENT_VALUE);
    setError(null);
  }, [nextSortOrder, open, parentSectionId, section]);

  const selectedParentLabel = React.useMemo(() => {
    if (selectedParentId === ROOT_PARENT_VALUE) {
      return 'Top level (no parent)';
    }

    const parent = eligibleParents.find((item) => item.id === selectedParentId)
      ?? sections.find((item) => item.id === selectedParentId);

    return parent ? getSectionLabel(parent, parent.depth) : 'Top level (no parent)';
  }, [eligibleParents, sections, selectedParentId]);

  const handleSave = async () => {
    const request: SaveDashboardSectionRequest = {
      title: title.trim() || null,
      subtitle: subtitle.trim() || null,
      layoutDirection,
      size,
      sortOrder: Number.parseInt(sortOrder, 10) || 1,
      parentSectionId: selectedParentId === ROOT_PARENT_VALUE ? null : selectedParentId,
    };

    setSaving(true);
    setError(null);

    try {
      const saved = isEdit && section
        ? await updateDashboardSection(section.id, request)
        : await createDashboardSection(dashboardId, request);

      setOpen(false);
      onSaved?.(saved);
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : 'Failed to save section.');
    } finally {
      setSaving(false);
    }
  };

  const defaultTrigger = (
    <Button
      appearance={isEdit ? 'subtle' : 'primary'}
      size={isEdit ? 'small' : 'medium'}
      icon={isEdit ? <EditRegular /> : <AddRegular />}
      disabled={!dashboardId}
    >
      {isEdit ? 'Edit' : 'New section'}
    </Button>
  );

  return (
    <Dialog open={open} onOpenChange={(_, data) => setOpen(data.open)}>
      <DialogTrigger disableButtonEnhancement>
        {trigger ?? defaultTrigger}
      </DialogTrigger>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>{isEdit ? 'Edit section' : 'Create section'}</DialogTitle>
          <DialogContent className="flex flex-col gap-3 pt-3">
            {error && (
              <MessageBar intent="error">
                <MessageBarBody>{error}</MessageBarBody>
              </MessageBar>
            )}

            <Field label="Parent section">
              <Combobox
                value={selectedParentLabel}
                onOptionSelect={(_, data) => setSelectedParentId(data.optionValue ?? ROOT_PARENT_VALUE)}
              >
                <Option value={ROOT_PARENT_VALUE} text="Top level (no parent)">
                  Top level (no parent)
                </Option>
                {eligibleParents.map((parent) => (
                  <Option
                    key={parent.id}
                    value={parent.id}
                    text={getSectionLabel(parent, parent.depth)}
                  >
                    {getSectionLabel(parent, parent.depth)} (level {parent.depth})
                  </Option>
                ))}
              </Combobox>
            </Field>

            <Text size={200} className="text-neutral-600">
              Sections can be nested up to {MAX_SECTION_DEPTH} levels deep.
            </Text>

            <Field label="Title">
              <Input
                value={title}
                onChange={(_, data) => setTitle(data.value)}
                placeholder="e.g. Fleet metrics"
              />
            </Field>

            <Field label="Subtitle">
              <Textarea
                value={subtitle}
                onChange={(_, data) => setSubtitle(data.value)}
                placeholder="Optional subtitle"
                rows={2}
              />
            </Field>

            <Field label="Layout direction" required>
              <Combobox
                value={layoutDirection}
                onOptionSelect={(_, data) => setLayoutDirection((data.optionValue as 'Row' | 'Column') ?? 'Row')}
              >
                <Option value="Row" text="Row">Row</Option>
                <Option value="Column" text="Column">Column</Option>
              </Combobox>
            </Field>

            <Field label="Size" required>
              <Combobox
                value={size === 'FullWidth' ? 'Full Width' : size}
                onOptionSelect={(_, data) => setSize((data.optionValue as ReportSize) ?? 'FullWidth')}
              >
                <Option value="Small" text="Small">Small</Option>
                <Option value="Medium" text="Medium">Medium</Option>
                <Option value="Large" text="Large">Large</Option>
                <Option value="FullWidth" text="Full Width">Full Width</Option>
              </Combobox>
            </Field>

            <Field label="Sort order">
              <Input
                type="number"
                min={1}
                value={sortOrder}
                onChange={(_, data) => setSortOrder(data.value)}
              />
            </Field>
          </DialogContent>
          <DialogActions>
            <DialogTrigger disableButtonEnhancement>
              <Button appearance="secondary" disabled={saving}>Cancel</Button>
            </DialogTrigger>
            <Button appearance="primary" onClick={() => void handleSave()} disabled={saving || !dashboardId}>
              {saving ? 'Saving...' : isEdit ? 'Save changes' : 'Create section'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
