import { Badge } from '@fluentui/react-components';

function statusBadgeColor(
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

/** Generic Active/Inactive (and similar) status badge for Core HR and other modules. */
export function StatusBadge({ status }: { status: string }) {
  return (
    <Badge appearance="outline" color={statusBadgeColor(status)}>
      {status}
    </Badge>
  );
}
