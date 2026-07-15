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
    canManageUsers: hasPermission(user, 'platform.settings.users.read')
      || hasPermission(user, 'core.employees.read'),
    canEditUsers: hasPermission(user, 'platform.settings.users.write')
      || hasPermission(user, 'core.employees.write'),
    isDriverRestricted: Boolean(
      user?.roles.some((role) => role === 'Driver')
      && !user?.roles.some((role) =>
        ['SystemAdmin', 'Admin', 'FleetAdmin', 'FleetOperator'].includes(role)),
    ),
  }), [user]);
}
