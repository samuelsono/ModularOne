import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import type { TableColumnDefinition } from '@fluentui/react-components';
import {
  Badge,
  Button,
  Checkbox,
  DataGrid,
  DataGridBody,
  DataGridCell,
  DataGridHeader,
  DataGridHeaderCell,
  DataGridRow,
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
  Spinner,
  Subtitle2,
  Text,
  createTableColumn,
} from '@fluentui/react-components';
import { AddRegular, EditRegular } from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import {
  createCategory,
  deleteCategory,
  getAdminCategories,
  updateCategory,
} from '@modules/support/services/supportService';
import type { SaveCategoryRequest, TicketCategory, TicketType } from '@modules/support/types/support';

const TICKET_TYPES: TicketType[] = ['Ticket', 'Feedback', 'BugReport'];

const EMPTY_FORM: SaveCategoryRequest = {
  id: '',
  label: '',
  appliesTo: ['Ticket'],
  isActive: true,
  sortOrder: 0,
};

function CategoryFormDialog({
  open,
  initial,
  onClose,
  onSaved,
}: {
  open: boolean;
  initial: SaveCategoryRequest | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const isEdit = initial !== null && initial.id.length > 0;
  const [form, setForm] = useState<SaveCategoryRequest>(initial ?? EMPTY_FORM);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    setForm(initial ?? EMPTY_FORM);
    setError(null);
  }, [initial, open]);

  function toggleAppliesTo(type: TicketType, checked: boolean) {
    setForm((current) => ({
      ...current,
      appliesTo: checked
        ? [...new Set([...current.appliesTo, type])]
        : current.appliesTo.filter((item) => item !== type),
    }));
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setIsSaving(true);
    setError(null);

    try {
      if (isEdit) {
        await updateCategory(form.id, form);
      } else {
        await createCategory(form);
      }
      onSaved();
      onClose();
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : 'Failed to save category.');
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <form onSubmit={(event) => void handleSubmit(event)}>
          <DialogBody>
            <DialogTitle>{isEdit ? 'Edit category' : 'Add category'}</DialogTitle>
            <DialogContent className="flex flex-col gap-3 pt-2">
              {error ? (
                <MessageBar intent="error">
                  <MessageBarBody>{error}</MessageBarBody>
                </MessageBar>
              ) : null}

              <Field label="ID" required>
                <Input
                  value={form.id}
                  disabled={isEdit}
                  maxLength={64}
                  onChange={(_, data) => setForm((current) => ({ ...current, id: data.value }))}
                />
              </Field>

              <Field label="Label" required>
                <Input
                  value={form.label}
                  maxLength={128}
                  onChange={(_, data) => setForm((current) => ({ ...current, label: data.value }))}
                />
              </Field>

              <Field label="Sort order">
                <Input
                  type="number"
                  value={String(form.sortOrder)}
                  onChange={(_, data) => setForm((current) => ({
                    ...current,
                    sortOrder: Number.parseInt(data.value, 10) || 0,
                  }))}
                />
              </Field>

              <Field label="Applies to">
                <div className="flex flex-col gap-2">
                  {TICKET_TYPES.map((type) => (
                    <Checkbox
                      key={type}
                      label={type}
                      checked={form.appliesTo.includes(type)}
                      onChange={(_, data) => toggleAppliesTo(type, Boolean(data.checked))}
                    />
                  ))}
                </div>
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
            <Button type="button" appearance="secondary" onClick={onClose}>Cancel</Button>
            <Button type="submit" appearance="primary" disabled={isSaving}>
              {isSaving ? 'Saving...' : 'Save'}
            </Button>
          </DialogActions>
        </form>
      </DialogSurface>
    </Dialog>
  );
}

export function TicketCategoriesManager() {
  const [categories, setCategories] = useState<TicketCategory[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingCategory, setEditingCategory] = useState<SaveCategoryRequest | null>(null);

  const loadCategories = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const items = await getAdminCategories();
      setCategories(items);
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load categories.');
      setCategories([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadCategories();
  }, [loadCategories]);

  const columns: TableColumnDefinition<TicketCategory>[] = useMemo(
    () => [
      createTableColumn<TicketCategory>({
        columnId: 'label',
        renderHeaderCell: () => 'Label',
        renderCell: (item) => item.label,
      }),
      createTableColumn<TicketCategory>({
        columnId: 'id',
        renderHeaderCell: () => 'ID',
        renderCell: (item) => item.id,
      }),
      createTableColumn<TicketCategory>({
        columnId: 'appliesTo',
        renderHeaderCell: () => 'Applies to',
        renderCell: (item) => (
          <div className="flex flex-wrap gap-1">
            {item.appliesTo.map((type) => (
              <Badge key={type} appearance="outline" size="small">{type}</Badge>
            ))}
          </div>
        ),
      }),
      createTableColumn<TicketCategory>({
        columnId: 'sortOrder',
        renderHeaderCell: () => 'Sort order',
        renderCell: (item) => item.sortOrder,
      }),
      createTableColumn<TicketCategory>({
        columnId: 'isActive',
        renderHeaderCell: () => 'Active',
        renderCell: (item) => (item.isActive ? 'Yes' : 'No'),
      }),
      createTableColumn<TicketCategory>({
        columnId: 'actions',
        renderHeaderCell: () => 'Actions',
        renderCell: (item) => (
          <div className="flex gap-2">
            <Button
              appearance="subtle"
              icon={<EditRegular />}
              onClick={() => {
                setEditingCategory({
                  id: item.id,
                  label: item.label,
                  appliesTo: item.appliesTo,
                  isActive: item.isActive,
                  sortOrder: item.sortOrder,
                });
                setDialogOpen(true);
              }}
            >
              Edit
            </Button>
            <Button
              appearance="subtle"
              onClick={() => {
                void deleteCategory(item.id)
                  .then(() => loadCategories())
                  .catch((deleteError) => {
                    setError(deleteError instanceof ApiError
                      ? deleteError.message
                      : 'Failed to delete category.');
                  });
              }}
            >
              Delete
            </Button>
          </div>
        ),
      }),
    ],
    [loadCategories],
  );

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-start justify-between gap-4">
        <div>
          <Subtitle2>Ticket categories</Subtitle2>
          <Text className="text-sm text-neutral-foreground-3">
            Manage the categories users can choose when submitting support tickets.
          </Text>
        </div>
        <Button
          appearance="primary"
          icon={<AddRegular />}
          onClick={() => {
            setEditingCategory(null);
            setDialogOpen(true);
          }}
        >
          Add category
        </Button>
      </div>

      {error ? (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}

      {isLoading ? (
        <Spinner label="Loading categories..." />
      ) : (
        <DataGrid items={categories} columns={columns} getRowId={(item) => item.id}>
          <DataGridHeader>
            <DataGridRow>
              {({ renderHeaderCell }) => (
                <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
              )}
            </DataGridRow>
          </DataGridHeader>
          <DataGridBody<TicketCategory>>
            {({ item, rowId }) => (
              <DataGridRow<TicketCategory> key={rowId}>
                {({ renderCell }) => (
                  <DataGridCell>{renderCell(item)}</DataGridCell>
                )}
              </DataGridRow>
            )}
          </DataGridBody>
        </DataGrid>
      )}

      <CategoryFormDialog
        open={dialogOpen}
        initial={editingCategory}
        onClose={() => setDialogOpen(false)}
        onSaved={() => void loadCategories()}
      />
    </div>
  );
}
