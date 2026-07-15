import type {
  ApprovalDecisionRequest,
  CreateExpenseClaimRequest,
  ExpenseCategory,
  ExpenseCategoryBalance,
  ExpenseClaim,
  ExpenseReportSummary,
  SaveExpenseCategoryRequest,
  UpdateExpenseClaimRequest,
} from '@modules/expense/types/expense';
import { authorizedFetch } from '@platform/api/authService';

export function getExpenseCategories(): Promise<ExpenseCategory[]> {
  return authorizedFetch<ExpenseCategory[]>('/api/expense/categories');
}

export function getAdminExpenseCategories(): Promise<ExpenseCategory[]> {
  return authorizedFetch<ExpenseCategory[]>('/api/expense/admin/categories');
}

export function createExpenseCategory(request: SaveExpenseCategoryRequest): Promise<ExpenseCategory> {
  return authorizedFetch<ExpenseCategory>('/api/expense/admin/categories', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function updateExpenseCategory(id: string, request: SaveExpenseCategoryRequest): Promise<ExpenseCategory> {
  return authorizedFetch<ExpenseCategory>(`/api/expense/admin/categories/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export function createExpenseClaim(request: CreateExpenseClaimRequest): Promise<ExpenseClaim> {
  return authorizedFetch<ExpenseClaim>('/api/expense/claims', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function updateExpenseClaim(id: string, request: UpdateExpenseClaimRequest): Promise<ExpenseClaim> {
  return authorizedFetch<ExpenseClaim>(`/api/expense/claims/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export function submitExpenseClaim(id: string): Promise<ExpenseClaim> {
  return authorizedFetch<ExpenseClaim>(`/api/expense/claims/${encodeURIComponent(id)}/submit`, {
    method: 'POST',
  });
}

export function getMyExpenseClaims(status?: string, year?: number): Promise<ExpenseClaim[]> {
  const params = new URLSearchParams();
  if (status && status !== 'All') {
    params.set('status', status);
  }
  if (year) {
    params.set('year', String(year));
  }

  const query = params.size > 0 ? `?${params.toString()}` : '';
  return authorizedFetch<ExpenseClaim[]>(`/api/expense/claims${query}`);
}

export function cancelExpenseClaim(id: string): Promise<ExpenseClaim> {
  return authorizedFetch<ExpenseClaim>(`/api/expense/claims/${encodeURIComponent(id)}/cancel`, {
    method: 'POST',
  });
}

export function getPendingExpenseApprovals(): Promise<ExpenseClaim[]> {
  return authorizedFetch<ExpenseClaim[]>('/api/expense/approvals/pending');
}

export function getPendingExpensePayments(): Promise<ExpenseClaim[]> {
  return authorizedFetch<ExpenseClaim[]>('/api/expense/approvals/payment-pending');
}

export function decideExpenseApproval(id: string, request: ApprovalDecisionRequest): Promise<ExpenseClaim> {
  return authorizedFetch<ExpenseClaim>(`/api/expense/approvals/${encodeURIComponent(id)}/decide`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function markExpenseClaimPaid(id: string): Promise<ExpenseClaim> {
  return authorizedFetch<ExpenseClaim>(`/api/expense/approvals/${encodeURIComponent(id)}/mark-paid`, {
    method: 'POST',
  });
}

export function getExpenseReportSummary(year?: number): Promise<ExpenseReportSummary> {
  const query = year ? `?year=${encodeURIComponent(String(year))}` : '';
  return authorizedFetch<ExpenseReportSummary>(`/api/expense/reports/summary${query}`);
}

export function getExpenseBalances(year?: number): Promise<ExpenseCategoryBalance[]> {
  const query = year ? `?year=${encodeURIComponent(String(year))}` : '';
  return authorizedFetch<ExpenseCategoryBalance[]>(`/api/expense/reports/balances${query}`);
}

export function getExpenseHistory(status?: string, year?: number): Promise<ExpenseClaim[]> {
  const params = new URLSearchParams();
  if (status && status !== 'All') {
    params.set('status', status);
  }
  if (year) {
    params.set('year', String(year));
  }

  const query = params.size > 0 ? `?${params.toString()}` : '';
  return authorizedFetch<ExpenseClaim[]>(`/api/expense/reports/history${query}`);
}
