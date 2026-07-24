<script setup lang="ts">
import { RouterLink } from 'vue-router';
import { navItems } from '@/components/layout/navItems';
import logoLockup from '@/assets/brand/logo-lockup.svg';
</script>

<template>
  <aside class="hidden w-[248px] shrink-0 flex-col border-r border-border bg-sidebar md:flex">
    <div class="flex h-16 items-center px-5 text-text">
      <img :src="logoLockup" alt="Tameru" class="h-7" />
    </div>

    <nav class="flex-1 space-y-1 px-3 py-2">
      <RouterLink
        v-for="item in navItems"
        :key="item.key"
        :to="{ name: item.route }"
        class="flex items-center gap-3 rounded-control px-3 py-2 text-sm font-medium text-text-muted hover:bg-surface-2 hover:text-text"
        active-class="!bg-accent-soft !text-accent"
      >
        <component :is="item.icon" :size="20" :stroke-width="1.5" />
        <span class="flex-1">{{ $t(`nav.${item.key}`) }}</span>
        <span
          v-if="item.placeholder"
          class="rounded-full bg-surface-2 px-1.5 py-0.5 text-[10px] font-medium text-text-muted"
        >
          {{ $t('common.soon') }}
        </span>
      </RouterLink>
    </nav>
  </aside>
</template>
