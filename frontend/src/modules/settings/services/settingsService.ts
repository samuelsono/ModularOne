import type {
  AppSettingsOverview,
  CarTrackSettings,
  ExpenseSettings,
  ExternalAuthSettings,
  PlatformSettings,
  TestCarTrackConnectionResponse,
  UpdateCarTrackSettingsRequest,
  UpdateExpenseSettingsRequest,
  UpdateExternalAuthSettingsRequest,
  UpdatePlatformSettingsRequest,
} from '@modules/settings/types/settings';
import { authorizedFetch } from '@platform/api/authService';

export async function getSettingsOverview(): Promise<AppSettingsOverview> {
  return authorizedFetch<AppSettingsOverview>('/api/settings');
}

export async function getCarTrackSettings(): Promise<CarTrackSettings> {
  return authorizedFetch<CarTrackSettings>('/api/settings/cartrack');
}

export async function updateCarTrackSettings(
  request: UpdateCarTrackSettingsRequest,
): Promise<CarTrackSettings> {
  return authorizedFetch<CarTrackSettings>('/api/settings/cartrack', {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export async function testCarTrackConnection(): Promise<TestCarTrackConnectionResponse> {
  return authorizedFetch<TestCarTrackConnectionResponse>('/api/settings/cartrack/test', {
    method: 'POST',
  });
}

export async function getPlatformSettings(): Promise<PlatformSettings> {
  return authorizedFetch<PlatformSettings>('/api/settings/platform');
}

export async function getExpenseSettings(): Promise<ExpenseSettings> {
  return authorizedFetch<ExpenseSettings>('/api/expense/settings');
}

export async function updateExpenseSettings(
  request: UpdateExpenseSettingsRequest,
): Promise<ExpenseSettings> {
  return authorizedFetch<ExpenseSettings>('/api/expense/settings', {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export async function updatePlatformSettings(
  request: UpdatePlatformSettingsRequest,
): Promise<PlatformSettings> {
  return authorizedFetch<PlatformSettings>('/api/settings/platform', {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export async function getGoogleAuthSettings(): Promise<ExternalAuthSettings> {
  return authorizedFetch<ExternalAuthSettings>('/api/settings/auth/google');
}

export async function updateGoogleAuthSettings(
  request: UpdateExternalAuthSettingsRequest,
): Promise<ExternalAuthSettings> {
  return authorizedFetch<ExternalAuthSettings>('/api/settings/auth/google', {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export async function getMicrosoftAuthSettings(): Promise<ExternalAuthSettings> {
  return authorizedFetch<ExternalAuthSettings>('/api/settings/auth/microsoft');
}

export async function updateMicrosoftAuthSettings(
  request: UpdateExternalAuthSettingsRequest,
): Promise<ExternalAuthSettings> {
  return authorizedFetch<ExternalAuthSettings>('/api/settings/auth/microsoft', {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}
