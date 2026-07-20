import { authorizedFetch } from '@platform/api/authService';
import type {
  CreateTenderScrapeRunRequest,
  SaveTenderQueryRequest,
  SaveTenderSourceRequest,
  SaveTenderWatchSubscriptionRequest,
  TenderMatch,
  TenderMatchStatus,
  TenderQuery,
  TenderResultsScope,
  TenderScrapeRun,
  TenderSource,
  TenderWatchSubscription,
} from '@modules/tenders/types/tenders';

export async function listSources(): Promise<TenderSource[]> {
  return authorizedFetch<TenderSource[]>('/api/tenders/sources');
}

export async function createSource(data: SaveTenderSourceRequest): Promise<TenderSource> {
  return authorizedFetch<TenderSource>('/api/tenders/sources', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function updateSource(id: string, data: SaveTenderSourceRequest): Promise<TenderSource> {
  return authorizedFetch<TenderSource>(`/api/tenders/sources/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export async function deleteSource(id: string): Promise<void> {
  await authorizedFetch<void>(`/api/tenders/sources/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });
}

export async function listQueries(): Promise<TenderQuery[]> {
  return authorizedFetch<TenderQuery[]>('/api/tenders/queries');
}

export async function createQuery(data: SaveTenderQueryRequest): Promise<TenderQuery> {
  return authorizedFetch<TenderQuery>('/api/tenders/queries', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function updateQuery(id: string, data: SaveTenderQueryRequest): Promise<TenderQuery> {
  return authorizedFetch<TenderQuery>(`/api/tenders/queries/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export async function deleteQuery(id: string): Promise<void> {
  await authorizedFetch<void>(`/api/tenders/queries/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });
}

export async function listResults(filters?: {
  status?: TenderMatchStatus;
  sourceId?: string;
  keyword?: string;
  search?: string;
  includeExpired?: boolean;
  scope?: TenderResultsScope;
}): Promise<TenderMatch[]> {
  const params = new URLSearchParams();
  if (filters?.status) {
    params.set('status', filters.status);
  }
  if (filters?.sourceId) {
    params.set('sourceId', filters.sourceId);
  }
  if (filters?.keyword) {
    params.set('keyword', filters.keyword);
  }
  if (filters?.search) {
    params.set('search', filters.search);
  }
  if (filters?.includeExpired) {
    params.set('includeExpired', 'true');
  }
  if (filters?.scope) {
    params.set('scope', filters.scope);
  }
  const query = params.toString();
  return authorizedFetch<TenderMatch[]>(`/api/tenders/results${query ? `?${query}` : ''}`);
}

export async function markResultSeen(id: string): Promise<TenderMatch> {
  return authorizedFetch<TenderMatch>(`/api/tenders/results/${encodeURIComponent(id)}/seen`, {
    method: 'POST',
  });
}

export async function archiveResult(id: string): Promise<TenderMatch> {
  return authorizedFetch<TenderMatch>(`/api/tenders/results/${encodeURIComponent(id)}/archive`, {
    method: 'POST',
  });
}

export async function refreshResultDocuments(id: string): Promise<TenderMatch> {
  return authorizedFetch<TenderMatch>(
    `/api/tenders/results/${encodeURIComponent(id)}/refresh-documents`,
    { method: 'POST' },
  );
}

export async function bulkMarkResultsSeen(ids: string[]): Promise<{ updated: number }> {
  return authorizedFetch<{ updated: number }>('/api/tenders/results/bulk-seen', {
    method: 'POST',
    body: JSON.stringify({ ids }),
  });
}

export async function bulkArchiveResults(ids: string[]): Promise<{ updated: number }> {
  return authorizedFetch<{ updated: number }>('/api/tenders/results/bulk-archive', {
    method: 'POST',
    body: JSON.stringify({ ids }),
  });
}

export async function exportResultsCsv(filters?: {
  status?: TenderMatchStatus;
  sourceId?: string;
  keyword?: string;
  search?: string;
  includeExpired?: boolean;
  scope?: TenderResultsScope;
}): Promise<void> {
  const params = new URLSearchParams();
  if (filters?.status) {
    params.set('status', filters.status);
  }
  if (filters?.sourceId) {
    params.set('sourceId', filters.sourceId);
  }
  if (filters?.keyword) {
    params.set('keyword', filters.keyword);
  }
  if (filters?.search) {
    params.set('search', filters.search);
  }
  if (filters?.includeExpired) {
    params.set('includeExpired', 'true');
  }
  if (filters?.scope) {
    params.set('scope', filters.scope);
  }

  const { getAccessToken } = await import('@platform/api/tokenStorage');
  const token = getAccessToken();
  const response = await fetch(`/api/tenders/results/export?${params.toString()}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (!response.ok) {
    throw new Error('Failed to export tender results.');
  }

  const blob = await response.blob();
  const disposition = response.headers.get('Content-Disposition');
  const match = disposition?.match(/filename="?([^"]+)"?/i);
  const fileName = match?.[1] ?? 'tender-results.csv';
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

export async function listRuns(take = 50): Promise<TenderScrapeRun[]> {
  return authorizedFetch<TenderScrapeRun[]>(`/api/tenders/runs?take=${take}`);
}

export async function getRun(id: string): Promise<TenderScrapeRun> {
  return authorizedFetch<TenderScrapeRun>(`/api/tenders/runs/${encodeURIComponent(id)}`);
}

export async function createRun(data?: CreateTenderScrapeRunRequest): Promise<TenderScrapeRun> {
  return authorizedFetch<TenderScrapeRun>('/api/tenders/runs', {
    method: 'POST',
    body: JSON.stringify(data ?? {}),
  });
}

export async function getMySubscription(): Promise<TenderWatchSubscription | null> {
  return authorizedFetch<TenderWatchSubscription | null>('/api/tenders/subscriptions');
}

export async function upsertMySubscription(
  data: SaveTenderWatchSubscriptionRequest,
): Promise<TenderWatchSubscription> {
  return authorizedFetch<TenderWatchSubscription>('/api/tenders/subscriptions', {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export async function deleteMySubscription(): Promise<void> {
  await authorizedFetch<void>('/api/tenders/subscriptions', {
    method: 'DELETE',
  });
}
