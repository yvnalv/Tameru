import { defineStore } from 'pinia';
import { ref } from 'vue';
import { i18n, setLocale, type AppLocale } from '@/i18n';

export type Density = 'comfortable' | 'compact';

const DENSITY_KEY = 'tameru.density';

export const useUiStore = defineStore('ui', () => {
  const locale = ref<AppLocale>(i18n.global.locale.value as AppLocale);
  const density = ref<Density>((localStorage.getItem(DENSITY_KEY) as Density) || 'comfortable');
  const sidebarCollapsed = ref(false);

  function changeLocale(next: AppLocale): void {
    setLocale(next);
    locale.value = next;
  }

  function toggleLocale(): void {
    changeLocale(locale.value === 'en' ? 'id' : 'en');
  }

  function setDensity(next: Density): void {
    density.value = next;
    localStorage.setItem(DENSITY_KEY, next);
  }

  function toggleSidebar(): void {
    sidebarCollapsed.value = !sidebarCollapsed.value;
  }

  return { locale, density, sidebarCollapsed, changeLocale, toggleLocale, setDensity, toggleSidebar };
});
