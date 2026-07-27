import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { getStoredAccessToken } from './tokenStorage';

// Aspire Host HTTP endpoint (see CarTrack.AppHost/AppHost.cs)
// Android emulator: use http://10.0.2.2:51705/api
const API_BASE_URL = 'http://localhost:51705/api';

declare module 'axios' {
  export interface AxiosRequestConfig {
    skipAuthRefresh?: boolean;
    _retry?: boolean;
  }
}

export const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const setAuthToken = (token: string | null) => {
  if (token) {
    api.defaults.headers.common.Authorization = `Bearer ${token}`;
  } else {
    delete api.defaults.headers.common.Authorization;
  }
};

api.interceptors.request.use((config) => {
  const token = getStoredAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  } else {
    delete config.headers.Authorization;
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as InternalAxiosRequestConfig | undefined;
    const status = error.response?.status;

    if (!original || status !== 401 || original.skipAuthRefresh || original._retry) {
      return Promise.reject(error);
    }

    original._retry = true;

    try {
      // Lazy import avoids circular init issues between api and authService.
      const { refreshSession } = await import('./authService');
      const accessToken = await refreshSession();
      original.headers.Authorization = `Bearer ${accessToken}`;
      return api(original);
    } catch (refreshError) {
      return Promise.reject(refreshError);
    }
  },
);
