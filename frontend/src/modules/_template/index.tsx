import type { ModuleDefinition } from '@platform/module/types';
import TemplatePage from './pages/TemplatePage';

/**
 * Copy this folder to `src/modules/<name>/`, rename exports, then register once in `app/modules.tsx`.
 * Do not import sibling modules — use `@platform/*` or app composition.
 */
export const templateModule: ModuleDefinition = {
  id: 'template',
  routes: [
    {
      path: 'template',
      element: <TemplatePage />,
    },
  ],
  navItems: [
    {
      path: '/template',
      label: 'Template',
      shortLabel: 'Tmpl',
      // permission: 'template.read',
    },
  ],
  appModule: {
    name: 'Template module',
    description: 'Replace with your module description',
    slug: 'template',
    image: '/apps/1.png',
    homePath: '/template',
  },
  searchProviders: [
    {
      id: 'template',
      matchesPath: (p) => p.startsWith('/template'),
      placeholder: 'Search template',
      enabled: true,
    },
  ],
  permissions: [
    // 'template.read',
  ],
};
