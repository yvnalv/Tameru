<script setup lang="ts">
import { onMounted, ref, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { ChevronLeft, ChevronRight } from 'lucide-vue-next';
import { getOverview, getCategoryTracker } from '@/lib/reports';
import { listCategories } from '@/lib/categories';
import type { Category, CategoryTrackerReport, OverviewReport } from '@/types/api';
import { displayName } from '@/lib/seededNames';
import { formatShortDate } from '@/lib/format';
import AppCard from '@/components/ui/AppCard.vue';
import AppButton from '@/components/ui/AppButton.vue';
import AppInput from '@/components/ui/AppInput.vue';
import SpendBar from '@/components/ui/SpendBar.vue';
import Money from '@/components/ui/Money.vue';

const { t, locale } = useI18n();

const categories = ref<Category[]>([]);
const overview = ref<OverviewReport | null>(null);
const tracker = ref<CategoryTrackerReport | null>(null);
const loading = ref(true);
const failed = ref(false);

const now = new Date();
const year = ref(now.getFullYear());
const granularity = ref<'monthly' | 'daily'>('monthly');
const from = ref(`${now.getFullYear()}-01-01`);
const to = ref(`${now.getFullYear()}-12-31`);

const catName = (id: string) => {
  const c = categories.value.find((x) => x.id === id);
  return displayName(c?.name ?? null, locale.value) || id.slice(0, 6);
};
const monthLabel = (m: number) => new Date(2020, m - 1, 1).toLocaleString(locale.value, { month: 'narrow' });

// --- heatmap ---------------------------------------------------------------
const overviewMax = computed(() =>
  Math.max(1, ...(overview.value?.categories.flatMap((c) => c.months) ?? [0])),
);
const trackerMax = computed(() =>
  Math.max(1, ...(tracker.value?.categories.flatMap((c) => c.amounts) ?? [0])),
);
function heat(value: number, max: number): Record<string, string> {
  if (value <= 0) return {};
  const alpha = Math.min(0.5, (value / max) * 0.46 + 0.06);
  return { backgroundColor: `rgba(53,208,122,${alpha.toFixed(3)})` };
}

/** Compact money for dense matrix cells (e.g. `3,2 jt`), empty for zero. */
function compact(value: number): string {
  if (!value) return '';
  return new Intl.NumberFormat(locale.value, { notation: 'compact', maximumFractionDigits: 1 }).format(value);
}

// --- overview summary (top categories) -------------------------------------
const topCategories = computed(() =>
  [...(overview.value?.categories ?? [])].sort((a, b) => b.total - a.total).slice(0, 7),
);
const overviewSegments = computed(() =>
  topCategories.value.map((c) => ({ label: catName(c.categoryId), value: c.total })),
);
const trackerPeriodLabel = (iso: string) =>
  granularity.value === 'monthly'
    ? new Date(`${iso}T00:00:00`).toLocaleString(locale.value, { month: 'short' })
    : formatShortDate(iso, locale.value);

// --- loaders ---------------------------------------------------------------
async function loadOverview(): Promise<void> {
  overview.value = await getOverview(year.value);
}
async function loadTracker(): Promise<void> {
  if (from.value > to.value) return;
  tracker.value = await getCategoryTracker(granularity.value, from.value, to.value);
}

async function changeYear(delta: number): Promise<void> {
  year.value += delta;
  await loadOverview();
}
function setGranularity(g: 'monthly' | 'daily'): void {
  granularity.value = g;
  loadTracker();
}

async function reload(): Promise<void> {
  loading.value = true;
  failed.value = false;
  try {
    categories.value = await listCategories({ includeInactive: true });
    await Promise.all([loadOverview(), loadTracker()]);
  } catch {
    failed.value = true;
  } finally {
    loading.value = false;
  }
}

onMounted(reload);
</script>

<template>
  <div class="space-y-4">
    <h1 class="text-lg font-semibold">{{ t('reports.title') }}</h1>

    <div v-if="loading" class="py-16 text-center text-sm text-text-muted">{{ t('common.loading') }}</div>
    <div v-else-if="failed" class="py-16 text-center">
      <p class="text-sm text-text-muted">{{ t('errors.network_error') }}</p>
      <AppButton class="mt-4" variant="secondary" @click="reload">{{ t('common.retry') }}</AppButton>
    </div>

    <template v-else>
      <!-- Yearly overview -->
      <AppCard>
        <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 class="text-sm font-semibold">{{ t('reports.overview') }}</h2>
            <p class="text-[13px] text-text-muted">{{ t('reports.overviewNote') }}</p>
          </div>
          <div class="flex items-center gap-2">
            <button class="rounded-control border border-border p-2 text-text-muted hover:bg-surface-2 hover:text-text" @click="changeYear(-1)"><ChevronLeft :size="16" /></button>
            <span class="tnum min-w-[3.5rem] text-center text-sm font-medium">{{ year }}</span>
            <button class="rounded-control border border-border p-2 text-text-muted hover:bg-surface-2 hover:text-text" @click="changeYear(1)"><ChevronRight :size="16" /></button>
          </div>
        </div>

        <template v-if="overview && overview.categories.length">
          <!-- Top categories summary -->
          <div class="mb-5">
            <SpendBar :segments="overviewSegments" />
            <ul class="mt-3 grid grid-cols-2 gap-x-6 gap-y-1.5 sm:grid-cols-3">
              <li v-for="c in topCategories" :key="c.categoryId" class="flex items-center justify-between text-[13px]">
                <span class="truncate text-text-muted">{{ catName(c.categoryId) }}</span>
                <Money :value="c.total" class="ml-2 shrink-0 font-medium" />
              </li>
            </ul>
          </div>

          <!-- Category × month matrix -->
          <div class="scroll-slim overflow-x-auto">
            <table class="w-full min-w-[720px] border-separate border-spacing-0 text-sm">
              <thead>
                <tr class="text-[12px] uppercase text-text-muted">
                  <th class="sticky left-0 z-10 bg-surface px-3 py-2 text-left font-medium">{{ t('reports.category') }}</th>
                  <th v-for="m in 12" :key="m" class="px-2 py-2 text-right font-medium">{{ monthLabel(m) }}</th>
                  <th class="px-3 py-2 text-right font-medium">{{ t('reports.total') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in overview.categories" :key="row.categoryId" class="border-t border-border">
                  <td class="sticky left-0 z-10 truncate bg-surface px-3 py-2 font-medium">{{ catName(row.categoryId) }}</td>
                  <td v-for="(v, i) in row.months" :key="i" class="tnum px-2 py-2 text-right text-[13px]" :style="heat(v, overviewMax)">
                    {{ compact(v) }}
                  </td>
                  <td class="tnum px-3 py-2 text-right font-medium"><Money :value="row.total" /></td>
                </tr>
              </tbody>
              <tfoot>
                <tr class="border-t border-border text-[13px] font-semibold">
                  <td class="sticky left-0 z-10 bg-surface px-3 py-2">{{ t('reports.total') }}</td>
                  <td v-for="(v, i) in overview.monthlyTotals" :key="i" class="tnum px-2 py-2 text-right">
                    {{ compact(v) }}
                  </td>
                  <td class="tnum px-3 py-2 text-right"><Money :value="overview.total" /></td>
                </tr>
              </tfoot>
            </table>
          </div>
        </template>
        <p v-else class="py-8 text-center text-[13px] text-text-muted">{{ t('reports.noSpend') }}</p>
      </AppCard>

      <!-- Category tracker -->
      <AppCard>
        <div class="mb-4 flex flex-wrap items-end justify-between gap-3">
          <div>
            <h2 class="text-sm font-semibold">{{ t('reports.tracker') }}</h2>
            <p class="text-[13px] text-text-muted">{{ t('reports.trackerNote') }}</p>
          </div>
          <div class="flex flex-wrap items-center gap-2">
            <div class="flex rounded-control border border-border p-0.5">
              <button
                class="rounded-[9px] px-3 py-1 text-[13px] font-medium"
                :class="granularity === 'monthly' ? 'bg-accent-soft text-accent' : 'text-text-muted'"
                @click="setGranularity('monthly')"
              >{{ t('reports.monthly') }}</button>
              <button
                class="rounded-[9px] px-3 py-1 text-[13px] font-medium"
                :class="granularity === 'daily' ? 'bg-accent-soft text-accent' : 'text-text-muted'"
                @click="setGranularity('daily')"
              >{{ t('reports.daily') }}</button>
            </div>
            <AppInput :model-value="from" type="date" class="!w-auto" @update:model-value="from = $event; loadTracker()" />
            <AppInput :model-value="to" type="date" class="!w-auto" @update:model-value="to = $event; loadTracker()" />
          </div>
        </div>

        <div v-if="tracker && tracker.categories.length" class="scroll-slim overflow-x-auto">
          <table class="w-full min-w-[640px] border-separate border-spacing-0 text-sm">
            <thead>
              <tr class="text-[12px] uppercase text-text-muted">
                <th class="sticky left-0 z-10 bg-surface px-3 py-2 text-left font-medium">{{ t('reports.category') }}</th>
                <th v-for="(p, i) in tracker.periods" :key="i" class="px-2 py-2 text-right font-medium">{{ trackerPeriodLabel(p) }}</th>
                <th class="px-3 py-2 text-right font-medium">{{ t('reports.total') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in tracker.categories" :key="row.categoryId" class="border-t border-border">
                <td class="sticky left-0 z-10 truncate bg-surface px-3 py-2 font-medium">{{ catName(row.categoryId) }}</td>
                <td v-for="(v, i) in row.amounts" :key="i" class="tnum px-2 py-2 text-right text-[13px]" :style="heat(v, trackerMax)">
                  {{ compact(v) }}
                </td>
                <td class="tnum px-3 py-2 text-right font-medium"><Money :value="row.total" /></td>
              </tr>
            </tbody>
            <tfoot>
              <tr class="border-t border-border text-[13px] font-semibold">
                <td class="sticky left-0 z-10 bg-surface px-3 py-2">{{ t('reports.total') }}</td>
                <td v-for="(v, i) in tracker.periodTotals" :key="i" class="tnum px-2 py-2 text-right">
                  {{ compact(v) }}
                </td>
                <td class="tnum px-3 py-2 text-right"><Money :value="tracker.total" /></td>
              </tr>
            </tfoot>
          </table>
        </div>
        <p v-else class="py-8 text-center text-[13px] text-text-muted">{{ t('reports.noSpend') }}</p>
      </AppCard>
    </template>
  </div>
</template>
