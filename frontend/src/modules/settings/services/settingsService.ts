import type {
  AppSettingsOverview,
  CarTrackSettings,
  PlatformSettings,
  TestCarTrackConnectionResponse,
  UpdateCarTrackSettingsRequest,
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

export async function updatePlatformSettings(
  request: UpdatePlatformSettingsRequest,
): Promise<PlatformSettings> {
  return authorizedFetch<PlatformSettings>('/api/settings/platform', {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}
