import { useState } from 'react';
import type { SelectTabData, SelectTabEvent, TabValue } from '@fluentui/react-components';
import {
  Avatar,
  Badge,
  Button,
  Dialog,
  DialogSurface,
  Tab,
  TabList,
} from '@fluentui/react-components';
import {
  BuildingRegular,
  CalendarRegular,
  CardUiRegular,
  ContactCardRegular,
  DismissRegular,
  EditRegular,
  LocationRegular,
  MailRegular,
  PersonKeyRegular,
  PersonRegular,
} from '@fluentui/react-icons';
import type { Driver } from '@modules/fleet/types/driver';
import { DetailRow, DetailSection } from '@platform/ui/DetailLayout';
import { formatDateOnlyDisplay } from '@platform/utils/dateOnly';

interface DriverDetailsDialogProps {
  driver: Driver | null;
  open: boolean;
  onClose: () => void;
  onEdit?: (driver: Driver) => void;
}

function formatDate(value: string | null | undefined): string {
  return formatDateOnlyDisplay(value, 'en-ZA', '—');
}

function formatAddress(driver: Driver): string {
  const parts = [
    driver.unitName,
    [driver.streetNumber, driver.streetName].filter(Boolean).join(' ').trim(),
    driver.suburb,
    driver.city,
    driver.province,
    driver.postalCode,
  ].filter(Boolean);

  return parts.length > 0 ? parts.join(', ') : '—';
}

export function DriverDetailsDialog({
  driver,
  open,
  onClose,
  onEdit,
}: DriverDetailsDialogProps) {
  const [selectedTab, setSelectedTab] = useState<TabValue>('overview');

  if (!driver) {
    return null;
  }

  const statusColor = driver.employmentStatus === 'Active' ? 'success' : 'informative';

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
        <div className="border-b border-neutral-stroke-2 px-6 pb-4 pt-6">
          <div className="flex items-start gap-4">
            <Avatar name={driver.name} color="brand" size={72} />
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <h2 className="text-2xl font-semibold text-neutral-foreground-1">
                  {driver.name}
                </h2>
                <Badge color={statusColor} appearance="filled">
                  {driver.employmentStatus}
                </Badge>
                {driver.linkedUser && (
                  <Badge appearance="outline">Has login</Badge>
                )}
              </div>
              <p className="mt-1 text-sm text-neutral-foreground-2">
                {driver.driverId} • {driver.jobTitle || driver.department || 'Driver'}
              </p>
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
            <Tab value="licence">Licence</Tab>
            <Tab value="address">Address</Tab>
            <Tab value="login">Login</Tab>
          </TabList>
        </div>

        <div className="flex-1 overflow-y-auto px-6">
          {selectedTab === 'overview' && (
            <>
              <DetailSection title="Contact">
                <DetailRow icon={<MailRegular fontSize={16} />} label="Work email" value={driver.workEmail || '—'} />
                <DetailRow icon={<ContactCardRegular fontSize={16} />} label="Work phone" value={driver.workPhone || '—'} />
                <DetailRow icon={<ContactCardRegular fontSize={16} />} label="WhatsApp" value={driver.whatsappNumber || '—'} />
                <DetailRow icon={<PersonRegular fontSize={16} />} label="Gender" value={driver.gender || '—'} />
              </DetailSection>

              <DetailSection title="Employment">
                <DetailRow icon={<BuildingRegular fontSize={16} />} label="Employee number" value={driver.employeeNumber || '—'} />
                <DetailRow icon={<BuildingRegular fontSize={16} />} label="Job title" value={driver.jobTitle || '—'} />
                <DetailRow icon={<BuildingRegular fontSize={16} />} label="Department" value={driver.department || '—'} />
                <DetailRow icon={<BuildingRegular fontSize={16} />} label="Branch" value={driver.branch || '—'} />
                <DetailRow icon={<PersonRegular fontSize={16} />} label="Manager" value={driver.manager || '—'} />
                <DetailRow icon={<CalendarRegular fontSize={16} />} label="Work start date" value={formatDate(driver.workStartDate)} />
                <DetailRow icon={<BuildingRegular fontSize={16} />} label="Employment type" value={driver.employmentType || '—'} />
              </DetailSection>
            </>
          )}

          {selectedTab === 'licence' && (
            <DetailSection title="Licence & compliance">
              <DetailRow icon={<CardUiRegular fontSize={16} />} label="Licence number" value={driver.licenceNumber || '—'} />
              <DetailRow icon={<CardUiRegular fontSize={16} />} label="Licence code" value={driver.licenceCode || '—'} />
              <DetailRow icon={<CalendarRegular fontSize={16} />} label="Issued" value={formatDate(driver.licenceIssued)} />
              <DetailRow icon={<CalendarRegular fontSize={16} />} label="Expiry" value={formatDate(driver.licenceExpiry)} />
              <DetailRow icon={<PersonRegular fontSize={16} />} label="ID number" value={driver.idNumber || '—'} />
              <DetailRow icon={<CardUiRegular fontSize={16} />} label="PDP" value={driver.hasPdp ? 'Yes' : 'No'} />
            </DetailSection>
          )}

          {selectedTab === 'address' && (
            <DetailSection title="Residential address">
              <DetailRow icon={<LocationRegular fontSize={16} />} label="Full address" value={formatAddress(driver)} />
              <DetailRow icon={<LocationRegular fontSize={16} />} label="Unit" value={driver.unitName || '—'} />
              <DetailRow icon={<LocationRegular fontSize={16} />} label="Street" value={[driver.streetNumber, driver.streetName].filter(Boolean).join(' ').trim() || '—'} />
              <DetailRow icon={<LocationRegular fontSize={16} />} label="Suburb" value={driver.suburb || '—'} />
              <DetailRow icon={<LocationRegular fontSize={16} />} label="City" value={driver.city || '—'} />
              <DetailRow icon={<LocationRegular fontSize={16} />} label="Province" value={driver.province || '—'} />
              <DetailRow icon={<LocationRegular fontSize={16} />} label="Postal code" value={driver.postalCode || '—'} />
            </DetailSection>
          )}

          {selectedTab === 'login' && (
            <DetailSection title="Platform login">
              {driver.linkedUser ? (
                <>
                  <DetailRow icon={<PersonKeyRegular fontSize={16} />} label="Username" value={driver.linkedUser.username} />
                  <DetailRow icon={<MailRegular fontSize={16} />} label="Login email" value={driver.linkedUser.email} />
                  <DetailRow icon={<PersonRegular fontSize={16} />} label="Display name" value={driver.linkedUser.displayName ?? '—'} />
                  <DetailRow icon={<PersonKeyRegular fontSize={16} />} label="Account status" value={driver.linkedUser.isActive ? 'Active' : 'Inactive'} />
                </>
              ) : (
                <DetailRow icon={<PersonKeyRegular fontSize={16} />} label="Linked account" value="No login linked" />
              )}
            </DetailSection>
          )}
        </div>

        <div className="border-t border-neutral-stroke-2 px-6 py-4">
          <Button
            appearance="secondary"
            className="w-full"
            icon={<EditRegular />}
            onClick={() => onEdit?.(driver)}
          >
            Edit driver
          </Button>
        </div>
      </DialogSurface>
    </Dialog>
  );
}
