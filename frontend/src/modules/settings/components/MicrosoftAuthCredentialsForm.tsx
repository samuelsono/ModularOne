import { useCallback } from 'react';
import { ExternalAuthCredentialsForm } from '@modules/settings/components/ExternalAuthCredentialsForm';
import {
  getMicrosoftAuthSettings,
  updateMicrosoftAuthSettings,
} from '@modules/settings/services/settingsService';

export function MicrosoftAuthCredentialsForm() {
  const load = useCallback(() => getMicrosoftAuthSettings(), []);
  const save = useCallback(
    (request: { clientId: string; clientSecret?: string; tenantId?: string | null }) =>
      updateMicrosoftAuthSettings({
        clientId: request.clientId,
        clientSecret: request.clientSecret,
        tenantId: request.tenantId,
      }),
    [],
  );

  return (
    <ExternalAuthCredentialsForm
      providerLabel="Microsoft"
      providerDescription="Connect Microsoft Entra ID (Azure AD) so users can sign in with Microsoft. Login activates automatically once Client ID and secret are saved."
      showTenantId
      defaultTenantId="common"
      load={load}
      save={save}
      callbackPathHint="/api/auth/external/Microsoft/callback"
    />
  );
}
