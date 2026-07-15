import type { ModuleDefinition } from '@platform/module/types';
import ExpenseLayout from './pages/ExpenseLayout';
import ExpenseClaimsPage from './pages/ExpenseClaimsPage';
import ExpenseApprovalsPage from './pages/ExpenseApprovalsPage';
import ExpenseCategoriesPage from './pages/ExpenseCategoriesPage';
import ExpenseReportsPage from './pages/ExpenseReportsPage';
import ExpenseBalancesPage from './pages/ExpenseBalancesPage';

export const expenseModule: ModuleDefinition = {
  id: 'expense',
  routes: [
    {
      path: 'expense',
      element: <ExpenseLayout />,
      children: [
        { index: true, element: <ExpenseClaimsPage /> },
        { path: 'approvals', element: <ExpenseApprovalsPage /> },
        { path: 'categories', element: <ExpenseCategoriesPage /> },
        { path: 'reports', element: <ExpenseReportsPage /> },
        { path: 'balances', element: <ExpenseBalancesPage /> },
      ],
    },
  ],
  navItems: [
    { path: '/expense/reports', label: 'Home', shortLabel: 'Home', permission: 'expense.reports.read' },
    { path: '/expense', label: 'Claims', shortLabel: 'Claims', permission: 'expense.claims.read' },
    { path: '/expense/approvals', label: 'Approvals', shortLabel: 'Approve', permission: 'expense.approvals.read' },
    { path: '/expense/categories', label: 'Categories', shortLabel: 'Category', permission: 'expense.categories.read' },
    { path: '/expense/balances', label: 'Balances', shortLabel: 'Balance', permission: 'expense.claims.read' },
  ],
  appModule: {
    name: 'Expense claims',
    description: 'Submit and manage expense claims',
    slug: 'expense',
    image: '/apps/3.png',
    homePath: '/expense',
  },
  searchProviders: [
    {
      id: 'expense-claims',
      matchesPath: (p) => p === '/expense' || p === '/expense/',
      placeholder: 'Search by category, amount, status, or description',
      enabled: true,
    },
    {
      id: 'expense-approvals',
      matchesPath: (p) => p.startsWith('/expense/approvals'),
      placeholder: 'Search by employee, category, amount, or description',
      enabled: true,
    },
    {
      id: 'expense-categories',
      matchesPath: (p) => p.startsWith('/expense/categories'),
      placeholder: 'Search by category name or code',
      enabled: true,
    },
    {
      id: 'expense-balances',
      matchesPath: (p) => p.startsWith('/expense/balances'),
      placeholder: 'Search by category or amount',
      enabled: true,
    },
    {
      id: 'expense-reports',
      matchesPath: (p) => p.startsWith('/expense/reports'),
      placeholder: 'Search expense history by category, status, or description',
      enabled: true,
    },
    {
      id: 'expense-fallback',
      matchesPath: (p) => p === '/expense' || p.startsWith('/expense/'),
      placeholder: 'Search expense claims',
      enabled: true,
    },
  ],
};
