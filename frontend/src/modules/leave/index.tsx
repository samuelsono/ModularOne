import type { ModuleDefinition } from '@platform/module/types';
import { withAnyPermission, withPermission } from '@platform/permissions/PermissionGate';
import LeaveLayout from './pages/LeaveLayout';
import LeaveReportsPage from './pages/LeaveReportsPage';
import LeaveRequestsPage from './pages/LeaveRequestsPage';
import LeaveApprovalsPage from './pages/LeaveApprovalsPage';
import LeaveCalendarPage from './pages/LeaveCalendarPage';
import LeavePoliciesPage from './pages/LeavePoliciesPage';
import LeaveBalancesPage from './pages/LeaveBalancesPage';

export const leaveModule: ModuleDefinition = {
  id: 'leave',
  routes: [
    {
      path: 'leave',
      element: <LeaveLayout />,
      children: [
        {
          index: true,
          element: withAnyPermission(
            ['leave.reports.read', 'leave.requests.read'],
            <LeaveReportsPage />,
          ),
        },
        { path: 'requests', element: withPermission('leave.requests.read', <LeaveRequestsPage />) },
        { path: 'approvals', element: withPermission('leave.approvals.read', <LeaveApprovalsPage />) },
        { path: 'calendar', element: withPermission('leave.calendar.read', <LeaveCalendarPage />) },
        { path: 'policies', element: withPermission('leave.policies.read', <LeavePoliciesPage />) },
        { path: 'balances', element: withPermission('leave.balances.read', <LeaveBalancesPage />) },
      ],
    },
  ],
  navItems: [
    {
      path: '/leave',
      label: 'Dashboard',
      shortLabel: 'Home',
      anyPermissions: ['leave.reports.read', 'leave.requests.read'],
    },
    { path: '/leave/requests', label: 'Requests', shortLabel: 'Requests', permission: 'leave.requests.read' },
    { path: '/leave/approvals', label: 'Approvals', shortLabel: 'Approve', permission: 'leave.approvals.read' },
    { path: '/leave/calendar', label: 'Calendar', shortLabel: 'Calendar', permission: 'leave.calendar.read' },
    { path: '/leave/policies', label: 'Policies', shortLabel: 'Policy', permission: 'leave.policies.read' },
    { path: '/leave/balances', label: 'Balances', shortLabel: 'Balance', permission: 'leave.balances.read' },
  ],
  appModule: {
    name: 'Leave management',
    description: 'Manage Staff Leave',
    slug: 'leave',
    image: '/apps/2.png',
    homePath: '/leave',
  },
  searchProviders: [
    {
      id: 'leave-home',
      matchesPath: (p) => p === '/leave' || p === '/leave/',
      placeholder: 'Search leave history, pending approvals, or balance liability',
      enabled: true,
    },
    {
      id: 'leave-requests',
      matchesPath: (p) => p.startsWith('/leave/requests'),
      placeholder: 'Search by type, status, dates, or notes',
      enabled: true,
    },
    {
      id: 'leave-approvals',
      matchesPath: (p) => p.startsWith('/leave/approvals'),
      placeholder: 'Search by employee, type, dates, or document',
      enabled: true,
    },
    {
      id: 'leave-balances',
      matchesPath: (p) => p.startsWith('/leave/balances'),
      placeholder: 'Search by leave type',
      enabled: true,
    },
    {
      id: 'leave-policies',
      matchesPath: (p) => p.startsWith('/leave/policies'),
      placeholder: 'Search leave types or public holidays',
      enabled: true,
    },
    {
      id: 'leave-calendar',
      matchesPath: (p) => p.startsWith('/leave/calendar'),
      placeholder: 'Search by employee, department, or leave type',
      enabled: true,
    },
    {
      id: 'leave-fallback',
      matchesPath: (p) => p === '/leave' || p.startsWith('/leave/'),
      placeholder: 'Search leave records',
      enabled: true,
    },
  ],
};
