<script setup lang="ts">
import { onMounted, ref, computed, watch } from 'vue';
import { RouterLink } from 'vue-router';
import { useI18n } from 'vue-i18n';
import {
  Plus,
  ArrowRight,
  TrendingUp,
  ShieldCheck,
  Sparkles,
  ArrowDownLeft,
  ArrowUpRight,
  ArrowLeftRight,
  Wallet,
  Building2,
  CreditCard,
  BarChart3,
  PieChart,
} from 'lucide-vue-next';
import {
  getCashflow,
  getNetWorth,
  getCategoryTracker,
  getFinancialHealth,
  getEnvelopeReport,
} from '@/lib/reports';
import { getSafeToSpend } from '@/lib/decision';
import { listTransactions } from '@/lib/transactions';
import { listCategories } from '@/lib/categories';
import type {
  CashflowReport,
  Category,
  EnvelopeReport,
  FinancialHealthReport,
  NetWorthReport,
  SafeToSpendDto,
  Transaction,
} from '@/types/api';
import { displayName } from '@/lib/seededNames';
import { formatShortDate } from '@/lib/format';
import { getChartTheme } from '@/lib/chartTheme';
import { useThemeStore } from '@/stores/theme';
import { useUiStore } from '@/stores/ui';
import { useTransactionModalStore } from '@/stores/transactionModal';
import SpendBar from '@/components/ui/SpendBar.vue';
import CashflowChart from '@/components/ui/CashflowChart.vue';
import DonutChart from '@/components/ui/DonutChart.vue';
import Money from '@/components/ui/Money.vue';
import AppButton from '@/components/ui/AppButton.vue';
import LoadingBlock from '@/components/ui/LoadingBlock.vue';
import InsightsPanel from '@/components/decision/InsightsPanel.vue';

const { t, locale } = useI18n();
const themeStore = useThemeStore();
const ui = useUiStore();
const transactionModal = useTransactionModalStore();

const netWorth = ref<NetWorthReport | null>(null);
const cashflow = ref<CashflowReport | null>(null);
const health = ref<FinancialHealthReport | null>(null);
const envelopes = ref<EnvelopeReport | null>(null);
const safeToSpend = ref<SafeToSpendDto | null>(null);
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

// Active account tab in the decorative/functional card
const selectedAccountId = ref<string | null>(null);
const activeAccount = computed(() => {
  if (!accounts.value.length) return null;
  return accounts.value.find((a) => a.accountId === selectedAccountId.value) ?? accounts.value[0];
});

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

const donutColor = (i: number) => {
  const ct = getChartTheme(themeStore.isDark);
  return ct.spectrum[i % ct.spectrum.length];
};

function signedAmount(tx: Transaction): number {
  return tx.type === 'Expense' ? -tx.amount : tx.amount;
}

// Quick action shortcuts
function onSend(): void {
  transactionModal.openCreate({ type: 'Expense', accountId: activeAccount.value?.accountId });
}

function onReceive(): void {
  transactionModal.openCreate({ type: 'Income', accountId: activeAccount.value?.accountId });
}

function onAddRecord(): void {
  transactionModal.openCreate({ accountId: activeAccount.value?.accountId });
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
    const [nw, cf, fh, env, cats, spend, income, txns, safe] = await Promise.all([
      getNetWorth(),
      getCashflow(y, m),
      getFinancialHealth(y, m),
      getEnvelopeReport(y, m),
      listCategories({ includeInactive: true }),
      getCategoryTracker('monthly', `${y}-${pad(m)}-01`, `${y}-${pad(m)}-${pad(dim)}`, 'Expense'),
      getCategoryTracker('monthly', `${y}-${pad(m)}-01`, `${y}-${pad(m)}-${pad(dim)}`, 'Income'),
      listTransactions({ page: 1, pageSize: 10 }),
      getSafeToSpend().catch(() => null),
    ]);
    netWorth.value = nw;
    cashflow.value = cf;
    health.value = fh;
    envelopes.value = env;
    categories.value = cats;
    monthSpend.value = spend.categories.map((c) => ({ categoryId: c.categoryId, total: c.total }));
    monthIncome.value = income.categories.map((c) => ({ categoryId: c.categoryId, total: c.total }));
    recent.value = txns.items;
    safeToSpend.value = safe;

    if (!selectedAccountId.value && nw.accounts.length > 0) {
      selectedAccountId.value = nw.accounts[0].accountId;
    }
  } catch {
    failed.value = true;
  } finally {
    loading.value = false;
  }
}

