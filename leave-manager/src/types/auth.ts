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

export interface AuthSession {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: AuthUser;
}

export interface LoginResponse {
  requiresTwoFactor: boolean;
  mfaToken: string | null;
  session: AuthSession | null;
}

export interface LoginRequest {
  username: string;
  password: string;
  rememberMe?: boolean;
}

export interface RefreshTokenRequest {
  refreshToken: string;
  rememberMe?: boolean;
}
