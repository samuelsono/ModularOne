import type { RouteObject } from 'react-router-dom';

export interface ModuleNavItem {
  label: string;
  path: string;
  /** Short label shown under the sidebar icon (legacy shell). */
  shortLabel: string;
  permission?: string;
  anyPermissions?: string[];
  icon?: string;
}

export interface SearchProvider {
  id: string;
  /** return true if this provider applies to the current pathname */
  matchesPath: (pathname: string) => boolean;
  placeholder?: string;
  enabled?: boolean;
}

export interface AppModuleContribution {
  name: string;
  description: string;
  slug: string;
  image: string;
  homePath: string;
}

export interface ModuleDefinition {
  id: string;
  routes: RouteObject[];
  navItems: ModuleNavItem[];
  appModule?: AppModuleContribution;
  searchProviders?: SearchProvider[];
  /** permission keys this module introduces (optional documentation/list) */
  permissions?: string[];
}