// Auto-reload when transactions are created or updated
watch(
  () => transactionModal.lastEventTimestamp,
  () => {
    load();
  },
);

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <LoadingBlock v-if="loading" />

    <div v-else-if="failed" class="py-24 text-center">
      <p class="text-sm text-text-muted">{{ t('errors.network_error') }}</p>
      <AppButton class="mt-4" variant="secondary" @click="load">{{ t('common.retry') }}</AppButton>
    </div>

    <div v-else class="space-y-6">
      <!-- 1. Top Row: 4-Tier KPI & Decision Support Row -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <!-- Safe-to-Spend (Uncommitted Liquidity Decision Card) -->
        <div class="rounded-card border border-primary/30 bg-primary/[0.04] p-5 shadow-card transition-all flex flex-col justify-between">
          <div class="flex items-center justify-between gap-2">
            <div class="flex items-center gap-2 min-w-0">
              <span class="text-xs font-bold uppercase tracking-wider text-primary truncate">{{ t('decision.safeToSpend') }}</span>
              <span class="shrink-0 rounded-full bg-primary/15 px-2 py-0.5 text-[10px] font-bold text-primary">
                {{ safeToSpend?.daysRemaining ?? 0 }} {{ t('decision.days') }}
              </span>
            </div>
            <button
              type="button"
              class="shrink-0 flex items-center gap-1 px-2.5 py-1 text-xs font-semibold rounded-lg bg-primary text-primary-contrast hover:opacity-90 shadow-sm transition-all"
              @click="ui.openSimulator()"
              :title="t('decision.simulatePrompt')"
            >
              <Sparkles :size="13" />
              <span>{{ t('decision.simulateAction') }}</span>
            </button>
          </div>
          <div class="mt-3">
            <div class="tnum text-2xl font-bold tracking-tight text-text sm:text-3xl whitespace-nowrap overflow-hidden text-ellipsis">
              <Money :value="safeToSpend?.safeToSpend ?? 0" :currency="currency" />
            </div>
          </div>
          <div class="mt-3 flex items-center justify-between border-t border-border/80 pt-2.5 text-xs text-text-muted">
            <span>{{ t('decision.dailyPacing') }}</span>
            <span class="font-semibold text-text tnum whitespace-nowrap">
              <Money :value="safeToSpend?.dailyAllowance ?? 0" :currency="currency" /> / {{ t('decision.day') }}
            </span>
          </div>
        </div>

        <!-- Net Worth / Total Balance Card -->
        <div class="rounded-card border border-border bg-surface p-5 shadow-card transition-all flex flex-col justify-between">
          <div class="flex items-center justify-between gap-2">
            <div class="flex items-center gap-2 min-w-0">
              <span class="text-xs font-semibold uppercase tracking-wider text-text-muted truncate">{{ t('dashboard.netWorth') }}</span>
              <span class="shrink-0 rounded-full bg-surface-2 px-2 py-0.5 text-[10px] font-semibold text-text-muted">
                {{ t('dashboard.acrossAccounts', { count: accounts.length }) }}
              </span>
            </div>
            <div class="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-accent/15 text-accent">
              <Wallet :size="15" />
            </div>
          </div>
          <div class="mt-3">
            <div class="tnum text-2xl font-bold tracking-tight text-text sm:text-3xl whitespace-nowrap overflow-hidden text-ellipsis">
              <Money :value="netWorth?.total ?? 0" :currency="currency" />
            </div>
          </div>
          <div class="mt-3 flex items-center justify-between border-t border-border pt-2.5 text-xs text-text-muted">
            <span>{{ t('dashboard.thisMonth') }}</span>
            <span class="font-semibold text-positive whitespace-nowrap" v-if="(cashflow?.net ?? 0) >= 0">
              +<Money :value="cashflow?.net ?? 0" :currency="currency" />
            </span>
            <span class="font-semibold text-negative whitespace-nowrap" v-else>
              <Money :value="cashflow?.net ?? 0" :currency="currency" />
            </span>
          </div>
        </div>

        <!-- Monthly Inflow (Income) Card -->
        <div class="rounded-card border border-border bg-surface p-5 shadow-card transition-all flex flex-col justify-between">
          <div class="flex items-center justify-between gap-2">
            <div class="flex items-center gap-2 min-w-0">
              <span class="text-xs font-semibold uppercase tracking-wider text-text-muted truncate">{{ t('dashboard.monthIncome') }}</span>
              <span class="shrink-0 rounded-full bg-positive/15 px-2 py-0.5 text-[10px] font-bold text-positive">
                {{ currentMonthLabel }}
              </span>
            </div>
            <div class="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-positive/15 text-positive">
              <ArrowDownLeft :size="15" />
            </div>
          </div>
          <div class="mt-3">
            <div class="tnum text-2xl font-bold tracking-tight text-positive sm:text-3xl whitespace-nowrap overflow-hidden text-ellipsis">
              +<Money :value="cashflow?.income ?? 0" :currency="currency" />
            </div>
          </div>
          <div class="mt-3 flex items-center justify-between border-t border-border pt-2.5 text-xs text-text-muted">
            <span>{{ t('dashboard.savingsRate') }}</span>
            <span class="font-semibold text-text tnum whitespace-nowrap">{{ health?.savingsRate ?? 0 }}%</span>
          </div>
        </div>

        <!-- Monthly Outflow (Expense) Card -->
        <div class="rounded-card border border-border bg-surface p-5 shadow-card transition-all flex flex-col justify-between">
          <div class="flex items-center justify-between gap-2">
            <div class="flex items-center gap-2 min-w-0">
              <span class="text-xs font-semibold uppercase tracking-wider text-text-muted truncate">{{ t('dashboard.monthExpense') }}</span>
              <span class="shrink-0 rounded-full bg-negative/15 px-2 py-0.5 text-[10px] font-bold text-negative whitespace-nowrap">
                {{ t('dashboard.dailyBurn') }}: <Money :value="health?.dailyBurnRate ?? 0" :currency="currency" />
              </span>
            </div>
            <div class="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-negative/15 text-negative">
              <ArrowUpRight :size="15" />
            </div>
          </div>
          <div class="mt-3">
            <div class="tnum text-2xl font-bold tracking-tight text-negative sm:text-3xl whitespace-nowrap overflow-hidden text-ellipsis flex items-baseline">
              <span>-</span><Money :value="cashflow?.expense ?? 0" :currency="currency" />
            </div>
          </div>
          <div class="mt-3 flex items-center justify-between border-t border-border pt-2.5 text-xs text-text-muted">
            <span>{{ t('dashboard.projectedMonthEnd') }}</span>
            <span class="font-semibold text-text tnum whitespace-nowrap">
              <Money :value="health?.projectedMonthEndExpense ?? 0" :currency="currency" />
            </span>
          </div>
        </div>
      </div>

      <!-- 1b. Proactive Insights Panel -->
      <InsightsPanel />

      <!-- 2. Second Row: Active Account & Balance Card + Statistics Cashflow Chart -->
      <div class="grid grid-cols-1 gap-6 lg:grid-cols-12">
        <!-- Reference Active Account Card (5 Cols) -->
        <div class="rounded-card border border-border bg-surface p-6 shadow-card lg:col-span-5 flex flex-col justify-between space-y-4">
          <div>
            <div class="flex items-center justify-between">
              <h3 class="text-sm font-bold flex items-center gap-2">
                <Building2 :size="16" class="text-accent" />
                <span>{{ t('dashboard.accounts') }}</span>
              </h3>
              <RouterLink
                :to="{ name: 'accounts' }"
                class="text-xs font-semibold text-accent hover:underline flex items-center gap-1"
              >
                {{ t('dashboard.viewAll') }}
                <ArrowRight :size="12" />
              </RouterLink>
            </div>

            <!-- Account Switcher Tabs -->
            <div v-if="accounts.length" class="mt-3 flex flex-wrap gap-1 rounded-full border border-border bg-surface-2 p-1 text-xs">
              <button
                v-for="acc in accounts.slice(0, 4)"
                :key="acc.accountId"
                type="button"
                class="flex-1 rounded-full py-1 px-2.5 font-medium transition-all text-center truncate"
                :class="
                  (activeAccount?.accountId === acc.accountId)
                    ? 'bg-surface text-text shadow-sm font-bold'
                    : 'text-text-muted hover:text-text'
                "
                @click="selectedAccountId = acc.accountId"
              >
                {{ acc.name }}
              </button>
            </div>

            <!-- Modern Cobalt Account Balance Card -->
            <div class="mt-4 rounded-2xl bg-accent p-5 text-accent-contrast shadow-card relative overflow-hidden">
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-2">
                  <Building2 :size="18" />
                  <span class="text-xs font-semibold uppercase tracking-wider opacity-90">
                    {{ activeAccount?.name ?? 'Main Account' }}
                  </span>
                </div>
                <CreditCard :size="20" class="opacity-80" />
              </div>

              <div class="mt-6">
                <div class="text-xs opacity-80 font-medium">Available Balance</div>
                <div class="text-2xl font-bold tnum tracking-tight mt-0.5">
                  <Money :value="activeAccount?.balance ?? 0" :currency="activeAccount?.currencyCode ?? currency" />
                </div>
              </div>

              <div class="mt-5 flex items-center justify-between text-xs opacity-80 font-mono">
                <div>**** {{ activeAccount?.accountId?.slice(-4) ?? '2026' }}</div>
                <div class="text-[11px] font-sans font-medium uppercase">
                  {{ t(`enums.accountType.${activeAccount?.type ?? 'Bank'}`) }}
                </div>
              </div>
            </div>
          </div>

          <!-- Quick Action Buttons Row -->
          <div class="grid grid-cols-3 gap-2.5 pt-1">
            <button
              type="button"
              class="flex flex-col items-center justify-center rounded-control border border-border bg-surface-2 py-2 text-[11px] font-semibold text-text transition-colors hover:bg-surface-2/70 active:scale-95"
              @click="onSend"
            >
              <ArrowUpRight :size="15" class="mb-1 text-negative" />
              <span>Send</span>
            </button>
            <button
              type="button"
              class="flex flex-col items-center justify-center rounded-control border border-border bg-surface-2 py-2 text-[11px] font-semibold text-text transition-colors hover:bg-surface-2/70 active:scale-95"
              @click="onReceive"
            >
              <ArrowDownLeft :size="15" class="mb-1 text-positive" />
              <span>Receive</span>
            </button>
            <button
              type="button"
              class="flex flex-col items-center justify-center rounded-control border border-border bg-surface-2 py-2 text-[11px] font-semibold text-text transition-colors hover:bg-surface-2/70 active:scale-95"
              @click="onAddRecord"
            >
              <Plus :size="15" class="mb-1 text-accent" />
              <span>Record</span>
            </button>
          </div>
        </div>

        <!-- Statistics & Cashflow Chart Card (7 Cols) -->
        <div class="rounded-card border border-border bg-surface p-6 shadow-card lg:col-span-7 flex flex-col justify-between space-y-4">
          <div>
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-border pb-3">
              <div>
                <h3 class="text-sm font-bold flex items-center gap-2">
                  <BarChart3 :size="16" class="text-accent" />
                  <span>{{ t('dashboard.cashflow') }}</span>
                </h3>
                <p class="text-xs text-text-muted">{{ t('dashboard.monthIncome') }} vs {{ t('dashboard.monthExpense') }} ({{ cashflow?.year }})</p>
              </div>

              <!-- View Switcher -->
              <div class="inline-flex rounded-full border border-border bg-surface-2 p-0.5 text-xs">
                <button
                  type="button"
                  class="rounded-full px-2.5 py-0.5 font-medium transition-all"
                  :class="cashflowMode === 'cashflow' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                  @click="cashflowMode = 'cashflow'"
                >
                  {{ t('dashboard.viewCashflow') }}
                </button>
                <button
                  type="button"
                  class="rounded-full px-2.5 py-0.5 font-medium transition-all"
                  :class="cashflowMode === 'savings' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                  @click="cashflowMode = 'savings'"
                >
                  {{ t('dashboard.viewSavingsRate') }}
                </button>
              </div>
            </div>

            <div class="pt-2">
              <CashflowChart :months="cashflow?.trend ?? []" :currency="currency" :mode="cashflowMode" />
            </div>
          </div>

          <div class="flex items-center justify-between border-t border-border pt-3 text-xs text-text-muted">
            <span class="flex items-center gap-2">
              <span class="h-2 w-2 rounded-full bg-positive"></span>
              <span>Inflow: <strong class="text-text font-mono"><Money :value="cashflow?.income ?? 0" :currency="currency" /></strong></span>
              <span class="text-border-strong">·</span>
              <span class="h-2 w-2 rounded-full bg-negative"></span>
              <span>Outflow: <strong class="text-text font-mono"><Money :value="cashflow?.expense ?? 0" :currency="currency" /></strong></span>
            </span>
            <span class="font-bold text-positive">Net: <Money :value="cashflow?.net ?? 0" :currency="currency" colored /></span>
          </div>
        </div>
      </div>

      <!-- 3. Third Row: 3 Financial Health Pillars -->
      <div class="grid gap-4 sm:grid-cols-3">
        <!-- Savings Rate Pillar -->
        <div class="rounded-card border border-border bg-surface p-5 shadow-card">
          <div class="flex items-center justify-between">
            <span class="text-xs font-semibold uppercase tracking-wider text-text-muted">{{ t('dashboard.savingsRate') }}</span>
            <span
              v-if="health"
              class="rounded-full px-2.5 py-0.5 text-[11px] font-bold"
              :class="{
                'bg-accent/15 text-accent': health.healthStatus === 'Excellent' || health.healthStatus === 'Healthy',
                'bg-amber-400/15 text-amber-500': health.healthStatus === 'Low',
                'bg-negative/15 text-negative': health.healthStatus === 'Deficit',
              }"
            >
              {{ t(`dashboard.status${health.healthStatus}`) }}
            </span>
          </div>
          <div class="mt-3 flex items-baseline gap-2">
            <span class="tnum text-3xl font-bold tracking-tight" :class="health && health.savingsRate >= 0 ? 'text-text' : 'text-negative'">
              {{ health?.savingsRate ?? 0 }}%
            </span>
            <span class="text-xs text-text-muted font-medium">{{ t('dashboard.savingsTarget') }}</span>
          </div>
          <p v-if="health?.previousSavingsRate !== null" class="mt-2 text-xs text-text-muted border-t border-border pt-2">
            <span>{{ t('reports.worstMonth') }}: <strong class="text-text tnum">{{ health?.previousSavingsRate }}%</strong></span>
          </p>
        </div>

        <!-- Financial Runway Pillar -->
        <div class="rounded-card border border-border bg-surface p-5 shadow-card">
          <div class="flex items-center justify-between">
            <span class="text-xs font-semibold uppercase tracking-wider text-text-muted">{{ t('dashboard.runway') }}</span>
            <div class="flex h-7 w-7 items-center justify-center rounded-full bg-accent/15 text-accent">
              <ShieldCheck :size="16" />
            </div>
          </div>
          <div class="mt-3 flex items-baseline gap-1.5">
            <span class="tnum text-3xl font-bold tracking-tight text-text">
              {{ health?.runwayMonths ?? 0 }}
            </span>
            <span class="text-xs font-medium text-text-muted">{{ t('dashboard.runwayMonths', { months: '' }).trim() }}</span>
          </div>
          <p class="mt-2 text-xs text-text-muted border-t border-border pt-2 truncate">
            {{ t('dashboard.avgBurn', { amount: new Intl.NumberFormat(locale, { notation: 'compact', maximumFractionDigits: 1 }).format(health?.trailing3MonthAvgExpense ?? 0) }) }}
          </p>
        </div>

        <!-- Burn Pace & Projected Month End -->
        <div class="rounded-card border border-border bg-surface p-5 shadow-card">
          <div class="flex items-center justify-between">
            <span class="text-xs font-semibold uppercase tracking-wider text-text-muted">{{ t('dashboard.dailyBurn') }}</span>
            <span class="tnum text-[11px] font-semibold text-text-muted">
              {{ t('dashboard.pacePassed', { day: health?.daysPassed ?? 1, total: health?.totalDaysInMonth ?? 30, percent: Math.round(((health?.daysPassed ?? 1) / (health?.totalDaysInMonth ?? 30)) * 100) }) }}
            </span>
          </div>
          <div class="mt-3 flex items-baseline gap-2">
            <span class="tnum text-3xl font-bold tracking-tight text-text">
              <Money :value="health?.dailyBurnRate ?? 0" :currency="currency" />
            </span>
            <span class="text-xs text-text-muted font-medium">/ {{ t('reports.daily').toLowerCase() }}</span>
          </div>
          <p class="mt-2 text-xs text-text-muted border-t border-border pt-2 truncate">
            {{ t('dashboard.projectedMonthEnd') }}: <strong class="text-text font-bold"><Money :value="health?.projectedMonthEndExpense ?? 0" :currency="currency" /></strong>
          </p>
        </div>
      </div>

      <!-- 4. Fourth Row: Spending Distribution Donut + Net Worth Asset Allocation -->
      <div class="grid grid-cols-1 gap-6 lg:grid-cols-12">
        <!-- Donut with distribution switch (7 Cols) -->
        <div class="rounded-card border border-border bg-surface p-6 shadow-card lg:col-span-7 flex flex-col justify-between space-y-4">
          <div>
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-border pb-3">
              <div>
                <h3 class="text-sm font-bold flex items-center gap-2">
                  <PieChart :size="16" class="text-accent" />
                  <span>{{ t('dashboard.distribution') }}</span>
                </h3>
                <p class="text-xs text-text-muted">Monthly expenditure allocation</p>
              </div>

              <!-- Switcher -->
              <div class="inline-flex rounded-full border border-border bg-surface-2 p-0.5 text-xs">
                <button
                  type="button"
                  class="rounded-full px-2.5 py-0.5 font-medium transition-all"
                  :class="donutMode === 'category' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                  @click="donutMode = 'category'"
                >
                  {{ t('dashboard.byCategory') }}
                </button>
                <button
                  type="button"
                  class="rounded-full px-2.5 py-0.5 font-medium transition-all"
                  :class="donutMode === 'envelope' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                  @click="donutMode = 'envelope'"
                >
                  {{ t('dashboard.byEnvelope') }}
                </button>
                <button
                  type="button"
                  class="rounded-full px-2.5 py-0.5 font-medium transition-all"
                  :class="donutMode === 'income' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                  @click="donutMode = 'income'"
                >
                  {{ t('dashboard.byIncome') }}
                </button>
              </div>
            </div>

            <template v-if="donutData.length">
              <div class="pt-2">
                <DonutChart :data="donutData" :currency="currency" />
              </div>
              <ul class="mt-4 grid grid-cols-1 gap-2 sm:grid-cols-2">
                <li v-for="(d, i) in donutData" :key="i" class="flex items-center gap-2 rounded-control border border-border bg-surface-2 p-2.5 text-xs">
                  <span class="h-2.5 w-2.5 shrink-0 rounded-full" :style="{ backgroundColor: donutColor(i) }" />
                  <span class="truncate text-text font-medium flex-1">{{ d.name }}</span>
                  <Money :value="d.value" :currency="currency" class="shrink-0 font-bold tnum text-text" />
                </li>
              </ul>
            </template>
            <p v-else class="py-12 text-center text-xs text-text-muted">{{ t('dashboard.noExpenses') }}</p>
          </div>
        </div>

        <!-- Net Worth Asset Breakdown & Insights (5 Cols) -->
        <div class="rounded-card border border-border bg-surface p-6 shadow-card lg:col-span-5 flex flex-col justify-between space-y-4">
          <div>
            <div class="border-b border-border pb-3">
              <h3 class="text-sm font-bold flex items-center gap-2">
                <Sparkles :size="16" class="text-accent" />
                <span>{{ t('dashboard.insightsTitle') }}</span>
              </h3>
              <p class="text-xs text-text-muted">Automated financial health audit</p>
            </div>

            <div class="mt-4 space-y-3">
              <!-- Asset Spectrum Bar -->
              <div v-if="nwSegments.length" class="space-y-2">
                <div class="flex items-center justify-between text-xs">
                  <span class="font-medium text-text-muted">Asset Allocation</span>
                  <span class="font-mono text-xs font-bold tnum text-text"><Money :value="netWorth?.total ?? 0" :currency="currency" /></span>
                </div>
                <SpendBar :segments="nwSegments" :label="t('dashboard.netWorth')" />
              </div>

              <!-- Insights List -->
              <div v-if="insights.length" class="space-y-2.5 pt-2">
                <div
                  v-for="(insight, i) in insights"
                  :key="i"
                  class="flex items-start gap-2.5 rounded-control border border-border bg-surface-2 p-3 text-xs"
                >
                  <component
                    :is="insight.icon === 'trend' ? TrendingUp : (insight.icon === 'shield' ? ShieldCheck : Sparkles)"
                    :size="15"
                    class="mt-0.5 shrink-0"
                    :class="{
                      'text-positive': insight.type === 'positive',
                      'text-accent': insight.type === 'neutral',
                      'text-negative': insight.type === 'warning',
                    }"
                  />
                  <span class="text-text font-medium leading-relaxed">{{ insight.text }}</span>
                </div>
              </div>
            </div>
          </div>

          <div class="border-t border-border pt-3 text-xs text-text-muted flex justify-between">
            <span>Health Status: <strong class="text-text">{{ health?.healthStatus ?? 'Stable' }}</strong></span>
            <span class="text-positive font-bold">100% Calculated</span>
          </div>
        </div>
      </div>

      <!-- 5. Fifth Row: Recent Transactions + Accounts Table -->
      <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <!-- Recent Transactions -->
        <div class="rounded-card border border-border bg-surface shadow-card overflow-hidden">
          <div class="flex items-center justify-between px-6 py-4 border-b border-border">
            <h3 class="text-sm font-bold">{{ t('dashboard.recent') }}</h3>
            <RouterLink
              :to="{ name: 'transactions' }"
              class="inline-flex items-center gap-1 text-xs font-semibold text-accent hover:underline"
            >
              {{ t('dashboard.viewAll') }}
              <ArrowRight :size="13" />
            </RouterLink>
          </div>
          <ul v-if="recent.length" class="divide-y divide-border">
            <li
              v-for="tx in recent"
              :key="tx.id"
              class="group flex cursor-pointer items-center gap-3 px-6 py-3 transition-colors hover:bg-surface-2/60"
              @click="transactionModal.openEdit(tx)"
            >
              <div
                class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl"
                :class="{
                  'bg-positive-soft text-positive': tx.type === 'Income',
                  'bg-negative-soft text-negative': tx.type === 'Expense',
                  'bg-accent-soft text-accent': tx.type === 'Transfer',
                }"
              >
                <ArrowUpRight v-if="tx.type === 'Income'" :size="18" />
                <ArrowDownLeft v-else-if="tx.type === 'Expense'" :size="18" />
                <ArrowLeftRight v-else :size="18" />
              </div>
              <div class="min-w-0 flex-1">
                <p class="truncate text-sm font-semibold text-text group-hover:text-accent transition-colors">{{ tx.title }}</p>
                <p class="truncate text-xs text-text-muted">
                  {{ formatShortDate(tx.date, locale) }} · {{ catName(tx.categoryId) }}
                </p>
              </div>
              <Money
                :value="signedAmount(tx)"
                :currency="tx.currencyCode"
                :colored="tx.type === 'Income'"
                class="shrink-0 text-sm font-bold tnum"
              />
            </li>
          </ul>
          <div v-else class="px-6 py-12 text-center">
            <p class="text-xs text-text-muted">{{ t('dashboard.noTransactions') }}</p>
            <button
              type="button"
              class="mt-4 inline-flex items-center gap-1.5 rounded-control bg-accent px-3 py-1.5 text-xs font-semibold text-accent-contrast shadow-sm"
              @click="transactionModal.openCreate()"
            >
              <Plus :size="14" />
              {{ t('dashboard.addTransaction') }}
            </button>
          </div>
        </div>

        <!-- Accounts Overview -->
        <div class="rounded-card border border-border bg-surface shadow-card overflow-hidden">
          <div class="flex items-center justify-between px-6 py-4 border-b border-border">
            <h3 class="text-sm font-bold">{{ t('dashboard.accounts') }}</h3>
            <RouterLink
              v-if="accounts.length"
              :to="{ name: 'accounts' }"
              class="inline-flex items-center gap-1 text-xs font-semibold text-accent hover:underline"
            >
              {{ t('dashboard.viewAll') }}
              <ArrowRight :size="13" />
            </RouterLink>
          </div>
          <ul v-if="accounts.length" class="divide-y divide-border">
            <li
              v-for="account in accounts"
              :key="account.accountId"
              class="flex items-center justify-between px-6 py-3 transition-colors hover:bg-surface-2/60 cursor-pointer"
              @click="selectedAccountId = account.accountId"
            >
              <div class="min-w-0 flex items-center gap-3">
                <div class="flex h-8 w-8 items-center justify-center rounded-full bg-surface-2 text-text-muted">
                  <Building2 :size="15" />
                </div>
                <div>
                  <p class="truncate text-sm font-semibold text-text">{{ account.name }}</p>
                  <p class="text-xs text-text-muted">{{ t(`enums.accountType.${account.type}`) }}</p>
                </div>
              </div>
              <Money :value="account.balance" :currency="account.currencyCode" class="shrink-0 text-sm font-bold tnum text-text" />
            </li>
          </ul>
          <div v-else class="px-6 py-12 text-center">
            <p class="text-xs text-text-muted">{{ t('dashboard.noAccounts') }}</p>
            <RouterLink :to="{ name: 'accounts' }" class="mt-4 inline-block">
              <AppButton variant="secondary"><Plus :size="16" />{{ t('dashboard.addAccount') }}</AppButton>
            </RouterLink>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
