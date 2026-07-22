import { useRef } from 'react';
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
import type { LeaveRequest } from '@modules/leave/types/leave';
import type { AuthUser } from '@platform/auth/types';
import {
  LeaveDocumentLink,
  LeaveRequestStatusCell,
  formatLeaveDateRange,
  formatLeaveDateTime,
} from './leaveTableUtils';
import {
  getAttachDocumentLabel,
  getLeaveRowActions,
  LEAVE_DOCUMENT_ACCEPT,
  type LeaveConfirmActionKind,
  type LeaveActionPermissions,
} from '@modules/leave/utils/leaveActionUtils';

interface LeaveRequestDetailPopoverProps {
  item: LeaveRequest;
  user: AuthUser | null | undefined;
  permissions: LeaveActionPermissions;
  onAction?: (kind: LeaveConfirmActionKind, id: string) => void;
  onUploadDocument?: (id: string, file: File) => void;
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="grid grid-cols-[120px_1fr] gap-2 text-sm">
      <Text className="text-neutral-foreground-3 font-bold!">{label}</Text>
      <Text className='max-w-[300px]'>{value}</Text>
    </div>
  );
}

export function LeaveRequestDetailPopover({
  item,
  user,
  permissions,
  onAction,
  onUploadDocument,
}: LeaveRequestDetailPopoverProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const actions = getLeaveRowActions(user, item, permissions);
  const cancelAction = actions.includes('cancel') ? 'cancel' : null;
  const canAttachDocument = actions.includes('attachDocument');

  return (
    <TeachingPopover>
      <TeachingPopoverTrigger disableButtonEnhancement>
        <div
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

      <TeachingPopoverSurface className='ml-5!'>
        <TeachingPopoverHeader>{item.leaveType}</TeachingPopoverHeader>
        <TeachingPopoverBody >
          <div className="flex flex-col gap-2 min-w-[340px] py-3">
            <DetailRow label="Dates" value={formatLeaveDateRange(item)} />
            <DetailRow label="Working days" value={item.workingDays.toFixed(1)} />
            <div className="grid grid-cols-[120px_1fr] gap-2 text-sm items-center">
              <Text className="text-neutral-foreground-3 font-bold!">Status</Text>
              <span>
                <LeaveRequestStatusCell
                status={item.status}
                startDate={item.startDate}
                endDate={item.endDate}
              />
              </span>
              
            </div>
            <DetailRow label="Submitted" value={formatLeaveDateTime(item.createdAt)} />
            {item.decidedAt ? (
              <DetailRow label="Decided" value={formatLeaveDateTime(item.decidedAt)} />
            ) : null}
            <DetailRow label="Notes"  value={item.notes ?? '—'} />
            <div className="grid grid-cols-[120px_1fr] gap-2 text-sm items-center">
              <Text className="text-neutral-foreground-3 font-bold!">Document</Text>
              <div className="flex flex-wrap items-center gap-2 min-w-0">
                <LeaveDocumentLink item={item} />
                {canAttachDocument ? (
                  <>
                    <input
                      ref={fileInputRef}
                      type="file"
                      className="hidden"
                      accept={LEAVE_DOCUMENT_ACCEPT}
                      onChange={(event) => {
                        const file = event.target.files?.[0];
                        event.target.value = '';
                        if (file) {
                          onUploadDocument?.(item.id, file);
                        }
                      }}
                    />
                    <Button
                      size="small"
                      appearance="secondary"
                      onClick={() => fileInputRef.current?.click()}
                    >
                      {getAttachDocumentLabel(item)}
                    </Button>
                  </>
                ) : null}
              </div>
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
            secondary={"OK"}
          />
        ) : null}
      </TeachingPopoverSurface>
    </TeachingPopover>
  );
}
