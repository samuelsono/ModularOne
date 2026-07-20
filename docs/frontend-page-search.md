# Activate page search for a route

The top-bar search box is driven by **search providers** each feature module registers. The shell enables the box and sets its placeholder from the first provider whose `matchesPath` returns true for the current URL. Pages read the query with `usePageSearchQuery()` and filter their own data.

## 1. Register a provider on the module

In the module’s `index.tsx` (`ModuleDefinition`), add (or extend) `searchProviders`:

```tsx
import type { ModuleDefinition } from '@platform/module/types';

export const invoicingModule: ModuleDefinition = {
  id: 'invoicing',
  routes: [/* ... */],
  navItems: [/* ... */],
  searchProviders: [
    {
      id: 'invoicing-invoices',
      // First match wins — put more specific paths before broader ones.
      matchesPath: (pathname) => pathname.startsWith('/invoicing/invoices'),
      placeholder: 'Search by invoice number or customer',
      enabled: true,
    },
    {
      id: 'invoicing-fallback',
      matchesPath: (pathname) => pathname === '/invoicing' || pathname.startsWith('/invoicing/'),
      placeholder: 'Search invoicing',
      enabled: true,
    },
  ],
};
```

The module must already be listed in `frontend/src/app/modules.tsx` (that file flattens every module’s providers into the registry).

## 2. Consume the query on the page

```tsx
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import { filterInvoices } from '@modules/invoicing/search/filters';

export default function InvoicesPage() {
  const searchQuery = usePageSearchQuery();
  const visible = filterInvoices(rows, searchQuery);
  // render visible...
}
```

Keep filtering inside the owning module (or `@platform` helpers). Do not import sibling modules for search.

## 3. Behaviour notes

| Piece | Role |
|-------|------|
| `ModuleDefinition.searchProviders` | Declares when search is on and the placeholder text |
| `@platform/search/registry` | Stores providers after `registerSearchProviders` in `app/modules.tsx` |
| `PageSearchProvider` | Clears the query on navigation; wires placeholder / enabled |
| `usePageSearchQuery()` | Returns the current string for client-side filtering |

- If **no** provider matches the path, the search box is **disabled**.
- Set `enabled: false` to reserve a path but keep the box off.
- Prefer **one provider per distinct list screen**; add a fallback only when useful.
- Order matters: `registry` uses `Array.find` — more specific `matchesPath` rules must come first.

## Checklist

1. Add `searchProviders` entry with `matchesPath` covering your route(s).
2. Ensure the module is registered in `app/modules.tsx`.
3. Call `usePageSearchQuery()` in the page/component and filter results.
4. Hard-refresh the route and confirm the top search box is enabled with your placeholder.

See also: [Adding a module](./adding-a-module.md).
