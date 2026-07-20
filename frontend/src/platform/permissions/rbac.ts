import type { AuthUser } from '@platform/auth/types';

const ADMIN_ROLES = new Set(['SystemAdmin', 'Admin']);

const CORE_STRUCTURE_PERMISSIONS = new Set([
  'core.companies.read',
  'core.companies.write',
  'core.departments.read',
  'core.departments.write',
  'core.positions.read',
  'core.positions.write',
]);

export function hasPermission(user: AuthUser | null | undefined, permission: string): boolean {
  if (!user) {
    return false;
  }

  if (user.roles.some((role) => ADMIN_ROLES.has(role))) {
    return true;
  }

  // Non-HR roles never see Company / Department / Position structure pages,
  // even if a stale session still lists those permission keys.
  if (
    CORE_STRUCTURE_PERMISSIONS.has(permission)
    && !user.roles.some((role) => role === 'HR')
  ) {
    return false;
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
