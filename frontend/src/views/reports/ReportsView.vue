<script setup lang="ts">
import { onMounted, ref, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { ChevronLeft, ChevronRight, CheckCircle2, AlertTriangle } from 'lucide-vue-next';
import { getCategoryTracker, getCashflow, getEnvelopeReport } from '@/lib/reports';
import { listCategories } from '@/lib/categories';
import type { Category, CashflowReport, EnvelopeReport } from '@/types/api';
import { displayName } from '@/lib/seededNames';
import { useThemeStore } from '@/stores/theme';
import AppCard from '@/components/ui/AppCard.vue';
import SpendBar from '@/components/ui/SpendBar.vue';
import { spectrumColor } from '@/lib/spectrum';
import Skeleton from '@/components/ui/Skeleton.vue';
import Money from '@/components/ui/Money.vue';
import CashflowChart from '@/components/ui/CashflowChart.vue';

type Tab = 'tracker' | 'cashflow' | 'allocation';
type Granularity = 'yearly' | 'monthly' | 'daily';
type Flow = 'Expense' | 'Income';

interface Matrix {
  labels: string[];
  rows: { categoryId: string; amounts: number[]; total: number }[];
  periodTotals: number[];
  total: number;
}

const { t, locale } = useI18n();

const activeTab = ref<Tab>('tracker');
const categories = ref<Category[]>([]);
const loading = ref(true);
const failed = ref(false);

const now = new Date();

// --- Tab 1: Category Tracker state ------------------------------------------
const trackerFlow = ref<Flow>('Expense');
const granularity = ref<Granularity>('monthly');
const matrix = ref<Matrix | null>(null);
const trackerLoading = ref(false);
const monthlyYear = ref(now.getFullYear());
const dailyYear = ref(now.getFullYear());
const dailyMonth = ref(now.getMonth() + 1);

// --- Tab 2: Cashflow & Trajectory state -------------------------------------
const cashflowYear = ref(now.getFullYear());
const cashflowData = ref<CashflowReport | null>(null);

// --- Tab 3: Allocation Analysis state ---------------------------------------
const allocationYear = ref(now.getFullYear());
const allocationMonth = ref<number | null>(now.getMonth() + 1);
const envelopeData = ref<EnvelopeReport | null>(null);

const catName = (id: string | null) =>
  id ? displayName(categories.value.find((c) => c.id === id)?.name ?? null, locale.value) || id.slice(0, 6) : '—';
const monthShort = (m: number) => new Date(2020, m - 1, 1).toLocaleString(locale.value, { month: 'short' });
const daysInMonth = (y: number, m: number) => new Date(y, m, 0).getDate();
const pad = (n: number) => String(n).padStart(2, '0');

function compact(value: number): string {
  return value ? new Intl.NumberFormat(locale.value, { notation: 'compact', maximumFractionDigits: 1 }).format(value) : '';
}
const themeStore = useThemeStore();

const maxCell = computed(() => Math.max(1, ...(matrix.value?.rows.flatMap((r) => r.amounts) ?? [0])));
function heat(value: number): Record<string, string> {
  if (value <= 0) return {};
  const ratio = Math.min(1, value / maxCell.value);
  const alpha = Math.min(0.6, ratio * 0.5 + 0.08);
  const isIncome = trackerFlow.value === 'Income';

  if (isIncome) {
    return themeStore.isDark
      ? { backgroundColor: `rgba(53, 208, 122, ${alpha.toFixed(3)})`, color: alpha > 0.35 ? '#0b0f0c' : 'var(--text)' }
      : { backgroundColor: `rgba(4, 120, 87, ${alpha.toFixed(3)})`, color: alpha > 0.35 ? '#ffffff' : 'var(--text)' };
  } else {
    return themeStore.isDark
      ? { backgroundColor: `rgba(255, 91, 96, ${alpha.toFixed(3)})`, color: alpha > 0.35 ? '#0b0f0c' : 'var(--text)' }
      : { backgroundColor: `rgba(220, 38, 38, ${alpha.toFixed(3)})`, color: alpha > 0.35 ? '#ffffff' : 'var(--text)' };
  }
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

async function loadTracker(): Promise<void> {
  trackerLoading.value = true;
  try {
    const cy = now.getFullYear();
    const flow = trackerFlow.value;
    if (granularity.value === 'yearly') {
      const tr = await getCategoryTracker('monthly', `${cy - 4}-01-01`, `${cy}-12-31`, flow);
      const cols = Array.from({ length: 5 }, (_, i) => String(cy - 4 + i));
      matrix.value = buildFixed(tr, cols, (iso) => iso.slice(0, 4), cols);
    } else if (granularity.value === 'monthly') {
      const y = monthlyYear.value;
      const tr = await getCategoryTracker('monthly', `${y}-01-01`, `${y}-12-31`, flow);
      const cols = Array.from({ length: 12 }, (_, i) => pad(i + 1));
      const labels = Array.from({ length: 12 }, (_, i) => monthShort(i + 1));
      matrix.value = buildFixed(tr, cols, (iso) => iso.slice(5, 7), labels);
    } else {
      const dim = daysInMonth(dailyYear.value, dailyMonth.value);
      const mm = pad(dailyMonth.value);
      const tr = await getCategoryTracker('daily', `${dailyYear.value}-${mm}-01`, `${dailyYear.value}-${mm}-${pad(dim)}`, flow);
      const cols = Array.from({ length: dim }, (_, i) => pad(i + 1));
      const labels = Array.from({ length: dim }, (_, i) => String(i + 1));
      matrix.value = buildFixed(tr, cols, (iso) => iso.slice(8, 10), labels);
    }
  } catch {
    failed.value = true;
  } finally {
    trackerLoading.value = false;
  }
}

async function loadCashflow(): Promise<void> {
  try {
    cashflowData.value = await getCashflow(cashflowYear.value, 1);
  } catch {
    failed.value = true;
  }
}

async function loadAllocation(): Promise<void> {
  try {
    envelopeData.value = await getEnvelopeReport(allocationYear.value, allocationMonth.value ?? undefined);
  } catch {
    failed.value = true;
  }
}

async function switchTab(tab: Tab): Promise<void> {
  activeTab.value = tab;
  if (tab === 'cashflow' && !cashflowData.value) {
    await loadCashflow();
  } else if (tab === 'allocation' && !envelopeData.value) {
    await loadAllocation();
  }
}

function setGranularity(g: Granularity): void {
  granularity.value = g;
  loadTracker();
}
function setFlow(f: Flow): void {
  trackerFlow.value = f;
  loadTracker();
}
function changeMonthlyYear(delta: number): void {
  monthlyYear.value += delta;
  loadTracker();
}
function changeDailyMonth(delta: number): void {
  const d = new Date(dailyYear.value, dailyMonth.value - 1 + delta, 1);
  dailyYear.value = d.getFullYear();
  dailyMonth.value = d.getMonth() + 1;
  loadTracker();
}
function changeCashflowYear(delta: number): void {
  cashflowYear.value += delta;
  loadCashflow();
}
function changeAllocationYear(delta: number): void {
  allocationYear.value += delta;
  loadAllocation();
}
function setAllocationMonth(m: number | null): void {
  allocationMonth.value = m;
  loadAllocation();
}

const dailyMonthLabel = computed(() =>
  new Date(dailyYear.value, dailyMonth.value - 1, 1).toLocaleString(locale.value, { month: 'long', year: 'numeric' }),
);
const rangeLabel = computed(() => `${now.getFullYear() - 4}–${now.getFullYear()}`);

const topCategories = computed(() => matrix.value?.rows.slice(0, 7) ?? []);
const segments = computed(() => topCategories.value.map((c) => ({ label: catName(c.categoryId), value: c.total })));

// Trajectory aggregates
const annualIncome = computed(() => cashflowData.value?.trend.reduce((s, m) => s + m.income, 0) ?? 0);
const annualExpense = computed(() => cashflowData.value?.trend.reduce((s, m) => s + m.expense, 0) ?? 0);
const annualNet = computed(() => annualIncome.value - annualExpense.value);
const annualSavingsRate = computed(() =>
  annualIncome.value > 0 ? Math.round((annualNet.value / annualIncome.value) * 1000) / 10 : 0,
);
const avgMonthlyBurn = computed(() => {
  const monthsWithSpend = cashflowData.value?.trend.filter((m) => m.expense > 0) ?? [];
  return monthsWithSpend.length ? Math.round(annualExpense.value / monthsWithSpend.length) : 0;
});

// Envelope items with targets
const envelopeItemsWithTarget = computed(() => {
  if (!envelopeData.value) return [];
  const list = envelopeData.value.envelopes.map((e) => {
    const rawName = categories.value.find((c) => c.id === e.budgetCategoryId)?.name ?? '';
    const localized = catName(e.budgetCategoryId);
    let target = 0;
    if (rawName.toLowerCase().includes('need')) target = 50;
    else if (rawName.toLowerCase().includes('invest')) target = 40;
    else if (rawName.toLowerCase().includes('want')) target = 10;

    return {
      id: e.budgetCategoryId,
      name: localized,
      amount: e.amount,
      percent: e.percent,
      targetPercent: target,
      variance: Math.round((e.percent - target) * 10) / 10,
    };
  });
  return list;
});

const envelopeSegments = computed(() =>
  envelopeItemsWithTarget.value.map((e) => ({ label: e.name, value: e.amount })),
);

onMounted(async () => {
  loading.value = true;
  try {
    categories.value = await listCategories({ includeInactive: true });
    await loadTracker();
  } catch {
    failed.value = true;
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <div class="space-y-4">
    <!-- Top Navigation Tabs for Reports -->
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div class="inline-flex rounded-xl bg-surface border border-border p-1 shadow-sm">
        <button
          v-for="tab in (['tracker', 'cashflow', 'allocation'] as Tab[])"
          :key="tab"
          type="button"
          :aria-pressed="activeTab === tab"
          class="rounded-lg px-4 py-1.5 text-xs font-semibold transition-all"
          :class="activeTab === tab ? 'bg-accent text-accent-contrast shadow-sm' : 'text-text-muted hover:text-text'"
          @click="switchTab(tab)"
        >
          {{ t(`reports.tab${tab.charAt(0).toUpperCase() + tab.slice(1)}`) }}
        </button>
      </div>
    </div>

    <!-- TAB 1: CATEGORY TRACKER (HEATMAP MATRIX) -->
    <AppCard v-if="activeTab === 'tracker'">
      <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 class="text-sm font-semibold text-text">{{ t('reports.tracker') }}</h2>
          <p class="text-xs text-text-muted">{{ t('reports.trackerNote') }}</p>
        </div>
        <div class="flex flex-wrap items-center gap-2">
          <!-- Flow switch: Expenses vs Income -->
          <div class="inline-flex rounded-xl bg-surface-2 p-1 border border-border">
            <button
              v-for="f in (['Expense', 'Income'] as Flow[])"
              :key="f"
              type="button"
              :aria-pressed="trackerFlow === f"
              class="rounded-lg px-3 py-1 text-xs font-semibold transition-all"
              :class="[
                trackerFlow === f
                  ? f === 'Expense'
                    ? 'bg-negative text-negative-contrast font-bold shadow-sm'
                    : 'bg-positive text-positive-contrast font-bold shadow-sm'
                  : f === 'Expense'
                    ? 'text-text-muted hover:text-negative'
                    : 'text-text-muted hover:text-positive',
              ]"
              @click="setFlow(f)"
            >
              {{ f === 'Expense' ? t('reports.flowExpenses') : t('reports.flowIncome') }}
            </button>
          </div>

          <!-- Granularity toggle -->
          <div class="inline-flex rounded-xl bg-surface-2 p-1 border border-border">
            <button
              v-for="g in (['yearly', 'monthly', 'daily'] as Granularity[])"
              :key="g"
              type="button"
              :aria-pressed="granularity === g"
              class="rounded-lg px-3 py-1 text-xs font-semibold capitalize transition-all"
              :class="granularity === g ? 'bg-surface text-text shadow-sm' : 'text-text-muted hover:text-text'"
              @click="setGranularity(g)"
            >
              {{ t(`reports.${g}`) }}
            </button>
          </div>

          <!-- Daily nav -->
          <div v-if="granularity === 'daily'" class="flex items-center gap-1.5">
            <button class="rounded-lg border border-border p-1.5 text-text-muted hover:bg-surface-2 hover:text-text" :aria-label="t('transactions.prev')" @click="changeDailyMonth(-1)"><ChevronLeft :size="16" /></button>
            <span class="min-w-[7.5rem] text-center text-xs font-semibold text-text">{{ dailyMonthLabel }}</span>
            <button class="rounded-lg border border-border p-1.5 text-text-muted hover:bg-surface-2 hover:text-text" :aria-label="t('transactions.next')" @click="changeDailyMonth(1)"><ChevronRight :size="16" /></button>
          </div>
          <!-- Monthly nav -->
          <div v-else-if="granularity === 'monthly'" class="flex items-center gap-1.5">
            <button class="rounded-lg border border-border p-1.5 text-text-muted hover:bg-surface-2 hover:text-text" :aria-label="t('transactions.prev')" @click="changeMonthlyYear(-1)"><ChevronLeft :size="16" /></button>
            <span class="tnum min-w-[3.5rem] text-center text-xs font-semibold text-text">{{ monthlyYear }}</span>
            <button class="rounded-lg border border-border p-1.5 text-text-muted hover:bg-surface-2 hover:text-text" :aria-label="t('transactions.next')" @click="changeMonthlyYear(1)"><ChevronRight :size="16" /></button>
          </div>
          <span v-else class="tnum text-xs font-medium text-text-muted">{{ rangeLabel }}</span>
        </div>
      </div>

      <div v-if="trackerLoading || loading" class="space-y-2 py-2">
        <Skeleton v-for="i in 6" :key="i" class="h-8 w-full" />
      </div>
      <div v-else-if="failed" class="py-12 text-center text-sm text-text-muted">{{ t('errors.network_error') }}</div>

      <template v-else-if="matrix && matrix.rows.length">
        <div class="mb-5">
          <SpendBar :segments="segments" :label="t('reports.tracker')" />
          <ul class="mt-3 grid grid-cols-2 gap-x-6 gap-y-1.5 sm:grid-cols-3">
            <li v-for="(c, i) in topCategories" :key="c.categoryId" class="flex items-center justify-between text-xs">
              <span class="flex min-w-0 items-center gap-2">
                <span
                  class="h-2.5 w-2.5 shrink-0 rounded-full"
                  :style="{ backgroundColor: spectrumColor(i) }"
                  aria-hidden="true"
                />
                <span class="truncate text-text-muted font-medium">{{ catName(c.categoryId) }}</span>
              </span>
              <Money :value="c.total" class="ml-2 shrink-0 font-semibold tnum" />
            </li>
          </ul>
        </div>

        <div class="scroll-slim overflow-x-auto rounded-xl border border-border">
          <table class="w-full min-w-[640px] border-separate border-spacing-0 text-sm">
            <thead>
              <tr class="text-[11px] uppercase tracking-wider text-text-muted bg-surface-2/70">
                <th class="sticky left-0 z-10 bg-surface-2/90 border-r border-b border-border px-3.5 py-2.5 text-left font-bold">{{ t('reports.category') }}</th>
                <th v-for="(l, i) in matrix.labels" :key="i" class="border-b border-border px-2.5 py-2.5 text-right font-bold">{{ l }}</th>
                <th class="border-b border-border px-3.5 py-2.5 text-right font-bold">{{ t('reports.total') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in matrix.rows" :key="row.categoryId" class="hover:bg-surface-2/40 transition-colors">
                <td class="sticky left-0 z-10 truncate bg-surface border-r border-b border-border px-3.5 py-2.5 font-semibold text-text text-xs">{{ catName(row.categoryId) }}</td>
                <td v-for="(v, i) in row.amounts" :key="i" class="tnum border-b border-border/40 px-2 py-2 text-right text-xs font-medium" :style="heat(v)">{{ compact(v) }}</td>
                <td class="tnum border-b border-border px-3.5 py-2 text-right font-semibold text-xs"><Money :value="row.total" /></td>
              </tr>
            </tbody>
            <tfoot>
              <tr class="border-t-2 border-border text-xs font-bold bg-surface-2/50">
                <td class="sticky left-0 z-10 bg-surface-2/80 border-r border-border px-3.5 py-2.5">{{ t('reports.total') }}</td>
                <td v-for="(v, i) in matrix.periodTotals" :key="i" class="tnum px-2 py-2.5 text-right">{{ compact(v) }}</td>
                <td class="tnum px-3.5 py-2.5 text-right text-text"><Money :value="matrix.total" /></td>
              </tr>
            </tfoot>
          </table>
        </div>
      </template>
      <p v-else class="py-10 text-center text-[13px] text-text-muted">{{ t('reports.noSpend') }}</p>
    </AppCard>

    <!-- TAB 2: CASHFLOW & TRAJECTORY -->
    <div v-else-if="activeTab === 'cashflow'" class="space-y-4">
      <!-- Annual Summary Header Card -->
      <AppCard>
        <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 class="text-sm font-semibold">{{ t('reports.annualSummary') }}</h2>
            <p class="text-[13px] text-text-muted">{{ cashflowYear }} financial trajectory & savings performance.</p>
          </div>
          <div class="flex items-center gap-1.5">
            <button class="rounded-control border border-border p-1.5 text-text-muted hover:bg-surface-2 hover:text-text" :aria-label="t('transactions.prev')" @click="changeCashflowYear(-1)"><ChevronLeft :size="16" /></button>
            <span class="tnum min-w-[3.5rem] text-center text-[13px] font-semibold">{{ cashflowYear }}</span>
            <button class="rounded-control border border-border p-1.5 text-text-muted hover:bg-surface-2 hover:text-text" :aria-label="t('transactions.next')" @click="changeCashflowYear(1)"><ChevronRight :size="16" /></button>
          </div>
        </div>

        <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
          <div class="rounded-control border border-border bg-surface-2 p-3">
            <p class="text-xs text-text-muted">{{ t('reports.totalEarned') }}</p>
            <p class="tnum mt-1 text-lg font-bold text-positive"><Money :value="annualIncome" /></p>
          </div>
          <div class="rounded-control border border-border bg-surface-2 p-3">
            <p class="text-xs text-text-muted">{{ t('reports.totalSpent') }}</p>
            <p class="tnum mt-1 text-lg font-bold text-negative"><Money :value="-annualExpense" /></p>
          </div>
          <div class="rounded-control border border-border bg-surface-2 p-3">
            <p class="text-xs text-text-muted">{{ t('reports.netAdded') }}</p>
            <p class="tnum mt-1 text-lg font-bold" :class="annualNet >= 0 ? 'text-positive' : 'text-negative'"><Money :value="annualNet" /></p>
          </div>
          <div class="rounded-control border border-border bg-surface-2 p-3">
            <p class="text-xs text-text-muted">{{ t('reports.avgSavingsRate') }}</p>
            <p class="tnum mt-1 text-lg font-bold" :class="annualSavingsRate >= 20 ? 'text-accent' : (annualSavingsRate >= 0 ? 'text-text' : 'text-negative')">
              {{ annualSavingsRate }}%
            </p>
          </div>
          <div class="rounded-control border border-border bg-surface-2 p-3">
            <p class="text-xs text-text-muted">{{ t('reports.avgMonthlyBurn') }}</p>
            <p class="tnum mt-1 text-lg font-bold text-text"><Money :value="avgMonthlyBurn" /></p>
          </div>
        </div>
      </AppCard>

      <!-- Cashflow Chart -->
      <AppCard>
        <div class="mb-2">
          <h3 class="text-sm font-semibold">{{ t('dashboard.cashflow') }}</h3>
        </div>
        <CashflowChart :months="cashflowData?.trend ?? []" mode="cashflow" />
      </AppCard>

      <!-- 12-Month Performance Table -->
      <AppCard>
        <div class="scroll-slim overflow-x-auto">
          <table class="w-full min-w-[600px] text-sm">
            <thead>
              <tr class="border-b border-border text-left text-xs uppercase text-text-muted">
                <th class="py-2.5 pl-2">{{ t('reports.month') }}</th>
                <th class="py-2.5 text-right">{{ t('reports.income') }}</th>
                <th class="py-2.5 text-right">{{ t('reports.expense') }}</th>
                <th class="py-2.5 text-right">{{ t('reports.net') }}</th>
                <th class="py-2.5 pr-2 text-right">{{ t('reports.savingsRate') }}</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-border">
              <tr v-for="m in (cashflowData?.trend ?? [])" :key="m.month" class="hover:bg-surface-2/40">
                <td class="py-2.5 pl-2 font-medium">{{ monthShort(m.month) }}</td>
                <td class="tnum py-2.5 text-right text-positive font-medium"><Money :value="m.income" /></td>
                <td class="tnum py-2.5 text-right text-text-muted"><Money :value="-m.expense" /></td>
                <td class="tnum py-2.5 text-right font-semibold" :class="m.net >= 0 ? 'text-positive' : 'text-negative'"><Money :value="m.net" /></td>
                <td class="tnum py-2.5 pr-2 text-right font-medium" :class="m.savingsRate && m.savingsRate >= 20 ? 'text-accent' : (m.savingsRate && m.savingsRate >= 0 ? 'text-text' : 'text-negative')">
                  {{ m.savingsRate ?? (m.income > 0 ? Math.round(((m.income - m.expense) / m.income) * 1000) / 10 : 0) }}%
                </td>
              </tr>
            </tbody>
            <tfoot>
              <tr class="border-t-2 border-border font-bold text-text">
                <td class="py-3 pl-2">{{ t('reports.total') }}</td>
                <td class="tnum py-3 text-right text-positive"><Money :value="annualIncome" /></td>
                <td class="tnum py-3 text-right text-negative"><Money :value="-annualExpense" /></td>
                <td class="tnum py-3 text-right" :class="annualNet >= 0 ? 'text-positive' : 'text-negative'"><Money :value="annualNet" /></td>
                <td class="tnum py-3 pr-2 text-right text-accent">{{ annualSavingsRate }}%</td>
              </tr>
            </tfoot>
          </table>
        </div>
      </AppCard>
    </div>

    <!-- TAB 3: ALLOCATION (50/40/10) -->
    <div v-else-if="activeTab === 'allocation'" class="space-y-4">
      <AppCard>
        <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 class="text-sm font-semibold">{{ t('reports.allocationTitle') }}</h2>
            <p class="text-[13px] text-text-muted">{{ t('reports.allocationSubtitle') }}</p>
          </div>
          <div class="flex items-center gap-1.5">
            <button class="rounded-control border border-border p-1.5 text-text-muted hover:bg-surface-2 hover:text-text" :aria-label="t('transactions.prev')" @click="changeAllocationYear(-1)"><ChevronLeft :size="16" /></button>
            <span class="tnum min-w-[3.5rem] text-center text-[13px] font-semibold">{{ allocationYear }}</span>
            <button class="rounded-control border border-border p-1.5 text-text-muted hover:bg-surface-2 hover:text-text" :aria-label="t('transactions.next')" @click="changeAllocationYear(1)"><ChevronRight :size="16" /></button>
          </div>
        </div>

        <!-- Month Filter Pills -->
        <div class="mb-5 flex flex-wrap items-center gap-1">
          <button
            type="button"
            class="rounded-control px-3 py-1 text-xs font-medium transition"
            :class="allocationMonth === null ? 'bg-accent text-accent-contrast' : 'border border-border text-text-muted hover:text-text'"
            @click="setAllocationMonth(null)"
          >
            {{ t('reports.yearly') }}
          </button>
          <button
            v-for="m in 12"
            :key="m"
            type="button"
            class="rounded-control px-2.5 py-1 text-xs font-medium transition"
            :class="allocationMonth === m ? 'bg-accent text-accent-contrast' : 'border border-border text-text-muted hover:text-text'"
            @click="setAllocationMonth(m)"
          >
            {{ monthShort(m) }}
          </button>
        </div>

        <!-- Spend Bar -->
        <div v-if="envelopeSegments.length" class="mb-6">
          <SpendBar :segments="envelopeSegments" label="Envelopes" />
        </div>

        <!-- Formula Rule Cards -->
        <div class="grid gap-3 sm:grid-cols-3">
          <div
            v-for="env in envelopeItemsWithTarget"
            :key="env.name"
            class="rounded-control border border-border bg-surface-2 p-3.5"
          >
            <div class="flex items-center justify-between">
              <span class="text-sm font-bold text-text">{{ env.name }}</span>
              <span
                class="inline-flex items-center gap-1 rounded px-2 py-0.5 text-[11px] font-semibold"
                :class="env.targetPercent && env.percent <= env.targetPercent ? 'bg-accent-soft text-accent' : 'bg-negative/15 text-negative'"
              >
                <component :is="env.targetPercent && env.percent <= env.targetPercent ? CheckCircle2 : AlertTriangle" :size="12" />
                {{ env.targetPercent && env.percent <= env.targetPercent ? t('reports.compliant') : t('reports.overTarget') }}
              </span>
            </div>
            <div class="mt-3 flex items-baseline justify-between">
              <span class="text-xs text-text-muted">{{ t('reports.actualRule') }}</span>
              <span class="tnum text-base font-bold text-text">{{ env.percent }}% (<Money :value="env.amount" />)</span>
            </div>
            <div class="mt-1 flex items-baseline justify-between text-xs text-text-muted">
              <span>{{ t('reports.targetRule') }}</span>
              <span class="tnum font-medium">{{ env.targetPercent }}%</span>
            </div>
          </div>
        </div>

        <p v-if="!envelopeItemsWithTarget.length" class="py-12 text-center text-sm text-text-muted">
          {{ t('reports.noDataPeriod') }}
        </p>
      </AppCard>
    </div>
  </div>
</template>
