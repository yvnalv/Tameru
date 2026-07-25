<script setup lang="ts">
import { useRouter } from 'vue-router';
import { LogOut, Languages, Rows2, Rows3, PanelLeftClose, PanelLeftOpen } from 'lucide-vue-next';
import { useAuthStore } from '@/stores/auth';
import { useUiStore } from '@/stores/ui';
import { useDensity } from '@/composables/useDensity';
import IconButton from '@/components/ui/IconButton.vue';
import AvatarChip from '@/components/ui/AvatarChip.vue';

const router = useRouter();
const auth = useAuthStore();
const ui = useUiStore();
const density = useDensity();

async function signOut(): Promise<void> {
  await auth.logout();
  router.push({ name: 'login' });
}
</script>

<template>
  <header
    class="flex h-16 items-center justify-between border-b border-border bg-bg/80 px-4 backdrop-blur md:px-8"
  >
    <div class="flex items-center gap-2">
      <IconButton
        class="hidden md:inline-flex"
        :icon="ui.sidebarCollapsed ? PanelLeftOpen : PanelLeftClose"
        :label="ui.sidebarCollapsed ? $t('common.expand') : $t('common.collapse')"
        :size="18"
        @click="ui.toggleSidebar()"
      />
      <h1 class="text-base font-semibold">
        {{ $t(`nav.${(router.currentRoute.value.name as string) || 'dashboard'}`) }}
      </h1>
    </div>

    <div class="flex items-center gap-1">
      <IconButton
        :icon="density.compact.value ? Rows2 : Rows3"
        :label="$t('common.density')"
        :size="18"
        @click="density.toggle()"
      />

      <button
        class="inline-flex items-center gap-1.5 rounded-control px-2.5 py-1.5 text-sm font-medium text-text-muted hover:bg-surface-2 hover:text-text"
        :title="$t('common.language')"
        @click="ui.toggleLocale()"
      >
        <Languages :size="18" :stroke-width="1.5" />
        <span class="uppercase">{{ ui.locale }}</span>
      </button>

      <IconButton :icon="LogOut" :label="$t('common.signOut')" :size="18" @click="signOut" />

      <!-- Logged-in owner -->
      <div v-if="auth.user" class="ml-1 flex items-center gap-2 border-l border-border pl-2.5">
        <AvatarChip :name="auth.user.displayName || auth.user.email" />
        <div class="hidden leading-tight sm:block">
          <p class="max-w-[9rem] truncate text-sm font-medium">{{ auth.user.displayName || auth.user.email }}</p>
          <p class="max-w-[9rem] truncate text-[12px] text-text-muted">{{ auth.user.email }}</p>
        </div>
      </div>
    </div>
  </header>
</template>
