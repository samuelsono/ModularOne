import { useMemo } from 'react';
import { useAuth } from '@platform/auth/AuthContext';
import {
  canReadSubmodule,
  canWriteSubmodule,
  hasModuleAccess,
  hasPermission,
} from '@platform/permissions/rbac';
import {
  filterAppModules,
  filterFleetNavItems,
  filterModuleNavItems,
  type AppModuleDefinition,
} from '@platform/permissions/apps';
import type { ModuleNavItem } from '@platform/module/types';

export function usePermissions() {
  const { user } = useAuth();

  return useMemo(() => ({
    user,
    hasPermission: (permission: string) => hasPermission(user, permission),
    hasModuleAccess: (moduleSlug: string) => hasModuleAccess(user, moduleSlug),
    canReadSubmodule: (moduleSlug: string, submoduleSlug: string) =>
      canReadSubmodule(user, moduleSlug, submoduleSlug),
    canWriteSubmodule: (moduleSlug: string, submoduleSlug: string) =>
      canWriteSubmodule(user, moduleSlug, submoduleSlug),
    visibleAppModules: filterAppModules(user) as AppModuleDefinition[],
    visibleFleetNavItems: filterFleetNavItems(user) as ModuleNavItem[],
    visibleModuleNavItems: (moduleSlug: string) =>
      filterModuleNavItems(user, moduleSlug) as ModuleNavItem[],
    canManageUsers: hasPermission(user, 'platform.settings.users.read'),
    canEditUsers: hasPermission(user, 'platform.settings.users.write'),
    isAdmin: Boolean(
      user?.roles.some((role) => role === 'Admin' || role === 'SystemAdmin'),
    ),
    isHr: Boolean(user?.roles.some((role) => role === 'HR')),
    isManager: Boolean(user?.roles.some((role) => role === 'Manager')),
    isDriverRestricted: Boolean(
      user?.roles.some((role) => role === 'Driver')
      && !user?.roles.some((role) =>
        ['SystemAdmin', 'Admin', 'FleetAdmin', 'FleetOperator'].includes(role)),
    ),
  }), [user]);
}
