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
  Textarea,
  ToggleButton,
  Tooltip,
} from '@fluentui/react-components';
import { AddRegular, PeopleRegular, PersonRegular, ReceiptRegular } from '@fluentui/react-icons';
import { createTableColumn, type TableColumnDefinition } from '@fluentui/react-components';
import AppTitle from '@platform/ui/AppTitle';
import EmptyListOrTable from '@platform/ui/EmptyListOrTable';
import { AutoFitDataGrid } from '@platform/ui/AutoFitDataGrid';
import { ExpenseStatusBadge } from '@modules/expense/components/expenseBadges';
import { ExpenseClaimForm } from '../components/ExpenseClaimForm';
import { ExpenseClaimDetailPopover } from '@modules/expense/components/ExpenseClaimDetailPopover';
import { withAuditableColumns } from '@platform/ui/auditTableColumns';
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import { usePermissions } from '@platform/permissions/usePermissions';
import { ApiError } from '@platform/api/apiClient';
import {
  cancelExpenseClaim,
  decideExpenseApproval,
  getMyExpenseClaims,
  submitExpenseClaim,
} from '@modules/expense/services/expenseService';
import type { ExpenseClaim } from '@modules/expense/types/expense';
import {
  EXPENSE_STATUS_FILTERS,
  expenseStatusLabel,
  isApprovalStage,
  isCancellableExpenseClaim,
  isEditableExpenseClaim,
} from '@modules/expense/types/expense';
import { matchesSearchQuery } from '@platform/search/searchText';
import DataReload from '../components/DataReload';

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
    claim.travelStartPoint,
    claim.travelDestination,
    ...(claim.travelWaypoints ?? []),
    claim.receiptFileName,
  ]));
}

