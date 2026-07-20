import type { ReactNode } from 'react';
import { Badge, Button, InfoLabel, type TableColumnDefinition, type TableColumnSizingOptions, createTableColumn } from '@fluentui/react-components';
import { ArrowDownloadRegular } from '@fluentui/react-icons';
import type { LeaveBalance, LeaveHistoryRow, LeaveRequest } from '@modules/leave/types/leave';
import { downloadLeaveDocument } from '@modules/leave/services/leaveService';
import { LeaveRequestDetailPopover } from './LeaveRequestDetailPopover';
import type { AuthUser } from '@platform/auth/types';
import type { LeaveActionKind, LeaveActionPermissions } from '@modules/leave/utils/leaveActionUtils';
import { withAuditableColumns } from '@platform/ui/auditTableColumns';

export const leaveTableColumnSizing: TableColumnSizingOptions = {
  requester: { minWidth: 140, idealWidth: 280, defaultWidth: 200 },
  employee: { minWidth: 140, idealWidth: 280, defaultWidth: 200 },
  department: { minWidth: 120, idealWidth: 180, defaultWidth: 150 },
  leaveType: { minWidth: 120, idealWidth: 120, defaultWidth: 120 },
  dates: { minWidth: 220, idealWidth: 420, defaultWidth: 300 },
  workingDays: { minWidth: 80, idealWidth: 80, defaultWidth: 80 },
  status: { minWidth: 180, idealWidth: 180, defaultWidth: 180 },
  approvalStatus: { minWidth: 120, idealWidth: 120, defaultWidth: 120 },
  document: { minWidth: 260, idealWidth: 380, defaultWidth: 300 },
  notes: { minWidth: 160, idealWidth: 320, defaultWidth: 220 },
  createdAt: { minWidth: 170, idealWidth: 280, defaultWidth: 210 },
  actions: { minWidth: 150, idealWidth: 170, defaultWidth: 170 },
  remaining: { minWidth: 100, idealWidth: 120, defaultWidth: 110 },
  used: { minWidth: 100, idealWidth: 120, defaultWidth: 110 },
  pending: { minWidth: 100, idealWidth: 120, defaultWidth: 110 },
};

function LeaveTableText({ children, nowrap = false }: { children: ReactNode; nowrap?: boolean }) {
  return (
    <span className={nowrap ? 'whitespace-nowrap' : undefined}>
      {children}
    </span>
  );
}

