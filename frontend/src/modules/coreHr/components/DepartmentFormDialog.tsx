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
import { ApiError } from '@platform/api/apiClient';
import { createDepartment, getCompanies, updateDepartment } from '@modules/coreHr/services/coreHrService';
import type { Company, SaveDepartmentRequest } from '@modules/coreHr/types/coreHr';

const EMPTY_FORM: SaveDepartmentRequest = {
  companyId: '',
  name: '',
  code: '',
  description: null,
  isActive: true,
  sortOrder: 0,
};

export function DepartmentFormDialog({
  open,
  initial,
  onClose,
  onSaved,
}: {
  open: boolean;
  initial: (SaveDepartmentRequest & { id?: string }) | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const isEdit = initial !== null && Boolean(initial.id);
  const [form, setForm] = useState<SaveDepartmentRequest>(initial ?? EMPTY_FORM);
  const [companies, setCompanies] = useState<Company[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    setForm(initial ?? EMPTY_FORM);
    setError(null);
  }, [initial, open]);

  useEffect(() => {
    if (!open) return;
    void getCompanies().then(setCompanies).catch(() => setCompanies([]));
  }, [open]);

  const selectedCompany = useMemo(
    () => companies.find((company) => company.id === form.companyId),
    [companies, form.companyId],
  );

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setIsSaving(true);
    setError(null);

    const payload: SaveDepartmentRequest = {
      companyId: form.companyId,
      name: form.name.trim(),
      code: form.code.trim(),
      description: form.description?.trim() || null,
      isActive: form.isActive,
      sortOrder: form.sortOrder,
    };

    try {
      if (isEdit && initial?.id) {
        await updateDepartment(initial.id, payload);
      } else {
        await createDepartment(payload);
      }
      onSaved();
      onClose();
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : 'Failed to save department.');
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <form onSubmit={(event) => void handleSubmit(event)}>
          <DialogBody>
            <DialogTitle>{isEdit ? 'Edit department' : 'Add department'}</DialogTitle>
            <DialogContent className="flex flex-col gap-3 pt-2">
              {error ? <MessageBar intent="error"><MessageBarBody>{error}</MessageBarBody></MessageBar> : null}
              <Field label="Company" required>
                <Dropdown
                  value={selectedCompany?.name ?? ''}
                  selectedOptions={form.companyId ? [form.companyId] : []}
                  onOptionSelect={(_, data) => {
                    if (data.optionValue) setForm((c) => ({ ...c, companyId: data.optionValue! }));
                  }}
                >
                  {companies.map((company) => (
                    <Option key={company.id} value={company.id} text={company.name}>{company.name}</Option>
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
            <Button type="submit" appearance="primary" disabled={!form.companyId || !form.name.trim() || !form.code.trim() || isSaving}>{isSaving ? 'Saving...' : 'Save'}</Button>
          </DialogActions>
        </form>
      </DialogSurface>
    </Dialog>
  );
}
