import React from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuth } from './AuthContext';
import * as notificationService from '../services/notificationService';
import { getAccessToken } from '../services/tokenStorage';
import type { NotificationRecipientDto } from '../types/notification';

interface NotificationContextValue {
  notifications: NotificationRecipientDto[];
  unreadCount: number;
  isLoading: boolean;
  refreshInbox: (archived?: boolean) => Promise<void>;
  markRead: (recipientId: string) => Promise<void>;
  markAllRead: () => Promise<void>;
  archive: (recipientId: string) => Promise<void>;
}

const NotificationContext = React.createContext<NotificationContextValue | undefined>(undefined);

function sortNotifications(items: NotificationRecipientDto[]): NotificationRecipientDto[] {
  return [...items].sort(
    (left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime(),
  );
}

export function NotificationProvider({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth();
  const [notifications, setNotifications] = React.useState<NotificationRecipientDto[]>([]);
  const [unreadCount, setUnreadCount] = React.useState(0);
  const [isLoading, setIsLoading] = React.useState(false);
  const connectionRef = React.useRef<signalR.HubConnection | null>(null);

  const refreshUnreadCount = React.useCallback(async () => {
    const response = await notificationService.getUnreadCount();
    setUnreadCount(response.count);
  }, []);

  const refreshInbox = React.useCallback(async (archived = false) => {
    setIsLoading(true);
    try {
      const inbox = await notificationService.getNotifications(archived);
      setNotifications(sortNotifications(inbox));
      if (!archived) {
        await refreshUnreadCount();
      }
    } finally {
      setIsLoading(false);
    }
  }, [refreshUnreadCount]);

  const upsertNotification = React.useCallback((incoming: NotificationRecipientDto) => {
    setNotifications((current) => {
      const withoutExisting = current.filter(
        (item) => item.recipientId !== incoming.recipientId,
      );
      return sortNotifications([incoming, ...withoutExisting]);
    });
  }, []);

  React.useEffect(() => {
    if (!isAuthenticated) {
      setNotifications([]);
      setUnreadCount(0);
      if (connectionRef.current) {
        void connectionRef.current.stop();
        connectionRef.current = null;
      }
      return;
    }

    let cancelled = false;

    async function bootstrap() {
      try {
        await refreshInbox(false);
      } catch {
        if (!cancelled) {
          setNotifications([]);
          setUnreadCount(0);
        }
      }
    }

    void bootstrap();

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        accessTokenFactory: () => getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    connection.on('notification', (incoming: NotificationRecipientDto) => {
      upsertNotification(incoming);
      void refreshUnreadCount();
    });

    connectionRef.current = connection;

    void connection.start().catch(() => {
      // REST inbox still works if the hub is unavailable.
    });

    return () => {
      cancelled = true;
      void connection.stop();
      connectionRef.current = null;
    };
  }, [isAuthenticated, refreshInbox, refreshUnreadCount, upsertNotification]);

  const markRead = React.useCallback(async (recipientId: string) => {
    const updated = await notificationService.markRead(recipientId);
    setNotifications((current) =>
      current.map((item) => (item.recipientId === recipientId ? updated : item)),
    );
    await refreshUnreadCount();
  }, [refreshUnreadCount]);

  const markAllRead = React.useCallback(async () => {
    await notificationService.markAllRead();
    setNotifications((current) =>
      current.map((item) => ({
        ...item,
        isRead: true,
        readAt: item.readAt ?? new Date().toISOString(),
      })),
    );
    setUnreadCount(0);
  }, []);

  const archive = React.useCallback(async (recipientId: string) => {
    const updated = await notificationService.archiveNotification(recipientId);
    setNotifications((current) => current.filter((item) => item.recipientId !== recipientId));
    if (!updated.isRead) {
      await refreshUnreadCount();
    }
  }, [refreshUnreadCount]);

  const value = React.useMemo<NotificationContextValue>(() => ({
    notifications,
    unreadCount,
    isLoading,
    refreshInbox,
    markRead,
    markAllRead,
    archive,
  }), [notifications, unreadCount, isLoading, refreshInbox, markRead, markAllRead, archive]);

  return (
    <NotificationContext.Provider value={value}>
      {children}
    </NotificationContext.Provider>
  );
}

export function useNotifications(): NotificationContextValue {
  const context = React.useContext(NotificationContext);
  if (!context) {
    throw new Error('useNotifications must be used within NotificationProvider');
  }

  return context;
}
