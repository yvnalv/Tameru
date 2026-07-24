import { createI18n } from 'vue-i18n';
import en from './locales/en';
import id from './locales/id';

export type AppLocale = 'en' | 'id';
export const SUPPORTED_LOCALES: AppLocale[] = ['en', 'id'];

const STORAGE_KEY = 'tameru.locale';

function initialLocale(): AppLocale {
  const stored = localStorage.getItem(STORAGE_KEY);
  if (stored === 'en' || stored === 'id') return stored;
  return 'en'; // English is the default UI language (CLAUDE.md → Internationalization).
}

export const i18n = createI18n({
  legacy: false,
  locale: initialLocale(),
  fallbackLocale: 'en',
  messages: { en, id },
});

export function setLocale(locale: AppLocale): void {
  i18n.global.locale.value = locale;
  localStorage.setItem(STORAGE_KEY, locale);
  document.documentElement.setAttribute('lang', locale);
}
