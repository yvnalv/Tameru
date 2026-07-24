<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { iconForRoute } from '@/components/layout/navItems';

// A navigable "coming soon" stub for menu sections whose real screens land in M7.
const route = useRoute();
const { t } = useI18n();

const name = computed(() => (route.name as string) ?? 'dashboard');
const icon = computed(() => iconForRoute(name.value));
</script>

<template>
  <div class="flex flex-col items-center justify-center rounded-card border border-border bg-surface px-6 py-20 text-center">
    <div class="flex h-14 w-14 items-center justify-center rounded-full bg-surface-2 text-text-muted">
      <component :is="icon" v-if="icon" :size="26" :stroke-width="1.5" />
    </div>
    <h2 class="mt-4 text-lg font-semibold">{{ t(`nav.${name}`) }}</h2>
    <p class="mt-1 text-sm text-text-muted">{{ t('common.comingSoonNote') }}</p>
  </div>
</template>
