<script setup lang="ts">
import { ref, computed } from 'vue';
import { RouterLink, useRoute } from 'vue-router';
import { MoreHorizontal } from 'lucide-vue-next';
import { mobileNavItems, mobileMoreItems } from '@/components/layout/navItems';
import MobileMoreSheet from '@/components/layout/MobileMoreSheet.vue';

const route = useRoute();
const sheetOpen = ref(false);

// The current route lives in the overflow group, so "More" shows as the active slot.
const moreActive = computed(() => mobileMoreItems.some((i) => i.route === route.name));
</script>

<template>
  <!-- Rounded bottom-nav pill (DESIGN_LANGUAGE.md → mobile shell). Active icon in green. -->
  <nav
    class="fixed inset-x-0 bottom-0 z-20 flex justify-center pb-[env(safe-area-inset-bottom)] md:hidden"
  >
    <div
      class="mx-3 mb-3 flex w-full max-w-md items-center justify-around rounded-full border border-border bg-sidebar/95 px-1 py-1.5 shadow-lift backdrop-blur"
    >
      <RouterLink
        v-for="item in mobileNavItems"
        :key="item.key"
        :to="{ name: item.route }"
        class="flex min-w-0 flex-1 flex-col items-center gap-0.5 rounded-full px-1 py-1.5 text-[10px] font-medium text-text-muted"
        active-class="!text-accent"
      >
        <component :is="item.icon" :size="20" :stroke-width="1.5" />
        <span class="w-full truncate text-center">{{ $t(`nav.${item.key}`) }}</span>
      </RouterLink>

      <button
        type="button"
        class="flex min-w-0 flex-1 flex-col items-center gap-0.5 rounded-full px-1 py-1.5 text-[10px] font-medium"
        :class="moreActive ? 'text-accent' : 'text-text-muted'"
        :aria-expanded="sheetOpen"
        aria-haspopup="dialog"
        @click="sheetOpen = true"
      >
        <MoreHorizontal :size="20" :stroke-width="1.5" />
        <span class="w-full truncate text-center">{{ $t('nav.more') }}</span>
      </button>
    </div>
  </nav>

  <!-- Overflow sheet: everything the pill cannot hold. Without it these routes would be
       unreachable on a phone, since the sidebar is hidden below `md`. -->
  <Teleport to="body">
    <MobileMoreSheet v-if="sheetOpen" @close="sheetOpen = false" />
  </Teleport>
</template>
