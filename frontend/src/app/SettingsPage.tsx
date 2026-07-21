import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { tokens, Button, Subtitle2, Text } from '@fluentui/react-components';
import {
  Dismiss24Regular,
  PersonRegular,
  PlugConnectedRegular,
  SettingsRegular,
  PeopleTeamRegular,
  CheckmarkCircleRegular,
  ChatHelpRegular,
} from '@fluentui/react-icons';
import { usePermissions } from '@platform/permissions/usePermissions';
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import { ApprovalsStubPanel } from '@modules/expense/components/ApprovalsStubPanel';
import { AccountProfilePanel } from '@modules/settings/components/AccountProfilePanel';
import { AccountSecurityPanel } from '@modules/settings/components/AccountSecurityPanel';
import { CarTrackCredentialsForm } from '@modules/settings/components/CarTrackCredentialsForm';
import { GoogleAuthCredentialsForm } from '@modules/settings/components/GoogleAuthCredentialsForm';
import { MicrosoftAuthCredentialsForm } from '@modules/settings/components/MicrosoftAuthCredentialsForm';
import { SecurityAuditPanel } from '@modules/users/components/SecurityAuditPanel';
import { PlatformApplicationsPanel } from '@modules/settings/components/PlatformApplicationsPanel';
import { InstalledAppsPanel } from '@modules/settings/components/InstalledAppsPanel';
import { ExpenseMileageRatePanel } from '@modules/settings/components/ExpenseMileageRatePanel';
import { UsersSettingsPanel } from '@modules/users/components/UsersSettingsPanel';
import type { UserListItem } from '@modules/users/types/user';
import { SendNotificationDialog } from '@modules/notifications/components/SendNotificationDialog';
import { SupportTicketsManager } from '@modules/support/components/SupportTicketsManager';
import { TicketCategoriesManager } from '@modules/support/components/TicketCategoriesManager';
import { HelpArticlesManager } from '@modules/help/components/HelpArticlesManager';
import { AdjustUserLeaveBalancesDialog } from './components/AdjustUserLeaveBalancesDialog';

type SettingsCategoryId = 'general' | 'integrations' | 'account' | 'access' | 'approvals' | 'helpSupport';
type SettingsSectionId =
  | 'appearance'
  | 'applications'
  | 'installedApps'
  | 'expenseMileageRate'
  | 'notifications'
  | 'cartrack'
  | 'googleAuth'
  | 'microsoftAuth'
  | 'profile'
  | 'security'
  | 'users'
  | 'auditLog'
  | 'approvalQueues'
  | 'supportTickets'
  | 'ticketCategories'
  | 'helpArticles';

interface SettingsSection {
  id: SettingsSectionId;
  label: string;
}

interface SettingsCategory {
  id: SettingsCategoryId;
  label: string;
  icon: typeof SettingsRegular;
  sections: SettingsSection[];
}

const SETTINGS_CATEGORIES: SettingsCategory[] = [
  {
    id: 'general',
    label: 'General',
    icon: SettingsRegular,
    sections: [
      { id: 'appearance', label: 'Appearance' },
      { id: 'applications', label: 'Applications' },
      { id: 'installedApps', label: 'Installed apps' },
      { id: 'expenseMileageRate', label: 'Expense mileage rate' },
      { id: 'notifications', label: 'Notifications' },
    ],
  },
  {
    id: 'integrations',
    label: 'Integrations',
    icon: PlugConnectedRegular,
    sections: [
      { id: 'cartrack', label: 'CarTrack API' },
      { id: 'googleAuth', label: 'Google Auth' },
      { id: 'microsoftAuth', label: 'Microsoft Auth' },
    ],
  },
  {
    id: 'account',
    label: 'Account',
    icon: PersonRegular,
    sections: [
      { id: 'profile', label: 'Profile' },
      { id: 'security', label: 'Security' },
    ],
  },
  {
    id: 'access',
    label: 'Users & access',
    icon: PeopleTeamRegular,
    sections: [
      { id: 'users', label: 'Users' },
      { id: 'auditLog', label: 'Audit log' },
    ],
  },
  {
    id: 'approvals',
    label: 'Approvals',
    icon: CheckmarkCircleRegular,
    sections: [
      { id: 'approvalQueues', label: 'Queues' },
    ],
  },
  {
    id: 'helpSupport',
    label: 'Help & Support',
    icon: ChatHelpRegular,
    sections: [
      { id: 'helpArticles', label: 'Help articles' },
      { id: 'supportTickets', label: 'Support tickets' },
      { id: 'ticketCategories', label: 'Ticket categories' },
    ],
  },
];

