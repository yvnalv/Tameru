import { defineStore } from 'pinia';
import { ref, computed } from 'vue';

export type Theme = 'dark' | 'light';

const STORAGE_KEY = 'tameru-theme';

function getInitialTheme(): Theme {
  if (typeof window === 'undefined') return 'light';
  try {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved === 'dark' || saved === 'light') {
      return saved;
    }
  } catch {
    // localStorage disabled or not accessible
  }
  // Default to light for modern reference, but check system preference if preferred
  return 'light';
}

export const useThemeStore = defineStore('theme', () => {
  const theme = ref<Theme>(getInitialTheme());

  const isDark = computed(() => theme.value === 'dark');

  function apply(): void {
    if (typeof document === 'undefined') return;
    document.documentElement.setAttribute('data-theme', theme.value);
    document.documentElement.classList.toggle('dark', theme.value === 'dark');
    try {
      localStorage.setItem(STORAGE_KEY, theme.value);
    } catch {
      // ignore storage errors
    }
  }

  function toggleTheme(): void {
    theme.value = theme.value === 'dark' ? 'light' : 'dark';
    apply();
  }

  function setTheme(newTheme: Theme): void {
    theme.value = newTheme;
    apply();
  }

  // Initial apply
  apply();

  return { theme, isDark, toggleTheme, setTheme, apply };
});

