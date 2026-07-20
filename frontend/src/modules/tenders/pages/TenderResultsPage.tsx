import { useCallback, useEffect, useMemo, useState } from 'react';
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
  Dropdown,
  Field,
  Input,
  Link,
  MessageBar,
  MessageBarBody,
  Option,
  Spinner,
  Text,
  createTableColumn,
} from '@fluentui/react-components';
import { ArrowDownloadRegular, BookmarkSearchRegular, CopyRegular, PageFitRegular, SearchRegular } from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import {
  archiveResult,
  bulkArchiveResults,
  bulkMarkResultsSeen,
  deleteMySubscription,
  exportResultsCsv,
  getMySubscription,
  listResults,
  listSources,
  markResultSeen,
  refreshResultDocuments,
  upsertMySubscription,
} from '@modules/tenders/services/tendersService';
import type {
  TenderMatch,
  TenderMatchStatus,
  TenderResultsScope,
  TenderSource,
  TenderWatchSubscription,
} from '@modules/tenders/types/tenders';

const STATUS_OPTIONS: Array<{ value: '' | TenderMatchStatus; label: string }> = [
  { value: '', label: 'All (excl. expired)' },
  { value: 'New', label: 'New' },
  { value: 'Seen', label: 'Seen' },
  { value: 'Archived', label: 'Archived' },
  { value: 'Expired', label: 'Expired' },
];

function matchesSearch(item: TenderMatch, query: string): boolean {
  const q = query.trim().toLowerCase();
  if (!q) {
    return true;
  }

  return (
    item.title.toLowerCase().includes(q) ||
    (item.summary?.toLowerCase().includes(q) ?? false) ||
    item.canonicalUrl.toLowerCase().includes(q) ||
    (item.sourceName?.toLowerCase().includes(q) ?? false) ||
    item.matchedKeywords.some((keyword) => keyword.toLowerCase().includes(q))
  );
}

