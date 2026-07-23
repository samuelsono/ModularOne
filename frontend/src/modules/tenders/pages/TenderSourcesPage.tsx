import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import type { TableColumnDefinition } from '@fluentui/react-components';
import {
  Badge,
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
  Spinner,
  Text,
  createTableColumn,
} from '@fluentui/react-components';
import { AddRegular, BookSearchRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import { AutoFitDataGrid } from '@platform/ui/AutoFitDataGrid';
import {
  createSource,
  deleteSource,
  listSources,
  updateSource,
} from '@modules/tenders/services/tendersService';
import type {
  SaveTenderSourceRequest,
  TenderParserKind,
  TenderSource,
  TenderSourceAuthKind,
} from '@modules/tenders/types/tenders';

const PARSER_KINDS: TenderParserKind[] = [
  'Auto',
  'GenericHtml',
  'RssAtom',
  'ETenders',
  'BrowserRendered',
];

const AUTH_KINDS: TenderSourceAuthKind[] = ['None', 'Basic', 'Bearer', 'CookieHeader'];

const EMPTY_FORM: SaveTenderSourceRequest = {
  name: '',
  url: '',
  parserKind: 'Auto',
  isEnabled: true,
  scrapeIntervalMinutes: null,
  maxDetailPagesPerRun: 50,
  robotsRespect: true,
  authKind: 'None',
  authUsername: null,
  authSecret: null,
};

function toForm(source: TenderSource): SaveTenderSourceRequest {
  return {
    name: source.name,
    url: source.url,
    parserKind: source.parserKind,
    isEnabled: source.isEnabled,
    scrapeIntervalMinutes: source.scrapeIntervalMinutes,
    maxDetailPagesPerRun: source.maxDetailPagesPerRun,
    robotsRespect: source.robotsRespect,
    authKind: source.authKind,
    authUsername: source.authUsername,
    authSecret: null,
  };
}

function SourceFormDialog({
  open,
  sourceId,
  initial,
  onClose,
  onSaved,
}: {
  open: boolean;
  sourceId: string | null;
  initial: SaveTenderSourceRequest;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [form, setForm] = useState(initial);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const isEdit = Boolean(sourceId);

  useEffect(() => {
    setForm(initial);
    setError(null);
  }, [initial, open]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setIsSaving(true);
    setError(null);
    try {
      const payload: SaveTenderSourceRequest = { ...form };
      if (payload.authKind === 'None') {
        payload.authUsername = null;
        payload.authSecret = '';
      } else if (isEdit && (payload.authSecret == null || payload.authSecret === '')) {
        delete payload.authSecret;
      }
      if (sourceId) {
        await updateSource(sourceId, payload);
      } else {
        await createSource(payload);
      }
      onSaved();
      onClose();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to save source.');
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <form onSubmit={handleSubmit}>
          <DialogBody>
            <DialogTitle>{isEdit ? 'Edit source' : 'Add source'}</DialogTitle>
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
              <Field label="URL" required>
                <Input
                  value={form.url}
                  onChange={(_, d) => setForm((c) => ({ ...c, url: d.value }))}
                  placeholder="https://..."
                  required
                />
              </Field>
              <Field label="Parser">
                <Dropdown
                  value={form.parserKind}
                  selectedOptions={[form.parserKind]}
                  onOptionSelect={(_, d) =>
                    setForm((c) => ({
                      ...c,
                      parserKind: (d.optionValue as TenderParserKind) ?? 'Auto',
                    }))
                  }
                >
                  {PARSER_KINDS.map((kind) => (
                    <Option key={kind} value={kind}>
                      {kind}
                    </Option>
                  ))}
                </Dropdown>
              </Field>
              <Field label="Scrape interval (minutes, blank = manual only)">
                <Input
                  type="number"
                  value={form.scrapeIntervalMinutes?.toString() ?? ''}
                  onChange={(_, d) =>
                    setForm((c) => ({
                      ...c,
                      scrapeIntervalMinutes: d.value ? Number(d.value) : null,
                    }))
                  }
                />
              </Field>
              <Text className="text-xs text-neutral-foreground-3">
                When set, the scheduler auto-scrapes this source on that cadence (with jitter).
              </Text>
              <Checkbox
                label="Enabled"
                checked={form.isEnabled}
                onChange={(_, d) => setForm((c) => ({ ...c, isEnabled: Boolean(d.checked) }))}
              />
              <Checkbox
                label="Respect robots.txt"
                checked={form.robotsRespect}
                onChange={(_, d) => setForm((c) => ({ ...c, robotsRespect: Boolean(d.checked) }))}
              />
              <Field label="Authentication">
                <Dropdown
                  value={form.authKind}
                  selectedOptions={[form.authKind]}
                  onOptionSelect={(_, d) =>
                    setForm((c) => ({
                      ...c,
                      authKind: (d.optionValue as TenderSourceAuthKind) ?? 'None',
                      ...(d.optionValue === 'None'
                        ? { authUsername: null, authSecret: '' }
                        : {}),
                    }))
                  }
                >
                  {AUTH_KINDS.map((kind) => (
                    <Option key={kind} value={kind}>
                      {kind}
                    </Option>
                  ))}
                </Dropdown>
              </Field>
              {form.authKind !== 'None' && (
                <>
                  {form.authKind === 'Basic' && (
                    <Field label="Username" required>
                      <Input
                        value={form.authUsername ?? ''}
                        onChange={(_, d) => setForm((c) => ({ ...c, authUsername: d.value }))}
                        required
                      />
                    </Field>
                  )}
                  <Field
                    label={
                      form.authKind === 'CookieHeader'
                        ? 'Cookie header'
                        : form.authKind === 'Bearer'
                          ? 'Bearer token'
                          : 'Password'
                    }
                    hint={
                      isEdit
                        ? 'Leave blank to keep the existing secret.'
                        : 'Stored encrypted with Data Protection.'
                    }
                    required={!isEdit}
                  >
                    <Input
                      type="password"
                      value={form.authSecret ?? ''}
                      onChange={(_, d) => setForm((c) => ({ ...c, authSecret: d.value }))}
                      required={!isEdit}
                      autoComplete="new-password"
                    />
                  </Field>
                </>
              )}
              {form.parserKind === 'ETenders' && (
                <Text className="text-xs text-neutral-foreground-3">
                  Uses the National Treasury OCDS API
                  (ocds-api.etenders.gov.za). dateFrom is a published lookback;
                  dateTo is always later than today so only listings that still
                  expire in the future are searched. Keep the source URL as
                  https://www.etenders.gov.za/.
                </Text>
              )}
              {form.parserKind === 'BrowserRendered' && (
                <Text className="text-xs text-amber-700">
                  BrowserRendered needs Tenders:Scrape:EnablePlaywright=true and Chromium
                  (`playwright install chromium`).
                </Text>
              )}
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={onClose} type="button">
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

export default function TenderSourcesPage() {
  const [sources, setSources] = useState<TenderSource[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<TenderSource | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setSources(await listSources());
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to load sources.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const columns: TableColumnDefinition<TenderSource>[] = useMemo(
    () => [
      createTableColumn({
        columnId: 'name',
        renderHeaderCell: () => 'Name',
        renderCell: (item) => item.name,
      }),
      createTableColumn({
        columnId: 'url',
        renderHeaderCell: () => 'URL',
        renderCell: (item) => (
          <Text truncate className="max-w-[320px] block">
            {item.url}
          </Text>
        ),
      }),
      createTableColumn({
        columnId: 'parser',
        renderHeaderCell: () => 'Parser / auth',
        renderCell: (item) => (
          <div className="flex flex-col gap-1">
            <Text className="text-xs">{item.parserKind}</Text>
            {item.authKind !== 'None' && (
              <Badge size="small" appearance="outline">
                {item.authKind}
                {item.hasAuthSecret ? '' : ' (no secret)'}
              </Badge>
            )}
          </div>
        ),
      }),
      createTableColumn({
        columnId: 'status',
        renderHeaderCell: () => 'Status',
        renderCell: (item) => (
          <div className="flex flex-col gap-1">
            <Badge appearance="tint" color={item.isEnabled ? 'success' : 'informative'}>
              {item.isEnabled ? 'Enabled' : 'Disabled'}
            </Badge>
            {item.isCircuitOpen && (
              <Badge appearance="filled" color="danger">
                Circuit open
              </Badge>
            )}
            {item.consecutiveFailures > 0 && !item.isCircuitOpen && (
              <Text className="text-xs text-amber-700">{item.consecutiveFailures} fail(s)</Text>
            )}
          </div>
        ),
      }),
      createTableColumn({
        columnId: 'last',
        renderHeaderCell: () => 'Last / next',
        renderCell: (item) => (
          <div className="flex flex-col text-xs gap-0.5">
            <Text className="text-xs">
              Last:{' '}
              {item.lastError
                ? item.lastError
                : item.lastSuccessAt
                  ? new Date(item.lastSuccessAt).toLocaleString()
                  : '—'}
            </Text>
            <Text className="text-xs text-neutral-foreground-3">
              Next:{' '}
              {item.scrapeIntervalMinutes
                ? item.nextDueAt
                  ? new Date(item.nextDueAt).toLocaleString()
                  : 'pending'
                : 'manual only'}
            </Text>
          </div>
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
                  if (!window.confirm(`Delete source “${item.name}”?`)) {
                    return;
                  }
                  try {
                    await deleteSource(item.id);
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
          <Text weight="semibold">Tender sources</Text>
          <Text className="block text-sm text-neutral-foreground-3">
            Portal listing URLs or RSS/Atom feeds to watch.
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
          Add source
        </Button>
      </div>

      {error && (
        <MessageBar intent="error" className='mx-2'>
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      {loading ? (
        <Spinner label="Loading sources…" />
      ) : (
        <div className="flex-1 min-h-0 overflow-auto px-2">
          <AutoFitDataGrid
            items={sources}
            columns={columns}
            getRowId={(item) => item.id}
            size="small"
            storageKey="tenders.sources"
            enableColumnSizing
            columnSizingOptions={{
              name: { minWidth: 140, idealWidth: 200, defaultWidth: 180 },
              url: { minWidth: 180, idealWidth: 320, defaultWidth: 280 },
              parser: { minWidth: 120, idealWidth: 160, defaultWidth: 140 },
              status: { minWidth: 120, idealWidth: 150, defaultWidth: 140 },
              last: { minWidth: 180, idealWidth: 260, defaultWidth: 220 },
              actions: { minWidth: 100, idealWidth: 110, defaultWidth: 100 },
            }}
          />
        </div>
      )}

      { !loading && sources.length === 0 && (
         <div className="h-full w-full flex flex-col gap-3 items-center justify-center p-6 text-sm text-neutral-foreground-3">
              <BookSearchRegular className='size-26 text-gray-300' />
              No sources found for the current filters.
          </div>
      )}

      <SourceFormDialog
        open={dialogOpen}
        sourceId={editing?.id ?? null}
        initial={editing ? toForm(editing) : EMPTY_FORM}
        onClose={() => setDialogOpen(false)}
        onSaved={() => void load()}
      />
    </div>
  );
}
