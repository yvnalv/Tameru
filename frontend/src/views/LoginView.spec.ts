import { describe, it, expect, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { createRouter, createMemoryHistory } from 'vue-router';
import { createI18n } from 'vue-i18n';
import en from '@/i18n/locales/en';
import id from '@/i18n/locales/id';
import LoginView from '@/views/LoginView.vue';

const router = createRouter({
  history: createMemoryHistory(),
  routes: [
    { path: '/login', name: 'login', component: LoginView },
    { path: '/dashboard', name: 'dashboard', component: { template: '<div />' } },
  ],
});

const i18n = createI18n({ legacy: false, locale: 'en', messages: { en, id } });

describe('LoginView', () => {
  beforeEach(() => setActivePinia(createPinia()));

  it('renders both the email and password fields', async () => {
    router.push('/login');
    await router.isReady();

    const wrapper = mount(LoginView, { global: { plugins: [router, i18n] } });

    const inputs = wrapper.findAll('input');
    expect(inputs).toHaveLength(2);
    const email = wrapper.find('input[type="email"]');
    expect(email.exists()).toBe(true);
    expect(wrapper.find('input[type="password"]').exists()).toBe(true);
    // The email field is labelled, not missing.
    expect(wrapper.find('label[for="email"]').text()).toBe(en.login.email);
    // The '@' in the placeholder must survive vue-i18n's linked-message syntax.
    expect(email.attributes('placeholder')).toBe('you@example.com');
  });
});