export default function TenderResultsPage() {
  const pageSearch = usePageSearchQuery();
  const [results, setResults] = useState<TenderMatch[]>([]);
  const [sources, setSources] = useState<TenderSource[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<'' | TenderMatchStatus>('');
  const [sourceId, setSourceId] = useState<string>('');
  const [keywordInput, setKeywordInput] = useState('');
  const [keyword, setKeyword] = useState('');
  const [includeExpired, setIncludeExpired] = useState(false);
  const [scope, setScope] = useState<TenderResultsScope>('All');
  const [subscription, setSubscription] = useState<TenderWatchSubscription | null>(null);
  const [watchSaving, setWatchSaving] = useState(false);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [busy, setBusy] = useState(false);
  const [copyHint, setCopyHint] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [matches, sub, sourceList] = await Promise.all([
        listResults({
          status: statusFilter || undefined,
          sourceId: sourceId || undefined,
          keyword: keyword.trim() || undefined,
          includeExpired: includeExpired || statusFilter === 'Expired',
          scope,
        }),
        getMySubscription(),
        listSources(),
      ]);
      setResults(matches);
      setSubscription(sub);
      setSources(sourceList);
      setSelectedIds(new Set());
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to load results.');
    } finally {
      setLoading(false);
    }
  }, [includeExpired, keyword, scope, sourceId, statusFilter]);

  useEffect(() => {
    void load();
  }, [load]);

  const visible = useMemo(
    () => results.filter((item) => matchesSearch(item, pageSearch)),
    [pageSearch, results],
  );

  async function toggleWatch(enabled: boolean) {
    setWatchSaving(true);
    setError(null);
    try {
      if (enabled) {
        setSubscription(
          await upsertMySubscription({
            sourceId: null,
            queryId: null,
            notifyInApp: true,
            notifyEmail: false,
          }),
        );
      } else {
        await deleteMySubscription();
        setSubscription(null);
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to update watch settings.');
    } finally {
      setWatchSaving(false);
    }
  }

  function toggleSelected(id: string, checked: boolean) {
    setSelectedIds((current) => {
      const next = new Set(current);
      if (checked) {
        next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
  }

  function toggleSelectAll(checked: boolean) {
    if (!checked) {
      setSelectedIds(new Set());
      return;
    }
    setSelectedIds(new Set(visible.map((item) => item.id)));
  }

  async function handleBulkSeen() {
    if (selectedIds.size === 0) {
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await bulkMarkResultsSeen([...selectedIds]);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to mark results seen.');
    } finally {
      setBusy(false);
    }
  }

  async function handleBulkArchive() {
    if (selectedIds.size === 0) {
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await bulkArchiveResults([...selectedIds]);
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to archive results.');
    } finally {
      setBusy(false);
    }
  }

  async function handleExport() {
    setBusy(true);
    setError(null);
    try {
      await exportResultsCsv({
        status: statusFilter || undefined,
        sourceId: sourceId || undefined,
        keyword: keyword.trim() || undefined,
        search: pageSearch.trim() || undefined,
        includeExpired: includeExpired || statusFilter === 'Expired',
        scope,
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to export CSV.');
    } finally {
      setBusy(false);
    }
  }

  async function copyDocumentUrls(urls: string[]) {
    if (urls.length === 0) {
      return;
    }
    try {
      await navigator.clipboard.writeText(urls.join('\n'));
      setCopyHint(`Copied ${urls.length} document URL(s)`);
      window.setTimeout(() => setCopyHint(null), 2000);
    } catch {
      setError('Unable to copy document URLs.');
    }
  }

  const columns: TableColumnDefinition<TenderMatch>[] = useMemo(
    () => [
      createTableColumn({
        columnId: 'select',
        renderHeaderCell: () => (
          <Checkbox
            checked={visible.length > 0 && selectedIds.size === visible.length}
            onChange={(_, d) => toggleSelectAll(Boolean(d.checked))}
            aria-label="Select all"
          />
        ),
        renderCell: (item) => (
          <Checkbox
            checked={selectedIds.has(item.id)}
            onChange={(_, d) => toggleSelected(item.id, Boolean(d.checked))}
            aria-label={`Select ${item.title}`}
          />
        ),
      }),
      createTableColumn({
        columnId: 'status',
        renderHeaderCell: () => 'Status',
        renderCell: (item) => (
          <Badge
            appearance="tint"
            color={
              item.status === 'New'
                ? 'brand'
                : item.status === 'Expired'
                  ? 'danger'
                  : item.status === 'Archived'
                    ? 'warning'
                    : 'informative'
            }
          >
            {item.status}
          </Badge>
        ),
      }),
      createTableColumn({
        columnId: 'source',
        renderHeaderCell: () => 'Source',
        renderCell: (item) => (
          <Text className="text-xs">{item.sourceName ?? item.sourceId.slice(0, 8)}</Text>
        ),
      }),
      createTableColumn({
        columnId: 'title',
        renderHeaderCell: () => 'Title',
        renderCell: (item) => (
          <div className="flex flex-col gap-1 max-w-[420px]">
            <Link href={item.canonicalUrl} target="_blank" rel="noreferrer">
              {item.title}
            </Link>
            <div className="flex flex-wrap gap-1">
              {item.matchedKeywords.map((kw) => (
                <Badge key={kw} size="small" appearance="outline">
                  {kw}
                </Badge>
              ))}
            </div>
          </div>
        ),
      }),
      createTableColumn({
        columnId: 'closing',
        renderHeaderCell: () => 'Closing',
        renderCell: (item) => item.closingDate ?? '—',
      }),
      createTableColumn({
        columnId: 'docs',
        renderHeaderCell: () => 'Documents',
        renderCell: (item) =>
          item.documentUrls.length === 0 ? (
            <Text>—</Text>
          ) : (
            <div className="flex flex-col gap-1">
              {item.documentUrls.slice(0, 3).map((url) => (
                <Link key={url} href={url} target="_blank" rel="noreferrer" className="text-xs">
                  {url.split('/').pop() || url}
                </Link>
              ))}
              {item.documentUrls.length > 3 && (
                <Text className="text-xs">+{item.documentUrls.length - 3} more</Text>
              )}
              {item.documentMetadata.length > 0 && (
                <Text className="text-xs text-neutral-foreground-3">
                  {item.documentMetadata
                    .slice(0, 2)
                    .map((meta) => meta.fileName ?? meta.contentType ?? 'file')
                    .join(', ')}
                </Text>
              )}
              <Button
                size="small"
                appearance="subtle"
                icon={<CopyRegular />}
                onClick={() => void copyDocumentUrls(item.documentUrls)}
              >
                Copy URLs
              </Button>
            </div>
          ),
      }),
      createTableColumn({
        columnId: 'seen',
        renderHeaderCell: () => 'Collected',
        renderCell: (item) => new Date(item.firstSeenAt).toLocaleString(),
      }),
      createTableColumn({
        columnId: 'actions',
        renderHeaderCell: () => '',
        renderCell: (item) => (
          <div className="flex gap-1 flex-wrap">
            {item.status === 'New' && (
              <Button
                size="small"
                onClick={() => {
                  void (async () => {
                    await markResultSeen(item.id);
                    await load();
                  })();
                }}
              >
                Mark seen
              </Button>
            )}
            <Button
              size="small"
              appearance="subtle"
              disabled={busy}
              onClick={() => {
                void (async () => {
                  setBusy(true);
                  setError(null);
                  try {
                    await refreshResultDocuments(item.id);
                    await load();
                  } catch (err) {
                    setError(
                      err instanceof ApiError ? err.message : 'Unable to refresh documents.',
                    );
                  } finally {
                    setBusy(false);
                  }
                })();
              }}
            >
              Refresh docs
            </Button>
            {item.status !== 'Archived' && item.status !== 'Expired' && (
              <Button
                size="small"
                appearance="subtle"
                onClick={() => {
                  void (async () => {
                    await archiveResult(item.id);
                    await load();
                  })();
                }}
              >
                Archive
              </Button>
            )}
          </div>
        ),
      }),
    ],
    [busy, load, selectedIds, visible],
  );

  const watching = Boolean(subscription?.notifyInApp);
  const statusLabel =
    STATUS_OPTIONS.find((option) => option.value === statusFilter)?.label ?? 'All (excl. expired)';
  const sourceLabel = sourceId
    ? (sources.find((source) => source.id === sourceId)?.name ?? 'Source')
    : 'All sources';

  return (
    <div className="flex flex-col gap-3 px-4 h-full min-h-0">
      <div className="flex items-start justify-between gap-2 flex-wrap">
        <div>
          <Text weight="semibold">Tender results</Text>
          <Text className="block text-sm text-neutral-foreground-3">
            Filter, search, export, and act on matches. Known and expired tenders are not recollected.
          </Text>
        </div>
        <div className="flex items-center gap-3 flex-wrap">
          <Checkbox
            label="Notify me of new matches"
            checked={watching}
            disabled={watchSaving}
            onChange={(_, d) => void toggleWatch(Boolean(d.checked))}
          />
          <Button
            appearance="secondary"
            icon={<ArrowDownloadRegular />}
            disabled={busy}
            onClick={() => void handleExport()}
          >
            Export CSV
          </Button>
        </div>
      </div>

      <div className="flex flex-wrap gap-3 items-end">
        <Field label="Inbox" className="min-w-[140px]">
          <Dropdown
            value={scope === 'Mine' ? 'My matches' : 'Org inbox'}
            selectedOptions={[scope]}
            onOptionSelect={(_, data) =>
              setScope((data.optionValue as TenderResultsScope) ?? 'All')
            }
          >
            <Option value="All">Org inbox</Option>
            <Option value="Mine">My matches</Option>
          </Dropdown>
        </Field>

        <Field label="Status" className="min-w-[160px]">
          <Dropdown
            value={statusLabel}
            selectedOptions={[statusFilter]}
            onOptionSelect={(_, data) => {
              const next = (data.optionValue ?? '') as '' | TenderMatchStatus;
              setStatusFilter(next);
              if (next === 'Expired') {
                setIncludeExpired(true);
              }
            }}
          >
            {STATUS_OPTIONS.map((option) => (
              <Option key={option.value || 'all'} value={option.value}>
                {option.label}
              </Option>
            ))}
          </Dropdown>
        </Field>

        <Field label="Source" className="min-w-[180px]">
          <Dropdown
            value={sourceLabel}
            selectedOptions={[sourceId]}
            onOptionSelect={(_, data) => setSourceId(data.optionValue ?? '')}
          >
            <Option value="">All sources</Option>
            {sources.map((source) => (
              <Option key={source.id} value={source.id}>
                {source.name}
              </Option>
            ))}
          </Dropdown>
        </Field>

        <Field label="Keyword" className="min-w-[160px]">
          <Input
            value={keywordInput}
            placeholder="Matched keyword"
            onChange={(_, d) => setKeywordInput(d.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                const next = keywordInput.trim();
                if (next === keyword) {
                  void load();
                } else {
                  setKeyword(next);
                }
              }
            }}
          />
        </Field>

        <Checkbox
          label="Include expired"
          checked={includeExpired}
          disabled={statusFilter === 'Expired'}
          onChange={(_, d) => setIncludeExpired(Boolean(d.checked))}
        />

        <Button
          appearance="primary"
          onClick={() => {
            const next = keywordInput.trim();
            if (next === keyword) {
              void load();
            } else {
              setKeyword(next);
            }
          }}
          disabled={loading}
        >
          Apply
        </Button>
      </div>

      {selectedIds.size > 0 && (
        <div className="flex items-center gap-2 flex-wrap">
          <Text className="text-sm">{selectedIds.size} selected</Text>
          <Button size="small" disabled={busy} onClick={() => void handleBulkSeen()}>
            Mark seen
          </Button>
          <Button size="small" appearance="subtle" disabled={busy} onClick={() => void handleBulkArchive()}>
            Archive
          </Button>
          <Button size="small" appearance="transparent" onClick={() => setSelectedIds(new Set())}>
            Clear
          </Button>
        </div>
      )}

      {copyHint && (
        <MessageBar intent="success">
          <MessageBarBody>{copyHint}</MessageBarBody>
        </MessageBar>
      )}

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      {loading ? (
        <Spinner label="Loading results…" />
      ) : visible.length === 0 ? (
        <div className="h-full w-full flex flex-col gap-3 items-center justify-center p-6 text-sm text-neutral-foreground-3">
                  <BookmarkSearchRegular className='size-26 text-gray-300' />
                  No results found for the current filters.
              </div>
      ) : (
        <div className="min-h-0 overflow-auto">
          <DataGrid items={visible} columns={columns} getRowId={(item) => item.id}>
            <DataGridHeader>
              <DataGridRow>
                {({ renderHeaderCell }) => (
                  <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
                )}
              </DataGridRow>
            </DataGridHeader>
            <DataGridBody<TenderMatch>>
              {({ item, rowId }) => (
                <DataGridRow<TenderMatch> key={rowId}>
                  {({ renderCell }) => <DataGridCell>{renderCell(item)}</DataGridCell>}
                </DataGridRow>
              )}
            </DataGridBody>
          </DataGrid>
        </div>
      )}
    </div>
  );
}
