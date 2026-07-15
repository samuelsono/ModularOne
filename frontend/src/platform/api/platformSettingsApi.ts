import { authorizedFetch } from '@platform/api/authService';

/** Minimal platform settings read for shell bootstrap (avoids importing @modules/settings). */

export interface PlatformSettingsSlice {
  defaultModuleSlug: string;
  updatedAt: string | null;
}

export interface SettingsOverviewSlice {
  platform: PlatformSettingsSlice;
}

export function getSettingsOverview(): Promise<SettingsOverviewSlice> {
  return authorizedFetch<SettingsOverviewSlice>('/api/settings');
}
