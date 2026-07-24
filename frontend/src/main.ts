import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import { router } from '@/router';
import { i18n } from '@/i18n';
import { setUnauthorizedHandler } from '@/lib/api';
import { useAuthStore } from '@/stores/auth';
import { useThemeStore } from '@/stores/theme';
import '@/assets/styles/main.css';

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
app.use(i18n);
app.use(router);

// Lock in the dark theme (v1) and apply the persisted locale to <html lang>.
useThemeStore();
document.documentElement.setAttribute('lang', i18n.global.locale.value);

// On a hard 401 (refresh failed), the API client clears the session; drop the store user and route
// to /login without a circular import.
setUnauthorizedHandler(() => {
  useAuthStore().endSession();
  if (router.currentRoute.value.name !== 'login') {
    router.push({ name: 'login' });
  }
});

app.mount('#app');
