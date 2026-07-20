export type NotificationCategory = 'SystemAction' | 'AdminBroadcast';

export type NotificationActionType =
  | 'VehicleCreated'
  | 'VehicleUpdated'
  | 'VehicleDeleted'
  | 'VehicleSynced'
  | 'DriverCreated'
  | 'DriverUpdated'
  | 'DriverDeleted'
  | 'DriverAssigned'
  | 'DriverUnassigned'
  | 'LeaveSubmitted'
  | 'LeaveApproved'
  | 'LeaveRejected'
  | 'LeaveCancelled'
  | 'TenderMatchFound'
  | 'Custom';

export interface NotificationRecipientDto {
  recipientId: string;
  notificationId: string;
  title: string;
  body: string;
  category: NotificationCategory;
  actionType: NotificationActionType | null;
  relatedEntityId: string | null;
  isRead: boolean;
  isArchived: boolean;
  createdAt: string;
  readAt: string | null;
}

export type NotificationTarget =
  | { type: 'Everyone' }
  | { type: 'Group'; value: string }
  | { type: 'User'; value: string };

export interface BroadcastPayload {
  title: string;
  body: string;
  target: NotificationTarget;
}

export interface AuthUserLookup {
  id: string;
  displayName: string;
  email: string;
}
