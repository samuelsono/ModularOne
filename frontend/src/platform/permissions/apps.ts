import type { AuthUser } from '@platform/auth/types';
import type { AppModuleContribution, ModuleDefinition, ModuleNavItem } from '@platform/module/types';
import { hasModuleAccess, hasPermission } from '@platform/permissions/rbac';

export type AppModuleDefinition = AppModuleContribution;

export const DEFAULT_MODULE_SLUG = 'fleet';

/** Placeholder launcher cards for modules not yet implemented as feature folders. */
export const PLACEHOLDER_APP_MODULES: AppModuleDefinition[] = [
  {
    name: 'Accounting management',
    description: 'Billing and Invoicing',
    slug: 'accounting',
    image: '/apps/1.png',
    homePath: '/accounting',
  },
  {
    name: 'Payroll management',
    description: 'Manage Staff Payroll',
    slug: 'payroll',
    image: '/apps/4.png',
    homePath: '/payroll',
  },
  {
    name: 'Performance management',
    description: 'Manage Staff Performance',
    slug: 'performance',
    image: '/apps/5.png',
    homePath: '/performance',
  },
  {
    name: 'Recruitment management',
    description: 'Manage Staff Recruitment',
    slug: 'recruitment',
    image: '/apps/6.png',
    homePath: '/recruitment',
  },
];

export const PLACEHOLDER_NAV_ITEMS: Record<string, ModuleNavItem[]> = {
  accounting: [
    { path: '/accounting', label: 'Billing', shortLabel: 'Billing', permission: 'accounting.billing.read' },
    { path: '/accounting/invoicing', label: 'Invoicing', shortLabel: 'Invoice', permission: 'accounting.invoicing.read' },
    { path: '/accounting/receivables', label: 'Receivables', shortLabel: 'Receive', permission: 'accounting.receivables.read' },
    { path: '/accounting/settings', label: 'Settings', shortLabel: 'Settings', permission: 'accounting.settings.read' },
  ],
  payroll: [
    { path: '/payroll', label: 'Payroll runs', shortLabel: 'Runs', permission: 'payroll.runs.read' },
    { path: '/payroll/payslips', label: 'Payslips', shortLabel: 'Payslip', permission: 'payroll.payslips.read' },
    { path: '/payroll/deductions', label: 'Deductions', shortLabel: 'Deduct', permission: 'payroll.deductions.read' },
    { path: '/payroll/settings', label: 'Settings', shortLabel: 'Settings', permission: 'payroll.settings.read' },
  ],
  performance: [
    { path: '/performance', label: 'Reviews', shortLabel: 'Reviews', permission: 'performance.reviews.read' },
    { path: '/performance/goals', label: 'Goals', shortLabel: 'Goals', permission: 'performance.goals.read' },
    { path: '/performance/feedback', label: 'Feedback', shortLabel: 'Feedback', permission: 'performance.feedback.read' },
    { path: '/performance/reports', label: 'Reports', shortLabel: 'Reports', permission: 'performance.reports.read' },
  ],
  recruitment: [
    { path: '/recruitment', label: 'Job posts', shortLabel: 'Jobs', permission: 'recruitment.jobs.read' },
    { path: '/recruitment/applications', label: 'Applications', shortLabel: 'Apply', permission: 'recruitment.applications.read' },
    { path: '/recruitment/interviews', label: 'Interviews', shortLabel: 'Interview', permission: 'recruitment.interviews.read' },
    { path: '/recruitment/offers', label: 'Offers', shortLabel: 'Offers', permission: 'recruitment.offers.read' },
  ],
};

let registeredModules: ModuleDefinition[] = [];
let navBySlug: Record<string, ModuleNavItem[]> = { ...PLACEHOLDER_NAV_ITEMS };
let coreHrNavItems: ModuleNavItem[] = [];

/** Aggregated launcher definitions — mutated by registerModules(). */
export let APP_MODULES: AppModuleDefinition[] = [...PLACEHOLDER_APP_MODULES];

export function registerModules(modules: ModuleDefinition[]): void {
  registeredModules = modules;
  navBySlug = { ...PLACEHOLDER_NAV_ITEMS };
  coreHrNavItems = [];

  const fromModules: AppModuleDefinition[] = [];
  for (const mod of modules) {
    if (mod.appModule) {
      fromModules.push(mod.appModule);
    }
    if (mod.navItems.length > 0) {
      navBySlug[mod.id] = mod.navItems;
    }
    if (mod.id === 'coreHr') {
      coreHrNavItems = mod.navItems;
    }
  }

  const bySlug = new Map(
    [...PLACEHOLDER_APP_MODULES, ...fromModules].map((m) => [m.slug, m]),
  );
  const order = [
    'accounting',
    'leave',
    'expense',
    'payroll',
    'performance',
    'recruitment',
    'tenders',
    'fleet',
  ];
  APP_MODULES = order
    .map((slug) => bySlug.get(slug))
    .filter((m): m is AppModuleDefinition => Boolean(m));
}

