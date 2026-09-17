// Typed Identity API module (docs/API_SPEC.md → Auth). Components/stores call these, never axios.
import { api } from '@/lib/api';
import type { AuthTokens, AuthUser } from '@/types/api';

export function login(email: string, password: string): Promise<AuthTokens> {
  return api.post<AuthTokens>('/auth/login', { email, password });
}

export function refresh(refreshToken: string): Promise<AuthTokens> {
  return api.post<AuthTokens>('/auth/refresh', { refreshToken });
}

export function logout(refreshToken: string): Promise<void> {
  return api.post<void>('/auth/logout', { refreshToken });
}

export function me(): Promise<AuthUser> {
  return api.get<AuthUser>('/auth/me');
}

export interface UpdateProfileInput {
  displayName?: string;
  locale?: string;
  budgetCycleStartDay?: number;
}

export function updateProfile(input: UpdateProfileInput): Promise<AuthUser> {
  return api.patch<AuthUser>('/auth/me', input);
}
