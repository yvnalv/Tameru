<script setup lang="ts">
import { useRouter } from 'vue-router';
import { LogOut, Languages } from 'lucide-vue-next';
import { useAuthStore } from '@/stores/auth';
import { useUiStore } from '@/stores/ui';

const router = useRouter();
const auth = useAuthStore();
const ui = useUiStore();

async function signOut(): Promise<void> {
  await auth.logout();
  router.push({ name: 'login' });
}
</script>

<template>
  <header
    class="flex h-16 items-center justify-between border-b border-border bg-bg/80 px-4 backdrop-blur md:px-6"
  >
    <h1 class="text-base font-semibold">
      {{ $t(`nav.${(router.currentRoute.value.name as string) || 'dashboard'}`) }}
    </h1>

    <div class="flex items-center gap-1">
      <button
        class="inline-flex items-center gap-1.5 rounded-control px-2.5 py-1.5 text-sm font-medium text-text-muted hover:bg-surface-2 hover:text-text"
        :title="$t('common.language')"
        @click="ui.toggleLocale()"
      >
        <Languages :size="18" :stroke-width="1.5" />
        <span class="uppercase">{{ ui.locale }}</span>
      </button>

      <button
        class="inline-flex items-center gap-1.5 rounded-control px-2.5 py-1.5 text-sm font-medium text-text-muted hover:bg-surface-2 hover:text-text"
        :title="$t('common.signOut')"
        @click="signOut"
      >
        <LogOut :size="18" :stroke-width="1.5" />
        <span class="hidden sm:inline">{{ $t('common.signOut') }}</span>
      </button>
    </div>
  </header>
</template>
