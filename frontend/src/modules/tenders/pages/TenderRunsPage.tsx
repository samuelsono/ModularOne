import { useCallback, useEffect, useMemo, useState } from 'react';
import type { TableColumnDefinition } from '@fluentui/react-components';
import {
  Badge,
  Button,
  DataGrid,
  DataGridBody,
  DataGridCell,
  DataGridHeader,
  DataGridHeaderCell,
  DataGridRow,
  MessageBar,
  MessageBarBody,
  Spinner,
  Text,
  createTableColumn,
} from '@fluentui/react-components';
import { ChevronDownRegular, ChevronRightRegular, DeveloperBoardSearchRegular, PlayRegular } from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import AppPagination from '@platform/ui/AppPagination';
import { createRun, getRun, listRuns } from '@modules/tenders/services/tendersService';
import type { TenderScrapeRun } from '@modules/tenders/types/tenders';

function statusColor(status: TenderScrapeRun['status']) {
  switch (status) {
    case 'Succeeded':
      return 'success' as const;
    case 'Failed':
      return 'danger' as const;
    case 'Partial':
      return 'warning' as const;
    case 'Running':
      return 'brand' as const;
    default:
      return 'informative' as const;
  }
}

export default function TenderRunsPage() {
  const [runs, setRuns] = useState<TenderScrapeRun[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [starting, setStarting] = useState(false);
  const [activeRunId, setActiveRunId] = useState<string | null>(null);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(15);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setRuns(await listRuns());
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to load runs.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    setPage(1);
  }, [pageSize, runs.length]);

  const totalItems = runs.length;
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));
  const currentPage = Math.min(page, totalPages);
  const paginated = useMemo(() => {
    const startIndex = (currentPage - 1) * pageSize;
    return runs.slice(startIndex, startIndex + pageSize);
  }, [currentPage, pageSize, runs]);
  const rangeStart = totalItems === 0 ? 0 : (currentPage - 1) * pageSize + 1;
  const rangeEnd = totalItems === 0 ? 0 : Math.min(currentPage * pageSize, totalItems);

  useEffect(() => {
    if (!activeRunId) {
      return;
    }

    let cancelled = false;
    const timer = window.setInterval(() => {
      void (async () => {
        try {
          const run = await getRun(activeRunId);
          if (cancelled) {
            return;
          }
          setRuns((current) => {
            const others = current.filter((item) => item.id !== run.id);
            return [run, ...others];
          });
          if (run.status !== 'Queued' && run.status !== 'Running') {
            setActiveRunId(null);
            await load();
          }
        } catch {
          // keep polling
        }
      })();
    }, 2000);

    return () => {
      cancelled = true;
      window.clearInterval(timer);
    };
  }, [activeRunId, load]);

  async function handleRunNow() {
    setStarting(true);
    setError(null);
    try {
      const run = await createRun();
      setActiveRunId(run.id);
      setExpandedId(run.id);
      setRuns((current) => [run, ...current.filter((item) => item.id !== run.id)]);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to start scrape run.');
    } finally {
      setStarting(false);
    }
  }

  const columns: TableColumnDefinition<TenderScrapeRun>[] = useMemo(
    () => [
      createTableColumn({
        columnId: 'expand',
        renderHeaderCell: () => '',
        renderCell: (item) => (
          <Button
            appearance="subtle"
            size="small"
            icon={expandedId === item.id ? <ChevronDownRegular /> : <ChevronRightRegular />}
            aria-label="Toggle source logs"
            onClick={() => setExpandedId((current) => (current === item.id ? null : item.id))}
          />
        ),
      }),
      createTableColumn({
        columnId: 'created',
        renderHeaderCell: () => 'Created',
        renderCell: (item) => new Date(item.createdAt).toLocaleString(),
      }),
      createTableColumn({
        columnId: 'trigger',
        renderHeaderCell: () => 'Trigger',
        renderCell: (item) => item.trigger,
      }),
      createTableColumn({
        columnId: 'status',
        renderHeaderCell: () => 'Status',
        renderCell: (item) => (
          <Badge appearance="tint" color={statusColor(item.status)}>
            {item.status}
          </Badge>
        ),
      }),
      createTableColumn({
        columnId: 'new',
        renderHeaderCell: () => 'New',
        renderCell: (item) => item.matchesNew,
      }),
      createTableColumn({
        columnId: 'skippedKnown',
        renderHeaderCell: () => 'Skipped known',
        renderCell: (item) => item.itemsSkippedKnown,
      }),
      createTableColumn({
        columnId: 'skippedExpired',
        renderHeaderCell: () => 'Skipped expired',
        renderCell: (item) => item.itemsSkippedExpired,
      }),
      createTableColumn({
        columnId: 'unchanged',
        renderHeaderCell: () => 'Unchanged',
        renderCell: (item) => item.sourcesUnchanged,
      }),
      createTableColumn({
        columnId: 'error',
        renderHeaderCell: () => 'Notes',
        renderCell: (item) => (
          <Text className="text-xs max-w-[220px] block" truncate>
            {item.errorSummary ?? '—'}
          </Text>
        ),
      }),
    ],
    [expandedId],
  );

  const active = activeRunId ? runs.find((run) => run.id === activeRunId) : null;
  const expanded = expandedId ? runs.find((run) => run.id === expandedId) : null;

  return (
    <div className="flex flex-col gap-3 h-full">
      <div className="flex items-center justify-between gap-2 px-4">
        <div className="flex flex-col">
          <Text weight="semibold">Scrape runs</Text>
          <Text className="block text-sm text-neutral-foreground-3">
            Manual and scheduled runs. Expand a row for per-source logs.
          </Text>
        </div>
        <Button
          appearance="primary"
          icon={<PlayRegular />}
          disabled={starting || Boolean(activeRunId)}
          onClick={() => void handleRunNow()}
        >
          {starting ? 'Starting…' : 'Run now'}
        </Button>
      </div>

      {active && (
        <MessageBar intent="info" className="mx-4">
          <MessageBarBody>
            Run {active.status.toLowerCase()}… new {active.matchesNew}, skipped known{' '}
            {active.itemsSkippedKnown}, skipped expired {active.itemsSkippedExpired}.
          </MessageBarBody>
        </MessageBar>
      )}

      {error && (
        <MessageBar intent="error" className="mx-4">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      {loading ? (
        <Spinner label="Loading runs…" />
      ) : (
        <div className="px-4 flex flex-col gap-3 min-h-0 overflow-auto">
          <DataGrid items={paginated} columns={columns} getRowId={(item) => item.id}>
            <DataGridHeader>
              <DataGridRow>
                {({ renderHeaderCell }) => (
                  <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
                )}
              </DataGridRow>
            </DataGridHeader>
            <DataGridBody<TenderScrapeRun>>
              {({ item, rowId }) => (
                <DataGridRow<TenderScrapeRun> key={rowId}>
                  {({ renderCell }) => <DataGridCell>{renderCell(item)}</DataGridCell>}
                </DataGridRow>
              )}
            </DataGridBody>
          </DataGrid>

          {runs.length > 0 && (
            <AppPagination
              className="py-3 px-0!"
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

          {expanded && (
            <div className="rounded flex flex-col border border-neutral-stroke-3 p-3 bg-neutral-background-2">
              <Text weight="semibold" className="mb-2 block">
                Source logs — {expanded.trigger} run
              </Text>
              {expanded.sourceLogs.length === 0 ? (
                <Text className="text-sm text-neutral-foreground-3">No source logs yet.</Text>
              ) : (
                <div className="flex flex-col gap-2">
                  {expanded.sourceLogs.map((log) => (
                    <div
                      key={log.id}
                      className="grid grid-cols-1 md:grid-cols-6 gap-2 text-xs border-b border-neutral-stroke-3 pb-2"
                    >
                      <Text weight="semibold">{log.sourceName ?? log.sourceId.slice(0, 8)}</Text>
                      <Badge size="small" appearance="outline">
                        {log.status}
                      </Badge>
                      <Text>HTTP {log.httpStatus ?? '—'}</Text>
                      <Text>
                        seen {log.itemsSeen} / matched {log.itemsMatched}
                      </Text>
                      <Text>
                        skip known {log.itemsSkippedKnown} / expired {log.itemsSkippedExpired}
                      </Text>
                      <Text>
                        {log.durationMs}ms · {log.message ?? '—'}
                      </Text>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}


          { !loading && runs.length === 0 && (
                  <div className="h-[70vh] w-full flex flex-col gap-3 items-center justify-center p-6 text-sm text-neutral-foreground-3">
                                    <DeveloperBoardSearchRegular className='size-26 text-gray-300' />
                                    No runs found for the current filters.
                                </div>
                )}
        </div>
      )}
    </div>
  );
}
