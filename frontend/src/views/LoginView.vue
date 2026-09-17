<script setup lang="ts">
import { ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '@/stores/auth';
import { useUiStore } from '@/stores/ui';
import { ApiClientError } from '@/lib/api';
import AppButton from '@/components/ui/AppButton.vue';
import AppInput from '@/components/ui/AppInput.vue';
import FormField from '@/components/ui/FormField.vue';
import LogoLockup from '@/components/brand/LogoLockup.vue';
import ThemeToggle from '@/components/ui/ThemeToggle.vue';

const route = useRoute();
const router = useRouter();
const { t, te } = useI18n();
const auth = useAuthStore();
const ui = useUiStore();

const email = ref('');
const password = ref('');
const submitting = ref(false);
const errorMessage = ref('');

function messageFor(code: string): string {
  return te(`errors.${code}`) ? t(`errors.${code}`) : t('errors.generic');
}

async function onSubmit(): Promise<void> {
  errorMessage.value = '';
  submitting.value = true;
  try {
    await auth.login(email.value.trim(), password.value);
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : undefined;
    router.push(redirect ?? { name: 'dashboard' });
  } catch (error) {
    errorMessage.value =
      error instanceof ApiClientError ? messageFor(error.code) : t('errors.generic');
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <div class="flex min-h-screen flex-col items-center justify-center bg-bg px-4 text-text transition-colors duration-200">
    <div class="w-full max-w-sm">
      <div class="mb-6 flex items-center justify-between">
        <LogoLockup size="md" />
        <div class="flex items-center gap-2">
          <ThemeToggle variant="segmented" :size="13" />
          <button
            class="rounded-lg border border-border bg-surface px-2.5 py-1 text-xs font-semibold uppercase text-text-muted hover:bg-surface-2 hover:text-text transition-colors"
            @click="ui.toggleLocale()"
          >
            {{ ui.locale }}
          </button>
        </div>
      </div>

      <div class="rounded-card border border-border bg-surface p-7 shadow-lift">
        <h1 class="text-xl font-bold text-text">{{ t('login.title') }}</h1>
        <p class="mt-1 text-xs text-text-muted">{{ t('login.subtitle') }}</p>

        <form class="mt-6 space-y-4" @submit.prevent="onSubmit">
          <FormField :label="t('login.email')" for-id="email">
            <AppInput
              id="email"
              v-model="email"
              type="email"
              autocomplete="username"
              :placeholder="t('login.emailPlaceholder')"
              required
            />
          </FormField>

          <FormField :label="t('login.password')" for-id="password">
            <AppInput
              id="password"
              v-model="password"
              type="password"
              autocomplete="current-password"
              :placeholder="t('login.passwordPlaceholder')"
              required
            />
          </FormField>

          <p v-if="errorMessage" class="text-[13px] text-negative" role="alert">
            {{ errorMessage }}
          </p>

          <AppButton type="submit" block :loading="submitting">
            {{ submitting ? t('login.submitting') : t('login.submit') }}
          </AppButton>
        </form>
      </div>
    </div>
  </div>
</template>