function GeneralAppearancePanel() {
  return (
    <div className="max-w-xl flex flex-col gap-3">
      <Subtitle2>Appearance</Subtitle2>
      <Text className="text-sm text-neutral-foreground-3">
        App brand themes are configured under Installed applications. Use the sun / moon / desktop button in the
        navbar to switch between light, dark, and system color modes for the active app theme.
      </Text>
    </div>
  );
}

function GeneralNotificationsPanel() {
  return (
    <div className="max-w-xl flex flex-col gap-4">
      <Subtitle2>Notifications</Subtitle2>
      <Text className="text-sm text-neutral-foreground-3">
        View incoming alerts from the bell icon in the top navigation bar. Admins can broadcast messages to users, groups, or everyone below.
      </Text>
      <SendNotificationDialog />
    </div>
  );
}

function AccountProfileSection() {
  return <AccountProfilePanel />;
}

function SettingsContent({
  sectionId,
  searchQuery,
  onAdjustLeave,
}: {
  sectionId: SettingsSectionId;
  searchQuery: string;
  onAdjustLeave: (user: UserListItem) => void;
}) {
  switch (sectionId) {
    case 'appearance':
      return <GeneralAppearancePanel />;
    case 'applications':
      return <PlatformApplicationsPanel />;
    case 'installedApps':
      return <InstalledAppsPanel />;
    case 'expenseMileageRate':
      return <ExpenseMileageRatePanel />;
    case 'notifications':
      return <GeneralNotificationsPanel />;
    case 'cartrack':
      return <CarTrackCredentialsForm />;
    case 'googleAuth':
      return <GoogleAuthCredentialsForm />;
    case 'microsoftAuth':
      return <MicrosoftAuthCredentialsForm />;
    case 'profile':
      return <AccountProfileSection />;
    case 'security':
      return <AccountSecurityPanel />;
    case 'users':
      return (
        <UsersSettingsPanel
          searchQuery={searchQuery}
          onAdjustLeave={onAdjustLeave}
        />
      );
    case 'auditLog':
      return <SecurityAuditPanel />;
    case 'approvalQueues':
      return <ApprovalsStubPanel />;
    case 'supportTickets':
      return <SupportTicketsManager />;
    case 'ticketCategories':
      return <TicketCategoriesManager />;
    case 'helpArticles':
      return <HelpArticlesManager />;
    default:
      return null;
  }
}

