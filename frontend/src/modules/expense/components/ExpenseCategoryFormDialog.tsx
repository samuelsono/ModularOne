import { useEffect, useState, type FormEvent } from 'react';
import {
  Button,
  Checkbox,
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
} from '@fluentui/react-components';
import { ApiError } from '@platform/api/apiClient';
import { createExpenseCategory, updateExpenseCategory } from '@modules/expense/services/expenseService';
import type { SaveExpenseCategoryRequest } from '@modules/expense/types/expense';

const EMPTY_FORM: SaveExpenseCategoryRequest = {
  name: '',
  code: '',
  description: null,
  isActive: true,
  sortOrder: 0,
};

function parseSortOrder(value: string): number {
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : 0;
}

export function ExpenseCategoryFormDialog({
  open,
  initial,
  onClose,
  onSaved,
}: {
  open: boolean;
  initial: (SaveExpenseCategoryRequest & { id?: string }) | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const isEdit = initial !== null && Boolean(initial.id);
  const [form, setForm] = useState<SaveExpenseCategoryRequest>(initial ?? EMPTY_FORM);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    setForm(initial ?? EMPTY_FORM);
    setError(null);
  }, [initial, open]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setIsSaving(true);
    setError(null);

    const payload: SaveExpenseCategoryRequest = {
      name: form.name.trim(),
      code: form.code.trim(),
      description: form.description?.trim() || null,
      isActive: form.isActive,
      sortOrder: form.sortOrder,
    };

    try {
      if (isEdit && initial?.id) {
        await updateExpenseCategory(initial.id, payload);
      } else {
        await createExpenseCategory(payload);
      }

      onSaved();
      onClose();
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : 'Failed to save expense category.');
    } finally {
      setIsSaving(false);
    }
  }

  const canSave = Boolean(form.name.trim() && form.code.trim());

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <form onSubmit={(event) => void handleSubmit(event)}>
          <DialogBody>
            <DialogTitle>{isEdit ? 'Edit expense category' : 'Add expense category'}</DialogTitle>
            <DialogContent className="flex flex-col gap-3 pt-2">
              {error ? (
                <MessageBar intent="error">
                  <MessageBarBody>{error}</MessageBarBody>
                </MessageBar>
              ) : null}

              <Field label="Name" required>
                <Input
                  value={form.name}
                  maxLength={128}
                  onChange={(_, data) => setForm((current) => ({ ...current, name: data.value }))}
                />
              </Field>

              <div className="grid grid-cols-2 gap-3">
                <Field label="Code" required>
                  <Input
                    value={form.code}
                    maxLength={32}
                    onChange={(_, data) => setForm((current) => ({ ...current, code: data.value }))}
                  />
                </Field>

                <Field label="Sort order">
                  <Input
                    type="number"
                    value={String(form.sortOrder)}
                    onChange={(_, data) => setForm((current) => ({
                      ...current,
                      sortOrder: parseSortOrder(data.value),
                    }))}
                  />
                </Field>
              </div>

              <Field label="Description">
                <Input
                  value={form.description ?? ''}
                  maxLength={512}
                  onChange={(_, data) => setForm((current) => ({
                    ...current,
                    description: data.value || null,
                  }))}
                />
              </Field>

              <Checkbox
                label="Active"
                checked={form.isActive}
                onChange={(_, data) => setForm((current) => ({
                  ...current,
                  isActive: Boolean(data.checked),
                }))}
              />
            </DialogContent>
          </DialogBody>
          <DialogActions>
            <Button type="button" appearance="secondary" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" appearance="primary" disabled={!canSave || isSaving}>
              {isSaving ? 'Saving...' : 'Save'}
            </Button>
          </DialogActions>
        </form>
      </DialogSurface>
    </Dialog>
  );
}
