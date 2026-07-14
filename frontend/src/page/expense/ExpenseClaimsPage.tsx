import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Button,
  Field,
  MessageBar,
  MessageBarBody,
  Select,
  Spinner,
} from '@fluentui/react-components';
import { AddRegular, ReceiptRegular } from '@fluentui/react-icons';
import { createTableColumn, type TableColumnDefinition } from '@fluentui/react-components';
import AppTitle from '../../components/common/AppTitle';
import EmptyListOrTable from '../../components/common/EmptyLIstOrTable';
import { AutoFitDataGrid } from '../../components/common/AutoFitDataGrid';
import { ExpenseCategoryName, ExpenseStatusBadge } from '../../components/expense/expenseBadges';
import { ExpenseClaimForm } from '../../components/expense/ExpenseClaimForm';
import { withAuditableColumns } from '../../components/common/auditTableColumns';
import { usePageSearchQuery } from '../../context/PageSearchContext';
import { usePermissions } from '../../hooks/usePermissions';
import { ApiError } from '../../services/apiClient';
import {
  cancelExpenseClaim,
  getMyExpenseClaims,
  submitExpenseClaim,
} from '../../services/expenseService';
import type { ExpenseClaim } from '../../types/expense';
import {
  EXPENSE_STATUS_FILTERS,
  expenseStatusLabel,
  isCancellableExpenseClaim,
  isEditableExpenseClaim,
} from '../../types/expense';
import { matchesSearchQuery } from '../../utils/searchText';

