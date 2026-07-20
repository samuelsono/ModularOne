import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { MessageBar, MessageBarBody, Spinner } from '@fluentui/react-components';
import { useAuth } from '@platform/auth/AuthContext';
import { usePermissions } from '@platform/permissions/usePermissions';

/** Renders children only when the user has the required permission; otherwise redirects or shows a denial. */
export function PermissionGate({
  permission,
  anyPermissions,
  children,
  fallbackPath = '/',
  showDenied = false,
}: {
  permission?: string;
  anyPermissions?: string[];
  children: ReactNode;
  fallbackPath?: string;
  showDenied?: boolean;
}) {
  const { isLoading } = useAuth();
  const { hasPermission, user } = usePermissions();

  if (isLoading || !user) {
    return <Spinner label="Checking access…" />;
  }

  const allowed = permission
    ? hasPermission(permission)
    : (anyPermissions?.some((key) => hasPermission(key)) ?? false);

  if (allowed) {
    return <>{children}</>;
  }

  if (showDenied) {
    return (
      <MessageBar intent="error" className="m-3">
        <MessageBarBody>You do not have permission to view this page.</MessageBarBody>
      </MessageBar>
    );
  }

  return <Navigate to={fallbackPath} replace />;
}

/** Wrap a route element so it never mounts (or fetches) without the given permission. */
export function withPermission(permission: string, element: ReactNode): ReactNode {
  return (
    <PermissionGate permission={permission} showDenied>
      {element}
    </PermissionGate>
  );
}

export function withAnyPermission(permissions: string[], element: ReactNode): ReactNode {
  return (
    <PermissionGate anyPermissions={permissions} showDenied>
      {element}
    </PermissionGate>
  );
}
