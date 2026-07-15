import { Divider } from '@fluentui/react-components';
import { LeaveTypesManager } from '@modules/leave/components/LeaveTypesManager';
import { PublicHolidaysManager } from '@modules/leave/components/PublicHolidaysManager';
import { usePermissions } from '@platform/permissions/usePermissions';

export default function LeavePoliciesPage() {
  const { hasPermission } = usePermissions();
  const canRead = hasPermission('leave.policies.read');
  const canWrite = hasPermission('leave.policies.write');

  if (!canRead) {
    return (
      <div className="flex flex-col items-center justify-center h-full gap-3 p-8 text-center">
        <p className="text-sm text-neutral-foreground-3">
          You do not have permission to view leave policies.
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-8 h-full overflow-auto pb-20">
      <LeaveTypesManager canWrite={canWrite} />
      <Divider />
      <PublicHolidaysManager canWrite={canWrite} />
    </div>
  );
}
