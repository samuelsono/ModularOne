import type { ModuleDefinition } from '@platform/module/types';
import ReportsPage from './pages/ReportsPage';

export const reportingModule: ModuleDefinition = {
  id: 'reporting',
  routes: [
    { path: 'reports', element: <ReportsPage /> },
  ],
  navItems: [],
  searchProviders: [
    {
      id: 'reports',
      matchesPath: (p) => p.startsWith('/reports'),
      placeholder: 'Search by report or dashboard name',
      enabled: true,
    },
  ],
};
