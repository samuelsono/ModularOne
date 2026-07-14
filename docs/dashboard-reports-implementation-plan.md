# Dashboard & Reports Module — Implementation Plan

## Overview

The report module lets users define analytics widgets (metric cards and charts) against fleet data, arrange them into dashboard sections, and render the result on the Dashboard page. Reports are created and managed on the Reports page; the Dashboard page consumes the backend API to display them.

**Concept:**

1. Create a **Dashboard**
2. Create **Sections** (grid row or grid column)
3. Create **Reports** of type Metric Card, Column, Pie Chart, Donut Chart, or Line Chart
4. Each report **targets a table**, applies an aggregate (`Count`, `Sum`, `Average`, `Min`, `Max`), and supports **grouping by up to 3 columns** depending on report type

Visual references live in [`examples/`](../examples/) (Power BI–style dashboard screenshots).

---

## Current State

| Area | Status |
|------|--------|
| **Dashboard page** (`frontend/src/page/Dashboard.tsx`) | Static hardcoded metric cards + one chart |
| **Reports page** (`frontend/src/page/ReportsPage.tsx`, `frontend/src/components/ReportsTable.tsx`) | Mock data only |
| **Create Report dialog** (`frontend/src/components/reports/CreateReport.tsx`) | Wrong concept — PDF/Excel scheduled export, not analytics widgets |
| **Backend** | No dashboard/report entities, services, or endpoints |
| **Examples** (`examples/`) | Three screenshots showing target layouts |

The examples show the target UX: **sections with grid layouts**, **metric cards with comparisons**, and **charts** (column, stacked column, pie/donut, line, grouped bar) driven by aggregations and grouping.

---

## Domain Model

Three layers:

```
Dashboard
  └── DashboardSection (grid row OR grid column)
        └── Report (widget)
```

### Dashboard

| Field | Purpose |
|-------|---------|
| `Id`, `Name`, `Description` | Identity |
| `IsDefault` | Which dashboard loads at `/` |
| `SortOrder` | Multi-dashboard support later |

### DashboardSection

| Field | Purpose |
|-------|---------|
| `DashboardId` | Parent |
| `Title`, `Subtitle` | Section header (e.g. "Overall Business Health") |
| `LayoutDirection` | `Row` \| `Column` |
| `SortOrder` | Section ordering |

### Report

| Field | Purpose |
|-------|---------|
| `SectionId` | Placement on dashboard |
| `Name`, `Description` | Label |
| `ReportType` | `MetricCard`, `Column`, `Pie`, `Donut`, `Line` |
| `Size` | `Small`, `Medium`, `Large`, `FullWidth` → maps to grid spans |
| `SortOrder` | Position within section |
| `IsVisible` | Toggle without deleting |
| **Query config** | |
| `TargetTable` | `Vehicles`, `Drivers` (extensible) |
| `AggregateFunction` | `Count`, `Sum`, `Average`, `Min`, `Max` |
| `AggregateField` | Column to aggregate (null for `Count`) |
| `GroupByColumns` | JSON array, 0–3 column names |
| `Filters` | JSON array of `{ field, operator, value }` |
| `ComparisonEnabled` | Metric cards only — vs previous period |
| `ChartOptions` | JSON — legend, colors, axis labels |

### Report Type Rules

| Type | Group columns | Notes |
|------|---------------|-------|
| **MetricCard** | 0 | Single aggregate; optional period comparison |
| **Column** | 1 (simple) or 2 (stacked/grouped) | Maps to Google Charts `ColumnChart` |
| **Pie** | 1 | `PieChart` |
| **Donut** | 1 | `PieChart` with `pieHole` |
| **Line** | 1 (X-axis) + optional 2nd (series) | `LineChart` |

---

## Target Data Sources (v1)

Whitelist columns per table — **never** accept arbitrary SQL from the client.

### Vehicles

