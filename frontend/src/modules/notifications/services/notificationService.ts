import type {
  AuthUserLookup,
  BroadcastPayload,
  NotificationRecipientDto,
} from '@modules/notifications/types/notification';
import { authorizedFetch } from '@platform/api/authService';

export async function getNotifications(
  archived = false,
): Promise<NotificationRecipientDto[]> {
  const query = archived ? '?archived=true' : '';
  return authorizedFetch<NotificationRecipientDto[]>(`/api/notifications${query}`);
}

export async function getUnreadCount(): Promise<{ count: number }> {
  return authorizedFetch<{ count: number }>('/api/notifications/unread-count');
}

export async function markRead(recipientId: string): Promise<NotificationRecipientDto> {
  return authorizedFetch<NotificationRecipientDto>(
    `/api/notifications/${encodeURIComponent(recipientId)}/read`,
    { method: 'POST' },
  );
}

export async function markAllRead(): Promise<{ count: number }> {
  return authorizedFetch<{ count: number }>('/api/notifications/read-all', {
    method: 'POST',
  });
}

export async function archiveNotification(
  recipientId: string,
): Promise<NotificationRecipientDto> {
  return authorizedFetch<NotificationRecipientDto>(
    `/api/notifications/${encodeURIComponent(recipientId)}/archive`,
    { method: 'POST' },
  );
}

export async function deleteNotification(recipientId: string): Promise<void> {
  await authorizedFetch<void>(
    `/api/notifications/${encodeURIComponent(recipientId)}`,
    { method: 'DELETE' },
  );
}

export async function broadcastNotification(
  payload: BroadcastPayload,
): Promise<NotificationRecipientDto[]> {
  return authorizedFetch<NotificationRecipientDto[]>('/api/notifications/broadcast', {
    method: 'POST',
    body: JSON.stringify({
      title: payload.title,
      body: payload.body,
      target: {
        type: payload.target.type,
        value: 'value' in payload.target ? payload.target.value : null,
      },
    }),
  });
}

export async function getAuthUsersForBroadcast(): Promise<AuthUserLookup[]> {
  return authorizedFetch<AuthUserLookup[]>('/api/auth/users');
}
