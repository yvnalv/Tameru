import { describe, it, expect, beforeEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import type { RouteLocationNormalized } from 'vue-router';
import { authGuard } from '@/router';
import { useAuthStore } from '@/stores/auth';
import type { AuthUser } from '@/types/api';

function route(path: string, isPublic = false): RouteLocationNormalized {
  return {
    fullPath: path,
    path,
    name: path === '/login' ? 'login' : 'dashboard',
    meta: { public: isPublic },
    query: {},
    params: {},
    hash: '',
    matched: [],
    redirectedFrom: undefined,
  } as unknown as RouteLocationNormalized;
}

const owner: AuthUser = { id: '1', email: 'o@t.local', displayName: 'Owner', locale: 'en' };

describe('authGuard', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    localStorage.clear();
  });

  it('redirects an unauthenticated user to /login with a redirect back', () => {
    const result = authGuard(route('/dashboard'));
    expect(result).toEqual({ name: 'login', query: { redirect: '/dashboard' } });
  });

  it('lets an authenticated user through to a protected route', () => {
    useAuthStore().user = owner;
    expect(authGuard(route('/dashboard'))).toBe(true);
  });

  it('bounces an authenticated user away from /login', () => {
    useAuthStore().user = owner;
    expect(authGuard(route('/login', true))).toEqual({ name: 'dashboard' });
  });

  it('allows an unauthenticated user to reach /login', () => {
    expect(authGuard(route('/login', true))).toBe(true);
  });
});