export default function SettingsPage() {
  const navigate = useNavigate();
  const search = usePageSearchQuery();
  const { canManageUsers, hasPermission, isAdmin } = usePermissions();
  const canEditPlatformSettings = hasPermission('platform.settings.write');
  const canEditExpenseSettings = hasPermission('expense.settings.write');
  const canManageSupport = hasPermission('platform.support.write');
  const canManageHelp = hasPermission('platform.help.write');
  const [activeCategoryId, setActiveCategoryId] = useState<SettingsCategoryId>('integrations');
  const [activeSectionId, setActiveSectionId] = useState<SettingsSectionId>('cartrack');
  const [leaveAdjustmentUser, setLeaveAdjustmentUser] = useState<UserListItem | null>(null);

  const availableCategories = useMemo(
    () => SETTINGS_CATEGORIES
      .filter((category) => {
        if (category.id === 'access') {
          // Users & Access: Admin and HR only (via platform.settings.users.*).
          return canManageUsers;
        }

        if (category.id === 'approvals') {
          return isAdmin;
        }

        if (category.id === 'integrations') {
          return isAdmin;
        }

        if (category.id === 'helpSupport') {
          return canManageSupport || canManageHelp;
        }

        return true;
      })
      .map((category) => ({
        ...category,
        sections: category.sections.filter((section) => {
          if (section.id === 'applications' || section.id === 'installedApps') {
            return canEditPlatformSettings;
          }

          if (section.id === 'expenseMileageRate') {
            return canEditExpenseSettings;
          }

          if (section.id === 'supportTickets' || section.id === 'ticketCategories') {
            return canManageSupport;
          }

          if (section.id === 'helpArticles') {
            return canManageHelp;
          }

          return true;
        }),
      }))
      .filter((category) => category.sections.length > 0),
    [canManageUsers, isAdmin, canEditPlatformSettings, canEditExpenseSettings, canManageSupport, canManageHelp],
  );

  const filteredCategories = useMemo(() => {
    const normalized = search.trim().toLowerCase();
    if (!normalized) {
      return availableCategories;
    }

    return availableCategories
      .map((category) => ({
        ...category,
        sections: category.sections.filter(
          (section) =>
            section.label.toLowerCase().includes(normalized)
            || category.label.toLowerCase().includes(normalized),
        ),
      }))
      .filter(
        (category) =>
          category.label.toLowerCase().includes(normalized) || category.sections.length > 0,
      );
  }, [availableCategories, search]);

  const activeCategory = availableCategories.find((category) => category.id === activeCategoryId)
    ?? availableCategories[0];

  const activeSection = activeCategory.sections.find((section) => section.id === activeSectionId)
    ?? activeCategory.sections[0];

  function selectCategory(category: SettingsCategory) {
    setActiveCategoryId(category.id);
    setActiveSectionId(category.sections[0]?.id ?? 'cartrack');
  }

  return (
    <div className="flex flex-col h-full" style={{ backgroundColor: tokens.colorNeutralBackground1 }}>
      <div className="flex items-center justify-between px-4 py-3 border-b border-neutral-stroke-2">
        <Subtitle2>Settings</Subtitle2>
        <Button
          appearance="subtle"
          icon={<Dismiss24Regular />}
          aria-label="Close settings"
          onClick={() => navigate(-1)}
        />
      </div>

      <div className="flex flex-1 min-h-0">
        <aside className="w-[220px] shrink-0 border-r border-neutral-stroke-2 p-3 flex flex-col gap-3">
          <nav className="flex flex-col gap-1">
            {filteredCategories.map((category) => {
              const Icon = category.icon;
              const isActive = category.id === activeCategoryId;

              return (
                <button
                  key={category.id}
                  type="button"
                  onClick={() => selectCategory(category)}
                  className={`flex items-center gap-2 px-3 py-2 text-left text-sm border-l-2 ${
                    isActive
                      ? 'border-neutral-foreground-1 bg-neutral-background-2 font-semibold'
                      : 'border-transparent hover:bg-neutral-background-2'
                  }`}
                >
                  <Icon className="size-4 shrink-0" />
                  <span>{category.label}</span>
                </button>
              );
            })}
          </nav>
        </aside>

        <aside className="w-[220px] shrink-0 border-r border-neutral-stroke-2 py-4">
          <nav className="flex flex-col">
            {activeCategory.sections.map((section) => {
              const isActive = section.id === activeSection?.id;

              return (
                <button
                  key={section.id}
                  type="button"
                  onClick={() => setActiveSectionId(section.id)}
                  className={`text-left px-4 py-2.5 text-sm border-l-2 ${
                    isActive
                      ? 'border-brand-background-1 bg-brand-background-2/30 font-semibold'
                      : 'border-transparent hover:bg-neutral-background-2'
                  }`}
                >
                  {section.label}
                </button>
              );
            })}
          </nav>
        </aside>

        <main className="flex-1 min-w-0 overflow-y-auto p-6">
          {activeSection?.id !== 'profile' && (
            <Subtitle2 className="mb-4 block">{activeSection?.label}</Subtitle2>
          )}
          {activeSection && (
            <SettingsContent
              sectionId={activeSection.id}
              searchQuery={search}
              onAdjustLeave={setLeaveAdjustmentUser}
            />
          )}
        </main>
      </div>

      <AdjustUserLeaveBalancesDialog
        open={leaveAdjustmentUser !== null}
        user={leaveAdjustmentUser}
        onClose={() => setLeaveAdjustmentUser(null)}
      />
    </div>
  );
}
