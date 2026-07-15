import { useEffect, useState } from 'react';
import type { SelectTabData, SelectTabEvent, TabValue } from '@fluentui/react-components';
import {
  Avatar,
  Badge,
  Button,
  Dialog,
  DialogSurface,
  Spinner,
  Tab,
  TabList,
  Text,
} from '@fluentui/react-components';
import {
  BuildingRegular,
  CalendarRegular,
  DismissRegular,
  EditRegular,
  MailRegular,
  PersonKeyRegular,
  PersonRegular,
  VehicleCarRegular,
} from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import { getUser } from '@modules/users/services/userService';
import type { UserDetail, UserListItem } from '@modules/users/types/user';
import { DetailRow, DetailSection } from '@platform/ui/DetailLayout';
import { formatDateOnlyDisplay } from '@platform/utils/dateOnly';

interface EmployeeDetailsDialogProps {
  employee: UserListItem | null;
  open: boolean;
  onClose: () => void;
  onEdit?: (employee: UserListItem) => void;
}

function formatDateTime(value: string | null | undefined): string {
  if (!value) {
    return '—';
  }

  return new Date(value).toLocaleString('en-ZA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function formatDate(value: string | null | undefined): string {
  return formatDateOnlyDisplay(value, 'en-ZA', '—');
}

function displayNameFor(employee: UserListItem | UserDetail): string {
  return employee.displayName ?? employee.username ?? '—';
}

export function EmployeeDetailsDialog({
  employee,
  open,
  onClose,
  onEdit,
}: EmployeeDetailsDialogProps) {
  const [selectedTab, setSelectedTab] = useState<TabValue>('overview');
  const [detail, setDetail] = useState<UserDetail | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open || !employee) {
      setDetail(null);
      setError(null);
      return;
    }

    let cancelled = false;
    const employeeId = employee.id;

    async function loadDetail() {
      setIsLoading(true);
      setError(null);

      try {
        const user = await getUser(employeeId);
        if (!cancelled) {
          setDetail(user);
        }
      } catch (loadError) {
        if (!cancelled) {
          const message = loadError instanceof ApiError
            ? loadError.message
            : 'Failed to load employee details.';
          setError(message);
          setDetail(null);
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadDetail();

    return () => {
      cancelled = true;
    };
  }, [employee, open]);

  if (!employee) {
    return null;
  }

  const profile = detail?.staffProfile;
  const displayName = displayNameFor(detail ?? employee);
  const statusColor = employee.isActive ? 'success' : 'informative';

  return (
    <Dialog
      open={open}
      onOpenChange={(_, data) => {
        if (!data.open) {
          onClose();
        }
      }}
    >
      <DialogSurface className="flex! max-h-[90vh]! w-[640px]! max-w-[95vw]! flex-col! p-0!">
        <div className="border-b border-[#e3e5e7] px-6 pb-4 pt-6">
          <div className="flex items-start gap-4">
            <Avatar name={displayName} color="brand" size={72} />
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <h2 className="text-2xl font-semibold text-neutral-foreground-1">
                  {displayName}
                </h2>
                <Badge color={statusColor} appearance="filled">
                  {employee.isActive ? 'Active' : 'Inactive'}
                </Badge>
                {employee.invitePendingAt && (
                  <Badge appearance="outline">Invite pending</Badge>
                )}
              </div>
              <p className="mt-1 text-sm text-neutral-foreground-2">
                {employee.username} • {profile?.jobTitle || profile?.department || 'Employee'}
              </p>
              <div className="mt-3 flex flex-wrap gap-1">
                {employee.roles.map((role) => (
                  <Badge key={role} appearance="outline" size="small">{role}</Badge>
                ))}
              </div>
            </div>
            <Button
              appearance="subtle"
              aria-label="Close"
              icon={<DismissRegular />}
              onClick={onClose}
            />
          </div>

          <TabList
            className="mt-5"
            selectedValue={selectedTab}
            onTabSelect={(_event: SelectTabEvent, data: SelectTabData) => setSelectedTab(data.value)}
          >
            <Tab value="overview">Overview</Tab>
            <Tab value="employment">Employment</Tab>
            <Tab value="access">Access</Tab>
          </TabList>
        </div>

        <div className="flex-1 overflow-y-auto px-6">
          {isLoading && (
            <div className="flex justify-center py-8">
              <Spinner label="Loading employee..." />
            </div>
          )}

          {error && (
            <Text className="py-4 text-sm text-red-600">{error}</Text>
          )}

          {!isLoading && !error && detail && selectedTab === 'overview' && (
            <>
              <DetailSection title="Contact">
                <DetailRow icon={<MailRegular fontSize={16} />} label="Email" value={detail.email} />
                <DetailRow icon={<PersonRegular fontSize={16} />} label="Username" value={detail.username} />
                <DetailRow icon={<PersonRegular fontSize={16} />} label="First name" value={detail.firstName ?? '—'} />
                <DetailRow icon={<PersonRegular fontSize={16} />} label="Last name" value={detail.lastName ?? '—'} />
              </DetailSection>

              <DetailSection title="Activity">
                <DetailRow icon={<CalendarRegular fontSize={16} />} label="Created" value={formatDateTime(detail.createdAt)} />
                <DetailRow icon={<CalendarRegular fontSize={16} />} label="Last login" value={formatDateTime(detail.lastLoginAt)} />
                <DetailRow icon={<PersonKeyRegular fontSize={16} />} label="Invite status" value={detail.invitePendingAt ? 'Pending setup' : 'Complete'} />
              </DetailSection>
            </>
          )}

          {!isLoading && !error && detail && selectedTab === 'employment' && (
            <DetailSection title="Employment">
              <DetailRow icon={<BuildingRegular fontSize={16} />} label="Employee number" value={profile?.employeeNumber ?? '—'} />
              <DetailRow icon={<BuildingRegular fontSize={16} />} label="Job title" value={profile?.jobTitle ?? '—'} />
              <DetailRow icon={<BuildingRegular fontSize={16} />} label="Department" value={profile?.department ?? '—'} />
              <DetailRow icon={<BuildingRegular fontSize={16} />} label="Branch" value={profile?.branch ?? '—'} />
              <DetailRow icon={<PersonRegular fontSize={16} />} label="Manager" value={profile?.managerDisplayName ?? '—'} />
              <DetailRow icon={<CalendarRegular fontSize={16} />} label="Work start date" value={formatDate(profile?.workStartDate)} />
              <DetailRow icon={<BuildingRegular fontSize={16} />} label="Employment status" value={profile?.employmentStatus ?? '—'} />
            </DetailSection>
          )}

          {!isLoading && !error && detail && selectedTab === 'access' && (
            <>
              <DetailSection title="Platform access">
                <DetailRow icon={<PersonKeyRegular fontSize={16} />} label="Account status" value={detail.isActive ? 'Active' : 'Inactive'} />
                <DetailRow icon={<PersonKeyRegular fontSize={16} />} label="Roles" value={detail.roles.join(', ') || '—'} />
              </DetailSection>

              <DetailSection title="Driver link">
                {detail.driverLink ? (
                  <>
                    <DetailRow icon={<VehicleCarRegular fontSize={16} />} label="Driver code" value={detail.driverLink.driverCode} />
                    <DetailRow icon={<PersonRegular fontSize={16} />} label="Driver name" value={detail.driverLink.driverName} />
                  </>
                ) : (
                  <DetailRow icon={<VehicleCarRegular fontSize={16} />} label="Linked driver" value="No driver linked" />
                )}
              </DetailSection>
            </>
          )}
        </div>

        <div className="border-t border-[#e3e5e7] px-6 py-4">
          <Button
            appearance="secondary"
            className="w-full"
            icon={<EditRegular />}
            onClick={() => onEdit?.(employee)}
          >
            Edit employee
          </Button>
        </div>
      </DialogSurface>
    </Dialog>
  );
}