export function getRegisteredModules(): ModuleDefinition[] {
  return registeredModules;
}

export function getAppModule(slug: string): AppModuleDefinition | undefined {
  return APP_MODULES.find((module) => module.slug === slug);
}

export function filterAppModules(user: AuthUser | null | undefined): AppModuleDefinition[] {
  return APP_MODULES.filter((module) => hasModuleAccess(user, module.slug));
}

export function filterAppModulesByInstalled(
  modules: AppModuleDefinition[],
  installedSlugs: string[],
): AppModuleDefinition[] {
  const installedSet = new Set(installedSlugs.map((s) => s.toLowerCase()));
  return modules.filter((module) => installedSet.has(module.slug.toLowerCase()));
}

export type SidebarNavEntry =
  | ModuleNavItem
  | { type: 'divider'; id: string };

export function filterModuleNavItems(
  user: AuthUser | null | undefined,
  moduleSlug: string,
): ModuleNavItem[] {
  const items = navBySlug[moduleSlug] ?? [];
  return items.filter((item) => {
    // Hide any nav entry the user cannot open — require an explicit permission grant.
    if (item.anyPermissions?.length) {
      return item.anyPermissions.some((p) => hasPermission(user, p));
    }
    if (item.permission) {
      return hasPermission(user, item.permission);
    }
    // Items without a permission gate are treated as inaccessible (avoid leaking menus).
    return false;
  });
}

export function buildModuleSidebarNavItems(
  user: AuthUser | null | undefined,
  moduleSlug: string,
): SidebarNavEntry[] {
  const entries: SidebarNavEntry[] = [...filterModuleNavItems(user, moduleSlug)];

  if (moduleSlug === 'leave' || moduleSlug === 'expense') {
    const coreItems = coreHrNavItems.filter((item) =>
      item.permission ? hasPermission(user, item.permission) : true,
    );
    if (entries.length > 0 && coreItems.length > 0) {
      entries.push({ type: 'divider', id: 'core-hr-divider' });
    }
    entries.push(...coreItems);
  }

  return entries;
}

export function isModuleNavItem(entry: SidebarNavEntry): entry is ModuleNavItem {
  return !('type' in entry);
}

export function filterFleetNavItems(user: AuthUser | null | undefined): ModuleNavItem[] {
  return filterModuleNavItems(user, 'fleet');
}

const FLEET_ROUTE_PREFIXES = [
  '/live-tracking',
  '/vehicle-list',
  '/drivers',
  '/reports',
  '/employees',
];

/** Apps that host Core HR (`/core/*`) in their sidebar. */
export const CORE_HOST_MODULE_SLUGS = ['leave', 'expense'] as const;

export function isCoreRoute(pathname: string): boolean {
  return pathname === '/core' || pathname.startsWith('/core/');
}

export function resolveModuleFromPath(pathname: string): string | null {
  if (pathname.startsWith('/settings') || pathname.startsWith('/auth')) {
    return null;
  }

  if (pathname.startsWith('/accounting')) {
    return 'accounting';
  }

  if (pathname.startsWith('/leave')) {
    return 'leave';
  }

  if (pathname.startsWith('/expense')) {
    return 'expense';
  }

  // Core HR is shared under Leave/Expense shells — resolved with stored host context.
  if (isCoreRoute(pathname)) {
    return null;
  }

  if (pathname.startsWith('/payroll')) {
    return 'payroll';
  }

  if (pathname.startsWith('/performance')) {
    return 'performance';
  }

  if (pathname.startsWith('/recruitment')) {
    return 'recruitment';
  }

  if (
    pathname === '/'
    || FLEET_ROUTE_PREFIXES.some((prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`))
  ) {
    return 'fleet';
  }

  return null;
}

/**
 * Prefer a Leave/Expense host when the URL is `/core/*` (those modules own Core HR nav).
 * Falls back to the first visible Core host so refresh never jumps to Fleet.
 */
export function resolveCoreHostModuleSlug(
  storedSlug: string | null,
  visibleSlugs: string[],
): string | null {
  if (
    storedSlug
    && (CORE_HOST_MODULE_SLUGS as readonly string[]).includes(storedSlug)
    && visibleSlugs.includes(storedSlug)
  ) {
    return storedSlug;
  }

  for (const slug of CORE_HOST_MODULE_SLUGS) {
    if (visibleSlugs.includes(slug)) {
      return slug;
    }
  }

  return null;
}
