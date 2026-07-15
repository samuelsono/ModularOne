import type { ModuleDefinition } from '@platform/module/types';
import Dashboard from './pages/Dashboard';
import VehicleList from './pages/VehicleList';
import LiveTracking from './pages/LiveTracking';
import DriversList from './pages/DriversList';

export const fleetModule: ModuleDefinition = {
  id: 'fleet',
  routes: [
    { index: true, element: <Dashboard /> },
    { path: 'vehicle-list', element: <VehicleList /> },
    { path: 'live-tracking', element: <LiveTracking /> },
    { path: 'drivers', element: <DriversList /> },
  ],
  navItems: [
    { path: '/', label: 'Home', shortLabel: 'Home', permission: 'fleet.dashboard.read' },
    { path: '/live-tracking', label: 'Live tracking', shortLabel: 'Track', permission: 'fleet.tracking.read' },
    { path: '/vehicle-list', label: 'Vehicles', shortLabel: 'Vehicles', permission: 'fleet.vehicles.read' },
    { path: '/drivers', label: 'Drivers', shortLabel: 'Drivers', permission: 'fleet.drivers.read' },
    { path: '/reports', label: 'Reports', shortLabel: 'Reports', permission: 'fleet.reports.read' },
    { path: '/employees', label: 'Employees', shortLabel: 'Users', permission: 'platform.settings.users.read' },
  ],
  appModule: {
    name: 'Fleet management',
    description: 'Manage Vehicle Tracking',
    slug: 'fleet',
    image: '/apps/9.png',
    homePath: '/',
  },
  searchProviders: [
    {
      id: 'fleet-dashboard',
      matchesPath: (p) => p === '/' || p === '',
      placeholder: 'Search dashboard reports and sections',
      enabled: true,
    },
    {
      id: 'fleet-vehicles',
      matchesPath: (p) => p.startsWith('/vehicle-list'),
      placeholder: 'Search by registration, VIN, make, or model',
      enabled: true,
    },
    {
      id: 'fleet-tracking',
      matchesPath: (p) => p.startsWith('/live-tracking'),
      placeholder: 'Search by registration, VIN, make, or model',
      enabled: true,
    },
    {
      id: 'fleet-drivers',
      matchesPath: (p) => p.startsWith('/drivers'),
      placeholder: 'Search by first name, last name, or email',
      enabled: true,
    },
  ],
};
