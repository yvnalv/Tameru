import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import * as authApi from '@/lib/auth';
import { clearSession, getRefreshToken, getUser, setSession } from '@/lib/session';
import { setLocale, type AppLocale } from '@/i18n';
import type { AuthUser } from '@/types/api';

export const useAuthStore = defineStore('auth', () => {
  const user = ref<AuthUser | null>(getUser());
  const isAuthenticated = computed(() => user.value !== null);

  function applyUserLocale(u: AuthUser): void {
    if (u.locale === 'en' || u.locale === 'id') {
      setLocale(u.locale as AppLocale);
    }
  }

  async function login(email: string, password: string): Promise<void> {
    const tokens = await authApi.login(email, password);
    setSession(tokens);
    user.value = tokens.user;
    applyUserLocale(tokens.user);
  }

  async function logout(): Promise<void> {
    const refreshToken = getRefreshToken();
    if (refreshToken) {
      try {
        await authApi.logout(refreshToken);
      } catch {
        // Best-effort revoke; clear the local session regardless.
      }
    }
    clearSession();
    user.value = null;
  }

  /** Drop the local session without a server round-trip (used on hard 401). */
  function endSession(): void {
    clearSession();
    user.value = null;
  }

  return { user, isAuthenticated, login, logout, endSession };
});