export function formatLeaveDate(value: string): string {
  return new Date(`${value}T00:00:00`).toLocaleDateString('en-ZA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  });
}

export function formatLeaveDateRange(item: Pick<LeaveRequest, 'startDate' | 'endDate' | 'startDayPortion' | 'endDayPortion'>): string {
  const range = `${formatLeaveDate(item.startDate)} – ${formatLeaveDate(item.endDate)}`;
  if (item.startDate === item.endDate && item.startDayPortion === 'Half') {
    return `${formatLeaveDate(item.startDate)} (half day)`;
  }
  if (item.startDayPortion === 'Half' || item.endDayPortion === 'Half') {
    return `${range} (partial)`;
  }
  return range;
}

export function formatLeaveDateTime(value: string): string {
  return new Date(value).toLocaleString('en-ZA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function getLocalDateKey(date = new Date()): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function isLeaveCurrentlyRunning(
  status: string,
  startDate: string,
  endDate: string,
): boolean {
  if (status !== 'Approved') {
    return false;
  }

  const today = getLocalDateKey();
  return today >= startDate && today <= endDate;
}

/** Approved leave whose end date is already in the past. */
export function isLeaveCompleted(
  status: string,
  endDate: string,
): boolean {
  if (status !== 'Approved') {
    return false;
  }

  return getLocalDateKey() > endDate;
}

export function sortLeaveHistoryItems<
  T extends { status: string; startDate: string; endDate: string },
>(items: T[]): T[] {
  return [...items].sort((a, b) => {
    const aRunning = isLeaveCurrentlyRunning(a.status, a.startDate, a.endDate);
    const bRunning = isLeaveCurrentlyRunning(b.status, b.startDate, b.endDate);

    if (aRunning !== bRunning) {
      return aRunning ? -1 : 1;
    }

    return b.startDate.localeCompare(a.startDate);
  });
}

export function leaveStatusBadgeColor(
  status: string,
): 'informative' | 'success' | 'danger' | 'warning' {
  switch (status) {
    case 'Approved':
      return 'success';
    case 'Rejected':
      return 'danger';
    case 'Cancelled':
      return 'warning';
    default:
      return 'informative';
  }
}

export function LeaveStatusBadge({ status }: { status: string }) {
  return (
    <Badge appearance="outline" color={leaveStatusBadgeColor(status)}>
      {status}
    </Badge>
  );
}

export function LeaveRequestStatusCell({
  status,
  startDate,
  endDate,
}: {
  status: string;
  startDate: string;
  endDate: string;
}) {
  if (isLeaveCurrentlyRunning(status, startDate, endDate)) {
    return (
      <span className="inline-flex flex-wrap items-center gap-1">
        <Badge appearance="filled" color="success">
          On leave
        </Badge>
        <LeaveStatusBadge status={status} />
      </span>
    );
  }

  if (isLeaveCompleted(status, endDate)) {
    return (
      <span className="inline-flex flex-wrap items-center gap-1">
        <Badge appearance="filled" color="informative">
          Completed
        </Badge>
        <LeaveStatusBadge status={status} />
      </span>
    );
  }

  return <LeaveStatusBadge status={status} />;
}

export function LeaveDocumentLink({ item }: { item: LeaveRequest }) {
  if (!item.hasDocument) {
    return <>—</>;
  }

  return (
    <Button
      appearance="subtle"
      size="small"
      className="max-w-full"
      icon={<ArrowDownloadRegular />}
      onClick={() => void downloadLeaveDocument(item.id, item.documentFileName)}
    >
      <span className="truncate">{item.documentFileName ?? 'Download'}</span>
    </Button>
  );}

export const leaveRequestColumns: TableColumnDefinition<LeaveRequest>[] = withAuditableColumns([
  createTableColumn<LeaveRequest>({
    columnId: 'leaveType',
    renderHeaderCell: () => 'Type',
    renderCell: (item) => (
      <LeaveTableText nowrap>
        <span className="inline-flex items-center gap-2">
          {item.leaveTypeColor ? (
            <span
              className="inline-block w-3 h-3 rounded-full shrink-0"
              style={{ backgroundColor: item.leaveTypeColor }}
            />
          ) : null}
          {item.leaveType}
        </span>
      </LeaveTableText>
    ),
  }),
  createTableColumn<LeaveRequest>({
    columnId: 'employee',
    renderHeaderCell: () => 'Employee',
    renderCell: (item) => <LeaveTableText nowrap>{item.requesterDisplayName}</LeaveTableText>,
  }),
  createTableColumn<LeaveRequest>({
    columnId: 'dates',
    renderHeaderCell: () => 'Dates',
    renderCell: (item) => <LeaveTableText nowrap>{formatLeaveDateRange(item)}</LeaveTableText>,
  }),
  createTableColumn<LeaveRequest>({
    columnId: 'workingDays',
    renderHeaderCell: () => 'Days',
    renderCell: (item) => <LeaveTableText nowrap>{item.workingDays.toFixed(1)}</LeaveTableText>,
  }),
  createTableColumn<LeaveRequest>({
    columnId: 'status',
    renderHeaderCell: () => 'Status',
    renderCell: (item) => (
      <LeaveTableText nowrap>
        <LeaveRequestStatusCell
          status={item.status}
          startDate={item.startDate}
          endDate={item.endDate}
        />
      </LeaveTableText>
    ),
  }),
  createTableColumn<LeaveRequest>({
    columnId: 'notes',
    renderHeaderCell: () => 'Notes',
    renderCell: (item) => item.notes ?? '—',
  }),
  createTableColumn<LeaveRequest>({
    columnId: 'document',
    renderHeaderCell: () => 'Document',
    renderCell: (item) => <LeaveDocumentLink item={item} />,
  }),
  createTableColumn<LeaveRequest>({
    columnId: 'submittedAt',
    renderHeaderCell: () => 'Submitted',
    renderCell: (item) => <LeaveTableText nowrap>{formatLeaveDateTime(item.createdAt)}</LeaveTableText>,
  }),
]);

export function createLeaveRequestColumnsWithDetailPopover({
  user,
  permissions,
  onAction,
  renderActions,
}: {
  user: AuthUser | null | undefined;
  permissions: LeaveActionPermissions;
  onAction?: (kind: LeaveActionKind, id: string) => void;
  renderActions?: (item: LeaveRequest) => ReactNode;
}): TableColumnDefinition<LeaveRequest>[] {
  const columns: TableColumnDefinition<LeaveRequest>[] = [
    createTableColumn<LeaveRequest>({
      columnId: 'leaveType',
      renderHeaderCell: () => 'Type',
      renderCell: (item) => (
        <LeaveRequestDetailPopover
          item={item}
          user={user}
          permissions={permissions}
          onAction={onAction}
        />
      ),
    }),
    ...leaveRequestColumns.slice(1),
  ];

  if (renderActions) {
    columns.push(createLeaveActionsColumn(renderActions));
  }

  return columns;
}

export const leaveBalanceLiabilityColumns: TableColumnDefinition<LeaveBalance>[] = [
  createTableColumn<LeaveBalance>({
    columnId: 'leaveType',
    renderHeaderCell: () => 'Type',
    renderCell: (item) => (
      <LeaveTableText nowrap>
        <span className="inline-flex items-center gap-2">
          {item.leaveTypeColor ? (
            <span
              className="inline-block w-3 h-3 rounded-full shrink-0"
              style={{ backgroundColor: item.leaveTypeColor }}
            />
          ) : null}
          {item.leaveTypeName}
        </span>
      </LeaveTableText>
    ),
  }),
  createTableColumn<LeaveBalance>({
    columnId: 'allocated',
    renderHeaderCell: () => 'Allocated',
    renderCell: (item) => <LeaveTableText nowrap>{item.allocated.toFixed(1)}</LeaveTableText>,
  }),
  createTableColumn<LeaveBalance>({
    columnId: 'used',
    renderHeaderCell: () => 'Used',
    renderCell: (item) => <LeaveTableText nowrap>{item.used.toFixed(1)}</LeaveTableText>,
  }),
  createTableColumn<LeaveBalance>({
    columnId: 'pending',
    renderHeaderCell: () => 'Pending',
    renderCell: (item) => <LeaveTableText nowrap>{item.pending.toFixed(1)}</LeaveTableText>,
  }),
  createTableColumn<LeaveBalance>({
    columnId: 'remaining',
    renderHeaderCell: () => 'Remaining',
    renderCell: (item) => <LeaveTableText nowrap>{item.remaining.toFixed(1)}</LeaveTableText>,
  }),
  
];

export function createLeaveActionsColumn<TItem>(
  renderActions: (item: TItem) => ReactNode,
): TableColumnDefinition<TItem> {
  return createTableColumn<TItem>({
    columnId: 'actions',
    renderHeaderCell: () => 'Actions',
    renderCell: (item) => <div className="w-fit">{renderActions(item)}</div>,
  });
}

export const leaveHistoryColumns: TableColumnDefinition<LeaveHistoryRow>[] = withAuditableColumns([
  createTableColumn<LeaveHistoryRow>({
    columnId: 'employee',
    renderHeaderCell: () => 'Employee',
    renderCell: (item) => <LeaveTableText nowrap>{item.requesterDisplayName}</LeaveTableText>,
  }),
  createTableColumn<LeaveHistoryRow>({
    columnId: 'department',
    renderHeaderCell: () => 'Department',
    renderCell: (item) => <LeaveTableText nowrap>{item.department ?? '—'}</LeaveTableText>,
  }),
  createTableColumn<LeaveHistoryRow>({
    columnId: 'leaveType',
    renderHeaderCell: () => 'Type',
    renderCell: (item) => <LeaveTableText nowrap>{item.leaveType}</LeaveTableText>,
  }),
  createTableColumn<LeaveHistoryRow>({
    columnId: 'dates',
    renderHeaderCell: () => 'Dates',
    renderCell: (item) => (
      <LeaveTableText nowrap>
        {`${formatLeaveDate(item.startDate)} – ${formatLeaveDate(item.endDate)}`}
      </LeaveTableText>
    ),
  }),
  createTableColumn<LeaveHistoryRow>({
    columnId: 'workingDays',
    renderHeaderCell: () => 'Days',
    renderCell: (item) => <LeaveTableText nowrap>{item.workingDays.toFixed(1)}</LeaveTableText>,
  }),
  createTableColumn<LeaveHistoryRow>({
    columnId: 'status',
    renderHeaderCell: () => 'Status',
    renderCell: (item) => (
      <LeaveTableText nowrap>
        <LeaveRequestStatusCell
          status={item.status}
          startDate={item.startDate}
          endDate={item.endDate}
        />
      </LeaveTableText>
    ),
  }),
]);

export const leaveApprovalColumns: TableColumnDefinition<LeaveRequest>[] = withAuditableColumns([
  createTableColumn<LeaveRequest>({
    columnId: 'requester',
    renderHeaderCell: () => 'Employee',
    renderCell: (item) => <LeaveTableText nowrap>{item.requesterDisplayName}</LeaveTableText>,
  }),
  createTableColumn<LeaveRequest>({
    columnId: 'leaveType',
    renderHeaderCell: () => 'Type',
    renderCell: (item) => (
      <InfoLabel
         info={
          <>
            {item.notes ?? '—'}
          </>
         }
      >
      <span className="inline-flex items-center gap-2 whitespace-nowrap">
        {item.leaveTypeColor ? (
          <span
            className="inline-block w-3 h-3 rounded-full shrink-0"
            style={{ backgroundColor: item.leaveTypeColor }}
          />
        ) : null}
        {item.leaveType}
      </span>
      </InfoLabel>

    ),
  }),
  createTableColumn<LeaveRequest>({
    columnId: 'dates',
    renderHeaderCell: () => 'Dates',
    renderCell: (item) => <LeaveTableText nowrap>{formatLeaveDateRange(item)}</LeaveTableText>,
  }),
  createTableColumn<LeaveRequest>({
    columnId: 'workingDays',
    renderHeaderCell: () => 'Days',
    renderCell: (item) => <LeaveTableText nowrap>{item.workingDays.toFixed(1)}</LeaveTableText>,
  }),
  createTableColumn<LeaveRequest>({
    columnId: 'document',
    renderHeaderCell: () => 'Document',
    renderCell: (item) => <LeaveDocumentLink item={item} />,
  }),
  createTableColumn<LeaveRequest>({
    columnId: 'submittedAt',
    renderHeaderCell: () => 'Submitted',
    renderCell: (item) => <LeaveTableText nowrap>{formatLeaveDateTime(item.createdAt)}</LeaveTableText>,
  }),
  createTableColumn<LeaveRequest>({
    columnId: 'approvalStatus',
    renderHeaderCell: () => 'Status',
    renderCell: (item) => (
      <LeaveTableText nowrap>
        <LeaveStatusBadge status={item.status} />
      </LeaveTableText>
    ),
  }),
]);
