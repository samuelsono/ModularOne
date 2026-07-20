import type { CreateExpenseClaimRequest, ExpenseClaim } from '@modules/expense/types/approval';
import { createExpenseClaim as createClaim, decideExpenseApproval, getPendingExpenseApprovals } from '@modules/expense/services/expenseService';

export { decideExpenseApproval, getPendingExpenseApprovals };

export function createExpenseClaim(request: CreateExpenseClaimRequest): Promise<ExpenseClaim> {
  return createClaim({
    categoryId: request.categoryId ?? '',
    expenseDate: request.expenseDate ?? '',
    description: request.description,
    notes: request.notes ?? null,
    amount: request.amount,
    currency: request.currency ?? 'ZAR',
    kilometersTravelled: request.kilometersTravelled ?? null,
    travelStartPoint: request.travelStartPoint ?? null,
    travelDestination: request.travelDestination ?? null,
    travelWaypoints: request.travelWaypoints ?? null,
  });
}
