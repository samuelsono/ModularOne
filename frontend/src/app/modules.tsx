import type { RouteObject } from 'react-router-dom';
import type { ModuleDefinition } from '@platform/module/types';
import { registerModules } from '@platform/permissions/apps';
import { withAnyPermission, withPermission } from '@platform/permissions/PermissionGate';
import { registerSearchProviders } from '@platform/search/registry';
import ModulePlaceholder from '@platform/shell/ModulePlaceholder';

import { authModule } from '@modules/auth';
import { leaveModule } from '@modules/leave';
import { expenseModule } from '@modules/expense';
import { fleetModule } from '@modules/fleet';
import { coreHrModule } from '@modules/coreHr';
import { usersModule, EmployeesListPage } from '@modules/users';
import { reportingModule } from '@modules/reporting';
import { settingsModule } from '@modules/settings';
import { notificationsModule } from '@modules/notifications';
import { supportModule } from '@modules/support';
import { helpModule } from '@modules/help';
import { tendersModule } from '@modules/tenders';
import SettingsPage from './SettingsPage';
import FleetHomePage from './FleetHomePage';

/** Cross-module UI composition that must not live inside feature modules (ADR 0001). */
function applyCompositionOverrides(defs: ModuleDefinition[]): ModuleDefinition[] {
  return defs.map((mod) => {
    if (mod.id === 'fleet') {
      return {
        ...mod,
        routes: mod.routes.map((route) =>
          route.index
            ? { ...route, element: withPermission('fleet.dashboard.read', <FleetHomePage />) }
            : route,
        ),
      };
    }

    if (mod.id === 'settings') {
      return {
        ...mod,
        routes: [{ path: 'settings', element: <SettingsPage /> }],
      };
    }

    return mod;
  });
}

/** All feature modules (app composition root). */
export const modules: ModuleDefinition[] = applyCompositionOverrides([
  authModule,
  leaveModule,
  expenseModule,
  fleetModule,
  coreHrModule,
  usersModule,
  reportingModule,
  settingsModule,
  notificationsModule,
  supportModule,
  helpModule,
  tendersModule,
]);

registerModules(modules);

const searchProviders = modules.flatMap((m) => m.searchProviders ?? []);
registerSearchProviders(searchProviders);

function mergeCoreEmployeesRoute(routes: RouteObject[]): RouteObject[] {
  return routes.map((route) => {
    if (route.path === 'core' && route.children) {
      return {
        ...route,
        children: [
          ...route.children,
          {
            path: 'employees',
            element: withAnyPermission(
              ['core.employees.read', 'platform.settings.users.read'],
              <EmployeesListPage />,
            ),
          },
        ],
      };
    }
    return route;
  });
}

export function assembleAuthRoutes(): RouteObject[] {
  return authModule.routes;
}

export function assembleProtectedChildRoutes(): RouteObject[] {
  const featureRoutes = modules
    .filter((m) => m.id !== 'auth')
    .flatMap((m) => m.routes);

  const withCoreEmployees = mergeCoreEmployeesRoute(featureRoutes);

  return [
    ...withCoreEmployees,
    { path: 'accounting/*', element: <ModulePlaceholder /> },
    { path: 'payroll/*', element: <ModulePlaceholder /> },
    { path: 'performance/*', element: <ModulePlaceholder /> },
    { path: 'recruitment/*', element: <ModulePlaceholder /> },
  ];
}
