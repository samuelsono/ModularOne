import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Badge,
  Card,
  Field,
  MessageBar,
  MessageBarBody,
  Select,
  Spinner,
  createTableColumn,
  type TableColumnDefinition,
} from '@fluentui/react-components';
import AppTitle from '../../components/common/AppTitle';
import { AutoFitDataGrid } from '../../components/common/AutoFitDataGrid';
import { ExpenseCategoryName, ExpenseStatusBadge } from '../../components/expense/expenseBadges';
import { withAuditableColumns } from '../../components/common/auditTableColumns';
import { usePageSearchQuery } from '../../context/PageSearchContext';
import { ApiError } from '../../services/apiClient';
import { getExpenseHistory, getExpenseReportSummary } from '../../services/expenseService';
import type { ExpenseClaim, ExpenseReportAmount, ExpenseReportSummary } from '../../types/expense';
import { EXPENSE_STATUS_FILTERS, expenseStatusLabel } from '../../types/expense';
import { matchesSearchQuery } from '../../utils/searchText';
import SummaryCard from '../../components/reportCards/SummaryCard';
import AppPagination from '../../components/common/AppPagination';

const zarFormatter = new Intl.NumberFormat(undefined, {
  style: 'currency',
  currency: 'ZAR',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

function formatCurrency(amount: number): string {
  return zarFormatter.format(amount ?? 0);
}

function formatExpenseDate(value: string): string {
  return new Date(value).toLocaleDateString();
}

function filterExpenseHistory(items: ExpenseClaim[], query: string): ExpenseClaim[] {
  const normalized = query.trim();
  if (!normalized) {
    return items;
  }

  return items.filter((item) => matchesSearchQuery(normalized, [
    item.requesterDisplayName,
    item.managerDisplayName,
    item.categoryName,
    item.description,
    item.notes,
    item.status,
    item.createdByDisplayName,
    item.updatedByDisplayName,
    item.amount,
    item.currency,
    item.expenseDate,
  ]));
}

export default function ExpenseReportsPage() {
  const searchQuery = usePageSearchQuery();
  const currentYear = new Date().getFullYear();
  const [year, setYear] = useState(currentYear);
  const [statusFilter, setStatusFilter] = useState('All');
  const [summary, setSummary] = useState<ExpenseReportSummary | null>(null);
  const [history, setHistory] = useState<ExpenseClaim[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const [summaryResult, historyResult] = await Promise.all([
        getExpenseReportSummary(year),
        getExpenseHistory(statusFilter, year),
      ]);
      setSummary(summaryResult);
      setHistory(historyResult);
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load expense dashboard.');
      setSummary(null);
      setHistory([]);
    } finally {
      setIsLoading(false);
    }
  }, [statusFilter, year]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  useEffect(() => {
    setPage(1);
  }, [statusFilter, year, searchQuery]);

  const filteredHistory = useMemo(
    () => filterExpenseHistory(history, searchQuery),
    [history, searchQuery],
  );

  const totalHistoryItems = filteredHistory.length;
  const totalHistoryPages = Math.max(1, Math.ceil(totalHistoryItems / pageSize));
  const currentHistoryPage = Math.min(page, totalHistoryPages);

  const paginatedHistory = useMemo(() => {
    const startIndex = (currentHistoryPage - 1) * pageSize;
    return filteredHistory.slice(startIndex, startIndex + pageSize);
  }, [currentHistoryPage, filteredHistory, pageSize]);

  const historyRangeStart = totalHistoryItems === 0 ? 0 : (currentHistoryPage - 1) * pageSize + 1;
  const historyRangeEnd = totalHistoryItems === 0
    ? 0
    : Math.min(currentHistoryPage * pageSize, totalHistoryItems);

  const categoryColumns = useMemo<TableColumnDefinition<ExpenseReportAmount>[]>(() => [
    createTableColumn<ExpenseReportAmount>({
      columnId: 'category',
      renderHeaderCell: () => 'Category',
      renderCell: (item) => <ExpenseCategoryName name={item.label} />,
    }),
    createTableColumn<ExpenseReportAmount>({
      columnId: 'count',
      renderHeaderCell: () => 'Claims',
      renderCell: (item) => item.count,
    }),
    createTableColumn<ExpenseReportAmount>({
      columnId: 'amount',
      renderHeaderCell: () => 'Amount',
      renderCell: (item) => formatCurrency(item.amount),
    }),
  ], []);

  const historyColumns = useMemo<TableColumnDefinition<ExpenseClaim>[]>(() => withAuditableColumns([
    createTableColumn<ExpenseClaim>({
      columnId: 'requester',
      renderHeaderCell: () => 'Requester',
      renderCell: (item) => item.requesterDisplayName,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'expenseDate',
      renderHeaderCell: () => 'Date',
      renderCell: (item) => formatExpenseDate(item.expenseDate),
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'categoryName',
      renderHeaderCell: () => 'Category',
      renderCell: (item) => <ExpenseCategoryName name={item.categoryName} code={item.categoryCode} />,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'description',
      renderHeaderCell: () => 'Description',
      renderCell: (item) => item.description,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'amount',
      renderHeaderCell: () => 'Amount',
      renderCell: (item) => `${item.currency} ${item.amount.toFixed(2)}`,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'status',
      renderHeaderCell: () => 'Status',
      renderCell: (item) => <ExpenseStatusBadge status={item.status} />,
    }),
  ]), []);

  return (
    <div className="flex flex-col gap-4 h-full min-h-0">
      <div className="flex items-start justify-between gap-4 px-3">
        <AppTitle title="Expense dashboard" subtitle="Track submitted, pending, and approved spend across your main business claim categories." />
        <div className="flex gap-5">
          <Field label="Status" orientation="horizontal">
            <Select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
              {EXPENSE_STATUS_FILTERS.map((status) => (
                <option key={status} value={status}>
                  {status === 'All' ? 'All' : expenseStatusLabel(status)}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Year" orientation="horizontal">
            <Select value={String(year)} onChange={(event) => setYear(Number(event.target.value))}>
              {[currentYear, currentYear - 1, currentYear - 2].map((value) => (
                <option key={value} value={String(value)}>{value}</option>
              ))}
            </Select>
          </Field>
        </div>
      </div>

      {error ? (
        <MessageBar intent="error" className="mx-3">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}

      {isLoading ? (
        <div className="flex-1 flex items-center justify-center">
          <Spinner label="Loading expense dashboard..." />
        </div>
      ) : summary ? (
        <div className="flex-1 min-h-0 overflow-auto flex flex-col gap-4">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5 container max-w-8xl mx-auto w-full py-5">
            <SummaryCard label="Awaiting approval" description='Total No. awaiting approval' value={summary.pendingCount} />
            <SummaryCard label="Approval amount" description='Amount awaiting approval' value={formatCurrency(summary.pendingAmount)} />
            <SummaryCard label="Pending payment" description='Amount pending payment' value={formatCurrency(summary.pendingPaymentAmount)} />
            <SummaryCard label="Paid YTD" description='Year to Date' value={formatCurrency(summary.paidYtdAmount)} />
            <SummaryCard label="Submitted YTD" description='Year to Date Submitted' value={formatCurrency(summary.totalSubmittedYtd)} />
          </div>

          <div className="grid gap-4 xl:grid-cols-7 container max-w-8xl mx-auto">
            <Card className='px-0! col-span-5'> 
              <div className='px-4'>
                 <AppTitle title="Spend by category" subtitle="Approved, pending, rejected, and cancelled submissions grouped by claim category." />
              </div>

              <div className="p-0">
                <AutoFitDataGrid
                  items={summary.byCategory}
                  columns={categoryColumns}
                  getRowId={(item) => item.label}
                  size="small"
                />
              </div>
            </Card>

            <Card className='col-span-2'>
              <div className='px-4'>
                 <AppTitle title="Status snapshot" subtitle="Current count of claim outcomes in the selected reporting period." />
              </div>

              <div className="p-4 flex flex-col gap-3">
                {summary.byStatus.map((item) => (
                  <div key={item.label} className="flex items-center justify-between border-b border-neutral-stroke-2 pb-2 last:border-b-0">
                    <ExpenseStatusBadge status={item.label} />
                    <Badge>{item.count}</Badge>
                  </div>
                ))}
              </div>
            </Card>
          </div>

            <div className='px-4 mt-6'>
                 <AppTitle title="Claim history" subtitle="Your expense claims and those of people you manage, including requester and audit trail." />
            </div>
            <div className="">
              <AutoFitDataGrid
                items={paginatedHistory}
                columns={historyColumns}
                getRowId={(item) => item.id}
                size="small"
              />
              <AppPagination
                className="py-4"
                page={currentHistoryPage}
                totalPages={totalHistoryPages}
                totalItems={totalHistoryItems}
                rangeStart={historyRangeStart}
                rangeEnd={historyRangeEnd}
                pageSize={pageSize}
                onPageChange={setPage}
                onPageSizeChange={(size) => {
                  setPageSize(size);
                  setPage(1);
                }}
              />
            </div>
        </div>
      ) : null}
    </div>
  );
}
