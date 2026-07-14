import type { AuthUser } from '../types/auth';

const ADMIN_ROLES = new Set(['SystemAdmin', 'Admin']);

export function hasPermission(user: AuthUser | null | undefined, permission: string): boolean {
  if (!user) {
    return false;
  }

  if (user.roles.some((role) => ADMIN_ROLES.has(role))) {
    return true;
  }

  return user.permissions.includes(permission);
}

export function hasAnyPermission(
  user: AuthUser | null | undefined,
  permissions: string[],
): boolean {
  return permissions.some((permission) => hasPermission(user, permission));
}

export function hasModuleAccess(user: AuthUser | null | undefined, moduleSlug: string): boolean {
  if (!user) {
    return false;
  }

  if (user.roles.some((role) => ADMIN_ROLES.has(role))) {
    return true;
  }

  return user.modules.includes(moduleSlug);
}

export function canReadSubmodule(
  user: AuthUser | null | undefined,
  moduleSlug: string,
  submoduleSlug: string,
): boolean {
  return hasPermission(user, `${moduleSlug}.${submoduleSlug}.read`)
    || hasPermission(user, `${moduleSlug}.${submoduleSlug}.write`);
}

export function canWriteSubmodule(
  user: AuthUser | null | undefined,
  moduleSlug: string,
  submoduleSlug: string,
): boolean {
  return hasPermission(user, `${moduleSlug}.${submoduleSlug}.write`);
}

export interface AppModuleDefinition {
  name: string;
  description: string;
  slug: string;
  image: string;
  homePath: string;
}

