<script setup lang="ts">
import { onMounted, ref, computed } from 'vue';
import { RouterLink } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { Plus, ArrowRight, TrendingUp, ShieldCheck, Sparkles } from 'lucide-vue-next';
import {
  getCashflow, getNetWorth, getCategoryTracker, getFinancialHealth, getEnvelopeReport,
} from '@/lib/reports';
import { listTransactions } from '@/lib/transactions';
import { listCategories } from '@/lib/categories';
import type {
  CashflowReport, Category, EnvelopeReport, FinancialHealthReport, NetWorthReport, Transaction,
} from '@/types/api';
import { displayName } from '@/lib/seededNames';
import { formatShortDate } from '@/lib/format';
import { chart } from '@/lib/chartTheme';
import BalanceCard from '@/components/ui/BalanceCard.vue';
import AppCard from '@/components/ui/AppCard.vue';
import SpendBar from '@/components/ui/SpendBar.vue';
import { spectrumColor } from '@/lib/spectrum';
import CashflowChart from '@/components/ui/CashflowChart.vue';
import DonutChart from '@/components/ui/DonutChart.vue';
import AvatarChip from '@/components/ui/AvatarChip.vue';
import Money from '@/components/ui/Money.vue';
import AppButton from '@/components/ui/AppButton.vue';
import LoadingBlock from '@/components/ui/LoadingBlock.vue';

const { t, locale } = useI18n();

const netWorth = ref<NetWorthReport | null>(null);
const cashflow = ref<CashflowReport | null>(null);
const health = ref<FinancialHealthReport | null>(null);
const envelopes = ref<EnvelopeReport | null>(null);
const categories = ref<Category[]>([]);
const monthSpend = ref<{ categoryId: string; total: number }[]>([]);
const monthIncome = ref<{ categoryId: string; total: number }[]>([]);
const recent = ref<Transaction[]>([]);
const loading = ref(true);
const failed = ref(false);

const cashflowMode = ref<'cashflow' | 'savings'>('cashflow');
const donutMode = ref<'category' | 'envelope' | 'income'>('category');

const now = new Date();
const pad = (n: number) => String(n).padStart(2, '0');
const catName = (id: string | null) =>
  id ? displayName(categories.value.find((c) => c.id === id)?.name ?? null, locale.value) || '—' : '—';

const currency = computed(() => netWorth.value?.currencyCode ?? 'IDR');
const accounts = computed(() => netWorth.value?.accounts ?? []);

const nwSegments = computed(() =>
  accounts.value.filter((a) => a.balance > 0).map((a) => ({ label: a.name, value: a.balance })),
);

const currentMonthLabel = computed(() =>
  new Date().toLocaleDateString(locale.value, { month: 'long', year: 'numeric' }),
);

// Dynamic Donut data based on chosen distribution mode
const donutData = computed(() => {
  if (donutMode.value === 'envelope') {
    if (!envelopes.value?.envelopes.length) return [];
    return envelopes.value.envelopes.map((e) => ({
      name: catName(e.budgetCategoryId),
      value: e.amount,
    }));
  }

  if (donutMode.value === 'income') {
    if (!monthIncome.value.length) return [];
    const sorted = [...monthIncome.value].sort((a, b) => b.total - a.total);
    const top = sorted.slice(0, 5).map((s) => ({ name: catName(s.categoryId), value: s.total }));
    const rest = sorted.slice(5).reduce((sum, s) => sum + s.total, 0);
    if (rest > 0) top.push({ name: t('dashboard.others'), value: rest });
    return top;
  }

  // Default: Expenses by Category
  const sorted = [...monthSpend.value].sort((a, b) => b.total - a.total);
  const top = sorted.slice(0, 6).map((s) => ({ name: catName(s.categoryId), value: s.total }));
  const rest = sorted.slice(6).reduce((sum, s) => sum + s.total, 0);
  if (rest > 0) top.push({ name: t('dashboard.others'), value: rest });
  return top;
});

const donutColor = (i: number) => chart.spectrum[i % chart.spectrum.length];

function signedAmount(tx: Transaction): number {
  return tx.type === 'Expense' ? -tx.amount : tx.amount;
}