const currencyFormatter = new Intl.NumberFormat(undefined, {
  style: 'currency',
  currency: 'ZAR',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

function formatCurrency(amount: number, currency: string): string {
  if (currency.toUpperCase() === 'ZAR') {
    return currencyFormatter.format(amount);
  }

  return `${currency} ${amount.toFixed(2)}`;
}

function formatDate(date: string): string {
  return new Date(date).toLocaleDateString();
}

function filterClaims(claims: ExpenseClaim[], query: string): ExpenseClaim[] {
  const normalized = query.trim();
  if (!normalized) {
    return claims;
  }

  return claims.filter((claim) => matchesSearchQuery(normalized, [
    claim.categoryName,
    claim.categoryCode,
    claim.description,
    claim.notes,
    claim.status,
    expenseStatusLabel(claim.status),
    claim.expenseDate,
    claim.amount,
    claim.currency,
  ]));
}

export default function ExpenseClaimsPage() {
  const searchQuery = usePageSearchQuery();
  const { hasPermission } = usePermissions();
  const canWrite = hasPermission('expense.claims.write');
  const currentYear = new Date().getFullYear();

  const [claims, setClaims] = useState<ExpenseClaim[]>([]);
  const [statusFilter, setStatusFilter] = useState('All');
  const [year, setYear] = useState(currentYear);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [editingClaim, setEditingClaim] = useState<ExpenseClaim | null>(null);
  const [actingId, setActingId] = useState<string | null>(null);

  const loadClaims = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const items = await getMyExpenseClaims(statusFilter, year);
      setClaims(items);
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load expense claims.');
      setClaims([]);
    } finally {
      setIsLoading(false);
    }
  }, [statusFilter, year]);

  useEffect(() => {
    void loadClaims();
  }, [loadClaims]);

  const filteredClaims = useMemo(
    () => filterClaims(claims, searchQuery),
    [claims, searchQuery],
  );

  const columns = useMemo<TableColumnDefinition<ExpenseClaim>[]>(() => withAuditableColumns([
    createTableColumn<ExpenseClaim>({
      columnId: 'category',
      renderHeaderCell: () => 'Category',
      renderCell: (item) => <ExpenseCategoryName name={item.categoryName} code={item.categoryCode} />,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'expenseDate',
      renderHeaderCell: () => 'Expense date',
      renderCell: (item) => formatDate(item.expenseDate),
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'description',
      renderHeaderCell: () => 'Description',
      renderCell: (item) => item.description,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'amount',
      renderHeaderCell: () => 'Amount',
      renderCell: (item) => formatCurrency(item.amount, item.currency),
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'status',
      renderHeaderCell: () => 'Status',
      renderCell: (item) => <ExpenseStatusBadge status={item.status} />,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'actions',
      renderHeaderCell: () => 'Actions',
      renderCell: (item) => (
        canWrite ? (
          <div className="flex flex-wrap gap-2">
            {isEditableExpenseClaim(item.status) ? (
              <Button
                appearance="secondary"
                size="small"
                onClick={() => {
                  setEditingClaim(item);
                  setFormOpen(true);
                }}
              >
                Edit
              </Button>
            ) : null}
            {isEditableExpenseClaim(item.status) ? (
              <Button
                appearance="primary"
                size="small"
                disabled={actingId === item.id}
                onClick={async () => {
                  setActingId(item.id);
                  setActionError(null);
                  try {
                    await submitExpenseClaim(item.id);
                    await loadClaims();
                  } catch (actionLoadError) {
                    setActionError(actionLoadError instanceof ApiError ? actionLoadError.message : 'Failed to submit expense claim.');
                  } finally {
                    setActingId(null);
                  }
                }}
              >
                Submit
              </Button>
            ) : null}
            {isCancellableExpenseClaim(item.status) ? (
              <Button
                appearance="secondary"
                size="small"
                disabled={actingId === item.id}
                onClick={async () => {
                  setActingId(item.id);
                  setActionError(null);
                  try {
                    await cancelExpenseClaim(item.id);
                    await loadClaims();
                  } catch (actionLoadError) {
                    setActionError(actionLoadError instanceof ApiError ? actionLoadError.message : 'Failed to cancel expense claim.');
                  } finally {
                    setActingId(null);
                  }
                }}
              >
                Cancel
              </Button>
            ) : null}
          </div>
        ) : null
      ),
    }),
  ]), [actingId, canWrite, loadClaims]);

  return (
    <div className="flex flex-col gap-4 h-full min-h-0">
      <div className="flex items-center justify-between gap-4 px-3">
        <AppTitle title="Expense claims" subtitle="Create drafts, submit for manager and finance approval, then track payment." />
        
         <div className="flex flex-wrap items-end gap-2 px-3">
        <Field label="Status" orientation="vertical">
          <Select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
            {EXPENSE_STATUS_FILTERS.map((status) => (
              <option key={status} value={status}>
                {status === 'All' ? 'All' : expenseStatusLabel(status)}
              </option>
            ))}
          </Select>
        </Field>
        <Field label="Year" orientation="vertical">
          <Select value={String(year)} onChange={(event) => setYear(Number(event.target.value))}>
            {[currentYear, currentYear - 1, currentYear - 2].map((value) => (
              <option key={value} value={String(value)}>{value}</option>
            ))}
          </Select>
        </Field>

        {canWrite ? (
          <Button appearance="primary" icon={<AddRegular />} onClick={() => {
            setEditingClaim(null);
            setFormOpen(true);
          }}>
            New claim
          </Button>
        ) : null}
      </div>
      </div>

     

      {error ? (
        <MessageBar intent="error" className="mx-3">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}

      {actionError ? (
        <MessageBar intent="error" className="mx-3">
          <MessageBarBody>{actionError}</MessageBarBody>
        </MessageBar>
      ) : null}

      <div className="flex-1 min-h-0 overflow-auto">
        {isLoading ? (
          <Spinner label="Loading expense claims..." />
        ) : filteredClaims.length === 0 ? (
          <EmptyListOrTable
            isLoading={false}
            isEmpty
            canWrite={canWrite}
            icon={ReceiptRegular}
            message="No expense claims found."
          >
            <Button appearance="primary" icon={<AddRegular />} onClick={() => {
              setEditingClaim(null);
              setFormOpen(true);
            }}>
              Create first claim
            </Button>
          </EmptyListOrTable>
        ) : (
          <AutoFitDataGrid
            items={filteredClaims}
            columns={columns}
            getRowId={(item) => item.id}
            size="small"
          />
        )}
      </div>

      <ExpenseClaimForm
        open={formOpen}
        claim={editingClaim}
        onOpenChange={(open) => {
          setFormOpen(open);
          if (!open) {
            setEditingClaim(null);
          }
        }}
        onSubmitted={() => void loadClaims()}
      />
    </div>
  );
}
