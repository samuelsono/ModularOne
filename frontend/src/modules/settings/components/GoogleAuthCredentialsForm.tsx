import { useCallback } from 'react';
import { ExternalAuthCredentialsForm } from '@modules/settings/components/ExternalAuthCredentialsForm';
import {
  getGoogleAuthSettings,
  updateGoogleAuthSettings,
} from '@modules/settings/services/settingsService';

export function GoogleAuthCredentialsForm() {
  const load = useCallback(() => getGoogleAuthSettings(), []);
  const save = useCallback(
    (request: { clientId: string; clientSecret?: string; tenantId?: string | null }) =>
      updateGoogleAuthSettings({
        clientId: request.clientId,
        clientSecret: request.clientSecret,
      }),
    [],
  );

  return (
    <ExternalAuthCredentialsForm
      providerLabel="Google"
      providerDescription="Connect Google OAuth so users can sign in with Google on the login page. Login activates automatically once Client ID and secret are saved."
      load={load}
      save={save}
      callbackPathHint="/api/auth/external/Google/callback"
    />
  );
}
