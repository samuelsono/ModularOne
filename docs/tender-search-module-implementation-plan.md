# Tender Search Module — Implementation Plan

**Date:** 2026-07-16  
**Status:** Phase 5 complete (Phases 0–5 implemented)  
**Related:** [ADR 0001 — Module Boundaries](./adr/0001-module-boundaries.md), [Adding a module](./adding-a-module.md), [System wishlist](./system-wishlist.md)

---

## Overview

Phased roadmap for a **Tender Search** capability inside Chronos Portal. Users register **source URLs** (portal listing pages, RSS/Atom feeds, search result pages) and **keyword queries** (e.g. `software`, `fleet telematics`). When a search **run** executes, the module:

1. Fetches each enabled source URL
2. Extracts tender listings that match the user’s keywords
3. **Skips** tenders already in the collection (known `ExternalKey`) — no re-collection
4. **Skips** tenders that are **expired** (past closing / submission deadline, or portal-marked closed)
5. Captures **document / attachment URLs** only for **new, non-expired** matches
6. Surfaces new results in-app; optionally runs on a **schedule** and notifies watchers of new matches only

**Goal:** a reliable, polite, efficient watcher for tender portals of interest — not a general-purpose web crawler or public search engine.

---

## Current State (July 2026)

| Area | What exists |
|------|-------------|
| Backend module | Phase 5 — auth sources, eTenders adapter, browser renderer hook, PDF metadata, force refresh, org/mine scope |
| Frontend module | Results (filters, bulk, export, refresh docs, org/mine) / Sources (auth + parsers) / Queries / Runs |
| App launcher | `tenders` card registered via `tendersModule.appModule` |
| Permissions | `tenders.sources\|queries\|results\|runs` (+ `.read` / `.write`); HR role includes module |
| Background jobs | Queue worker (5s) + schedule/expiry/undated-archive tick (1m) |
| Scraping | HTML + RSS/Atom + eTenders; optional BrowserRendered via `ITenderBrowserRenderer`; auth via Data Protection |
| Config | `Tenders:Scrape` (concurrency, circuit, cache, Playwright flag, document metadata) |

---

## User stories (MVP)

1. As a user with tender access, I can **add source URLs** and label them (e.g. “National Treasury”, “Provincial eTenders”).
2. As a user, I can define **keyword sets** (phrases, optional boolean-ish rules) and attach them to sources or run globally.
3. As a user, I can **Run now** and see matching tenders with title, link, matched keywords, and document URLs.
4. As a user, I can enable **auto-scrape** on a cadence (e.g. every 6 / 12 / 24 hours) per source or globally.
5. As a user, I am **notified** when a *new* matching tender appears (not on every re-scrape of the same item).
6. As a user, the system **does not recollect** a tender it has already stored — repeat listings on the portal are ignored for collection work.
7. As a user, the system **does not collect** (or re-process) tenders that are **expired** / past their closing date.

---

## Product scope

### In scope (Phases 0–4)

- CRUD for sources (URL, enabled, scrape interval, parser hint)
- Keyword queries (normalized, case-insensitive match)
- Manual run + scheduled run
- HTML list-page scraping + RSS/Atom where available
- Extract tender identity (canonical URL / external id), title, snippet, closing date if present, document links
- Result inbox with filters (new / seen / archived / expired)
- **No re-collection of known tenders** (skip by `ExternalKey` before detail/doc fetch)
- **No collection of expired tenders** (skip when closing date < today, or portal status closed/expired)
- Rate limiting, caching, robots.txt respect (configurable override for known partner portals)
- Run history / last success / last error per source (include skip counters: known / expired)

### Out of scope / deferred

| Idea | Why deferred |
|------|----------------|
| Full-site recursive crawl (follow every link) | Noise, cost, legal risk — stick to **seed URLs + bounded depth 0–1** |
| Headless browser for every page (Playwright) | Heavy; use only as **fallback** for JS-rendered portals (Phase 5) |
| Auto-download & store all PDFs | Storage + copyright; store **URLs** first; optional fetch later |
| AI summarization / bid scoring | Nice-to-have after solid extraction |
| Multi-tenant marketplace of shared scrapers | Single Chronos tenant first |
| Guaranteeing 100% parse of every government portal | Portals differ; use **adapters** + graceful partial results |

---

## Architecture Decisions

1. **Module key:** `tenders`  
   - Backend: `Modules/CarTrack.Modules.Tenders`  
   - Frontend: `frontend/src/modules/tenders`  
   - Optional Contracts: `CarTrack.Modules.Tenders.Contracts` if other modules need “new tender” events only

