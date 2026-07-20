import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  MessageBar,
  MessageBarBody,
  Select,
  Spinner,
  Tab,
  TabList,
  Textarea,
  createTableColumn,
  type TableColumnDefinition,
} from '@fluentui/react-components';
import { CalendarCheckmarkRegular } from '@fluentui/react-icons';
import AppTitle from '@platform/ui/AppTitle';
import EmptyListOrTable from '@platform/ui/EmptyListOrTable';
import { AutoFitDataGrid } from '@platform/ui/AutoFitDataGrid';
import { ExpenseCategoryName, ExpenseStatusBadge } from '@modules/expense/components/expenseBadges';
import { withAuditableColumns } from '@platform/ui/auditTableColumns';
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import { ApiError } from '@platform/api/apiClient';
import {
  decideExpenseApproval,
  getPendingExpenseApprovals,
  getPendingExpensePayments,
  markExpenseClaimPaid,
} from '@modules/expense/services/expenseService';
import type { ExpenseClaim } from '@modules/expense/types/expense';
import { expenseStatusLabel } from '@modules/expense/types/expense';
import { matchesSearchQuery } from '@platform/search/searchText';

type ApprovalTab = 'approvals' | 'payments';

function filterClaims(claims: ExpenseClaim[], query: string): ExpenseClaim[] {
  const normalized = query.trim();
  if (!normalized) {
    return claims;
  }

  return claims.filter((claim) => matchesSearchQuery(normalized, [
    claim.requesterDisplayName,
    claim.categoryName,
    claim.description,
    claim.notes,
    claim.status,
    expenseStatusLabel(claim.status),
    claim.expenseDate,
    claim.amount,
  ]));
}