export default function ExpenseClaimsPage() {
  const searchQuery = usePageSearchQuery();
  const { user, hasPermission, isAdmin, isHr, isManager } = usePermissions();
  const canWrite = hasPermission('expense.claims.write');
  const canApproveExpense = isAdmin || isHr || isManager;
  const currentUserId = user?.id;
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
  const [pendingRejectClaimId, setPendingRejectClaimId] = useState<string | null>(null);
  const [rejectNotes, setRejectNotes] = useState('');
  const [rejectValidationError, setRejectValidationError] = useState<string | null>(null);
  const [filterCurrentUserOnly, setFilterCurrentUserOnly] = useState(false);
  const isElevatedViewer = isAdmin || isHr || isManager;
  const isCurrentUserOnlyView = isElevatedViewer && filterCurrentUserOnly && Boolean(currentUserId);

   const scopedClaims = useMemo(
      () => (isCurrentUserOnlyView && currentUserId
        ? claims.filter((claim) => claim.requesterUserId === currentUserId)
        : claims),
      [claims, currentUserId, isCurrentUserOnlyView],
  );

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
    () => filterClaims(scopedClaims, searchQuery),
    [scopedClaims, searchQuery],
  );

  const handleClaimAction = useCallback(async (claimId: string, action: 'submit' | 'cancel' | 'approve' | 'reject', notes?: string) => {
    setActingId(claimId);
    setActionError(null);

    try {
      if (action === 'submit') {
        await submitExpenseClaim(claimId);
      } else if (action === 'cancel') {
        await cancelExpenseClaim(claimId);
      } else {
        await decideExpenseApproval(claimId, { approve: action === 'approve', notes: notes ?? null });
      }

      await loadClaims();
    } catch (actionLoadError) {
      if (action === 'submit') {
        setActionError(actionLoadError instanceof ApiError ? actionLoadError.message : 'Failed to submit expense claim.');
      } else if (action === 'cancel') {
        setActionError(actionLoadError instanceof ApiError ? actionLoadError.message : 'Failed to cancel expense claim.');
      } else if (action === 'approve') {
        setActionError(actionLoadError instanceof ApiError ? actionLoadError.message : 'Failed to approve expense claim.');
      } else {
        setActionError(actionLoadError instanceof ApiError ? actionLoadError.message : 'Failed to reject expense claim.');
      }
    } finally {
      setActingId(null);
    }
  }, [loadClaims]);

  const handleOpenRejectDialog = useCallback((claimId: string) => {
    setPendingRejectClaimId(claimId);
    setRejectNotes('');
    setRejectValidationError(null);
  }, []);

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
    await handleClaimAction(pendingRejectClaimId, 'reject', trimmedNotes);
    setPendingRejectClaimId(null);
    setRejectNotes('');
  }, [handleClaimAction, pendingRejectClaimId, rejectNotes]);

  const columns = useMemo<TableColumnDefinition<ExpenseClaim>[]>(() => withAuditableColumns([
    createTableColumn<ExpenseClaim>({
      columnId: 'category',
      renderHeaderCell: () => 'Category',
      renderCell: (item) => <ExpenseClaimDetailPopover item={item} />,
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'requestor',
      renderHeaderCell: () => 'Requestor',
      renderCell: (item) => item.requesterDisplayName,
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
      columnId: 'travel',
      renderHeaderCell: () => 'Travel',
      renderCell: (item) => (
        item.requiresTravelDetails
          ? `${item.travelStartPoint ?? '-'} -> ${item.travelDestination ?? '-'} (${item.kilometersTravelled ?? 0} km)`
          : '—'
      ),
    }),
    createTableColumn<ExpenseClaim>({
      columnId: 'receipt',
      renderHeaderCell: () => 'Receipt',
      renderCell: (item) => (item.hasReceipt ? (item.receiptFileName ?? 'Attached') : 'Missing'),
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
        canWrite || canApproveExpense ? (
          <div className="flex flex-wrap gap-2">
            {item.requesterUserId === currentUserId && isEditableExpenseClaim(item.status) ? (
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
            {item.requesterUserId === currentUserId && isEditableExpenseClaim(item.status) ? (
              <Button
                appearance="primary"
                size="small"
                disabled={actingId === item.id}
                onClick={() => void handleClaimAction(item.id, 'submit')}
              >
                Submit
              </Button>
            ) : null}
            {item.requesterUserId === currentUserId && isCancellableExpenseClaim(item.status) ? (
              <Button
                appearance="secondary"
                size="small"
                disabled={actingId === item.id}
                onClick={() => void handleClaimAction(item.id, 'cancel')}
              >
                Cancel
              </Button>
            ) : null}
            {canApproveExpense
              && item.requesterUserId !== currentUserId
              && isApprovalStage(item.status) ? (
                <>
                  <Button
                    appearance="primary"
                    size="small"
                    disabled={actingId === item.id}
                    onClick={() => void handleClaimAction(item.id, 'approve')}
                  >
                    Approve
                  </Button>
                  <Button
                    appearance="secondary"
                    size="small"
                    disabled={actingId === item.id}
                    onClick={() => handleOpenRejectDialog(item.id)}
                  >
                    Reject
                  </Button>
                </>
              ) : null}
          </div>
        ) : null
      ),
    }),
  ]), [actingId, canApproveExpense, canWrite, currentUserId, handleClaimAction, handleOpenRejectDialog]);

  return (
    <div className="flex flex-col gap-4 h-full min-h-0">
      <div className="flex items-center justify-between gap-4 px-3">
        <AppTitle title="Expense claims" subtitle="Create drafts, submit for manager and finance approval, then track payment." />
        
         <div className="flex flex-wrap items-end gap-2 px-3">
          <DataReload onReload={loadClaims} />
          {isElevatedViewer ? (
            <Tooltip
              content={filterCurrentUserOnly ? 'Show team leave balances' : 'Show only your leave balances'}
              relationship="label"
            >
              <ToggleButton
                appearance="primary"
                checked={filterCurrentUserOnly}
                icon={filterCurrentUserOnly ? <PeopleRegular /> : <PersonRegular /> }
                onClick={() => setFilterCurrentUserOnly((prev) => !prev)}
              />
            </Tooltip>
          ) : null}
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
        onOpenChange={(open: boolean) => {
          setFormOpen(open);
          if (!open) {
            setEditingClaim(null);
          }
        }}
        onSubmitted={() => void loadClaims()}
      />

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
