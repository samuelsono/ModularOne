import {
  Button,
  TeachingPopover,
  TeachingPopoverBody,
  TeachingPopoverHeader,
  TeachingPopoverSurface,
  TeachingPopoverTrigger,
  Text,
} from '@fluentui/react-components';
import { ExpenseCategoryName, ExpenseStatusBadge } from '@modules/expense/components/expenseBadges';
import type { ExpenseClaim } from '@modules/expense/types/expense';

interface ExpenseClaimDetailPopoverProps {
  item: ExpenseClaim;
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="grid grid-cols-[140px_1fr] gap-2 text-sm">
      <Text className="text-neutral-foreground-3 font-bold!">{label}</Text>
      <Text>{value}</Text>
    </div>
  );
}

function formatDate(value: string | null | undefined): string {
  if (!value) {
    return '—';
  }

  return new Date(value).toLocaleString();
}

function formatTravel(item: ExpenseClaim): string {
  if (!item.requiresTravelDetails) {
    return '—';
  }

  const route = `${item.travelStartPoint ?? '-'} -> ${item.travelDestination ?? '-'}`;
  const distance = `${item.kilometersTravelled ?? 0} km`;
  return `${route} (${distance})`;
}

export function ExpenseClaimDetailPopover({ item }: ExpenseClaimDetailPopoverProps) {
  return (
    <TeachingPopover>
      <TeachingPopoverTrigger disableButtonEnhancement>
        <div
          className="max-w-full cursor-pointer text-left! p-0! h-auto! min-w-0 font-normal! justify-start"
        >
          <ExpenseCategoryName name={item.categoryName} code={item.categoryCode} />
        </div>
      </TeachingPopoverTrigger>

      <TeachingPopoverSurface className="ml-5!">
        <TeachingPopoverHeader>{item.categoryName}</TeachingPopoverHeader>
        <TeachingPopoverBody>
          <div className="flex flex-col gap-2 min-w-[360px] py-3">
            <DetailRow label="Expense date" value={new Date(item.expenseDate).toLocaleDateString()} />
            <DetailRow label="Description" value={item.description} />
            <DetailRow label="Amount" value={`${item.currency} ${item.amount.toFixed(2)}`} />
            <DetailRow label="Travel" value={formatTravel(item)} />
            <DetailRow label="Waypoints" value={(item.travelWaypoints && item.travelWaypoints.length > 0) ? item.travelWaypoints.join(', ') : '—'} />
            <DetailRow label="Receipt" value={item.hasReceipt ? (item.receiptFileName ?? 'Attached') : 'Missing'} />
            <DetailRow label="Submitted" value={formatDate(item.submittedAt)} />
            <DetailRow label="Decided" value={formatDate(item.decidedAt)} />
            <div className="grid grid-cols-[140px_1fr] gap-2 text-sm items-center">
              <Text className="text-neutral-foreground-3 font-bold!">Status</Text>
              <span>
                <ExpenseStatusBadge status={item.status} />
              </span>
            </div>
            <DetailRow label={item.status === 'Rejected' ? 'Rejection notes' : 'Notes'} value={item.notes ?? '—'} />
          </div>
        </TeachingPopoverBody>
      </TeachingPopoverSurface>
    </TeachingPopover>
  );
}
