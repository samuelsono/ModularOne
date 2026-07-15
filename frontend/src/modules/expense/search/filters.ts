import type { ExpenseCategory, ExpenseCategoryBalance, ExpenseClaim } from '@modules/expense/types/expense';
import { matchesSearchQuery } from '@platform/search/searchText';

export function filterExpenseClaims(items: ExpenseClaim[], query: string): ExpenseClaim[] {
  const normalized = query.trim();
  if (!normalized) return items;
  return items.filter((item) => matchesSearchQuery(normalized, [
    item.requesterDisplayName, item.categoryName, item.categoryCode, item.description,
    item.notes, item.status, item.expenseDate, item.amount, item.currency,
  ]));
}

export function filterExpenseBalances(items: ExpenseCategoryBalance[], query: string): ExpenseCategoryBalance[] {
  const normalized = query.trim();
  if (!normalized) return items;
  return items.filter((item) => matchesSearchQuery(normalized, [
    item.categoryName, item.categoryCode, item.pendingAmount, item.pendingPaymentAmount,
    item.paidAmount, item.draftAmount, item.rejectedAmount, item.cancelledAmount, item.totalSubmitted,
  ]));
}

export function filterExpenseCategories(items: ExpenseCategory[], query: string): ExpenseCategory[] {
  const normalized = query.trim();
  if (!normalized) return items;
  return items.filter((item) => matchesSearchQuery(normalized, [
    item.name, item.code, item.description, item.isActive ? 'active' : 'inactive',
  ]));
}
