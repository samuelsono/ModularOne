import type { ModuleDefinition } from '@platform/module/types';
import EmployeesList from './pages/EmployeesList';

export const usersModule: ModuleDefinition = {
  id: 'users',
  routes: [
    { path: 'employees', element: <EmployeesList /> },
  ],
  navItems: [],
  searchProviders: [
    {
      id: 'employees',
      matchesPath: (p) => p.startsWith('/employees') || p.startsWith('/core/employees'),
      placeholder: 'Search by name, email, role, or manager',
      enabled: true,
    },
  ],
};

/** Mounted under CoreLayout by app route assembly (avoids coreHr → users import). */
export { default as EmployeesListPage } from './pages/EmployeesList';
