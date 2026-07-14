export interface SecurityAuditLogEntry {
  id: string;
  actorUserId: string;
  actorDisplayName: string | null;
  targetUserId: string | null;
  targetDisplayName: string | null;
  action: string;
  details: string | null;
  ipAddress: string | null;
  createdAt: string;
}

export interface SecurityAuditLogResponse {
  items: SecurityAuditLogEntry[];
}
