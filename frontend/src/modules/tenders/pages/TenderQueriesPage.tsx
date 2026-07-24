import { useCallback, useEffect, useMemo, useState, type FormEvent, type KeyboardEvent } from 'react';
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
  Dropdown,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Option,
  Spinner,
  Text,
  createTableColumn,
} from '@fluentui/react-components';
import { AddRegular, DeleteRegular, EditRegular, TagSearchRegular } from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import AppPagination from '@platform/ui/AppPagination';
import {
  createQuery,
  deleteQuery,
  listQueries,
  updateQuery,
} from '@modules/tenders/services/tendersService';
import type {
  SaveTenderQueryRequest,
  TenderQuery,
  TenderQueryMatchMode,
} from '@modules/tenders/types/tenders';

const MATCH_MODE_OPTIONS: Array<{ value: TenderQueryMatchMode; label: string }> = [
  { value: 'Any', label: 'Any keyword (OR)' },
  { value: 'All', label: 'All keywords (AND)' },
  { value: 'Phrase', label: 'Exact phrase' },
];

const EMPTY_FORM: SaveTenderQueryRequest = {
  name: '',
  keywords: [],
  matchMode: 'Any',
  isEnabled: true,
  sourceIds: [],
};

function QueryFormDialog({
  open,
  queryId,
  initial,
  onClose,
  onSaved,
}: {
  open: boolean;
  queryId: string | null;
  initial: SaveTenderQueryRequest;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [form, setForm] = useState(initial);
  const [keywordInput, setKeywordInput] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    setForm(initial);
    setKeywordInput('');
    setError(null);
  }, [initial, open]);

  function addKeyword() {
    const parts = keywordInput
      .split(/[,;|\n\r\t]+/)
      .map((part) => part.trim().toLowerCase())
      .filter(Boolean);
    if (parts.length === 0) {
      return;
    }
    setForm((current) => ({
      ...current,
      keywords: [...new Set([...current.keywords, ...parts])],
    }));
    setKeywordInput('');
  }

  function onKeywordKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Enter' || event.key === ',') {
      event.preventDefault();
      addKeyword();
    }
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const pending = keywordInput
      .split(/[,;|\n\r\t]+/)
      .map((part) => part.trim().toLowerCase())
      .filter(Boolean);
    const keywords = [...new Set([...form.keywords, ...pending])];
    if (keywords.length === 0) {
      setError('Add at least one keyword.');
      return;
    }

    setIsSaving(true);
    setError(null);
    try {
      const payload = { ...form, keywords };
      if (queryId) {
        await updateQuery(queryId, payload);
      } else {
        await createQuery(payload);
      }
      onSaved();
      onClose();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to save query.');
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <form onSubmit={handleSubmit}>
          <DialogBody>
            <DialogTitle>{queryId ? 'Edit query' : 'Add query'}</DialogTitle>
            <DialogContent className="flex flex-col gap-3">
              {error && (
                <MessageBar intent="error">
                  <MessageBarBody>{error}</MessageBarBody>
                </MessageBar>
              )}
              <Field label="Name" required>
                <Input
                  value={form.name}
                  onChange={(_, d) => setForm((c) => ({ ...c, name: d.value }))}
                  required
                />
              </Field>
              <Field label="Keywords" hint="Press Enter to add. Commas also split terms (e.g. software, develop, SAP).">
                <Input
                  value={keywordInput}
                  onChange={(_, d) => setKeywordInput(d.value)}
                  onKeyDown={onKeywordKeyDown}
                  placeholder="software"
                />
              </Field>
              <div className="flex flex-wrap gap-1">
                {form.keywords.map((keyword) => (
                  <Badge
                    key={keyword}
                    appearance="tint"
                    className="cursor-pointer"
                    onClick={() =>
                      setForm((c) => ({
                        ...c,
                        keywords: c.keywords.filter((item) => item !== keyword),
                      }))
                    }
                  >
                    {keyword} ×
                  </Badge>
                ))}
              </div>
              <Field
                label="Match mode"
                hint="Use Any (OR) to collect tenders that contain software OR develop. All (AND) only matches tenders that contain every keyword together — e.g. only “software development”."
              >
                <Dropdown
                  value={MATCH_MODE_OPTIONS.find((item) => item.value === form.matchMode)?.label ?? form.matchMode}
                  selectedOptions={[form.matchMode]}
                  onOptionSelect={(_, d) =>
                    setForm((c) => ({
                      ...c,
                      matchMode: (d.optionValue as TenderQueryMatchMode) ?? 'Any',
                    }))
                  }
                >
                  {MATCH_MODE_OPTIONS.map((mode) => (
                    <Option key={mode.value} value={mode.value} text={mode.label}>
                      {mode.label}
                    </Option>
                  ))}
                </Dropdown>
              </Field>
              {form.matchMode === 'All' && form.keywords.length > 1 && (
                <MessageBar intent="warning">
                  <MessageBarBody>
                    All mode requires every keyword in the same tender. Prefer Any
                    unless you intentionally want that narrower match.
                  </MessageBarBody>
                </MessageBar>
              )}
              <Checkbox
                label="Enabled"
                checked={form.isEnabled}
                onChange={(_, d) => setForm((c) => ({ ...c, isEnabled: Boolean(d.checked) }))}
              />
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" type="button" onClick={onClose}>
                Cancel
              </Button>
              <Button appearance="primary" type="submit" disabled={isSaving}>
                {isSaving ? 'Saving…' : 'Save'}
              </Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  );
}