export const APP_MODULES: AppModuleDefinition[] = [
  {
    name: 'Accounting management',
    description: 'Billing and Invoicing',
    slug: 'accounting',
    image: '/apps/1.png',
    homePath: '/accounting',
  },
  {
    name: 'Leave management',
    description: 'Manage Staff Leave',
    slug: 'leave',
    image: '/apps/2.png',
    homePath: '/leave',
  },
  {
    name: 'Expense claims',
    description: 'Submit and manage expense claims',
    slug: 'expense',
    image: '/apps/3.png',
    homePath: '/expense',
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
  {
    name: 'Fleet management',
    description: 'Manage Vehicle Tracking',
    slug: 'fleet',
    image: '/apps/9.png',
    homePath: '/',
  },
];

export const DEFAULT_MODULE_SLUG = 'fleet';

export function getAppModule(slug: string): AppModuleDefinition | undefined {
  return APP_MODULES.find((module) => module.slug === slug);
}

export function filterAppModules(user: AuthUser | null | undefined): AppModuleDefinition[] {
  return APP_MODULES.filter((module) => hasModuleAccess(user, module.slug));
}

export interface ModuleNavItem {
  to: string;
  label: string;
  shortLabel: string;
  permission: string;
}

export type SidebarNavEntry =
  | ModuleNavItem
  | { type: 'divider'; id: string };

export const CORE_HR_NAV_ITEMS: ModuleNavItem[] = [
  { to: '/core/companies', label: 'Companies', shortLabel: 'Company', permission: 'core.companies.read' },
  { to: '/core/departments', label: 'Departments', shortLabel: 'Dept', permission: 'core.departments.read' },
  { to: '/core/positions', label: 'Positions', shortLabel: 'Position', permission: 'core.positions.read' },
  { to: '/core/employees', label: 'Employees', shortLabel: 'Users', permission: 'core.employees.read' },
];

export const FLEET_NAV_ITEMS: ModuleNavItem[] = [
  { to: '/', label: 'Home', shortLabel: 'Home', permission: 'fleet.dashboard.read' },
  { to: '/live-tracking', label: 'Live tracking', shortLabel: 'Track', permission: 'fleet.tracking.read' },
  { to: '/vehicle-list', label: 'Vehicles', shortLabel: 'Vehicles', permission: 'fleet.vehicles.read' },
  { to: '/drivers', label: 'Drivers', shortLabel: 'Drivers', permission: 'fleet.drivers.read' },
  { to: '/reports', label: 'Reports', shortLabel: 'Reports', permission: 'fleet.reports.read' },
  { to: '/employees', label: 'Employees', shortLabel: 'Users', permission: 'platform.settings.users.read' },
];

export const ACCOUNTING_NAV_ITEMS: ModuleNavItem[] = [
  { to: '/accounting', label: 'Billing', shortLabel: 'Billing', permission: 'accounting.billing.read' },
  { to: '/accounting/invoicing', label: 'Invoicing', shortLabel: 'Invoice', permission: 'accounting.invoicing.read' },
  { to: '/accounting/receivables', label: 'Receivables', shortLabel: 'Receive', permission: 'accounting.receivables.read' },
  { to: '/accounting/settings', label: 'Settings', shortLabel: 'Settings', permission: 'accounting.settings.read' },
];

export const LEAVE_NAV_ITEMS: ModuleNavItem[] = [
  { to: '/leave', label: 'Dashboard', shortLabel: 'Home', permission: 'leave.reports.read' },
  { to: '/leave/requests', label: 'Requests', shortLabel: 'Requests', permission: 'leave.requests.read' },
  { to: '/leave/approvals', label: 'Approvals', shortLabel: 'Approve', permission: 'leave.approvals.read' },
  { to: '/leave/calendar', label: 'Calendar', shortLabel: 'Calendar', permission: 'leave.calendar.read' },
  { to: '/leave/policies', label: 'Policies', shortLabel: 'Policy', permission: 'leave.policies.read' },
  { to: '/leave/balances', label: 'Balances', shortLabel: 'Balance', permission: 'leave.balances.read' },
];

export const EXPENSE_NAV_ITEMS: ModuleNavItem[] = [
  { to: '/expense/reports', label: 'Home', shortLabel: 'Home', permission: 'expense.reports.read' },
  { to: '/expense', label: 'Claims', shortLabel: 'Claims', permission: 'expense.claims.read' },
  { to: '/expense/approvals', label: 'Approvals', shortLabel: 'Approve', permission: 'expense.approvals.read' },
  { to: '/expense/categories', label: 'Categories', shortLabel: 'Category', permission: 'expense.categories.read' },
  { to: '/expense/balances', label: 'Balances', shortLabel: 'Balance', permission: 'expense.claims.read' },
];

export const PAYROLL_NAV_ITEMS: ModuleNavItem[] = [
  { to: '/payroll', label: 'Payroll runs', shortLabel: 'Runs', permission: 'payroll.runs.read' },
  { to: '/payroll/payslips', label: 'Payslips', shortLabel: 'Payslip', permission: 'payroll.payslips.read' },
  { to: '/payroll/deductions', label: 'Deductions', shortLabel: 'Deduct', permission: 'payroll.deductions.read' },
  { to: '/payroll/settings', label: 'Settings', shortLabel: 'Settings', permission: 'payroll.settings.read' },
];

export const PERFORMANCE_NAV_ITEMS: ModuleNavItem[] = [
  { to: '/performance', label: 'Reviews', shortLabel: 'Reviews', permission: 'performance.reviews.read' },
  { to: '/performance/goals', label: 'Goals', shortLabel: 'Goals', permission: 'performance.goals.read' },
  { to: '/performance/feedback', label: 'Feedback', shortLabel: 'Feedback', permission: 'performance.feedback.read' },
  { to: '/performance/reports', label: 'Reports', shortLabel: 'Reports', permission: 'performance.reports.read' },
];

export const RECRUITMENT_NAV_ITEMS: ModuleNavItem[] = [
  { to: '/recruitment', label: 'Job posts', shortLabel: 'Jobs', permission: 'recruitment.jobs.read' },
  { to: '/recruitment/applications', label: 'Applications', shortLabel: 'Apply', permission: 'recruitment.applications.read' },
  { to: '/recruitment/interviews', label: 'Interviews', shortLabel: 'Interview', permission: 'recruitment.interviews.read' },
  { to: '/recruitment/offers', label: 'Offers', shortLabel: 'Offers', permission: 'recruitment.offers.read' },
];

export const MODULE_NAV_ITEMS: Record<string, ModuleNavItem[]> = {
  fleet: FLEET_NAV_ITEMS,
  accounting: ACCOUNTING_NAV_ITEMS,
  leave: LEAVE_NAV_ITEMS,
  expense: EXPENSE_NAV_ITEMS,
  payroll: PAYROLL_NAV_ITEMS,
  performance: PERFORMANCE_NAV_ITEMS,
  recruitment: RECRUITMENT_NAV_ITEMS,
};

const FLEET_ROUTE_PREFIXES = [
  '/live-tracking',
  '/vehicle-list',
  '/drivers',
  '/reports',
  '/employees',
];

export function filterModuleNavItems(
  user: AuthUser | null | undefined,
  moduleSlug: string,
): ModuleNavItem[] {
  const items = MODULE_NAV_ITEMS[moduleSlug] ?? [];
  return items.filter((item) => hasPermission(user, item.permission));
}

export function buildModuleSidebarNavItems(
  user: AuthUser | null | undefined,
  moduleSlug: string,
): SidebarNavEntry[] {
  const entries: SidebarNavEntry[] = [...filterModuleNavItems(user, moduleSlug)];

  if (moduleSlug === 'leave' || moduleSlug === 'expense') {
    const coreHrItems = CORE_HR_NAV_ITEMS.filter((item) => hasPermission(user, item.permission));
    if (entries.length > 0 && coreHrItems.length > 0) {
      entries.push({ type: 'divider', id: 'core-hr-divider' });
    }
    entries.push(...coreHrItems);
  }

  return entries;
}

export function isModuleNavItem(entry: SidebarNavEntry): entry is ModuleNavItem {
  return !('type' in entry);
}

export function filterFleetNavItems(user: AuthUser | null | undefined): ModuleNavItem[] {
  return filterModuleNavItems(user, 'fleet');
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

  if (pathname.startsWith('/core')) {
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