- **Groupable:** `Make`, `Model`, `FuelType`, `VehicleType`, `IgnitionStatus`, `Colour`, `RegisteredOwner`
- **Aggregatable:** `Speed`, `Odometer`, `FuelLevel`, `FuelPercentageLeft`, `Year`, `Tare`, `Gvm`
- **Always filter:** `IsDeleted = false`

### Drivers

- **Groupable:** `Department`, `Branch`, `City`, `Province`, `EmploymentType`, `EmploymentStatus`, `Gender`, `LicenceCode`
- **Aggregatable:** `Count` on most fields; dates for min/max if needed

---

## Backend Architecture

### New Files

Mirror the existing `Vehicles/` and `Drivers/` patterns:

```
CarTrack.Server/
  Reports/
    ReportEntities.cs          # Dashboard, DashboardSection, Report
    ReportDtos.cs
    ReportEndpoints.cs
    IReportService.cs
    ReportService.cs           # CRUD
    IReportQueryService.cs
    ReportQueryService.cs      # Dynamic aggregation engine
    ReportTableRegistry.cs     # Whitelisted columns + types per table
  Data/
    ApplicationDbContext.cs    # Add DbSets
  Migrations/
    AddDashboardReports.cs
```

Register services and endpoints in `Program.cs`.

### API Surface

| Endpoint | Purpose |
|----------|---------|
| `GET /api/reports/metadata` | Tables, columns, allowed aggregates, max group-by per type |
| `GET /api/reports` | All reports (for Reports management page) |
| `POST /api/reports` | Create report |
| `PUT /api/reports/{id}` | Update |
| `DELETE /api/reports/{id}` | Delete |
| `POST /api/reports/{id}/execute` | Run query → chart-ready payload |
| `GET /api/dashboards` | List dashboards |
| `GET /api/dashboards/{id}` | Dashboard + sections + reports |
| `GET /api/dashboards/default` | Default dashboard with **executed** report data |
| `POST /api/dashboards` | Create |
| `PUT /api/dashboards/{id}` | Update |
| `POST /api/dashboards/{id}/sections` | Add section |
| `PUT /api/dashboards/sections/{id}` | Update section |
| `DELETE /api/dashboards/sections/{id}` | Delete section |

### Query Execution Response

Unified payload for `react-google-charts`:

```json
{
  "reportId": "...",
  "reportType": "Column",
  "columns": ["Make", "Count"],
  "rows": [["Toyota", 12], ["Ford", 8]],
  "metric": { "value": 100, "comparisonValue": 90, "changePercent": 11.1 }
}
```

`ReportQueryService` builds `IQueryable` dynamically via EF Core:

- Resolve entity from `TargetTable`
- Apply whitelisted filters
- `GroupBy` on 1–3 columns
- `Select` with `Count` / `Sum` / `Average` / `Min` / `Max`
- Return materialized rows

---

## Frontend Architecture

### New Files

```
frontend/src/
  types/report.ts, dashboard.ts
  services/reportService.ts, dashboardService.ts
  components/reports/
    ReportWidget.tsx          # Type switch → chart or metric
    MetricCardWidget.tsx      # Extract from Dashboard.tsx
    ChartWidget.tsx           # react-google-charts wrapper
    ReportBuilderDialog.tsx   # Replace CreateReport.tsx
    ReportPreview.tsx         # Live preview while building
  components/dashboard/
    DashboardRenderer.tsx     # Sections + grid layout
    DashboardSection.tsx      # Row/col grid with widgets
  page/
    Dashboard.tsx             # Fetch default dashboard, render
    ReportsPage.tsx           # Wire ReportsTable to API
```

### Dashboard Layout Rendering

Map section `LayoutDirection` and report `Size` to CSS grid:

- **Row section:** `grid-cols-12`, widgets span 3 / 4 / 6 / 12
- **Column section:** `flex-col` stack

Reuse existing `MetricCard` / `MetricCardChart` patterns from `Dashboard.tsx` and `react-google-charts` (already installed).

### Report Builder UX (Reports Page)