2. **Own schema / DbContext:** `TendersDbContext`; store opaque `OwnerUserId` / `CreatedByUserId` — no FKs to Users tables.

3. **Scraping is a job, not a request:** HTTP API enqueues a run; a **hosted worker** processes the queue so UI stays responsive and retries are possible.

4. **Parser adapters:** each source has `ParserKind` (`GenericHtml`, `RssAtom`, `JsonApi`, `Custom:{name}`). Prefer structured feeds over brittle HTML CSS selectors.

5. **Efficiency first:** see [Efficiency design](#efficiency-design) — conditional HTTP, content hashing, skip-known / skip-expired, concurrency limits, keyword prefilter.

6. **Collect once:** once a tender is stored under its stable `ExternalKey`, later scrapes **must not** re-fetch its detail page or re-harvest documents. Default is insert-only for matches; no automatic “refresh” of known rows in MVP.

7. **Expire and forget (for collection):** candidates with a known past `ClosingDate` (or portal “closed/expired” flag) are never inserted. Existing rows whose closing date has passed are marked `Expired` by a lightweight maintenance pass and stay out of future collection paths.

8. **Politeness:** default concurrency 1–2 hosts, delay between requests, honor `Retry-After` / 429, cache `ETag` / `Last-Modified`.

9. **Notifications:** publish `TenderMatchFound` only for **new** inserts; do not notify for known or expired skips. (Optional admin “force re-scrape documents” is Phase 5+, off by default.)

10. **Permissions (proposed):**

| Key | Purpose |
|-----|---------|
| `tenders.sources.read/write` | Manage URLs |
| `tenders.queries.read/write` | Manage keywords |
| `tenders.results.read/write` | View / archive matches |
| `tenders.runs.read/write` | Trigger / view runs (`write` = run now + schedule) |

Staff who watch tenders get read + own results; Admin/HR/ops get source/query write as needed.

---

## Efficiency design

These improvements are **first-class requirements**, not afterthoughts.

### 1. Conditional fetch (skip unchanged pages)

- Persist `ETag`, `Last-Modified`, and body **content hash** (SHA-256) per source.
- Send `If-None-Match` / `If-Modified-Since`. On `304` or hash match → **skip parse**, mark run as `Unchanged`, zero notifications.
- Huge win for periodic scrapes of slow-changing portals.

### 2. Keyword index before deep document work

- On list pages: extract candidate titles/snippets **cheaply**, match keywords, **only then** resolve/follow document links for matches.
- Avoid downloading PDFs / opening every detail page for non-matching rows.

### 2a. Skip already-collected tenders (no re-collection)

- After list extraction (and ideally **before** keyword-expensive work where an `ExternalKey` is already known), load a set of known keys for that source (or global canonical URL set).
- If `ExternalKey` / canonical URL is already in `TenderMatch` → **skip**: no detail fetch, no document harvest, no notification, no overwrite.
- Count as `ItemsSkippedKnown` on the run log.
- This is the primary guarantee that periodic scrapes stay cheap even when portals re-list the same open tenders for weeks.

### 2b. Skip expired tenders (no collection)

- If the list/detail exposes a **closing / submission deadline** and it is **< start of today (org timezone, default UTC)** → skip collection.
- If the portal marks status as closed / awarded / cancelled / expired → treat as expired and skip.
- If closing date is **missing**, still allow collection (unknown expiry); a later maintenance job can expire rows once a date is learned or a TTL policy applies (optional Phase 3).
- Existing stored matches whose `ClosingDate` has passed → set `Status = Expired` (and optionally hide from default inbox); they remain in DB for history but are **never** re-collected.
- Count as `ItemsSkippedExpired` on the run log.

### 3. Bounded fan-out

- Depth: seed URL = 0; optional follow to detail page = 1 **only for keyword hits that are new and not expired**.
- Cap detail fetches per run (e.g. 50) with “truncated” warning in run log.

### 4. Prefer feeds & APIs

- If source URL is RSS/Atom or a documented JSON endpoint, use that adapter (stable IDs, less HTML churn).
- UI hint: “Paste feed URL if the portal offers one.”

### 5. Host-aware scheduler

- Do not scrape all sources at once on the hour → **jitter** schedules (spread across the interval window).
- Per-host semaphore (e.g. max 1 in-flight request per hostname) to avoid bans and self-DDoS.

### 6. Incremental keyword matching

- Normalize keywords (trim, lower, collapse whitespace); support phrase match + optional `OR` groups.
- Compile keyword sets once per run; use simple Aho–Corasick / multi-pattern matcher for large keyword lists instead of nested `Contains` loops.

### 7. Dedup key (collect-once identity)

- Stable `ExternalKey` = canonicalize URL (strip tracking query params) or feed GUID.
- Unique index on `(SourceId, ExternalKey)` (and/or global `CanonicalUrl`) enforces collect-once at the database layer.
- MVP: **no automatic update** when a known tender’s title/docs change on the portal. Re-listing is a skip, not an upsert.
- Phase 5+ (optional): admin “Refresh documents” for a single match, still never recreates a duplicate row.

### 8. Queue + backoff

- Manual and scheduled triggers enqueue `TenderScrapeJob` rows.
- Failed fetches: exponential backoff; circuit-break a source after N consecutive failures; surface in UI.

### 9. Optional content cache (short TTL)

- In-memory or Redis (Aspire already has Redis) cache of recent list HTML for **debug re-parse** without re-hitting origin (TTL 5–15 min, admin-only).

### 10. Parser registry, not one mega-scraper

- Pluggable `ITenderSourceParser` implementations keep GenericHtml small and allow optimized parsers for high-value portals without rewriting the engine.

---

## Target Data Model

### `TenderSource`

```text
Id, Name, Url, ParserKind, IsEnabled,
ScrapeIntervalMinutes (nullable — null = manual only),
NextDueAt, LastFetchedAt, LastSuccessAt, LastError,
ETag, LastModifiedHeader, ContentHash,
MaxDetailPagesPerRun, RobotsRespect (bool),
CreatedByUserId, CreatedAt, UpdatedAt
```

### `TenderQuery`

```text
Id, Name, Keywords (JSON array or normalized string),
MatchMode (Any|All|Phrase),
IsEnabled, Scope (Global | SourceIds[]),
CreatedByUserId, CreatedAt, UpdatedAt
```

### `TenderScrapeRun`

```text
Id, Trigger (Manual|Scheduled), Status (Queued|Running|Succeeded|Failed|Partial),
StartedAt, CompletedAt, SourcesAttempted, SourcesUnchanged,
MatchesNew, ItemsSkippedKnown, ItemsSkippedExpired, ErrorSummary,
RequestedByUserId (nullable for schedule)
```

### `TenderScrapeRunSource` (per-source log)

```text
RunId, SourceId, Status, HttpStatus, BytesFetched, DurationMs,
ItemsSeen, ItemsMatched, ItemsSkippedKnown, ItemsSkippedExpired, Message
```

### `TenderMatch`

```text
Id, SourceId, ExternalKey, CanonicalUrl, Title, Summary,
ClosingDate (nullable), IsExpired (computed or stored),
PortalStatus (nullable: Open|Closed|Awarded|Cancelled|Unknown),
MatchedKeywords (JSON),
DocumentUrls (JSON array), ContentHash,
FirstSeenAt, CollectedAt,
Status (New|Seen|Archived|Dismissed|Expired),
OwnerUserId (or shared org inbox — MVP: discoverer / watchers)

// Unique: (SourceId, ExternalKey)
```

### `TenderWatchSubscription` (optional Phase 2)

```text
UserId, QueryId and/or SourceId, NotifyInApp, NotifyEmail
```

### Not building (deferred)

- Full-text search DB over PDF contents
- Browser session / login cookie vault for authenticated portals (Phase 5+ with secrets)
- Public anonymous tender API

---

## Runtime flow

```text
[UI Run now / Scheduler tick]
        │
        ▼
  Enqueue TenderScrapeRun (Queued)
        │
        ▼
  TenderScrapeWorker (BackgroundService)
        │
        ├─ pick due sources (or run’s source filter)
        ├─ load known ExternalKey set for source (collect-once lookup)
        ├─ per host: wait / semaphore
        ├─ GET with conditional headers
        │     ├─ 304 / hash match → Unchanged
        │     └─ 200 → Parser.ExtractCandidates
        ├─ For each candidate:
        │     ├─ known ExternalKey? → skip (SkippedKnown)
        │     ├─ expired / closed? → skip (SkippedExpired)
        │     ├─ keywords match? else skip
        │     └─ else: resolve document URLs (depth ≤ 1) → INSERT TenderMatch (New)
        ├─ Publish TenderMatchFound for New only → Notifications
        └─ Maintenance: mark stored matches with ClosingDate < today as Expired
```

---

## Phased Implementation

### Phase 0 — Scaffold & shell ✅

**Status:** Implemented (July 2026).

**Goal:** empty but real module replacing any placeholder.

| Task | Detail |
|------|--------|
| Scaffold backend | `Modules/CarTrack.Modules.Tenders` + `TendersDbContext` (no migrations yet) |
| Register Host + architecture tests | Host, solution, `CarTrack.ArchitectureTests` |
| Scaffold frontend | `frontend/src/modules/tenders` — Results / Sources / Queries / Runs stubs |
| Permissions | `tenders.sources|queries|results|runs` (+ `.read` / `.write`); HR role seeded |
| Launcher + nav | App card `tenders`; ESLint `FEATURE_MODULES` |
| Health endpoint | `GET /api/tenders/health` (requires `tenders.results.read`) |

**Exit:** `/tenders` renders module UI (not generic placeholder), no business data yet.

---

### Phase 1 — Sources, queries, manual scrape (list pages) ✅

**Status:** Implemented (July 2026).

#### Backend

| API | Notes |
|-----|-------|
| CRUD `/api/tenders/sources` | Validate absolute http(s) URL |
| CRUD `/api/tenders/queries` | Normalize keywords |
| `POST /api/tenders/runs` | Enqueue manual run (optional `sourceIds`) |
| `GET /api/tenders/runs/{id}` | Status + counters |
| Worker + `GenericHtml` + `RssAtom` parsers | Keyword match on title/snippet |
| Collect-once + expiry gates | Skip known `ExternalKey`; skip past `ClosingDate` / closed portal status |
| Document URL extraction | Only for **new, non-expired** hits (list card and/or one detail page) |
| Unique index | `(SourceId, ExternalKey)` prevents duplicate inserts under race |
| Migration | `InitialTenders` → `TenderSources`, `TenderQueries`, `TenderScrapeRuns`, `TenderScrapeRunSources`, `TenderMatches` |

#### Frontend

- Sources list (enable, interval, last error)
- Queries editor (chips for keywords)
- Results table (new badge, docs links open in new tab; expired filter)
- “Run now” with live run status poll (show skipped known / skipped expired counts)

**Exit:** User can add URLs + keywords, run once, see **new** matches + document links; second run of the same listing does not recollect; expired listings are not stored.

---

### Phase 2 — Scheduled auto-scrape + notifications ✅

**Status:** Implemented (July 2026).

| Task | Detail |
|------|--------|
| Scheduler | Minute tick enqueues due sources (`NextDueAt <= now`, max 5/tick) |
| Jitter | `NextDueAt = now + interval + random(0..20% of interval)` after each attempt / on enable |
| Subscriptions | `TenderWatchSubscriptions` + GET/PUT/DELETE `/api/tenders/subscriptions` |
| Notifications | `TenderMatchFoundEvent` → in-app notify (watchers + manual-run owner) |
| Unchanged short-circuit | Conditional GET + content hash (from Phase 1) |
| Expiry maintenance | On schedule tick and each run |

**Exit:** Enabling interval on a source finds **new open** tenders only; known and expired items are skipped with no notification spam.

---

### Phase 3 — Efficiency & reliability hardening ✅

- Host semaphore + global concurrency cap (`TenderHostConcurrencyGate`)
- Circuit breaker + backoff UI (`ConsecutiveFailures` / `CircuitOpenedUntil`; Sources badges)
- Canonical URL normalization (strip `utm_*`, session ids, `;jsessionid`)
- Aho–Corasick when keyword count ≥ threshold
- Memory + optional Redis short-TTL fetch cache (`ITenderFetchCache`)
- Run source detail logs in UI (expandable Runs row)
- Caps: max detail pages, max runtime per run
- Warm known-key hash set cache per source (`TenderKnownKeyCache`)
- Undated-match archive TTL (`UndatedMatchArchiveDays`, default 90)
- Migration: `AddTenderSourceCircuitBreaker`

**Exit:** Periodic load is polite and cheap when pages are unchanged, listings repeat, or tenders have expired.

---

### Phase 4 — UX polish & reporting ✅

- Filters: New / Seen / Expired / Archived / by source / by keyword
- Mark seen / archive bulk actions (`POST /results/bulk-seen`, `/results/bulk-archive`)
- Page search provider for results (`/tenders`)
- Export CSV of current filters (`GET /results/export`, non-expired by default)
- “Copy document URLs” action per result
- Match DTOs include `sourceName`

**Exit:** Operators can triage and export the inbox without leaving the Results page.

---

### Phase 5+ — Adapters, auth & document tooling ✅

- Playwright/Chromium fallback hook: `ITenderBrowserRenderer` + `BrowserRendered` parser kind (opt-in via `EnablePlaywright`; default null renderer)
- Authenticated sources: Basic / Bearer / Cookie via Data Protection (`ProtectedAuthSecret`)
- Custom adapter: `ETenders` parser for SA Treasury-style listings
- Optional PDF/document metadata (HEAD/ranged GET — Content-Type, filename, length)
- Shared org inbox vs per-user results (`scope=All|Mine`)
- Force refresh documents: `POST /results/{id}/refresh-documents` (updates URLs/metadata, no duplicate row)
- Migration: `AddTenderPhase5AuthAndDocuments`

**Exit:** Operators can scrape authenticated / portal-specific sources, refresh docs on known matches, and choose org vs personal inbox views.

---

## Suggested API surface (end of Phase 5)

```http
GET/POST     /api/tenders/sources
GET/PATCH    /api/tenders/sources/{id}
DELETE       /api/tenders/sources/{id}

GET/POST     /api/tenders/queries
GET/PATCH    /api/tenders/queries/{id}
DELETE       /api/tenders/queries/{id}

POST         /api/tenders/runs
GET          /api/tenders/runs
GET          /api/tenders/runs/{id}

GET          /api/tenders/results
GET          /api/tenders/results/export
POST         /api/tenders/results/bulk-seen
POST         /api/tenders/results/bulk-archive
GET          /api/tenders/results/{id}
POST         /api/tenders/results/{id}/seen
POST         /api/tenders/results/{id}/archive
POST         /api/tenders/results/{id}/refresh-documents

GET/PUT/DELETE /api/tenders/subscriptions
```

---

## Frontend routes

| Path | Permission | Phase |
|------|------------|-------|
| `/tenders` | `tenders.results.read` | 0–1 (results home) |
| `/tenders/sources` | `tenders.sources.read` | 1 |
| `/tenders/queries` | `tenders.queries.read` | 1 |
| `/tenders/runs` | `tenders.runs.read` | 1–2 |

---

## Legal & operational notes

- Scraping third-party sites may be restricted by ToS / robots.txt — default **respect robots**; allow explicit override only for sources the org has permission to monitor.
- Store **links**, not republished tender bodies, unless legal review allows.
- Log User-Agent identifying Chronos Tender Search + contact; no stealth scrapers in MVP.
- Rate limits protect both Chronos and target sites.

---

## Testing & acceptance (MVP = end of Phase 2)

- [ ] Architecture tests: no module→module internals
- [ ] Fixture HTML/RSS parsers unit-tested (no live network in CI)
- [ ] Keyword Any/All/Phrase behaviour covered
- [ ] Unchanged source (304 / same hash) creates no new matches and no notifications
- [ ] Second scrape of the same tender **does not** insert again, fetch detail, or notify (`ItemsSkippedKnown`)
- [ ] Candidate with closing date in the past is **not** collected (`ItemsSkippedExpired`)
- [ ] Stored match past closing date is marked `Expired` and excluded from default collection paths
- [ ] Unique `(SourceId, ExternalKey)` rejects duplicate inserts under concurrent runs
- [ ] Schedule does not stampede all hosts at `:00`
- [ ] Manual run completes under concurrency caps without blocking API thread
- [ ] Lint boundaries + builds green

---

## Open questions

1. Results visibility: **per-user** watches only, or **shared org inbox** for all with `tenders.results.read`?
2. Which portals are day-one targets (list for custom adapters)?
3. Default scrape interval (6h vs 24h) and max sources per tenant?
4. Should keyword queries be global, or must each query bind to specific sources?
5. Is Redis available in all environments for the short-TTL cache, or memory-only first?
6. Timezone for “expired”: org setting vs always UTC?
7. If closing date is missing on the listing, collect anyway (proposed) or require a date?

---

## Estimation (indicative)

| Phase | Effort (order of magnitude) |
|-------|-----------------------------|
| 0 Scaffold | 0.5–1 day |
| 1 Manual scrape + results | 6–10 days |
| 2 Schedule + notifications | 3–5 days |
| 3 Efficiency hardening | 3–5 days |
| 4 UX polish | 2–3 days |

---

## Relation to other docs

| Doc | Role |
|-----|------|
| [adding-a-module.md](./adding-a-module.md) | Scaffold checklist |
| [notifications-implementation-plan.md](./notifications-implementation-plan.md) | Event → in-app pattern |
| [system-wishlist.md](./system-wishlist.md) | Platform wishlist index |