export default function ExpenseApprovalsPage() {
  const searchQuery = usePageSearchQuery();
  const [activeTab, setActiveTab] = useState<ApprovalTab>('approvals');
  const [stageFilter, setStageFilter] = useState('All');
  const [approvalClaims, setApprovalClaims] = useState<ExpenseClaim[]>([]);
  const [paymentClaims, setPaymentClaims] = useState<ExpenseClaim[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actingId, setActingId] = useState<string | null>(null);
  const [pendingRejectClaimId, setPendingRejectClaimId] = useState<string | null>(null);
  const [rejectNotes, setRejectNotes] = useState('');
  const [rejectValidationError, setRejectValidationError] = useState<string | null>(null);

  const loadClaims = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const [approvals, payments] = await Promise.all([
        getPendingExpenseApprovals(),
        getPendingExpensePayments(),
      ]);
      setApprovalClaims(approvals);
      setPaymentClaims(payments);
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load expense approvals.');
      setApprovalClaims([]);
      setPaymentClaims([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadClaims();
  }, [loadClaims]);

  const visibleApprovalClaims = useMemo(() => {
    const filtered = filterClaims(approvalClaims, searchQuery);
    if (stageFilter === 'All') {
      return filtered;
    }

    return filtered.filter((claim) => claim.status === stageFilter);
  }, [approvalClaims, searchQuery, stageFilter]);

  const visiblePaymentClaims = useMemo(
    () => filterClaims(paymentClaims, searchQuery),
    [paymentClaims, searchQuery],
  );

  const handleConfirmReject = useCallback(async () => {
    if (!pendingRejectClaimId) {
      return;
    }

    const trimmedNotes = rejectNotes.trim();
    if (!trimmedNotes) {
      setRejectValidationError('A reason is required to reject an expense claim.');
      return;
    }

    setRejectValidationError(null);
    setActingId(pendingRejectClaimId);
    try {
      await decideExpenseApproval(pendingRejectClaimId, { approve: false, notes: trimmedNotes });
      await loadClaims();
      setPendingRejectClaimId(null);
      setRejectNotes('');
    } catch (actionError) {
      setError(actionError instanceof ApiError ? actionError.message : 'Failed to reject expense claim.');
    } finally {
      setActingId(null);
    }
  }, [loadClaims, pendingRejectClaimId, rejectNotes]);

  const approvalColumns = useMemo<TableColumnDefinition<ExpenseClaim>[]>(() => withAuditableColumns([
    createTableColumn<ExpenseClaim>({
      columnId: 'requester',
      renderHeaderCell: () => 'Employee',
      renderCell: (item) => item.requesterDisplayName,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'category',
      renderHeaderCell: () => 'Category',
      renderCell: (item) => <ExpenseCategoryName name={item.categoryName} code={item.categoryCode} />,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'expenseDate',
      renderHeaderCell: () => 'Expense date',
      renderCell: (item) => new Date(item.expenseDate).toLocaleDateString(),
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'amount',
      renderHeaderCell: () => 'Amount',
      renderCell: (item) => `${item.currency} ${item.amount.toFixed(2)}`,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'description',
      renderHeaderCell: () => 'Description',
      renderCell: (item) => item.description,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'status',
      renderHeaderCell: () => 'Stage',
      renderCell: (item) => <ExpenseStatusBadge status={item.status} />,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'actions',
      renderHeaderCell: () => 'Actions',
      renderCell: (item) => (
        <div className="flex gap-2">
          <Button
            appearance="primary"
            size="small"
            disabled={actingId === item.id}
            onClick={async () => {
              setActingId(item.id);
              try {
                await decideExpenseApproval(item.id, { approve: true });
                await loadClaims();
              } catch (actionError) {
                setError(actionError instanceof ApiError ? actionError.message : 'Failed to approve expense claim.');
              } finally {
                setActingId(null);
              }
            }}
          >
            Approve
          </Button>
          <Button
            appearance="secondary"
            size="small"
            disabled={actingId === item.id}
            onClick={() => {
              setPendingRejectClaimId(item.id);
              setRejectNotes('');
              setRejectValidationError(null);
            }}
          >
            Reject
          </Button>
        </div>
      ),
    }),
  ]), [actingId, loadClaims]);

  const paymentColumns = useMemo<TableColumnDefinition<ExpenseClaim>[]>(() => withAuditableColumns([
    createTableColumn<ExpenseClaim>({
      columnId: 'requester',
      renderHeaderCell: () => 'Employee',
      renderCell: (item) => item.requesterDisplayName,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'category',
      renderHeaderCell: () => 'Category',
      renderCell: (item) => <ExpenseCategoryName name={item.categoryName} code={item.categoryCode} />,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'expenseDate',
      renderHeaderCell: () => 'Expense date',
      renderCell: (item) => new Date(item.expenseDate).toLocaleDateString(),
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'amount',
      renderHeaderCell: () => 'Amount',
      renderCell: (item) => `${item.currency} ${item.amount.toFixed(2)}`,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'description',
      renderHeaderCell: () => 'Description',
      renderCell: (item) => item.description,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'status',
      renderHeaderCell: () => 'Payment',
      renderCell: (item) => <ExpenseStatusBadge status={item.status} />,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'actions',
      renderHeaderCell: () => 'Actions',
      renderCell: (item) => (
        <Button
          appearance="primary"
          size="small"
          disabled={actingId === item.id}
          onClick={async () => {
            setActingId(item.id);
            try {
              await markExpenseClaimPaid(item.id);
              await loadClaims();
            } catch (actionError) {
              setError(actionError instanceof ApiError ? actionError.message : 'Failed to mark expense claim as paid.');
            } finally {
              setActingId(null);
            }
          }}
        >
          Mark paid
        </Button>
      ),
    }),
  ]), [actingId, loadClaims]);

  const activeItems = activeTab === 'approvals' ? visibleApprovalClaims : visiblePaymentClaims;
  const activeColumns = activeTab === 'approvals' ? approvalColumns : paymentColumns;

  return (
    <div className="flex flex-col gap-4 h-full min-h-0">
      <div className="flex items-start justify-between gap-4 px-3">
        <AppTitle
          title="Expense approvals"
          subtitle="Manager approval first, then finance approval, then mark approved claims as paid."
        />
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3 px-3">
        <TabList
          selectedValue={activeTab}
          onTabSelect={(_, data) => setActiveTab(data.value as ApprovalTab)}
        >
          <Tab value="approvals">Approval queue</Tab>
          <Tab value="payments">Payment queue</Tab>
        </TabList>

        {activeTab === 'approvals' ? (
          <Select value={stageFilter} onChange={(event) => setStageFilter(event.target.value)}>
            <option value="All">All stages</option>
            <option value="PendingManager">Pending manager</option>
            <option value="PendingFinance">Pending finance</option>
          </Select>
        ) : null}
      </div>

      {error ? (
        <MessageBar intent="error" className="mx-3">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}

      <div className="flex-1 min-h-0 overflow-auto">
        {isLoading ? (
          <Spinner label="Loading approvals..." />
        ) : activeItems.length === 0 ? (
          <EmptyListOrTable
            isLoading={false}
            isEmpty
            canWrite={false}
            icon={CalendarCheckmarkRegular}
            message={activeTab === 'approvals' ? 'No pending expense approvals.' : 'No claims awaiting payment.'}
          >
            <></>
          </EmptyListOrTable>
        ) : (
          <AutoFitDataGrid
            items={activeItems}
            columns={activeColumns}
            getRowId={(item) => item.id}
            size="small"
          />
        )}
      </div>

      <Dialog
        open={pendingRejectClaimId !== null}
        onOpenChange={(_, data) => {
          if (!data.open && !actingId) {
            setPendingRejectClaimId(null);
            setRejectNotes('');
            setRejectValidationError(null);
          }
        }}
      >
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Reject expense claim</DialogTitle>
            <DialogContent className="flex flex-col gap-3">
              <p>This will reject the selected expense claim. Please provide a reason.</p>
              <Field
                label="Rejection reason"
                required
                validationState={rejectValidationError ? 'error' : 'none'}
                validationMessage={rejectValidationError ?? undefined}
              >
                <Textarea
                  value={rejectNotes}
                  rows={3}
                  resize="vertical"
                  disabled={Boolean(actingId)}
                  placeholder="Enter the reason for rejecting this claim"
                  onChange={(_, data) => {
                    setRejectNotes(data.value);
                    if (rejectValidationError) {
                      setRejectValidationError(null);
                    }
                  }}
                />
              </Field>
            </DialogContent>
            <DialogActions>
              <Button
                appearance="primary"
                className="!bg-red-600 hover:!bg-red-700 !text-white !border-red-600"
                disabled={Boolean(actingId)}
                onClick={() => void handleConfirmReject()}
              >
                {actingId ? 'Working...' : 'Reject'}
              </Button>
              <Button
                appearance="secondary"
                disabled={Boolean(actingId)}
                onClick={() => {
                  setPendingRejectClaimId(null);
                  setRejectNotes('');
                  setRejectValidationError(null);
                }}
              >
                Cancel
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
}
