export interface CarTrackSettings {
  baseUrl: string;
  username: string;
  hasPassword: boolean;
  updatedAt: string | null;
}

export interface UpdateCarTrackSettingsRequest {
  baseUrl: string;
  username: string;
  password?: string;
}

export interface TestCarTrackConnectionResponse {
  success: boolean;
  message: string;
}

export interface AppSettingsOverview {
  applicationName: string;
  version: string;
  carTrack: CarTrackSettings;
  platform: PlatformSettings;
}

export interface PlatformSettings {
  defaultModuleSlug: string;
  updatedAt: string | null;
}

export interface UpdatePlatformSettingsRequest {
  defaultModuleSlug: string;
}
