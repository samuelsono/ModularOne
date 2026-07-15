import type { ModuleDefinition } from '@platform/module/types';
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
        { path: 'companies', element: <CompaniesPage /> },
        { path: 'departments', element: <DepartmentsPage /> },
        { path: 'positions', element: <PositionsPage /> },
      ],
    },
  ],
  navItems: [
    { path: '/core/companies', label: 'Companies', shortLabel: 'Company', permission: 'core.companies.read' },
    { path: '/core/departments', label: 'Departments', shortLabel: 'Dept', permission: 'core.departments.read' },
    { path: '/core/positions', label: 'Positions', shortLabel: 'Position', permission: 'core.positions.read' },
    { path: '/core/employees', label: 'Employees', shortLabel: 'Users', permission: 'core.employees.read' },
  ],
};
