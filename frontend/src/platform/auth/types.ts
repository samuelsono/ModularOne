export interface AuthUser {
  id: string;
  username: string;
  email: string;
  displayName: string | null;
  roles: string[];
  permissions: string[];
  modules: string[];
  mustChangePassword: boolean;
  twoFactorEnabled: boolean;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: AuthUser;
}

export interface LoginResponse {
  requiresTwoFactor: boolean;
  mfaToken: string | null;
  session: AuthResponse | null;
}

export interface LoginRequest {
  username: string;
  password: string;
  rememberMe?: boolean;
}

export interface MfaLoginRequest {
  mfaToken: string;
  code: string;
  rememberMe?: boolean;
}

export interface RefreshTokenRequest {
  refreshToken: string;
  rememberMe?: boolean;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface MfaSetupResponse {
  sharedKey: string;
  authenticatorUri: string;
}

export interface MfaVerifyRequest {
  code: string;
}

export interface MfaDisableRequest {
  password: string;
  code: string;
}

export interface MessageResponse {
  message: string;
}

export interface ApiProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}
