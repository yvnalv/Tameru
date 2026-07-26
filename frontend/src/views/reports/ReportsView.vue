<script setup lang="ts">
import { onMounted, ref, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { ChevronLeft, ChevronRight } from 'lucide-vue-next';
import { getCategoryTracker } from '@/lib/reports';
import { listCategories } from '@/lib/categories';
import type { Category } from '@/types/api';
import { displayName } from '@/lib/seededNames';
import AppCard from '@/components/ui/AppCard.vue';
import SpendBar from '@/components/ui/SpendBar.vue';
import Skeleton from '@/components/ui/Skeleton.vue';
import Money from '@/components/ui/Money.vue';

type Granularity = 'yearly' | 'monthly' | 'daily';
interface Matrix {
  labels: string[];
  rows: { categoryId: string; amounts: number[]; total: number }[];
  periodTotals: number[];
  total: number;
}

const { t, locale } = useI18n();

const categories = ref<Category[]>([]);
const granularity = ref<Granularity>('monthly');
const matrix = ref<Matrix | null>(null);
const loading = ref(true);
const failed = ref(false);

const now = new Date();
const monthlyYear = ref(now.getFullYear());
const dailyYear = ref(now.getFullYear());
const dailyMonth = ref(now.getMonth() + 1);

const catName = (id: string) => displayName(categories.value.find((c) => c.id === id)?.name ?? null, locale.value) || id.slice(0, 6);
const monthShort = (m: number) => new Date(2020, m - 1, 1).toLocaleString(locale.value, { month: 'short' });
const daysInMonth = (y: number, m: number) => new Date(y, m, 0).getDate();
const pad = (n: number) => String(n).padStart(2, '0');

function compact(value: number): string {
  return value ? new Intl.NumberFormat(locale.value, { notation: 'compact', maximumFractionDigits: 1 }).format(value) : '';
}
const maxCell = computed(() => Math.max(1, ...(matrix.value?.rows.flatMap((r) => r.amounts) ?? [0])));
function heat(value: number): Record<string, string> {
  if (value <= 0) return {};
  const alpha = Math.min(0.5, (value / maxCell.value) * 0.46 + 0.06);
  return { backgroundColor: `rgba(53,208,122,${alpha.toFixed(3)})` };
}

// Reshape a category-tracker response into fixed columns.
function buildFixed(
  tracker: { periods: string[]; categories: { categoryId: string; amounts: number[] }[] },
  colKeys: string[],
  keyOf: (iso: string) => string,
  labels: string[],
): Matrix {
  const index = new Map(colKeys.map((k, i) => [k, i]));
  const rows = tracker.categories
    .map((cat) => {
      const amounts = new Array(colKeys.length).fill(0);
      tracker.periods.forEach((p, i) => {
        const j = index.get(keyOf(p));
        if (j !== undefined) amounts[j] += cat.amounts[i];
      });
      return { categoryId: cat.categoryId, amounts, total: amounts.reduce((a, b) => a + b, 0) };
    })
    .filter((r) => r.total > 0)
    .sort((a, b) => b.total - a.total);
  const periodTotals = colKeys.map((_, c) => rows.reduce((s, r) => s + r.amounts[c], 0));
  return { labels, rows, periodTotals, total: periodTotals.reduce((a, b) => a + b, 0) };
}

async function load(): Promise<void> {
  loading.value = true;
  failed.value = false;
  try {
    const cy = now.getFullYear();
    if (granularity.value === 'yearly') {
      const tr = await getCategoryTracker('monthly', `${cy - 4}-01-01`, `${cy}-12-31`);
      const cols = Array.from({ length: 5 }, (_, i) => String(cy - 4 + i));
      matrix.value = buildFixed(tr, cols, (iso) => iso.slice(0, 4), cols);
    } else if (granularity.value === 'monthly') {
      const y = monthlyYear.value;
      const tr = await getCategoryTracker('monthly', `${y}-01-01`, `${y}-12-31`);
      const cols = Array.from({ length: 12 }, (_, i) => pad(i + 1));
      const labels = Array.from({ length: 12 }, (_, i) => monthShort(i + 1));
      matrix.value = buildFixed(tr, cols, (iso) => iso.slice(5, 7), labels);
    } else {
      const dim = daysInMonth(dailyYear.value, dailyMonth.value);
      const mm = pad(dailyMonth.value);
      const tr = await getCategoryTracker('daily', `${dailyYear.value}-${mm}-01`, `${dailyYear.value}-${mm}-${pad(dim)}`);
      const cols = Array.from({ length: dim }, (_, i) => pad(i + 1));
      const labels = Array.from({ length: dim }, (_, i) => String(i + 1));
      matrix.value = buildFixed(tr, cols, (iso) => iso.slice(8, 10), labels);
    }
  } catch {
    failed.value = true;
  } finally {
    loading.value = false;
  }
}

function setGranularity(g: Granularity): void {
  granularity.value = g;
  load();
}
function changeMonthlyYear(delta: number): void {
  monthlyYear.value += delta;
  load();
}
function changeDailyMonth(delta: number): void {
  const d = new Date(dailyYear.value, dailyMonth.value - 1 + delta, 1);
  dailyYear.value = d.getFullYear();
  dailyMonth.value = d.getMonth() + 1;
  load();
}
const dailyMonthLabel = computed(() =>
  new Date(dailyYear.value, dailyMonth.value - 1, 1).toLocaleString(locale.value, { month: 'long', year: 'numeric' }),
);
// Static range shown for the yearly view (monthly/daily have their own prev/next nav).
const rangeLabel = computed(() => `${now.getFullYear() - 4}–${now.getFullYear()}`);

const topCategories = computed(() => matrix.value?.rows.slice(0, 7) ?? []);
const segments = computed(() => topCategories.value.map((c) => ({ label: catName(c.categoryId), value: c.total })));

onMounted(async () => {
  categories.value = await listCategories({ includeInactive: true });
  await load();
});
</script>

<template>
  <div class="space-y-4">
    <h1 class="text-lg font-semibold">{{ t('reports.title') }}</h1>

    <AppCard>
      <!-- Header: title + granularity toggle + (daily) month nav -->
      <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 class="text-sm font-semibold">{{ t('reports.tracker') }}</h2>
          <p class="text-[13px] text-text-muted">{{ t('reports.trackerNote') }}</p>
        </div>
        <div class="flex flex-wrap items-center gap-2">
          <div class="flex rounded-control border border-border p-0.5">
            <button
              v-for="g in (['yearly', 'monthly', 'daily'] as Granularity[])"
              :key="g"
              class="rounded-[9px] px-3 py-1 text-[13px] font-medium capitalize"
              :class="granularity === g ? 'bg-accent-soft text-accent' : 'text-text-muted hover:text-text'"
              @click="setGranularity(g)"
            >{{ t(`reports.${g}`) }}</button>
          </div>
          <!-- Daily: month prev/next -->
          <div v-if="granularity === 'daily'" class="flex items-center gap-1.5">
            <button class="rounded-control border border-border p-1.5 text-text-muted hover:bg-surface-2 hover:text-text" :aria-label="t('transactions.prev')" @click="changeDailyMonth(-1)"><ChevronLeft :size="16" /></button>
            <span class="min-w-[7.5rem] text-center text-[13px] font-medium">{{ dailyMonthLabel }}</span>
            <button class="rounded-control border border-border p-1.5 text-text-muted hover:bg-surface-2 hover:text-text" :aria-label="t('transactions.next')" @click="changeDailyMonth(1)"><ChevronRight :size="16" /></button>
          </div>
          <!-- Monthly: year prev/next -->
          <div v-else-if="granularity === 'monthly'" class="flex items-center gap-1.5">
            <button class="rounded-control border border-border p-1.5 text-text-muted hover:bg-surface-2 hover:text-text" :aria-label="t('transactions.prev')" @click="changeMonthlyYear(-1)"><ChevronLeft :size="16" /></button>
            <span class="tnum min-w-[3.5rem] text-center text-[13px] font-medium">{{ monthlyYear }}</span>
            <button class="rounded-control border border-border p-1.5 text-text-muted hover:bg-surface-2 hover:text-text" :aria-label="t('transactions.next')" @click="changeMonthlyYear(1)"><ChevronRight :size="16" /></button>
          </div>
          <span v-else class="tnum text-[13px] text-text-muted">{{ rangeLabel }}</span>
        </div>
      </div>

      <div v-if="loading" class="space-y-2 py-2">
        <Skeleton v-for="i in 6" :key="i" class="h-8 w-full" />
      </div>
      <div v-else-if="failed" class="py-12 text-center text-sm text-text-muted">{{ t('errors.network_error') }}</div>

      <template v-else-if="matrix && matrix.rows.length">
        <!-- Top categories summary -->
        <div class="mb-5">
          <SpendBar :segments="segments" />
          <ul class="mt-3 grid grid-cols-2 gap-x-6 gap-y-1.5 sm:grid-cols-3">
            <li v-for="c in topCategories" :key="c.categoryId" class="flex items-center justify-between text-[13px]">
              <span class="truncate text-text-muted">{{ catName(c.categoryId) }}</span>
              <Money :value="c.total" class="ml-2 shrink-0 font-medium" />
            </li>
          </ul>
        </div>

        <!-- Matrix -->
        <div class="scroll-slim overflow-x-auto">
          <table class="w-full min-w-[640px] border-separate border-spacing-0 text-sm">
            <thead>
              <tr class="text-[12px] uppercase text-text-muted">
                <th class="sticky left-0 z-10 bg-surface px-3 py-2 text-left font-medium">{{ t('reports.category') }}</th>
                <th v-for="(l, i) in matrix.labels" :key="i" class="px-2 py-2 text-right font-medium">{{ l }}</th>
                <th class="px-3 py-2 text-right font-medium">{{ t('reports.total') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in matrix.rows" :key="row.categoryId" class="border-t border-border">
                <td class="sticky left-0 z-10 truncate bg-surface px-3 py-2 font-medium">{{ catName(row.categoryId) }}</td>
                <td v-for="(v, i) in row.amounts" :key="i" class="tnum px-2 py-2 text-right text-[13px]" :style="heat(v)">{{ compact(v) }}</td>
                <td class="tnum px-3 py-2 text-right font-medium"><Money :value="row.total" /></td>
              </tr>
            </tbody>
            <tfoot>
              <tr class="border-t border-border text-[13px] font-semibold">
                <td class="sticky left-0 z-10 bg-surface px-3 py-2">{{ t('reports.total') }}</td>
                <td v-for="(v, i) in matrix.periodTotals" :key="i" class="tnum px-2 py-2 text-right">{{ compact(v) }}</td>
                <td class="tnum px-3 py-2 text-right"><Money :value="matrix.total" /></td>
              </tr>
            </tfoot>
          </table>
        </div>
      </template>

      <p v-else class="py-10 text-center text-[13px] text-text-muted">{{ t('reports.noSpend') }}</p>
    </AppCard>
  </div>
</template>