// Decision support insights derived from real numbers
const insights = computed(() => {
  const list: { icon: 'trend' | 'shield' | 'sparkle'; text: string; type: 'positive' | 'neutral' | 'warning' }[] = [];
  if (!health.value) return list;

  // 1. Savings insight
  const rate = health.value.savingsRate;
  if (rate >= 20) {
    list.push({
      icon: 'trend',
      text: t('dashboard.insightHealthySavings', { rate }),
      type: 'positive',
    });
  } else if (rate >= 0) {
    list.push({
      icon: 'trend',
      text: t('dashboard.insightLowSavings', { rate }),
      type: 'neutral',
    });
  } else {
    const deficit = (cashflow.value?.expense ?? 0) - (cashflow.value?.income ?? 0);
    list.push({
      icon: 'trend',
      text: t('dashboard.insightDeficit', {
        amount: new Intl.NumberFormat(locale.value, { style: 'currency', currency: currency.value, maximumFractionDigits: 0 }).format(deficit),
      }),
      type: 'warning',
    });
  }

  // 2. Runway insight
  if (health.value.runwayMonths >= 6) {
    list.push({
      icon: 'shield',
      text: t('dashboard.insightRunwaySafe', { months: health.value.runwayMonths }),
      type: 'positive',
    });
  }

  // 3. Top expense category insight
  if (monthSpend.value.length) {
    const top = monthSpend.value.slice().sort((a, b) => b.total - a.total)[0];
    const totalExp = cashflow.value?.expense ?? 1;
    const pct = Math.round((top.total / (totalExp || 1)) * 100);
    list.push({
      icon: 'sparkle',
      text: t('dashboard.insightTopCategory', {
        name: catName(top.categoryId),
        amount: new Intl.NumberFormat(locale.value, { style: 'currency', currency: currency.value, maximumFractionDigits: 0 }).format(top.total),
        percent: pct,
      }),
      type: 'neutral',
    });
  }

  return list;
});

