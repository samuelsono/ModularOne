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
  installedAppSlugs: string[];
  defaultThemeName: string;
  appThemeNamesByModuleSlug: Record<string, string>;
  updatedAt: string | null;
}

export interface ExpenseSettings {
  kilometerRate: number;
  updatedAt: string | null;
}

export interface UpdateExpenseSettingsRequest {
  kilometerRate: number;
}

export interface UpdatePlatformSettingsRequest {
  defaultModuleSlug?: string;
  installedAppSlugs?: string[];
  defaultThemeName?: string;
  appThemeNamesByModuleSlug?: Record<string, string>;
}

export interface ExternalAuthSettings {
  provider: string;
  clientId: string;
  tenantId: string | null;
  hasClientSecret: boolean;
  isActivated: boolean;
  updatedAt: string | null;
}

export interface UpdateExternalAuthSettingsRequest {
  clientId: string;
  clientSecret?: string;
  tenantId?: string | null;
}

export interface ExternalAuthProviderStatus {
  provider: string;
  isActivated: boolean;
}
