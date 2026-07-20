export type { ApprovalDecisionRequest, ExpenseClaim } from '@modules/expense/types/expense';

export interface CreateExpenseClaimRequest {
  description: string;
  amount: number;
  currency?: string | null;
  categoryId?: string;
  expenseDate?: string;
  notes?: string | null;
  kilometersTravelled?: number | null;
  travelStartPoint?: string | null;
  travelDestination?: string | null;
  travelWaypoints?: string[] | null;
}