async function load(): Promise<void> {
  loading.value = true;
  failed.value = false;
  try {
    const y = now.getFullYear();
    const m = now.getMonth() + 1;
    const dim = new Date(y, m, 0).getDate();
    const [nw, cf, fh, env, cats, spend, income, txns] = await Promise.all([
      getNetWorth(),
      getCashflow(y, m),
      getFinancialHealth(y, m),
      getEnvelopeReport(y, m),
      listCategories({ includeInactive: true }),
      getCategoryTracker('monthly', `${y}-${pad(m)}-01`, `${y}-${pad(m)}-${pad(dim)}`, 'Expense'),
      getCategoryTracker('monthly', `${y}-${pad(m)}-01`, `${y}-${pad(m)}-${pad(dim)}`, 'Income'),
      listTransactions({ page: 1, pageSize: 10 }),
    ]);
    netWorth.value = nw;
    cashflow.value = cf;
    health.value = fh;
    envelopes.value = env;
    categories.value = cats;
    monthSpend.value = spend.categories.map((c) => ({ categoryId: c.categoryId, total: c.total }));
    monthIncome.value = income.categories.map((c) => ({ categoryId: c.categoryId, total: c.total }));
    recent.value = txns.items;
  } catch {
    failed.value = true;
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div>
    <LoadingBlock v-if="loading" />

    <div v-else-if="failed" class="py-24 text-center">
      <p class="text-sm text-text-muted">{{ t('errors.network_error') }}</p>
      <AppButton class="mt-4" variant="secondary" @click="load">{{ t('common.retry') }}</AppButton>
    </div>

    <div v-else class="space-y-4">
      <!-- 1. Net worth hero + this-month -->
      <div class="grid gap-4 lg:grid-cols-3">
        <BalanceCard
          class="lg:col-span-2"
          :label="t('dashboard.netWorth')"
          :value="netWorth?.total ?? 0"
          :currency="currency"
          :caption="t('dashboard.acrossAccounts', { count: accounts.length })"
        >
          <template #footer>
            <div v-if="nwSegments.length">
              <SpendBar :segments="nwSegments" :label="t('dashboard.netWorth')" />
              <ul class="mt-2.5 flex flex-wrap gap-x-4 gap-y-1">
                <li
                  v-for="(seg, i) in nwSegments"
                  :key="seg.label"
                  class="flex items-center gap-1.5 text-[12px] text-text-muted"
                >
                  <span
                    class="h-2 w-2 shrink-0 rounded-full"
                    :style="{ backgroundColor: spectrumColor(i) }"
                    aria-hidden="true"
                  />
                  <span class="truncate">{{ seg.label }}</span>
                </li>
              </ul>
            </div>
          </template>
        </BalanceCard>

        <AppCard>
          <div class="flex flex-wrap items-baseline justify-between gap-x-2">
            <h2 class="text-sm font-semibold">{{ t('dashboard.thisMonth') }}</h2>
            <span class="text-[12px] text-text-muted">{{ currentMonthLabel }}</span>
          </div>
          <dl class="mt-3 divide-y divide-border">
            <div class="flex items-center justify-between py-2.5">
              <dt class="text-[13px] text-text-muted">{{ t('dashboard.monthIncome') }}</dt>
              <dd><Money :value="cashflow?.income ?? 0" :currency="currency" colored class="text-sm font-medium" /></dd>
            </div>
            <div class="flex items-center justify-between py-2.5">
              <dt class="text-[13px] text-text-muted">{{ t('dashboard.monthExpense') }}</dt>
              <dd><Money :value="-(cashflow?.expense ?? 0)" :currency="currency" colored class="text-sm font-medium" /></dd>
            </div>
            <div class="flex items-center justify-between py-2.5">
              <dt class="text-[13px] font-medium">{{ t('dashboard.monthNet') }}</dt>
              <dd><Money :value="cashflow?.net ?? 0" :currency="currency" colored class="text-sm font-semibold" /></dd>
            </div>
          </dl>
        </AppCard>
      </div>

      <!-- 2. Decision Support KPI Strip (3 core financial health pillars) -->
      <div class="grid gap-4 sm:grid-cols-3">
        <!-- Savings Rate Pillar -->
        <AppCard>
          <div class="flex items-center justify-between">
            <span class="text-xs font-medium text-text-muted">{{ t('dashboard.savingsRate') }}</span>
            <span
              v-if="health"
              class="rounded px-2 py-0.5 text-[11px] font-semibold"
              :class="{
                'bg-accent-soft text-accent': health.healthStatus === 'Excellent' || health.healthStatus === 'Healthy',
                'bg-amber-400/10 text-amber-400': health.healthStatus === 'Low',
                'bg-negative/15 text-negative': health.healthStatus === 'Deficit',
              }"
            >
              {{ t(`dashboard.status${health.healthStatus}`) }}
            </span>
          </div>
          <div class="mt-2 flex items-baseline gap-2">
            <span class="tnum text-2xl font-bold tracking-tight" :class="health && health.savingsRate >= 0 ? 'text-text' : 'text-negative'">
              {{ health?.savingsRate ?? 0 }}%
            </span>
            <span class="text-[12px] text-text-muted">{{ t('dashboard.savingsTarget') }}</span>
          </div>
          <p v-if="health?.momExpensePercent !== null" class="mt-1 text-[12px] text-text-muted">
            <span v-if="health?.previousSavingsRate !== null" class="tnum">
              {{ health?.previousSavingsRate }}% {{ t('reports.worstMonth').toLowerCase() }}
            </span>
          </p>
        </AppCard>

        <!-- Financial Runway Pillar -->
        <AppCard>
          <div class="flex items-center justify-between">
            <span class="text-xs font-medium text-text-muted">{{ t('dashboard.runway') }}</span>
            <ShieldCheck :size="16" class="text-accent" />
          </div>
          <div class="mt-2 flex items-baseline gap-1.5">
            <span class="tnum text-2xl font-bold tracking-tight text-text">
              {{ health?.runwayMonths ?? 0 }}
            </span>
            <span class="text-sm font-medium text-text-muted">{{ t('dashboard.runwayMonths', { months: '' }).trim() }}</span>
          </div>
          <p class="mt-1 truncate text-[12px] text-text-muted">
            {{ t('dashboard.avgBurn', { amount: new Intl.NumberFormat(locale, { notation: 'compact', maximumFractionDigits: 1 }).format(health?.trailing3MonthAvgExpense ?? 0) }) }}
          </p>
        </AppCard>

        <!-- Burn Pace & Projected Month End -->
        <AppCard>
          <div class="flex items-center justify-between">
            <span class="text-xs font-medium text-text-muted">{{ t('dashboard.dailyBurn') }}</span>
            <span class="tnum text-[11px] text-text-muted">
              {{ t('dashboard.pacePassed', { day: health?.daysPassed ?? 1, total: health?.totalDaysInMonth ?? 30, percent: Math.round(((health?.daysPassed ?? 1) / (health?.totalDaysInMonth ?? 30)) * 100) }) }}
            </span>
          </div>
          <div class="mt-2 flex items-baseline gap-2">
            <span class="tnum text-2xl font-bold tracking-tight text-text">
              <Money :value="health?.dailyBurnRate ?? 0" :currency="currency" />
            </span>
            <span class="text-[12px] text-text-muted">/ {{ t('reports.daily').toLowerCase() }}</span>
          </div>
          <p class="mt-1 truncate text-[12px] text-text-muted">
            {{ t('dashboard.projectedMonthEnd') }}: <Money :value="health?.projectedMonthEndExpense ?? 0" :currency="currency" class="font-medium text-text" />
          </p>
        </AppCard>
      </div>

      <!-- 3. Cashflow Trend (with toggle) + Multi-mode Donut -->
      <div class="grid gap-4 lg:grid-cols-3">
        <AppCard class="lg:col-span-2">
          <div class="mb-3 flex flex-wrap items-center justify-between gap-2">
            <div class="flex items-center gap-3">
              <h2 class="text-sm font-semibold">{{ t('dashboard.cashflow') }}</h2>
              <span class="tnum text-[12px] text-text-muted">{{ cashflow?.year }}</span>
            </div>
            <!-- View switch: Cashflow bars vs Savings rate trend -->
            <div class="flex rounded-control border border-border p-0.5">
              <button
                type="button"
                :aria-pressed="cashflowMode === 'cashflow'"
                class="rounded-[9px] px-2.5 py-0.5 text-xs font-medium transition"
                :class="cashflowMode === 'cashflow' ? 'bg-accent-soft text-accent' : 'text-text-muted hover:text-text'"
                @click="cashflowMode = 'cashflow'"
              >
                {{ t('dashboard.viewCashflow') }}
              </button>
              <button
                type="button"
                :aria-pressed="cashflowMode === 'savings'"
                class="rounded-[9px] px-2.5 py-0.5 text-xs font-medium transition"
                :class="cashflowMode === 'savings' ? 'bg-accent-soft text-accent' : 'text-text-muted hover:text-text'"
                @click="cashflowMode = 'savings'"
              >
                {{ t('dashboard.viewSavingsRate') }}
              </button>
            </div>
          </div>
          <CashflowChart :months="cashflow?.trend ?? []" :currency="currency" :mode="cashflowMode" />
        </AppCard>

        <!-- Donut with distribution switch -->
        <AppCard>
          <div class="mb-3 flex flex-wrap items-center justify-between gap-2">
            <h2 class="text-sm font-semibold">{{ t('dashboard.distribution') }}</h2>
            <div class="flex rounded-control border border-border p-0.5 text-xs">
              <button
                type="button"
                :aria-pressed="donutMode === 'category'"
                class="rounded-[9px] px-2 py-0.5 font-medium transition"
                :class="donutMode === 'category' ? 'bg-accent-soft text-accent' : 'text-text-muted hover:text-text'"
                @click="donutMode = 'category'"
              >
                {{ t('dashboard.byCategory') }}
              </button>
              <button
                type="button"
                :aria-pressed="donutMode === 'envelope'"
                class="rounded-[9px] px-2 py-0.5 font-medium transition"
                :class="donutMode === 'envelope' ? 'bg-accent-soft text-accent' : 'text-text-muted hover:text-text'"
                @click="donutMode = 'envelope'"
              >
                {{ t('dashboard.byEnvelope') }}
              </button>
              <button
                type="button"
                :aria-pressed="donutMode === 'income'"
                class="rounded-[9px] px-2 py-0.5 font-medium transition"
                :class="donutMode === 'income' ? 'bg-accent-soft text-accent' : 'text-text-muted hover:text-text'"
                @click="donutMode = 'income'"
              >
                {{ t('dashboard.byIncome') }}
              </button>
            </div>
          </div>

          <template v-if="donutData.length">
            <DonutChart :data="donutData" :currency="currency" />
            <ul class="mt-2 space-y-1.5">
              <li v-for="(d, i) in donutData" :key="i" class="flex items-center gap-2 text-[13px]">
                <span class="h-2.5 w-2.5 shrink-0 rounded-sm" :style="{ backgroundColor: donutColor(i) }" />
                <span class="truncate text-text-muted">{{ d.name }}</span>
                <Money :value="d.value" :currency="currency" class="ml-auto shrink-0 font-medium" />
              </li>
            </ul>
          </template>
          <p v-else class="py-12 text-center text-[13px] text-text-muted">{{ t('dashboard.noExpenses') }}</p>
        </AppCard>
      </div>

      <!-- 4. Intelligent Decision Support Insights -->
      <AppCard v-if="insights.length">
        <div class="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-text-muted">
          <Sparkles :size="14" class="text-accent" />
          {{ t('dashboard.insightsTitle') }}
        </div>
        <div class="mt-3 grid gap-2.5 sm:grid-cols-2 lg:grid-cols-3">
          <div
            v-for="(insight, i) in insights"
            :key="i"
            class="flex items-start gap-2.5 rounded-control border border-border bg-surface-2 p-3 text-[13px]"
          >
            <component
              :is="insight.icon === 'trend' ? TrendingUp : (insight.icon === 'shield' ? ShieldCheck : Sparkles)"
              :size="16"
              class="mt-0.5 shrink-0"
              :class="{
                'text-accent': insight.type === 'positive',
                'text-text-muted': insight.type === 'neutral',
                'text-negative': insight.type === 'warning',
              }"
            />
            <span class="text-text">{{ insight.text }}</span>
          </div>
        </div>
      </AppCard>

      <!-- 5. Recent transactions + accounts -->
      <div class="grid gap-4 lg:grid-cols-2">
        <AppCard :padded="false">
          <div class="flex items-center justify-between px-5 py-4">
            <h2 class="text-sm font-semibold">{{ t('dashboard.recent') }}</h2>
            <RouterLink :to="{ name: 'transactions' }" class="inline-flex min-h-[24px] items-center gap-1 py-1 text-[13px] font-medium text-accent hover:underline">
              {{ t('dashboard.viewAll') }}<ArrowRight :size="14" />
            </RouterLink>
          </div>
          <ul v-if="recent.length" class="divide-y divide-border">
            <li v-for="tx in recent" :key="tx.id" class="flex items-center gap-3 px-5 py-2.5">
              <AvatarChip :name="tx.title" />
              <div class="min-w-0 flex-1">
                <p class="truncate text-sm font-medium">{{ tx.title }}</p>
                <p class="truncate text-[13px] text-text-muted">{{ formatShortDate(tx.date, locale) }} · {{ catName(tx.categoryId) }}</p>
              </div>
              <Money :value="signedAmount(tx)" :currency="tx.currencyCode" :colored="tx.type === 'Income'" class="shrink-0 text-sm font-medium" />
            </li>
          </ul>
          <div v-else class="px-5 py-8 text-center">
            <p class="text-[13px] text-text-muted">{{ t('dashboard.noTransactions') }}</p>
            <RouterLink :to="{ name: 'transactions' }" class="mt-4 inline-block">
              <AppButton variant="secondary"><Plus :size="16" />{{ t('dashboard.addTransaction') }}</AppButton>
            </RouterLink>
          </div>
        </AppCard>

        <AppCard :padded="false">
          <div class="flex items-center justify-between px-5 py-4">
            <h2 class="text-sm font-semibold">{{ t('dashboard.accounts') }}</h2>
            <RouterLink v-if="accounts.length" :to="{ name: 'accounts' }" class="inline-flex min-h-[24px] items-center gap-1 py-1 text-[13px] font-medium text-accent hover:underline">
              {{ t('dashboard.viewAll') }}<ArrowRight :size="14" />
            </RouterLink>
          </div>
          <ul v-if="accounts.length" class="divide-y divide-border">
            <li v-for="account in accounts" :key="account.accountId" class="flex items-center justify-between px-5 py-2.5">
              <div class="min-w-0">
                <p class="truncate text-sm font-medium">{{ account.name }}</p>
                <p class="text-[13px] text-text-muted">{{ t(`enums.accountType.${account.type}`) }}</p>
              </div>
              <Money :value="account.balance" :currency="account.currencyCode" class="shrink-0 text-sm font-medium" />
            </li>
          </ul>
          <div v-else class="px-5 py-8 text-center">
            <p class="text-[13px] text-text-muted">{{ t('dashboard.noAccounts') }}</p>
            <RouterLink :to="{ name: 'accounts' }" class="mt-4 inline-block">
              <AppButton variant="secondary"><Plus :size="16" />{{ t('dashboard.addAccount') }}</AppButton>
            </RouterLink>
          </div>
        </AppCard>
      </div>
    </div>
  </div>
</template>
