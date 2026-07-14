# Help Module — Implementation Plan

## Overview

Replace the current `HelpCenterDrawer.tsx` stub (which incorrectly renders notifications) with a real Help Centre experience:

- **Admin** creates, edits, and publishes help articles from a dedicated section in Settings
- **All users** open the Help drawer (question-mark icon in the Navbar) to browse and search published articles
- Clicking an article opens its full content inline within the same drawer, with a back button to return to the article list

---

## Current State

`HelpCenterDrawer.tsx` is named "Help Centre" but currently renders a hardcoded notifications list — it is a stub that must be replaced entirely.  
The `Help` menu item in `AvatarMenu` (Navigation.tsx) exists but has no `onClick` handler.

---

## 1. Data Model

### 1.1 `HelpArticle` (new EF Core entity)

```csharp
public class HelpArticle
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;     // URL-safe, auto-generated from title
    public string Body { get; set; } = string.Empty;     // Markdown content
    public string? CategoryName { get; set; }            // free-form category label (e.g. "Getting Started")
    public string[] Tags { get; set; } = [];             // PostgreSQL text[] column
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser CreatedBy { get; set; } = null!;
}
```

**Index:** `IsPublished` + `CategoryName` (drives the public article list query).  
**Full-text search index:** PostgreSQL `tsvector` on `Title + Body` (via EF Core `HasGeneratedTsVectorColumn`).

### 1.2 `ApplicationDbContext` changes

- Add `DbSet<HelpArticle> HelpArticles`
- Migration: `AddHelpArticlesTable`

---

## 2. Backend — REST Endpoints

### File: `CarTrack.Server/Help/HelpEndpoints.cs`

All routes under `/api/help`.

#### Public (all authenticated users)

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/help/articles` | Returns all published articles. Query params: `?search=text&category=name`. Searches `Title` + `Body` via PostgreSQL full-text or `ILIKE` |
| `GET` | `/api/help/articles/{id}` | Returns a single published article by ID |
| `GET` | `/api/help/categories` | Returns distinct `CategoryName` values from published articles |

#### Admin only (`Admin` role)

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/help/admin/articles` | Returns all articles (published + draft) for the admin management view |
| `GET` | `/api/help/admin/articles/{id}` | Returns a single article (any status) |
| `POST` | `/api/help/admin/articles` | Create a new article |
| `PUT` | `/api/help/admin/articles/{id}` | Update an existing article |
| `PATCH` | `/api/help/admin/articles/{id}/publish` | Toggle `IsPublished` on/off |
| `DELETE` | `/api/help/admin/articles/{id}` | Delete an article |

### DTOs (`HelpDtos.cs`)

```csharp
public record HelpArticleSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string? CategoryName,
    string[] Tags,
    bool IsPublished,
    DateTime UpdatedAt
);

public record HelpArticleDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Body,       // Markdown
    string? CategoryName,
    string[] Tags,
    bool IsPublished,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record SaveHelpArticleRequest(
    string Title,
    string Body,
    string? CategoryName,
    string[] Tags,
    bool IsPublished
);
```

---

## 3. Frontend — Types

### File: `frontend/src/types/help.ts`

```ts
export interface HelpArticleSummary {
  id: string;
  title: string;
  slug: string;
  categoryName: string | null;
  tags: string[];
  isPublished: boolean;
  updatedAt: string;
}

export interface HelpArticleDetail extends HelpArticleSummary {
  body: string;       // Markdown string
  createdAt: string;
}

export interface SaveHelpArticleRequest {
  title: string;
  body: string;
  categoryName: string | null;
  tags: string[];
  isPublished: boolean;
}
```

---

## 4. Frontend — `helpService.ts`

### File: `frontend/src/services/helpService.ts`

```ts
// Public
getArticles(search?: string, category?: string): Promise<HelpArticleSummary[]>
getArticle(id: string): Promise<HelpArticleDetail>
getCategories(): Promise<string[]>

// Admin
getAdminArticles(): Promise<HelpArticleSummary[]>
getAdminArticle(id: string): Promise<HelpArticleDetail>
createArticle(data: SaveHelpArticleRequest): Promise<HelpArticleDetail>
updateArticle(id: string, data: SaveHelpArticleRequest): Promise<HelpArticleDetail>
togglePublish(id: string): Promise<HelpArticleDetail>
deleteArticle(id: string): Promise<void>
```

All calls use `authorizedFetch` from `authService.ts`.

---

## 5. Frontend — `HelpCenterDrawer.tsx` (full replacement)

**File:** `frontend/src/components/HelpCenterDrawer.tsx`

The component has two **internal views** rendered within the same `<OverlayDrawer>`:

### View A — Article List (default)

```
┌─────────────────────────────────────────┐
│ Help Centre                      [✕]    │
├─────────────────────────────────────────┤
│ [🔍 Search articles...              ]   │
│ [Category ▼]                            │
├─────────────────────────────────────────┤
│ Getting Started                         │
│  • How to add a vehicle           [→]  │
│  • How to assign a driver         [→]  │
│                                         │
│ Reports                                 │
│  • Creating a dashboard report    [→]  │
│                                         │
│ (empty state if no results found)       │
└─────────────────────────────────────────┘
```

