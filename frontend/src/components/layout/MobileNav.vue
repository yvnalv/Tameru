<script setup lang="ts">
import { RouterLink } from 'vue-router';
import { mobileNavItems } from '@/components/layout/navItems';
</script>

<template>
  <!-- Rounded bottom-nav pill (DESIGN_LANGUAGE.md → mobile shell). Active icon in green. -->
  <nav
    class="fixed inset-x-0 bottom-0 z-20 flex justify-center pb-[env(safe-area-inset-bottom)] md:hidden"
  >
    <div
      class="mx-4 mb-3 flex w-full max-w-md items-center justify-around rounded-full border border-border bg-sidebar/95 px-2 py-1.5 shadow-lift backdrop-blur"
    >
      <template v-for="item in mobileNavItems" :key="item.key">
        <RouterLink
          v-if="item.route"
          :to="{ name: item.route }"
          class="flex flex-col items-center gap-0.5 rounded-full px-3 py-1.5 text-[11px] font-medium text-text-muted"
          active-class="!text-accent"
        >
          <component :is="item.icon" :size="20" :stroke-width="1.5" />
          {{ $t(`nav.${item.key}`) }}
        </RouterLink>
        <span
          v-else
          class="flex flex-col items-center gap-0.5 rounded-full px-3 py-1.5 text-[11px] font-medium text-text-muted/50"
          :title="$t('common.comingSoon')"
        >
          <component :is="item.icon" :size="20" :stroke-width="1.5" />
          {{ $t(`nav.${item.key}`) }}
        </span>
      </template>
    </div>
  </nav>
</template>
