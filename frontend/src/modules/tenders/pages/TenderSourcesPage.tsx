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
import { AddRegular, BookSearchRegular, DeleteRegular, EditRegular, PlayRegular } from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import { AutoFitDataGrid } from '@platform/ui/AutoFitDataGrid';
import {
  createRun,
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
  eTendersDateFrom: null,
  eTendersDateTo: null,
  eTendersPageSize: 250,
};

function defaultETendersDateFrom(): string {
  const d = new Date();
  d.setUTCDate(d.getUTCDate() - 14);
  return d.toISOString().slice(0, 10);
}

function defaultETendersDateTo(): string {
  const d = new Date();
  d.setUTCDate(d.getUTCDate() + 30);
  return d.toISOString().slice(0, 10);
}

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
    eTendersDateFrom: source.eTendersDateFrom,
    eTendersDateTo: source.eTendersDateTo,
    eTendersPageSize: source.eTendersPageSize,
  };
}

function isETendersForm(form: SaveTenderSourceRequest): boolean {
  if (form.parserKind === 'ETenders') {
    return true;
  }
  if (form.parserKind !== 'Auto') {
    return false;
  }
  try {
    return new URL(form.url).hostname.toLowerCase().includes('etenders.gov.za');
  } catch {
    return false;
  }
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
                  onOptionSelect={(_, d) => {
                    const parserKind = (d.optionValue as TenderParserKind) ?? 'Auto';
                    setForm((c) => ({
                      ...c,
                      parserKind,
                      ...(parserKind === 'ETenders'
                        ? {
                            eTendersDateFrom: c.eTendersDateFrom ?? defaultETendersDateFrom(),
                            eTendersDateTo: c.eTendersDateTo ?? defaultETendersDateTo(),
                            eTendersPageSize: c.eTendersPageSize ?? 250,
                          }
                        : {}),
                    }));
                  }}
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
                  (ocds-api.etenders.gov.za). Keep the source URL as
                  https://www.etenders.gov.za/.
                </Text>
              )}
              {isETendersForm(form) && (
                <>
                  <Field
                    label="OCDS dateFrom"
                    hint="Published/closing window start (yyyy-MM-dd). Leave blank to use the app default lookback."
                  >
                    <Input
                      type="date"
                      value={form.eTendersDateFrom ?? ''}
                      onChange={(_, d) =>
                        setForm((c) => ({
                          ...c,
                          eTendersDateFrom: d.value || null,
                        }))
                      }
                    />
                  </Field>
                  <Field
                    label="OCDS dateTo"
                    hint="Published/closing window end. Must be later than today for open listings."
                  >
                    <Input
                      type="date"
                      value={form.eTendersDateTo ?? ''}
                      onChange={(_, d) =>
                        setForm((c) => ({
                          ...c,
                          eTendersDateTo: d.value || null,
                        }))
                      }
                    />
                  </Field>
                  <div className="flex gap-2">
                    <Button
                      type="button"
                      size="small"
                      appearance="secondary"
                      onClick={() =>
                        setForm((c) => ({
                          ...c,
                          eTendersDateFrom: defaultETendersDateFrom(),
                          eTendersDateTo: defaultETendersDateTo(),
                        }))
                      }
                    >
                      Use last 14 days → +30 days
                    </Button>
                    <Button
                      type="button"
                      size="small"
                      appearance="subtle"
                      onClick={() =>
                        setForm((c) => ({
                          ...c,
                          eTendersDateFrom: null,
                          eTendersDateTo: null,
                        }))
                      }
                    >
                      Clear dates
                    </Button>
                  </div>
                  <Field
                    label="OCDS page size"
                    hint="Results per API page (1–1000). Larger pages fetch the window faster."
                  >
                    <Input
                      type="number"
                      min={1}
                      max={1000}
                      value={String(form.eTendersPageSize ?? 250)}
                      onChange={(_, d) => {
                        const n = Number(d.value);
                        setForm((c) => ({
                          ...c,
                          eTendersPageSize: Number.isFinite(n)
                            ? Math.min(1000, Math.max(1, Math.trunc(n)))
                            : 250,
                        }));
                      }}
                    />
                  </Field>
                </>
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
  const [runningSourceId, setRunningSourceId] = useState<string | null>(null);
  const [runHint, setRunHint] = useState<string | null>(null);

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

  async function handleRunSource(source: TenderSource) {
    setRunningSourceId(source.id);
    setError(null);
    setRunHint(null);
    try {
      const run = await createRun({ sourceIds: [source.id] });
      setRunHint(`Scrape queued for “${source.name}” (run ${run.status.toLowerCase()}).`);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to start scrape for this source.');
    } finally {
      setRunningSourceId(null);
    }
  }

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
              icon={<PlayRegular />}
              appearance="subtle"
              aria-label={`Run scrape for ${item.name}`}
              title="Run scrape for this source"
              disabled={!item.isEnabled || runningSourceId === item.id}
              onClick={() => void handleRunSource(item)}
            />
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
    [load, runningSourceId],
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

      {runHint && (
        <MessageBar intent="success" className="mx-2">
          <MessageBarBody>{runHint}</MessageBarBody>
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
              actions: { minWidth: 120, idealWidth: 140, defaultWidth: 130 },
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