- Fluent UI `<SearchBox>` (controlled, debounced 300ms) calls `getArticles(search, category)` on change
- `<Dropdown>` for category filter — options populated from `getCategories()`
- Articles grouped by `categoryName` using a `Map`
- Each article row is a clickable `<div>` that transitions to View B
- Loading skeleton: `<Skeleton>` rows while fetching

### View B — Article Detail

```
┌─────────────────────────────────────────┐
│ [← Back]   How to add a vehicle  [✕]   │
├─────────────────────────────────────────┤
│ Getting Started  •  Updated 2 Jul 2026  │
│                                         │
│  [Rendered markdown content]            │
│                                         │
└─────────────────────────────────────────┘
```

- Back button sets `selectedArticleId` to `null`, returning to View A
- Article body rendered via a lightweight Markdown renderer (e.g. `react-markdown` — already small, no heavy deps)
- Tags displayed as `<Badge>` chips below the category line

### State shape

```ts
const [drawerOpen, setDrawerOpen] = useState(false);
const [selectedArticleId, setSelectedArticleId] = useState<string | null>(null);
const [search, setSearch] = useState('');
const [category, setCategory] = useState<string | undefined>();
```

### `Help` item in AvatarMenu

Wire the existing `<MenuItem icon={<QuestionCircleRegular />}>Help</MenuItem>` to open the drawer:

```tsx
// Pass an open/setter via a shared ref or lift state to Navigation level
// Simplest approach: a small context or callback prop
```

The simplest implementation is a `HelpDrawerContext` with `{ open: boolean, setOpen }` — both `Navigation.tsx` (question mark button) and the `Help` menu item call `setOpen(true)`.

---

## 6. Frontend — Admin Article Management (Settings)

### Location in Settings

Add a new category **"Help & Support"** to `SettingsPage.tsx` with a section **"Help Articles"**.

### Component: `HelpArticlesManager.tsx`

**File:** `frontend/src/components/settings/HelpArticlesManager.tsx`

Layout mirrors the existing settings pattern:
- Left: article list table (`<DataGrid>` with Title, Category, Status, Updated At columns)
- Right: `<HelpArticleFormPanel>` (inline panel) OR a dialog — opens on "New Article" or row click

#### `HelpArticleFormPanel`

Fields:
| Field | Control |
|-------|---------|
| Title | `<Input>` (required, max 200 chars) |
| Category | `<Combobox>` (freeform + existing categories as suggestions) |
| Tags | Tag-input using `<TagPicker>` (Fluent UI v9) |
| Body | `<Textarea>` for Markdown (with a live preview tab using `react-markdown`) |
| Published | `<Switch>` toggle |

Actions: **Save Draft**, **Publish** (sets `isPublished: true` + saves), **Delete** (with `<ConfirmAction>` dialog — already exists in the codebase).

---

## 7. Markdown Rendering

Install `react-markdown` (lightweight, no heavy peer deps):

```bash
npm install react-markdown
```

Use in the article detail view:
```tsx
import ReactMarkdown from 'react-markdown';
// ...
<ReactMarkdown>{article.body}</ReactMarkdown>
```

Apply Tailwind `prose` class (or a thin `makeStyles` override) to style rendered HTML.

---

## 8. Migration Plan

New EF Core migration: `AddHelpArticlesTable`

```
Notification.cs → Notification + NotificationRecipient (from notifications plan)
HelpArticle.cs  → HelpArticles table (this module)
```

---

## 9. Implementation Order

| # | Task | Layer |
|---|------|-------|
| 1 | Create `HelpArticle` model + update `ApplicationDbContext` | Backend |
| 2 | Run migration `AddHelpArticlesTable` | Backend |
| 3 | Create `HelpDtos.cs` | Backend |
| 4 | Implement `HelpEndpoints.cs` and register in `Program.cs` | Backend |
| 5 | Create `frontend/src/types/help.ts` | Frontend |
| 6 | Create `frontend/src/services/helpService.ts` | Frontend |
| 7 | Create `HelpDrawerContext.tsx` (open/close state shared between nav button + avatar menu item) | Frontend |
| 8 | Replace `HelpCenterDrawer.tsx` with article-list + article-detail views | Frontend |
| 9 | Wire "Help" `<MenuItem>` in `AvatarMenu` to `HelpDrawerContext` | Frontend |
| 10 | Install `react-markdown` | Frontend |
| 11 | Build `HelpArticleFormPanel.tsx` (article editor with preview) | Frontend |
| 12 | Build `HelpArticlesManager.tsx` (article list + form panel) | Frontend |
| 13 | Add "Help & Support" → "Help Articles" section to `SettingsPage.tsx` | Frontend |

---

## 10. Security Considerations

- All article write/delete endpoints require the `Admin` role
- Article `Body` is stored as raw Markdown and rendered client-side via `react-markdown` — no server-side HTML injection risk
- The `CreatedByUserId` FK is set server-side from the JWT claim, never from the request body
- `Slug` is auto-generated server-side from the title (sanitised to `[a-z0-9-]`) — not user-controlled
