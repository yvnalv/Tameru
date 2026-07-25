<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import type { MonthlyCashflow } from '@/types/api';
import { formatMoney } from '@/lib/format';

// Lightweight 12-month income-vs-expense bars. CSS/flex, solid fills, no gradient (design language).
const props = defineProps<{ months: MonthlyCashflow[]; currency?: string }>();
const { t, locale } = useI18n();

const max = computed(() =>
  Math.max(1, ...props.months.flatMap((m) => [m.income, m.expense])),
);

function heightPct(value: number): number {
  return value <= 0 ? 0 : Math.max(3, (value / max.value) * 100);
}

function monthLabel(month: number): string {
  return new Date(2020, month - 1, 1).toLocaleString(locale.value, { month: 'short' });
}
</script>

<template>
  <div>
    <div class="mb-4 flex items-center gap-4 text-xs text-text-muted">
      <span class="inline-flex items-center gap-1.5">
        <span class="h-2.5 w-2.5 rounded-sm" style="background: var(--positive)" />
        {{ t('enums.transactionType.Income') }}
      </span>
      <span class="inline-flex items-center gap-1.5">
        <span class="h-2.5 w-2.5 rounded-sm" style="background: var(--negative)" />
        {{ t('enums.transactionType.Expense') }}
      </span>
    </div>

    <div class="flex h-44 items-end gap-1.5 sm:gap-2">
      <div
        v-for="m in months"
        :key="m.month"
        class="group flex h-full flex-1 flex-col items-center justify-end"
      >
        <div class="flex h-full w-full items-end justify-center gap-[3px]">
          <div
            class="w-1/3 max-w-[10px] rounded-t-sm bg-[color:var(--positive)] transition-[height] duration-200"
            :style="{ height: `${heightPct(m.income)}%` }"
            :title="`${t('enums.transactionType.Income')}: ${formatMoney(m.income, currency ?? 'IDR')}`"
          />
          <div
            class="w-1/3 max-w-[10px] rounded-t-sm bg-[color:var(--negative)] transition-[height] duration-200"
            :style="{ height: `${heightPct(m.expense)}%` }"
            :title="`${t('enums.transactionType.Expense')}: ${formatMoney(m.expense, currency ?? 'IDR')}`"
          />
        </div>
        <span class="mt-2 text-[11px] uppercase text-text-muted">{{ monthLabel(m.month) }}</span>
      </div>
    </div>
  </div>
</template>
