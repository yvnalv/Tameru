// Persisted session (tokens + user) in localStorage. Kept separate from the auth store and the API
// client so both can read/write it without a circular import.
import type { AuthTokens, AuthUser } from '@/types/api';

const ACCESS_KEY = 'tameru.accessToken';
const REFRESH_KEY = 'tameru.refreshToken';
const USER_KEY = 'tameru.user';

export function getAccessToken(): string | null {
  return localStorage.getItem(ACCESS_KEY);
}

export function getRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_KEY);
}

export function getUser(): AuthUser | null {
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as AuthUser;
  } catch {
    return null;
  }
}

export function setSession(tokens: AuthTokens): void {
  localStorage.setItem(ACCESS_KEY, tokens.accessToken);
  localStorage.setItem(REFRESH_KEY, tokens.refreshToken);
  localStorage.setItem(USER_KEY, JSON.stringify(tokens.user));
}

export function clearSession(): void {
  localStorage.removeItem(ACCESS_KEY);
  localStorage.removeItem(REFRESH_KEY);
  localStorage.removeItem(USER_KEY);
}
