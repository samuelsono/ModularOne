import { useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Dropdown,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Option,
} from '@fluentui/react-components';
import { ApiError } from '../../services/apiClient';
import { createPosition, getDepartments, updatePosition } from '../../services/coreHrService';
import type { Department, SavePositionRequest } from '../../types/coreHr';

const EMPTY_FORM: SavePositionRequest = {
  departmentId: '',
  name: '',
  code: '',
  description: null,
  isActive: true,
  sortOrder: 0,
};

export function PositionFormDialog({
  open,
  initial,
  onClose,
  onSaved,
}: {
  open: boolean;
  initial: (SavePositionRequest & { id?: string }) | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const isEdit = initial !== null && Boolean(initial.id);
  const [form, setForm] = useState<SavePositionRequest>(initial ?? EMPTY_FORM);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    setForm(initial ?? EMPTY_FORM);
    setError(null);
  }, [initial, open]);

  useEffect(() => {
    if (!open) return;
    void getDepartments().then(setDepartments).catch(() => setDepartments([]));
  }, [open]);

  const selectedDepartment = useMemo(
    () => departments.find((department) => department.id === form.departmentId),
    [departments, form.departmentId],
  );

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setIsSaving(true);
    setError(null);

    const payload: SavePositionRequest = {
      departmentId: form.departmentId,
      name: form.name.trim(),
      code: form.code.trim(),
      description: form.description?.trim() || null,
      isActive: form.isActive,
      sortOrder: form.sortOrder,
    };

    try {
      if (isEdit && initial?.id) {
        await updatePosition(initial.id, payload);
      } else {
        await createPosition(payload);
      }
      onSaved();
      onClose();
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : 'Failed to save position.');
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <form onSubmit={(event) => void handleSubmit(event)}>
          <DialogBody>
            <DialogTitle>{isEdit ? 'Edit position' : 'Add position'}</DialogTitle>
            <DialogContent className="flex flex-col gap-3 pt-2">
              {error ? <MessageBar intent="error"><MessageBarBody>{error}</MessageBarBody></MessageBar> : null}
              <Field label="Department" required>
                <Dropdown
                  value={selectedDepartment ? `${selectedDepartment.companyName} / ${selectedDepartment.name}` : ''}
                  selectedOptions={form.departmentId ? [form.departmentId] : []}
                  onOptionSelect={(_, data) => {
                    if (data.optionValue) setForm((c) => ({ ...c, departmentId: data.optionValue! }));
                  }}
                >
                  {departments.map((department) => (
                    <Option key={department.id} value={department.id} text={`${department.companyName} / ${department.name}`}>
                      {department.companyName} / {department.name}
                    </Option>
                  ))}
                </Dropdown>
              </Field>
              <Field label="Name" required><Input value={form.name} maxLength={128} onChange={(_, data) => setForm((c) => ({ ...c, name: data.value }))} /></Field>
              <div className="grid grid-cols-2 gap-3">
                <Field label="Code" required><Input value={form.code} maxLength={32} onChange={(_, data) => setForm((c) => ({ ...c, code: data.value }))} /></Field>
                <Field label="Sort order"><Input type="number" value={String(form.sortOrder)} onChange={(_, data) => setForm((c) => ({ ...c, sortOrder: Number.parseInt(data.value, 10) || 0 }))} /></Field>
              </div>
              <Field label="Description"><Input value={form.description ?? ''} maxLength={512} onChange={(_, data) => setForm((c) => ({ ...c, description: data.value || null }))} /></Field>
              <Checkbox label="Active" checked={form.isActive} onChange={(_, data) => setForm((c) => ({ ...c, isActive: Boolean(data.checked) }))} />
            </DialogContent>
          </DialogBody>
          <DialogActions>
            <Button type="button" appearance="secondary" onClick={onClose}>Cancel</Button>
            <Button type="submit" appearance="primary" disabled={!form.departmentId || !form.name.trim() || !form.code.trim() || isSaving}>{isSaving ? 'Saving...' : 'Save'}</Button>
          </DialogActions>
        </form>
      </DialogSurface>
    </Dialog>
  );
}
