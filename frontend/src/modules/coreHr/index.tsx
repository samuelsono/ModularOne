import type { ModuleDefinition } from '@platform/module/types';
import { withPermission } from '@platform/permissions/PermissionGate';
import CoreLayout from './pages/CoreLayout';
import CompaniesPage from './pages/CompaniesPage';
import DepartmentsPage from './pages/DepartmentsPage';
import PositionsPage from './pages/PositionsPage';

export const coreHrModule: ModuleDefinition = {
  id: 'coreHr',
  routes: [
    {
      path: 'core',
      element: <CoreLayout />,
      children: [
        { path: 'companies', element: withPermission('core.companies.read', <CompaniesPage />) },
        { path: 'departments', element: withPermission('core.departments.read', <DepartmentsPage />) },
        { path: 'positions', element: withPermission('core.positions.read', <PositionsPage />) },
      ],
    },
  ],
  navItems: [
    { path: '/core/companies', label: 'Companies', shortLabel: 'Company', permission: 'core.companies.read' },
    { path: '/core/departments', label: 'Departments', shortLabel: 'Dept', permission: 'core.departments.read' },
    { path: '/core/positions', label: 'Positions', shortLabel: 'Position', permission: 'core.positions.read' },
    { path: '/core/employees', label: 'Employees', shortLabel: 'Users', permission: 'core.employees.read' },
  ],
  searchProviders: [
    {
      id: 'core-companies',
      matchesPath: (p) => p.startsWith('/core/companies'),
      placeholder: 'Search companies by name or code',
      enabled: true,
    },
    {
      id: 'core-departments',
      matchesPath: (p) => p.startsWith('/core/departments'),
      placeholder: 'Search departments by name or code',
      enabled: true,
    },
    {
      id: 'core-positions',
      matchesPath: (p) => p.startsWith('/core/positions'),
      placeholder: 'Search positions by name or code',
      enabled: true,
    },
  ],
};
