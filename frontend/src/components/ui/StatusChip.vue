<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';

// Status chips use the semantic palette, never the accent (DESIGN_LANGUAGE.md), so a green primary
// button never reads as a "cleared" status.
const props = defineProps<{ status: 'Cleared' | 'Uncleared' }>();
const { t } = useI18n();

const styles = computed(() =>
  props.status === 'Cleared'
    ? 'text-positive bg-positive-soft border border-positive/20'
    : 'text-warning bg-warning-soft border border-warning/20',
);

const label = computed(() => t(`enums.transactionStatus.${props.status}`));
</script>

<template>
  <span
    class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
    :class="styles"
  >
    {{ label }}
  </span>
</template>
