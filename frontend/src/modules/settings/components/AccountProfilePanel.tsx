import { useState } from 'react';
import type { SelectTabData, SelectTabEvent, TabValue } from '@fluentui/react-components';
import {
  Avatar,
  Badge,
  Tab,
  TabList,
} from '@fluentui/react-components';
import {
  BuildingRegular,
  MailRegular,
  PersonKeyRegular,
  PersonRegular,
  ShieldRegular,
} from '@fluentui/react-icons';
import { useAuth } from '@platform/auth/AuthContext';
import {
  DetailRow,
  DetailSection,
  detailPanelBodyClassName,
  detailPanelHeaderClassName,
  detailPanelHeaderStyle,
  detailPanelShellClassName,
  detailPanelShellStyle,
} from '@platform/ui/DetailLayout';

export function AccountProfilePanel() {
  const { user } = useAuth();
  const [selectedTab, setSelectedTab] = useState<TabValue>('overview');

  if (!user) {
    return null;
  }

  const displayName = user.displayName ?? user.username;

  return (
    <div className={detailPanelShellClassName} style={detailPanelShellStyle}>
      
      <div className={detailPanelHeaderClassName} style={detailPanelHeaderStyle}>
        <div className="flex items-start gap-4">
          <Avatar name={displayName} color="brand" size={72} />
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="text-2xl font-semibold text-neutral-foreground-1">
                {displayName}
              </h2>
              {user.twoFactorEnabled && (
                <Badge color="success" appearance="filled">2FA enabled</Badge>
              )}
              {user.mustChangePassword && (
                <Badge color="warning" appearance="filled">Password change required</Badge>
              )}
            </div>
            <p className="mt-1 text-sm text-neutral-foreground-2">
              {user.username} • {user.email}
            </p>
            <div className="mt-3 flex flex-wrap gap-1">
              {user.roles.map((role) => (
                <Badge key={role} appearance="outline" size="small">{role}</Badge>
              ))}
            </div>
          </div>
        </div>

        <TabList
          className="mt-5"
          selectedValue={selectedTab}
          onTabSelect={(_event: SelectTabEvent, data: SelectTabData) => setSelectedTab(data.value)}
        >
          <Tab value="overview">Overview</Tab>
          <Tab value="access">Access</Tab>
        </TabList>
      </div>

      <div className={detailPanelBodyClassName}>
        {selectedTab === 'overview' && (
          <>
            <DetailSection title="Account">
              <DetailRow icon={<PersonRegular fontSize={16} />} label="Display name" value={user.displayName ?? '—'} />
              <DetailRow icon={<PersonRegular fontSize={16} />} label="Username" value={user.username} />
              <DetailRow icon={<MailRegular fontSize={16} />} label="Email" value={user.email} />
            </DetailSection>

            <DetailSection title="Security">
              <DetailRow
                icon={<ShieldRegular fontSize={16} />}
                label="Two-factor auth"
                value={user.twoFactorEnabled ? 'Enabled' : 'Not enabled'}
              />
              <DetailRow
                icon={<PersonKeyRegular fontSize={16} />}
                label="Password status"
                value={user.mustChangePassword ? 'Change required on next sign-in' : 'Up to date'}
              />
            </DetailSection>
          </>
        )}

        {selectedTab === 'access' && (
          <>
            <DetailSection title="Roles">
              <DetailRow icon={<BuildingRegular fontSize={16} />} label="Assigned roles" value={user.roles.join(', ') || '—'} />
            </DetailSection>

            <DetailSection title="Modules">
              <DetailRow icon={<BuildingRegular fontSize={16} />} label="Available modules" value={user.modules.join(', ') || '—'} />
            </DetailSection>
          </>
        )}
      </div>
    </div>
  );
}
