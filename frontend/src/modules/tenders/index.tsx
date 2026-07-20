import type { ModuleDefinition } from '@platform/module/types';
import { withPermission } from '@platform/permissions/PermissionGate';
import TendersLayout from './pages/TendersLayout';
import TenderResultsPage from './pages/TenderResultsPage';
import TenderSourcesPage from './pages/TenderSourcesPage';
import TenderQueriesPage from './pages/TenderQueriesPage';
import TenderRunsPage from './pages/TenderRunsPage';

export const tendersModule: ModuleDefinition = {
  id: 'tenders',
  routes: [
    {
      path: 'tenders',
      element: <TendersLayout />,
      children: [
        { index: true, element: withPermission('tenders.results.read', <TenderResultsPage />) },
        { path: 'sources', element: withPermission('tenders.sources.read', <TenderSourcesPage />) },
        { path: 'queries', element: withPermission('tenders.queries.read', <TenderQueriesPage />) },
        { path: 'runs', element: withPermission('tenders.runs.read', <TenderRunsPage />) },
      ],
    },
  ],
  navItems: [
    {
      path: '/tenders',
      label: 'Results',
      shortLabel: 'Results',
      permission: 'tenders.results.read',
    },
    {
      path: '/tenders/sources',
      label: 'Sources',
      shortLabel: 'Sources',
      permission: 'tenders.sources.read',
    },
    {
      path: '/tenders/queries',
      label: 'Queries',
      shortLabel: 'Queries',
      permission: 'tenders.queries.read',
    },
    {
      path: '/tenders/runs',
      label: 'Runs',
      shortLabel: 'Runs',
      permission: 'tenders.runs.read',
    },
  ],
  appModule: {
    name: 'Tender management',
    description: 'Watch portals for matching tenders',
    slug: 'tenders',
    image: '/apps/1.png',
    homePath: '/tenders',
  },
  searchProviders: [
    {
      id: 'tenders-results',
      matchesPath: (p) => p === '/tenders' || p === '/tenders/',
      placeholder: 'Search results by title, keyword, or URL',
      enabled: true,
    },
    {
      id: 'tenders-sources',
      matchesPath: (p) => p === '/tenders/sources' || p.startsWith('/tenders/sources/'),
      placeholder: 'Search sources',
      enabled: false,
    },
  ],
  permissions: [
    'tenders.sources.read',
    'tenders.queries.read',
    'tenders.results.read',
    'tenders.runs.read',
  ],
};
