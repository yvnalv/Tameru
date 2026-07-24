// axios client for /api/v1: attaches the bearer token, unwraps the { success, data } envelope, and
// on 401 attempts a single refresh before giving up (docs/frontend/FRONTEND_ARCHITECTURE.md → API & auth).
import axios, {
  AxiosError,
  type AxiosInstance,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from 'axios';
import type { ApiResponse, AuthTokens } from '@/types/api';
import { clearSession, getAccessToken, getRefreshToken, setSession } from '@/lib/session';

/** A failed API call, carrying the backend's stable error code for i18n mapping. */
export class ApiClientError extends Error {
  constructor(
    public readonly code: string,
    message: string,
    public readonly status?: number,
    public readonly details?: { field: string; message: string }[] | null,
  ) {
    super(message);
    this.name = 'ApiClientError';
  }
}

// Set by the app so a hard auth failure can route to /login without api.ts importing the router.
let onUnauthorized: (() => void) | null = null;
export function setUnauthorizedHandler(handler: () => void): void {
  onUnauthorized = handler;
}

const baseURL = import.meta.env.VITE_API_BASE_URL ?? '/api/v1';

const http: AxiosInstance = axios.create({ baseURL });

http.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

let refreshInFlight: Promise<boolean> | null = null;

/** Try to rotate the refresh token exactly once; concurrent 401s share the same attempt. */
async function tryRefresh(): Promise<boolean> {
  if (refreshInFlight) return refreshInFlight;

  const refreshToken = getRefreshToken();
  if (!refreshToken) return false;

  refreshInFlight = (async () => {
    try {
      // Bare axios (no interceptors) to avoid a refresh→401→refresh loop.
      const { data } = await axios.post<ApiResponse<AuthTokens>>(
        `${baseURL}/auth/refresh`,
        { refreshToken },
      );
      if (data.success) {
        setSession(data.data);
        return true;
      }
      return false;
    } catch {
      return false;
    } finally {
      refreshInFlight = null;
    }
  })();

  return refreshInFlight;
}

interface RetriableConfig extends InternalAxiosRequestConfig {
  _retried?: boolean;
}

http.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiResponse<unknown>>) => {
    const original = error.config as RetriableConfig | undefined;
    const isAuthCall = original?.url?.includes('/auth/');

    if (error.response?.status === 401 && original && !original._retried && !isAuthCall) {
      original._retried = true;
      if (await tryRefresh()) {
        return http(original);
      }
      clearSession();
      onUnauthorized?.();
    }

    return Promise.reject(error);
  },
);

/** Turn an axios error into a typed ApiClientError with the backend's error code. */
function toClientError(error: unknown): ApiClientError {
  if (error instanceof AxiosError) {
    const payload = error.response?.data as ApiResponse<unknown> | undefined;
    if (payload && payload.success === false) {
      return new ApiClientError(
        payload.error.code,
        payload.message,
        error.response?.status,
        payload.error.details,
      );
    }
    if (error.code === 'ERR_NETWORK') {
      return new ApiClientError('network_error', 'Cannot reach the server.', undefined, null);
    }
    return new ApiClientError('internal_error', error.message, error.response?.status, null);
  }
  return new ApiClientError('internal_error', 'Unexpected error.', undefined, null);
}

/** Perform a request and return the unwrapped `data`, throwing ApiClientError on failure. */
export async function request<T>(config: AxiosRequestConfig): Promise<T> {
  try {
    const response = await http.request<ApiResponse<T>>(config);
    const payload = response.data;
    if (payload.success) {
      return payload.data;
    }
    throw new ApiClientError(payload.error.code, payload.message, response.status, payload.error.details);
  } catch (error) {
    if (error instanceof ApiClientError) throw error;
    throw toClientError(error);
  }
}

export const api = {
  get: <T>(url: string, config?: AxiosRequestConfig) => request<T>({ ...config, method: 'GET', url }),
  post: <T>(url: string, body?: unknown, config?: AxiosRequestConfig) =>
    request<T>({ ...config, method: 'POST', url, data: body }),
  put: <T>(url: string, body?: unknown, config?: AxiosRequestConfig) =>
    request<T>({ ...config, method: 'PUT', url, data: body }),
  patch: <T>(url: string, body?: unknown, config?: AxiosRequestConfig) =>
    request<T>({ ...config, method: 'PATCH', url, data: body }),
  delete: <T>(url: string, config?: AxiosRequestConfig) =>
    request<T>({ ...config, method: 'DELETE', url }),
};
