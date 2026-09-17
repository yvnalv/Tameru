import { describe, it, expect, beforeEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { useThemeStore } from '@/stores/theme';

describe('useThemeStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.classList.remove('dark');
  });

  it('initializes with a valid theme and sets DOM attributes', () => {
    const store = useThemeStore();
    expect(['light', 'dark']).toContain(store.theme);
    expect(document.documentElement.getAttribute('data-theme')).toBe(store.theme);
    expect(document.documentElement.classList.contains('dark')).toBe(store.theme === 'dark');
  });

  it('toggles theme between light and dark', () => {
    const store = useThemeStore();
    store.setTheme('light');
    expect(store.theme).toBe('light');
    expect(store.isDark).toBe(false);
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
    expect(document.documentElement.classList.contains('dark')).toBe(false);

    store.toggleTheme();
    expect(store.theme).toBe('dark');
    expect(store.isDark).toBe(true);
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(document.documentElement.classList.contains('dark')).toBe(true);

    store.toggleTheme();
    expect(store.theme).toBe('light');
    expect(store.isDark).toBe(false);
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
    expect(document.documentElement.classList.contains('dark')).toBe(false);
  });

  it('persists selected theme to localStorage', () => {
    const store = useThemeStore();
    store.setTheme('dark');
    expect(localStorage.getItem('tameru-theme')).toBe('dark');

    store.setTheme('light');
    expect(localStorage.getItem('tameru-theme')).toBe('light');
  });
});
