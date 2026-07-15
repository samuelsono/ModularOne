import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Dropdown,
  Field,
  MessageBar,
  MessageBarBody,
  Option,
  Spinner,
  createTableColumn,
  type TableColumnDefinition,
} from '@fluentui/react-components';
import AppTitle from '@platform/ui/AppTitle';
import { AutoFitDataGrid } from '@platform/ui/AutoFitDataGrid';
import { ExpenseCategoryName } from '@modules/expense/components/expenseBadges';
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import { ApiError } from '@platform/api/apiClient';
import { getExpenseBalances } from '@modules/expense/services/expenseService';
import type { ExpenseCategoryBalance } from '@modules/expense/types/expense';
import { matchesSearchQuery } from '@platform/search/searchText';

const formatter = new Intl.NumberFormat(undefined, {
  style: 'currency',
  currency: 'ZAR',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

function filterBalances(items: ExpenseCategoryBalance[], query: string): ExpenseCategoryBalance[] {
  const normalized = query.trim();
  if (!normalized) {
    return items;
  }

  return items.filter((item) => matchesSearchQuery(normalized, [
    item.categoryName,
    item.categoryCode,
    item.pendingAmount,
    item.pendingPaymentAmount,
    item.paidAmount,
    item.draftAmount,
    item.rejectedAmount,
    item.cancelledAmount,
    item.totalSubmitted,
  ]));
}

export default function ExpenseBalancesPage() {
  const searchQuery = usePageSearchQuery();
  const currentYear = new Date().getFullYear();
  const [year, setYear] = useState(currentYear);
  const [balances, setBalances] = useState<ExpenseCategoryBalance[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadBalances = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const items = await getExpenseBalances(year);
      setBalances(items);
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load expense balances.');
      setBalances([]);
    } finally {
      setIsLoading(false);
    }
  }, [year]);

  useEffect(() => {
    void loadBalances();
  }, [loadBalances]);

  const filteredBalances = useMemo(
    () => filterBalances(balances, searchQuery),
    [balances, searchQuery],
  );

  const columns = useMemo<TableColumnDefinition<ExpenseCategoryBalance>[]>(() => [
    createTableColumn<ExpenseCategoryBalance>({
      columnId: 'categoryName',
      renderHeaderCell: () => 'Category',
      renderCell: (item) => <ExpenseCategoryName name={item.categoryName} code={item.categoryCode} />,
    }),
    createTableColumn<ExpenseCategoryBalance>({
      columnId: 'pendingAmount',
      renderHeaderCell: () => 'In approval',
      renderCell: (item) => formatter.format(item.pendingAmount),
    }),
    createTableColumn<ExpenseCategoryBalance>({
      columnId: 'pendingPaymentAmount',
      renderHeaderCell: () => 'Pending payment',
      renderCell: (item) => formatter.format(item.pendingPaymentAmount),
    }),
    createTableColumn<ExpenseCategoryBalance>({
      columnId: 'paidAmount',
      renderHeaderCell: () => 'Paid',
      renderCell: (item) => formatter.format(item.paidAmount),
    }),
    createTableColumn<ExpenseCategoryBalance>({
      columnId: 'draftAmount',
      renderHeaderCell: () => 'Draft',
      renderCell: (item) => formatter.format(item.draftAmount),
    }),
    createTableColumn<ExpenseCategoryBalance>({
      columnId: 'rejectedAmount',
      renderHeaderCell: () => 'Rejected',
      renderCell: (item) => formatter.format(item.rejectedAmount),
    }),
    createTableColumn<ExpenseCategoryBalance>({
      columnId: 'cancelledAmount',
      renderHeaderCell: () => 'Cancelled',
      renderCell: (item) => formatter.format(item.cancelledAmount),
    }),
    createTableColumn<ExpenseCategoryBalance>({
      columnId: 'totalSubmitted',
      renderHeaderCell: () => 'Submitted total',
      renderCell: (item) => formatter.format(item.totalSubmitted),
    }),
  ], []);

  return (
    <div className="flex flex-col gap-4 h-full min-h-0">
      <div className="flex items-start justify-between gap-4 px-3">
        <AppTitle title="Expense balances" subtitle="See how much you have submitted in each claim category during the selected year." />
        <Field label="Year" orientation="horizontal">
          <Dropdown 
               positioning={"below-end"}
               value={String(year)} 
               onOptionSelect={(_, event) => setYear(Number(event.optionValue))}
               selectedOptions={year ? [String(year)] : []}
               style={{ minWidth: 92, width: 92 }}>
              {[currentYear, currentYear - 1, currentYear - 2].map((value) => (
               <Option key={value} value={String(value)} text={String(value)}>{value}</Option>
            ))}
          </Dropdown>
        </Field>
      </div>

      {error ? (
        <MessageBar intent="error" className="mx-3">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}

      <div className="flex-1 min-h-0 overflow-auto">
        {isLoading ? (
          <Spinner label="Loading expense balances..." />
        ) : (
          <AutoFitDataGrid
                items={filteredBalances}
                columns={columns}
                getRowId={(item) => item.categoryId}
                size="small"
              />
        )}
      </div>
    </div>
  );
}
