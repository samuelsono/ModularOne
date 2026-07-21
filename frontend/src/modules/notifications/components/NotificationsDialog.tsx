import { useCallback, useEffect, useMemo, useState } from 'react';
import type { JSXElement, SelectTabData, SelectTabEvent, TabValue } from '@fluentui/react-components';
import { tokens, Button, Menu, MenuItem, MenuList, MenuPopover, MenuTrigger, OverlayDrawer, DrawerBody, DrawerHeader, DrawerHeaderTitle, Spinner, Tab, TabList, Text } from '@fluentui/react-components';
import {
  AlertRegular,
  ArchiveRegular,
  CheckmarkCircleRegular,
  Dismiss24Regular,
  MoreHorizontalRegular,
} from '@fluentui/react-icons';
import { useNotifications } from '@modules/notifications/context/NotificationContext';
import type { NotificationRecipientDto } from '@modules/notifications/types/notification';
import { theme } from '../../../theme';

function formatRelativeTime(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '--';
  }

  const diffMs = Date.now() - date.getTime();
  const minutes = Math.floor(diffMs / 60000);

  if (minutes < 1) {
    return 'Just now';
  }
  if (minutes < 60) {
    return `${minutes} min ago`;
  }

  const hours = Math.floor(minutes / 60);
  if (hours < 24) {
    return `${hours} hr ago`;
  }

  const days = Math.floor(hours / 24);
  return `${days} day${days === 1 ? '' : 's'} ago`;
}

function NotificationRow({
  notification,
  onMarkRead,
  onArchive,
}: {
  notification: NotificationRecipientDto;
  onMarkRead: (recipientId: string) => Promise<void>;
  onArchive: (recipientId: string) => Promise<void>;
}) {
  return (
    <div
      className={`border-b px-4 py-3 ${
        notification.isRead ? '' : 'border-l-4 border-l-brand-stroke-1'
      }`}
      style={{
        borderBottomColor: tokens.colorNeutralStroke3,
        backgroundColor: notification.isRead
          ? tokens.colorNeutralBackground1
          : tokens.colorBrandBackground2,
      }}
    >
      <div className="flex items-start gap-2">
        <div className="min-w-0 flex-1">
          <div className="flex items-start justify-between gap-2">
            <p className={`text-sm ${notification.isRead ? 'font-medium' : 'font-semibold'} text-neutral-foreground-1`}>
              {notification.title}
            </p>
            <span className="shrink-0 text-xs text-neutral-foreground-3">
              {formatRelativeTime(notification.createdAt)}
            </span>
          </div>
          <p className="mt-1 line-clamp-2 text-xs text-neutral-foreground-2">
            {notification.body}
          </p>
        </div>

        <Menu>
          <MenuTrigger disableButtonEnhancement>
            <Button
              appearance="subtle"
              size="small"
              aria-label="Notification actions"
              icon={<MoreHorizontalRegular />}
            />
          </MenuTrigger>
          <MenuPopover>
            <MenuList>
              {!notification.isRead && (
                <MenuItem
                  icon={<CheckmarkCircleRegular />}
                  onClick={() => void onMarkRead(notification.recipientId)}
                >
                  Mark as read
                </MenuItem>
              )}
              <MenuItem
                icon={<ArchiveRegular />}
                onClick={() => void onArchive(notification.recipientId)}
              >
                Archive
              </MenuItem>
            </MenuList>
          </MenuPopover>
        </Menu>
      </div>
    </div>
  );
}

export const NotificationsDialog = (): JSXElement => {
  const {
    notifications,
    unreadCount,
    isLoading,
    refreshInbox,
    markRead,
    markAllRead,
    archive,
  } = useNotifications();

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [selectedTab, setSelectedTab] = useState<TabValue>('inbox');

  const showArchived = selectedTab === 'archived';

  useEffect(() => {
    if (drawerOpen) {
      void refreshInbox(showArchived);
    }
  }, [drawerOpen, showArchived, refreshInbox]);

  const handleTabSelect = useCallback((_event: SelectTabEvent, data: SelectTabData) => {
    setSelectedTab(data.value);
  }, []);

  const visibleNotifications = useMemo(
    () => notifications.filter((item) => item.isArchived === showArchived),
    [notifications, showArchived],
  );

  return (
    <>
      <div className="relative inline-flex">
        <Button
          appearance="transparent"
          aria-label="Notifications"
          icon={<AlertRegular />}
          onClick={() => setDrawerOpen(true)}
          style={{ color: theme.colorBrandBackgroundInverted }}
        />
        {unreadCount > 0 && (
          <span className="pointer-events-none absolute right-0 top-0 min-w-[18px] -translate-y-1 translate-x-1 rounded-full bg-[#b10e1c] px-1 text-center text-[10px] leading-4 text-white">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </div>

      <OverlayDrawer
        open={drawerOpen}
        position="end"
        size="medium"
        onOpenChange={(_, { open }) => setDrawerOpen(open)}
      >
        <DrawerHeader>
          <DrawerHeaderTitle
            action={
              <Button
                appearance="subtle"
                aria-label="Close"
                icon={<Dismiss24Regular />}
                onClick={() => setDrawerOpen(false)}
              />
            }
          >
            Notifications
          </DrawerHeaderTitle>
        </DrawerHeader>

        <DrawerBody className="flex min-h-0 flex-col p-0!">
          <div
            className="flex items-center justify-between border-b px-4 py-2"
            style={{ borderBottomColor: tokens.colorNeutralStroke3 }}
          >
            <TabList selectedValue={selectedTab} onTabSelect={handleTabSelect}>
              <Tab value="inbox">Inbox</Tab>
              <Tab value="archived">Archived</Tab>
            </TabList>
            {!showArchived && (
              <Button
                appearance="subtle"
                size="small"
                disabled={unreadCount === 0}
                onClick={() => void markAllRead()}
              >
                Mark all as read
              </Button>
            )}
          </div>

          {isLoading && (
            <div className="flex justify-center py-10">
              <Spinner size="medium" label="Loading notifications..." />
            </div>
          )}

          {!isLoading && visibleNotifications.length === 0 && (
            <div className="px-4 py-12 text-center">
              <Text weight="semibold">
                {showArchived ? 'No archived notifications' : "You're all caught up!"}
              </Text>
              <p className="mt-1 text-sm text-neutral-foreground-3">
                {showArchived
                  ? 'Archived notifications will appear here.'
                  : 'You have no new notifications.'}
              </p>
              <AlertRegular className="mx-auto mt-3" style={{ fontSize: '48px', color: '#0078D4' }} />
            </div>
          )}

          {!isLoading && visibleNotifications.length > 0 && (
            <div className="min-h-0 flex-1 overflow-y-auto">
              {visibleNotifications.map((notification) => (
                <NotificationRow
                  key={notification.recipientId}
                  notification={notification}
                  onMarkRead={markRead}
                  onArchive={archive}
                />
              ))}
            </div>
          )}
        </DrawerBody>
      </OverlayDrawer>
    </>
  );
};
