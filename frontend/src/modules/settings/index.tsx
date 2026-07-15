import type { ModuleDefinition } from '@platform/module/types';

/**
 * Settings feature panels live under this module; the settings shell page is
 * composed in `app/SettingsPage.tsx` so sibling modules can contribute without
 * settings importing them (ADR 0001).
 */
export const settingsModule: ModuleDefinition = {
  id: 'settings',
  routes: [],
  navItems: [],
  searchProviders: [
    {
      id: 'settings',
      matchesPath: (p) => p.startsWith('/settings'),
      placeholder: 'Search settings',
      enabled: true,
    },
  ],
};
