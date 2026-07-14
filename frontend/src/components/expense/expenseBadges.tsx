import { Badge } from '@fluentui/react-components';
import { expenseStatusLabel } from '../../types/expense';

const categoryColors = [
  '#0078D4',
  '#107C10',
  '#D83B01',
  '#5C2D91',
  '#008575',
  '#C239B3',
  '#8E562E',
  '#8764B8',
];

function expenseCategoryColor(value: string): string {
  const source = value.trim().toLowerCase();
  let hash = 0;

  for (let index = 0; index < source.length; index += 1) {
    hash = (hash * 31 + source.charCodeAt(index)) % categoryColors.length;
  }

  return categoryColors[hash];
}

function expenseStatusBadgeColor(
  status: string,
): 'informative' | 'success' | 'danger' | 'warning' | 'subtle' {
  switch (status) {
    case 'Paid':
    case 'Active':
      return 'success';
    case 'Rejected':
    case 'Inactive':
      return 'danger';
    case 'PendingManager':
    case 'PendingFinance':
    case 'Pending':
      return 'warning';
    case 'PendingPayment':
    case 'Approved':
      return 'informative';
    case 'Draft':
      return 'subtle';
    case 'Cancelled':
      return 'informative';
    default:
      return 'informative';
  }
}

export function ExpenseStatusBadge({ status }: { status: string }) {
  return (
    <Badge appearance="outline" color={expenseStatusBadgeColor(status)}>
      {expenseStatusLabel(status)}
    </Badge>
  );
}

export function ExpenseCategoryName({
  name,
  code,
}: {
  name: string;
  code?: string | null;
}) {
  const colorKey = code || name;

  return (
    <span className="flex flex-col items-start gap-0">
      <span className="inline-flex items-center gap-2">
        <span
          className="inline-block w-3 h-3 rounded-full shrink-0"
          style={{ backgroundColor: expenseCategoryColor(colorKey) }}
        />
        <span>{name}</span>
      </span>
    </span>
  );
}
