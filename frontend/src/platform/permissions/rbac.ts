import type { AuthUser } from '@platform/auth/types';

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
