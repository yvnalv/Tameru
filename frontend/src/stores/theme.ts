import { defineStore } from 'pinia';
import { ref } from 'vue';

// v1 is dark-only (ADR-0004). The store exists so a light theme can be added later by flipping
// `data-theme` on <html> without touching components.
export type Theme = 'dark' | 'light';

export const useThemeStore = defineStore('theme', () => {
  const theme = ref<Theme>('dark');

  function apply(): void {
    document.documentElement.setAttribute('data-theme', theme.value);
    document.documentElement.classList.toggle('dark', theme.value === 'dark');
  }

  apply();

  return { theme, apply };
});