export default function TenderQueriesPage() {
  const [queries, setQueries] = useState<TenderQuery[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<TenderQuery | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(15);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setQueries(await listQueries());
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to load queries.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    setPage(1);
  }, [pageSize, queries.length]);

  const totalItems = queries.length;
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));
  const currentPage = Math.min(page, totalPages);
  const paginated = useMemo(() => {
    const startIndex = (currentPage - 1) * pageSize;
    return queries.slice(startIndex, startIndex + pageSize);
  }, [currentPage, pageSize, queries]);
  const rangeStart = totalItems === 0 ? 0 : (currentPage - 1) * pageSize + 1;
  const rangeEnd = totalItems === 0 ? 0 : Math.min(currentPage * pageSize, totalItems);

  const columns: TableColumnDefinition<TenderQuery>[] = useMemo(
    () => [
      createTableColumn({
        columnId: 'name',
        renderHeaderCell: () => 'Name',
        renderCell: (item) => item.name,
      }),
      createTableColumn({
        columnId: 'keywords',
        renderHeaderCell: () => 'Keywords',
        renderCell: (item) => (
          <div className="flex flex-wrap gap-1">
            {item.keywords.map((keyword) => (
              <Badge key={keyword} appearance="outline">
                {keyword}
              </Badge>
            ))}
          </div>
        ),
      }),
      createTableColumn({
        columnId: 'mode',
        renderHeaderCell: () => 'Mode',
        renderCell: (item) =>
          MATCH_MODE_OPTIONS.find((mode) => mode.value === item.matchMode)?.label ?? item.matchMode,
      }),
      createTableColumn({
        columnId: 'status',
        renderHeaderCell: () => 'Status',
        renderCell: (item) => (
          <Badge appearance="tint" color={item.isEnabled ? 'success' : 'informative'}>
            {item.isEnabled ? 'Enabled' : 'Disabled'}
          </Badge>
        ),
      }),
      createTableColumn({
        columnId: 'actions',
        renderHeaderCell: () => '',
        renderCell: (item) => (
          <div className="flex gap-1">
            <Button
              icon={<EditRegular />}
              appearance="subtle"
              aria-label="Edit"
              onClick={() => {
                setEditing(item);
                setDialogOpen(true);
              }}
            />
            <Button
              icon={<DeleteRegular />}
              appearance="subtle"
              aria-label="Delete"
              onClick={() => {
                void (async () => {
                  if (!window.confirm(`Delete query “${item.name}”?`)) {
                    return;
                  }
                  try {
                    await deleteQuery(item.id);
                    await load();
                  } catch (err) {
                    setError(err instanceof ApiError ? err.message : 'Delete failed.');
                  }
                })();
              }}
            />
          </div>
        ),
      }),
    ],
    [load],
  );

  return (
    <div className="flex flex-col gap-3 h-full">
      <div className="flex items-center justify-between gap-2 px-4">
        <div className="flex flex-col">
          <Text weight="semibold">Keyword queries</Text>
          <Text className="block text-sm text-neutral-foreground-3">
            Only tenders matching these keywords are collected.
          </Text>
        </div>
        <Button
          appearance="primary"
          icon={<AddRegular />}
          onClick={() => {
            setEditing(null);
            setDialogOpen(true);
          }}
        >
          Add query
        </Button>
      </div>

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      {loading ? (
        <Spinner label="Loading queries…" />
      ) : (
        <>
          <DataGrid items={paginated} columns={columns} getRowId={(item) => item.id}>
            <DataGridHeader>
              <DataGridRow>
                {({ renderHeaderCell }) => (
                  <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
                )}
              </DataGridRow>
            </DataGridHeader>
            <DataGridBody<TenderQuery>>
              {({ item, rowId }) => (
                <DataGridRow<TenderQuery> key={rowId}>
                  {({ renderCell }) => <DataGridCell>{renderCell(item)}</DataGridCell>}
                </DataGridRow>
              )}
            </DataGridBody>
          </DataGrid>
          {queries.length > 0 && (
            <AppPagination
              className="py-3"
              page={currentPage}
              totalPages={totalPages}
              totalItems={totalItems}
              rangeStart={rangeStart}
              rangeEnd={rangeEnd}
              pageSize={pageSize}
              onPageChange={setPage}
              onPageSizeChange={(size) => {
                setPageSize(size);
                setPage(1);
              }}
            />
          )}
        </>
      )}

      { !loading && queries.length === 0 && (
        <div className="h-full w-full flex flex-col gap-3 items-center justify-center p-6 text-sm text-neutral-foreground-3">
                          <TagSearchRegular className='size-26 text-gray-300' />
                          No keyworgs found for the current filters.
                      </div>
      )}

      <QueryFormDialog
        open={dialogOpen}
        queryId={editing?.id ?? null}
        initial={
          editing
            ? {
                name: editing.name,
                keywords: editing.keywords,
                matchMode: editing.matchMode,
                isEnabled: editing.isEnabled,
                sourceIds: editing.sourceIds,
              }
            : EMPTY_FORM
        }
        onClose={() => setDialogOpen(false)}
        onSaved={() => void load()}
      />
    </div>
  );
}