Stepped form in `ReportBuilderDialog`:

1. **Basics** — name, type, size, visible on dashboard
2. **Data source** — target table
3. **Measure** — aggregate function + field
4. **Grouping** — 0–3 columns (validated by type)
5. **Filters** — optional field / operator / value rows
6. **Placement** — dashboard + section (or create new section)
7. **Preview** — calls `POST /api/reports/{id}/execute`

Replace the current export-oriented `CreateReport.tsx` fields (PDF, email schedule, date range).

---

## Implementation Phases

### Phase 1 — Backend Foundation (2–3 days)

- [x] EF entities + migration
- [x] `ReportTableRegistry` with Vehicles/Drivers metadata
- [x] CRUD endpoints for dashboards, sections, reports
- [x] Register in `Program.cs`

### Phase 2 — Query Engine (2 days)

- [x] `ReportQueryService` with whitelisted dynamic queries
- [x] `POST /api/reports/{id}/execute`
- [x] `GET /api/dashboards/default/render` (layout + all executed data in one call)
- [x] `GET /api/dashboards/{id}/render`
- [x] Validation: type vs group-by count, aggregate field type compatibility

### Phase 3 — Frontend Services & Types (1 day)

- [x] `reportService.ts`, `dashboardService.ts`
- [x] TypeScript types mirroring DTOs

### Phase 4 — Dashboard Rendering (2 days)

- [x] `DashboardRenderer` + `ReportWidget`
- [x] Refactor `Dashboard.tsx` to load from API
- [x] Grid layout from section/report config

### Phase 5 — Reports Management (2 days)

- [x] Wire `ReportsTable` to API (replace mock `items`)
- [x] Build `ReportBuilderDialog` with metadata-driven dropdowns
- [x] Edit / delete / toggle visibility (visibility toggle, edit dialog, delete with confirmation)

### Phase 6 — Seed & Polish (1 day)

- [x] Seed a default dashboard inspired by `examples/`, using real Vehicle/Driver data
- [x] Loading / error states, empty dashboard fallback
- [x] Delete report from Reports management
- [x] Multi-dashboard picker (dashboard home + Reports page + report builder)
- [x] Drag-and-drop layout editor (`PUT /api/dashboards/{id}/layout`)

---

## Example Mappings (Screenshots → CarTrack Data)

| Example widget | CarTrack equivalent |
|----------------|---------------------|
| "Total Purchase" metric | Count of Vehicles |
| "Top 5 vendors by amount" column | Top Makes by vehicle count |
| "Department wise" pie | Drivers by Department |
| "Count of transactions" donut | Vehicles by FuelType |
| "Revenue over time" line | Avg Odometer by Make (or count by month `CreatedAt`) |
| Stacked column | Vehicles by Make, series = `IgnitionStatus` |

---

## Out of Scope (v1)

- Map widget (in `ReportsTable` mock but not in the spec)
- PDF/Excel export reports (separate module later)
- Scheduled email delivery
- User-specific dashboards / permissions beyond existing JWT auth

---

## Decisions to Confirm Before Coding

1. **Reports library vs embedded** — Should reports on the Reports page be a reusable library placed into dashboard sections, or always owned by a section?
   - *Recommendation: owned by section for v1; add library later.*

2. **Single default dashboard** — Is one dashboard enough for v1?
   - *Recommendation: yes, with `IsDefault` flag.*

3. **Comparison metrics** — Metric card "vs previous period" needs a time field + period logic.
   - *Recommendation: defer to Phase 6 or implement only `CreatedAt`-based month comparison.*

4. **System Alerts table** on Dashboard — Keep as static mock or replace with a report?
   - *Recommendation: replace with a seeded "Drivers with expiring licence" table report in Phase 6.*

---

## Suggested Build Order

```
Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6
         (backend)   (query)   (types)   (dashboard) (builder) (seed)
```

Phases 1–2 unblock everything else. Phase 4 delivers visible value on the home page once the query engine works.
