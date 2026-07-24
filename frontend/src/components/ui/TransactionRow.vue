<script setup lang="ts">
import AvatarChip from '@/components/ui/AvatarChip.vue';
import Money from '@/components/ui/Money.vue';

defineProps<{
  title: string;
  subtitle?: string;
  /** Signed amount: positive = income (green +), negative = expense (red −). */
  amount: number;
  currency?: string;
}>();
</script>

<template>
  <div class="flex items-center gap-3 py-3">
    <AvatarChip :name="title" />
    <div class="min-w-0 flex-1">
      <p class="truncate text-sm font-medium">{{ title }}</p>
      <p v-if="subtitle" class="truncate text-[13px] text-text-muted">{{ subtitle }}</p>
    </div>
    <Money :value="amount" :currency="currency ?? 'IDR'" signed class="text-sm font-medium" />
  </div>
</template>
