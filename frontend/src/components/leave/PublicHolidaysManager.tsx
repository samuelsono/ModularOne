import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import type { TableColumnDefinition } from '@fluentui/react-components';
import {
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
  SpinButton,
  Spinner,
  Subtitle2,
  Text,
  createTableColumn,
} from '@fluentui/react-components';
import { AddRegular, ArrowSyncRegular, EditRegular } from '@fluentui/react-icons';
import { ApiError } from '../../services/apiClient';
import {
  createPublicHoliday,
  deletePublicHoliday,
  getAdminPublicHolidays,
  syncPublicHolidays,
  updatePublicHoliday,
} from '../../services/leaveService';
import type { PublicHoliday, SavePublicHolidayRequest } from '../../types/leave';
import AppTitle from '../common/AppTitle';
import { usePageSearchQuery } from '../../context/PageSearchContext';
import { filterPublicHolidays } from '../../utils/pageSearch';

const EMPTY_FORM: SavePublicHolidayRequest = {
  name: '',
  date: '',
  isRecurring: false,
  branch: null,
};

function formatHolidayDate(date: string): string {
  const parsed = new Date(`${date}T00:00:00`);
  if (Number.isNaN(parsed.getTime())) {
    return date;
  }

  return parsed.toLocaleDateString(undefined, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}

function HolidayFormDialog({
  open,
  initial,
  onClose,
  onSaved,
}: {
  open: boolean;
  initial: (SavePublicHolidayRequest & { id?: string }) | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const isEdit = initial !== null && Boolean(initial.id);
  const [form, setForm] = useState<SavePublicHolidayRequest>(initial ?? EMPTY_FORM);
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

    try {
      if (isEdit && initial?.id) {
        await updatePublicHoliday(initial.id, form);
      } else {
        await createPublicHoliday(form);
      }
      onSaved();
      onClose();
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : 'Failed to save public holiday.');
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <form onSubmit={(event) => void handleSubmit(event)}>
          <DialogBody>
            <DialogTitle>{isEdit ? 'Edit public holiday' : 'Add public holiday'}</DialogTitle>
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

              <Field label="Date" required>
                <Input
                  type="date"
                  value={form.date}
                  onChange={(_, data) => setForm((current) => ({ ...current, date: data.value }))}
                />
              </Field>

              <Field label="Branch (optional)">
                <Input
                  value={form.branch ?? ''}
                  placeholder="All branches"
                  maxLength={128}
                  onChange={(_, data) => setForm((current) => ({
                    ...current,
                    branch: data.value.trim() || null,
                  }))}
                />
              </Field>

              <Checkbox
                label="Recurring annually"
                checked={form.isRecurring}
                onChange={(_, data) => setForm((current) => ({
                  ...current,
                  isRecurring: Boolean(data.checked),
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

interface PublicHolidaysManagerProps {
  canWrite: boolean;
}

export function PublicHolidaysManager({ canWrite }: PublicHolidaysManagerProps) {
  const searchQuery = usePageSearchQuery();
  const currentYear = new Date().getFullYear();
  const [year, setYear] = useState(currentYear);
  const [holidays, setHolidays] = useState<PublicHoliday[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [isSyncing, setIsSyncing] = useState(false);
  const [syncMessage, setSyncMessage] = useState<string | null>(null);
  const [editingHoliday, setEditingHoliday] = useState<(SavePublicHolidayRequest & { id?: string }) | null>(null);

  const loadHolidays = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const items = await getAdminPublicHolidays(year);
      setHolidays(items);
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load public holidays.');
      setHolidays([]);
    } finally {
      setIsLoading(false);
    }
  }, [year]);

  useEffect(() => {
    void loadHolidays();
  }, [loadHolidays]);

  const filteredHolidays = useMemo(
    () => filterPublicHolidays(holidays, searchQuery),
    [holidays, searchQuery],
  );

  const columns: TableColumnDefinition<PublicHoliday>[] = useMemo(
    () => [
      createTableColumn<PublicHoliday>({
        columnId: 'name',
        renderHeaderCell: () => 'Name',
        renderCell: (item) => item.name,
      }),
      createTableColumn<PublicHoliday>({
        columnId: 'date',
        renderHeaderCell: () => 'Date',
        renderCell: (item) => formatHolidayDate(item.date),
      }),
      createTableColumn<PublicHoliday>({
        columnId: 'recurring',
        renderHeaderCell: () => 'Recurring',
        renderCell: (item) => (item.isRecurring ? 'Yes' : 'No'),
      }),
      createTableColumn<PublicHoliday>({
        columnId: 'branch',
        renderHeaderCell: () => 'Branch',
        renderCell: (item) => item.branch ?? 'All branches',
      }),
      ...(canWrite
        ? [
            createTableColumn<PublicHoliday>({
              columnId: 'actions',
              renderHeaderCell: () => 'Actions',
              renderCell: (item) => (
                <div className="flex gap-2">
                  <Button
                    appearance="subtle"
                    icon={<EditRegular />}
                    onClick={() => {
                      setEditingHoliday({
                        id: item.id,
                        name: item.name,
                        date: item.date,
                        isRecurring: item.isRecurring,
                        branch: item.branch,
                      });
                      setDialogOpen(true);
                    }}
                  >
                    Edit
                  </Button>
                  <Button
                    appearance="subtle"
                    onClick={() => {
                      void deletePublicHoliday(item.id)
                        .then(() => loadHolidays())
                        .catch((deleteError) => {
                          setError(deleteError instanceof ApiError
                            ? deleteError.message
                            : 'Failed to delete public holiday.');
                        });
                    }}
                  >
                    Delete
                  </Button>
                </div>
              ),
            }),
          ]
        : []),
    ],
    [canWrite, loadHolidays],
  );

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-start justify-between gap-4 px-3">
        <AppTitle title="Public holidays" subtitle="Manage South African public holidays used for working-day calculations." />
        <div className='flex items-end gap-3'>

          <Field label="Year">
          <SpinButton value={year} className="max-w-[140px]" onChange={(_, data) => setYear(Number.parseInt(data.value, 10) || currentYear)} />
      </Field>

        {canWrite ? (
          <>
            <Button
              appearance="secondary"
              icon={<ArrowSyncRegular />}
              disabled={isSyncing}
              onClick={() => {
                setIsSyncing(true);
                setSyncMessage(null);
                void syncPublicHolidays(year)
                  .then((result) => {
                    setSyncMessage(
                      result.added > 0
                        ? `Imported ${result.added} holiday(s) from OpenHolidays API.`
                        : 'Holiday calendar is already up to date.',
                    );
                    return loadHolidays();
                  })
                  .catch((syncError) => {
                    setError(syncError instanceof ApiError
                      ? syncError.message
                      : 'Failed to sync holidays from OpenHolidays API.');
                  })
                  .finally(() => setIsSyncing(false));
              }}
            >
              {isSyncing ? 'Syncing...' : 'Sync from API'}
            </Button>
            <Button
              appearance="primary"
              icon={<AddRegular />}
              onClick={() => {
                setEditingHoliday(null);
                setDialogOpen(true);
              }}
            >
              Add holiday
            </Button>
          </>
        ) : null}
        </div>

      </div>

      

      {syncMessage ? (
        <MessageBar intent="success">
          <MessageBarBody>{syncMessage}</MessageBarBody>
        </MessageBar>
      ) : null}

      {error ? (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}

      {isLoading ? (
        <Spinner label="Loading public holidays..." />
      ) : holidays.length === 0 ? (
        <Text className="text-sm text-neutral-foreground-3">
          No holidays found for {year}.
        </Text>
      ) : filteredHolidays.length === 0 ? (
        <Text className="text-sm text-neutral-foreground-3">
          No holidays match your search.
        </Text>
      ) : (
        <DataGrid items={filteredHolidays} columns={columns} getRowId={(item) => item.id}>
          <DataGridHeader>
            <DataGridRow>
              {({ renderHeaderCell }) => (
                <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
              )}
            </DataGridRow>
          </DataGridHeader>
          <DataGridBody<PublicHoliday>>
            {({ item, rowId }) => (
              <DataGridRow<PublicHoliday> key={rowId}>
                {({ renderCell }) => (
                  <DataGridCell>{renderCell(item)}</DataGridCell>
                )}
              </DataGridRow>
            )}
          </DataGridBody>
        </DataGrid>
      )}

      {canWrite ? (
        <HolidayFormDialog
          open={dialogOpen}
          initial={editingHoliday}
          onClose={() => setDialogOpen(false)}
          onSaved={() => void loadHolidays()}
        />
      ) : null}
    </div>
  );
}
