import {
  Button,
  TeachingPopover,
  TeachingPopoverBody,
  TeachingPopoverFooter,
  TeachingPopoverHeader,
  TeachingPopoverSurface,
  TeachingPopoverTrigger,
  Text,
} from '@fluentui/react-components';
import type { LeaveRequest } from '../../types/leave';
import type { AuthUser } from '../../types/auth';
import {
  LeaveDocumentLink,
  LeaveRequestStatusCell,
  formatLeaveDateRange,
  formatLeaveDateTime,
} from './leaveTableUtils';
import {
  getLeaveRowActions,
  type LeaveActionKind,
  type LeaveActionPermissions,
} from '../../utils/leaveActionUtils';

interface LeaveRequestDetailPopoverProps {
  item: LeaveRequest;
  user: AuthUser | null | undefined;
  permissions: LeaveActionPermissions;
  onAction?: (kind: LeaveActionKind, id: string) => void;
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="grid grid-cols-[120px_1fr] gap-2 text-sm">
      <Text className="text-neutral-foreground-3">{label}</Text>
      <Text className=''>{value}</Text>
    </div>
  );
}

export function LeaveRequestDetailPopover({
  item,
  user,
  permissions,
  onAction,
}: LeaveRequestDetailPopoverProps) {
  const actions = getLeaveRowActions(user, item, permissions);
  const cancelAction = actions.includes('cancel') ? 'cancel' : null;

  return (
    <TeachingPopover>
      <TeachingPopoverTrigger disableButtonEnhancement>
        <div
          appearance="subtle"
          className="max-w-full cursor-pointer flex text-left justify-start px-0 min-w-0 h-auto font-normal"
        >
          <span className="inline-flex items-center gap-2 truncate">
            {item.leaveTypeColor ? (
              <span
                className="inline-block w-3 h-3 rounded-full shrink-0"
                style={{ backgroundColor: item.leaveTypeColor }}
              />
            ) : null}
            <span className="truncate">{item.leaveType}</span>
          </span>
        </div>
      </TeachingPopoverTrigger>

      <TeachingPopoverSurface>
        <TeachingPopoverHeader>{item.leaveType}</TeachingPopoverHeader>
        <TeachingPopoverBody>
          <div className="flex flex-col gap-2 min-w-[340px] py-3">
            <DetailRow label="Dates" value={formatLeaveDateRange(item)} />
            <DetailRow label="Working days" value={item.workingDays.toFixed(1)} />
            <div className="grid grid-cols-[120px_1fr] gap-2 text-sm items-center">
              <Text className="text-neutral-foreground-3">Status</Text>
              <LeaveRequestStatusCell
                status={item.status}
                startDate={item.startDate}
                endDate={item.endDate}
              />
            </div>
            <DetailRow label="Submitted" value={formatLeaveDateTime(item.createdAt)} />
            {item.decidedAt ? (
              <DetailRow label="Decided" value={formatLeaveDateTime(item.decidedAt)} />
            ) : null}
            <DetailRow label="Notes" value={item.notes ?? '—'} />
            <div className="grid grid-cols-[120px_1fr] gap-2 text-sm items-center">
              <Text className="text-neutral-foreground-3">Document</Text>
              <LeaveDocumentLink item={item} />
            </div>
          </div>
        </TeachingPopoverBody>
        {cancelAction ? (
          <TeachingPopoverFooter
            primary={{
              children: "Cancel request",
              className: 'bg-red-700! hover:bg-red-600!',
              onClick: () => onAction?.(cancelAction, item.id),
            }}
            secondary={"Got it"}
          />
        ) : null}
      </TeachingPopoverSurface>
    </TeachingPopover>
  );
}
