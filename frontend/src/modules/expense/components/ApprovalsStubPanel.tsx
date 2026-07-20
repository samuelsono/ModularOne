import { useCallback, useEffect, useState } from 'react';
import {
  Button,
  Field,
  Input,
  Spinner,
  Subtitle2,
  Text,
} from '@fluentui/react-components';
import { ApiError } from '@platform/api/apiClient';
import {
  createExpenseClaim,
  decideExpenseApproval,
  getPendingExpenseApprovals,
} from '@modules/expense/services/approvalService';
import type { ExpenseClaim } from '@modules/expense/types/approval';
import { usePermissions } from '@platform/permissions/usePermissions';

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString('en-ZA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  });
}

export function ApprovalsStubPanel() {
  const { hasPermission } = usePermissions();
  const canApproveExpense = hasPermission('expense.approvals.write');
  const canRequestExpense = hasPermission('expense.claims.write');
  const showManagerQueues = hasPermission('expense.approvals.read');

  const [expenseItems, setExpenseItems] = useState<ExpenseClaim[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [expenseDescription, setExpenseDescription] = useState('');
  const [expenseAmount, setExpenseAmount] = useState('');

  const loadQueues = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const expense = showManagerQueues
        ? await getPendingExpenseApprovals()
        : [];
      setExpenseItems(expense);
    } catch (loadError) {
      const message = loadError instanceof ApiError
        ? loadError.message
        : 'Failed to load approval queues.';
      setError(message);
      setExpenseItems([]);
    } finally {
      setIsLoading(false);
    }
  }, [showManagerQueues]);

  useEffect(() => {
    void loadQueues();
  }, [loadQueues]);

  async function submitExpenseClaim() {
    await createExpenseClaim({
      description: expenseDescription,
      amount: Number(expenseAmount),
    });
    setExpenseDescription('');
    setExpenseAmount('');
    await loadQueues();
  }

  return (
    <div className="flex flex-col gap-6 max-w-4xl">
      <div>
        <Subtitle2>Expense approval queue (preview)</Subtitle2>
        <Text className="text-sm text-neutral-foreground-3 block mt-1">
          Expense claims use the manager hierarchy from staff profiles. Leave requests are managed in the Leave app.
        </Text>
      </div>

      {isLoading && <Spinner label="Loading approvals..." />}
      {error && <Text className="text-sm text-red-600">{error}</Text>}

      {canRequestExpense && (
        <section className="rounded border border-[#e3e5e7] p-4 flex flex-col gap-3">
          <Subtitle2>Submit expense claim (stub)</Subtitle2>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Description">
              <Input value={expenseDescription} onChange={(_, data) => setExpenseDescription(data.value)} />
            </Field>
            <Field label="Amount (ZAR)">
              <Input type="number" value={expenseAmount} onChange={(_, data) => setExpenseAmount(data.value)} />
            </Field>
          </div>
          <Button appearance="primary" onClick={() => void submitExpenseClaim()}>
            Submit expense claim
          </Button>
        </section>
      )}

      {hasPermission('expense.approvals.read') && (
        <section className="flex flex-col gap-3">
          <Subtitle2>Pending expense approvals</Subtitle2>
          {expenseItems.length === 0 ? (
            <Text className="text-sm text-neutral-foreground-3">No pending expense claims.</Text>
          ) : (
            expenseItems.map((item) => (
              <div key={item.id} className="rounded border border-[#e3e5e7] p-3 flex justify-between gap-3">
                <div>
                  <Text weight="semibold" block>{item.requesterDisplayName}</Text>
                  <Text size={200}>{item.description} · {item.currency} {item.amount.toFixed(2)}</Text>
                  <Text size={200} className="text-neutral-foreground-3">
                    Submitted {formatDate(item.createdAt)}
                  </Text>
                </div>
                {canApproveExpense && (
                  <div className="flex gap-2">
                    <Button
                      size="small"
                      onClick={() => {
                        const notes = window.prompt('Provide a reason for rejecting this expense claim:') ?? '';
                        const trimmedNotes = notes.trim();
                        if (!trimmedNotes) {
                          setError('A reason is required to reject an expense claim.');
                          return;
                        }

                        void decideExpenseApproval(item.id, { approve: false, notes: trimmedNotes }).then(loadQueues);
                      }}
                    >
                      Reject
                    </Button>
                    <Button size="small" appearance="primary" onClick={() => void decideExpenseApproval(item.id, { approve: true }).then(loadQueues)}>
                      Approve
                    </Button>
                  </div>
                )}
              </div>
            ))
          )}
        </section>
      )}
    </div>
  );
}
