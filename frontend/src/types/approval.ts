export type { ApprovalDecisionRequest, ExpenseClaim } from './expense';

export interface CreateExpenseClaimRequest {
  description: string;
  amount: number;
  currency?: string | null;
  categoryId?: string;
  expenseDate?: string;
  notes?: string | null;
}
