export type TenderParserKind =
  | 'Auto'
  | 'GenericHtml'
  | 'RssAtom'
  | 'ETenders'
  | 'BrowserRendered';
export type TenderQueryMatchMode = 'Any' | 'All' | 'Phrase';
export type TenderScrapeTrigger = 'Manual' | 'Scheduled';
export type TenderScrapeRunStatus = 'Queued' | 'Running' | 'Succeeded' | 'Failed' | 'Partial';
export type TenderMatchStatus = 'New' | 'Seen' | 'Archived' | 'Dismissed' | 'Expired';
export type TenderPortalStatus = 'Unknown' | 'Open' | 'Closed' | 'Awarded' | 'Cancelled' | 'Expired';
export type TenderSourceAuthKind = 'None' | 'Basic' | 'Bearer' | 'CookieHeader';
export type TenderResultsScope = 'All' | 'Mine';

export interface TenderSource {
  id: string;
  name: string;
  url: string;
  parserKind: TenderParserKind;
  isEnabled: boolean;
  scrapeIntervalMinutes: number | null;
  nextDueAt: string | null;
  lastFetchedAt: string | null;
  lastSuccessAt: string | null;
  lastError: string | null;
  maxDetailPagesPerRun: number;
  robotsRespect: boolean;
  consecutiveFailures: number;
  circuitOpenedUntil: string | null;
  isCircuitOpen: boolean;
  authKind: TenderSourceAuthKind;
  authUsername: string | null;
  hasAuthSecret: boolean;
  /** OCDS dateFrom (yyyy-MM-dd), null = app default lookback. */
  eTendersDateFrom: string | null;
  /** OCDS dateTo (yyyy-MM-dd), null = app default forward window. */
  eTendersDateTo: string | null;
  /** OCDS page size 1–1000, null = app default. */
  eTendersPageSize: number | null;
  createdAt: string;
  updatedAt: string;
}

export interface SaveTenderSourceRequest {
  name: string;
  url: string;
  parserKind: TenderParserKind;
  isEnabled: boolean;
  scrapeIntervalMinutes: number | null;
  maxDetailPagesPerRun: number;
  robotsRespect: boolean;
  authKind: TenderSourceAuthKind;
  authUsername: string | null;
  /** Omit to leave unchanged; empty string clears when clearing auth. */
  authSecret?: string | null;
  eTendersDateFrom?: string | null;
  eTendersDateTo?: string | null;
  eTendersPageSize?: number | null;
}

export interface TenderQuery {
  id: string;
  name: string;
  keywords: string[];
  matchMode: TenderQueryMatchMode;
  isEnabled: boolean;
  sourceIds: string[];
  createdAt: string;
  updatedAt: string;
}

export interface SaveTenderQueryRequest {
  name: string;
  keywords: string[];
  matchMode: TenderQueryMatchMode;
  isEnabled: boolean;
  sourceIds: string[];
}

export interface TenderDocumentMeta {
  url: string;
  contentType: string | null;
  fileName: string | null;
  contentLength: number | null;
}

export interface TenderMatch {
  id: string;
  sourceId: string;
  sourceName: string | null;
  externalKey: string;
  canonicalUrl: string;
  title: string;
  summary: string | null;
  closingDate: string | null;
  portalStatus: TenderPortalStatus;
  matchedKeywords: string[];
  documentUrls: string[];
  documentMetadata: TenderDocumentMeta[];
  status: TenderMatchStatus;
  firstSeenAt: string;
  collectedAt: string;
  documentsRefreshedAt: string | null;
  ownerUserId: string | null;
}

export interface TenderScrapeRunSource {
  id: string;
  sourceId: string;
  sourceName: string | null;
  status: string;
  httpStatus: number | null;
  bytesFetched: number;
  durationMs: number;
  itemsSeen: number;
  itemsMatched: number;
  itemsSkippedKnown: number;
  itemsSkippedExpired: number;
  message: string | null;
}

export interface TenderScrapeRun {
  id: string;
  trigger: TenderScrapeTrigger;
  status: TenderScrapeRunStatus;
  startedAt: string | null;
  completedAt: string | null;
  sourcesAttempted: number;
  sourcesUnchanged: number;
  matchesNew: number;
  itemsSkippedKnown: number;
  itemsSkippedExpired: number;
  errorSummary: string | null;
  sourceIds: string[];
  createdAt: string;
  sourceLogs: TenderScrapeRunSource[];
}

export interface CreateTenderScrapeRunRequest {
  sourceIds?: string[];
}

export interface TenderWatchSubscription {
  id: string;
  sourceId: string | null;
  queryId: string | null;
  notifyInApp: boolean;
  notifyEmail: boolean;
  updatedAt: string;
}

export interface SaveTenderWatchSubscriptionRequest {
  sourceId?: string | null;
  queryId?: string | null;
  notifyInApp: boolean;
  notifyEmail: boolean;
}
