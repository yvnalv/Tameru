import { defineStore } from 'pinia';
import { ref } from 'vue';
import { i18n, setLocale, type AppLocale } from '@/i18n';

export type Density = 'comfortable' | 'compact';

const DENSITY_KEY = 'tameru.density';
const SIDEBAR_KEY = 'tameru.sidebarCollapsed';

export const useUiStore = defineStore('ui', () => {
  const locale = ref<AppLocale>(i18n.global.locale.value as AppLocale);
  const density = ref<Density>((localStorage.getItem(DENSITY_KEY) as Density) || 'comfortable');
  const sidebarCollapsed = ref(localStorage.getItem(SIDEBAR_KEY) === '1');

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
    localStorage.setItem(SIDEBAR_KEY, sidebarCollapsed.value ? '1' : '0');
  }

  return { locale, density, sidebarCollapsed, changeLocale, toggleLocale, setDensity, toggleSidebar };
});
