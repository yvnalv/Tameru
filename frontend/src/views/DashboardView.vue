<script setup lang="ts">
import { onMounted, ref, computed } from 'vue';
import { RouterLink } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { Plus, ArrowRight } from 'lucide-vue-next';
import { getCashflow, getNetWorth, getCategoryTracker } from '@/lib/reports';
import { listTransactions } from '@/lib/transactions';
import { listCategories } from '@/lib/categories';
import type { CashflowReport, Category, NetWorthReport, Transaction } from '@/types/api';
import { displayName } from '@/lib/seededNames';
import { formatShortDate } from '@/lib/format';
import { chart } from '@/lib/chartTheme';
import BalanceCard from '@/components/ui/BalanceCard.vue';
import AppCard from '@/components/ui/AppCard.vue';
import SpendBar from '@/components/ui/SpendBar.vue';
import CashflowChart from '@/components/ui/CashflowChart.vue';
import DonutChart from '@/components/ui/DonutChart.vue';
import AvatarChip from '@/components/ui/AvatarChip.vue';
import Money from '@/components/ui/Money.vue';
import AppButton from '@/components/ui/AppButton.vue';
import LoadingBlock from '@/components/ui/LoadingBlock.vue';

const { t, locale } = useI18n();

const netWorth = ref<NetWorthReport | null>(null);
const cashflow = ref<CashflowReport | null>(null);
const categories = ref<Category[]>([]);
const monthSpend = ref<{ categoryId: string; total: number }[]>([]);
const recent = ref<Transaction[]>([]);
const loading = ref(true);
const failed = ref(false);

const now = new Date();
const pad = (n: number) => String(n).padStart(2, '0');
const catName = (id: string | null) =>
  id ? displayName(categories.value.find((c) => c.id === id)?.name ?? null, locale.value) || '—' : '—';

const currency = computed(() => netWorth.value?.currencyCode ?? 'IDR');
const accounts = computed(() => netWorth.value?.accounts ?? []);
const nwSegments = computed(() => accounts.value.map((a) => ({ label: a.name, value: Math.max(0, a.balance) })));

// Expenses-by-category donut: top 6 + "Others".
const donutData = computed(() => {
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

async function load(): Promise<void> {
  loading.value = true;
  failed.value = false;
  try {
    const y = now.getFullYear();
    const m = now.getMonth() + 1;
    const dim = new Date(y, m, 0).getDate();
    const [nw, cf, cats, spend, txns] = await Promise.all([
      getNetWorth(),
      getCashflow(y, m),
      listCategories({ includeInactive: true }),
      getCategoryTracker('monthly', `${y}-${pad(m)}-01`, `${y}-${pad(m)}-${pad(dim)}`),
      listTransactions({ page: 1, pageSize: 10 }),
    ]);
    netWorth.value = nw;
    cashflow.value = cf;
    categories.value = cats;
    monthSpend.value = spend.categories.map((c) => ({ categoryId: c.categoryId, total: c.total }));
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
      <!-- Net worth + this-month -->
      <div class="grid gap-4 lg:grid-cols-3">
        <BalanceCard
          class="lg:col-span-2"
          :label="t('dashboard.netWorth')"
          :value="netWorth?.total ?? 0"
          :currency="currency"
          :caption="t('dashboard.acrossAccounts', { count: accounts.length })"
        >
          <template #footer><SpendBar v-if="nwSegments.length" :segments="nwSegments" /></template>
        </BalanceCard>

        <AppCard>
          <h2 class="text-sm font-semibold">{{ t('dashboard.thisMonth') }}</h2>
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

      <!-- Cashflow trend + expenses donut -->
      <div class="grid gap-4 lg:grid-cols-3">
        <AppCard class="lg:col-span-2">
          <div class="mb-2 flex items-baseline justify-between">
            <h2 class="text-sm font-semibold">{{ t('dashboard.cashflow') }}</h2>
            <span class="tnum text-[13px] text-text-muted">{{ cashflow?.year }}</span>
          </div>
          <CashflowChart :months="cashflow?.trend ?? []" :currency="currency" />
        </AppCard>

        <AppCard>
          <h2 class="text-sm font-semibold">{{ t('dashboard.expenses') }}</h2>
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

      <!-- Recent transactions + accounts -->
      <div class="grid gap-4 lg:grid-cols-2">
        <AppCard :padded="false">
          <div class="flex items-center justify-between px-5 py-4">
            <h2 class="text-sm font-semibold">{{ t('dashboard.recent') }}</h2>
            <RouterLink :to="{ name: 'transactions' }" class="inline-flex items-center gap-1 text-[13px] font-medium text-accent hover:underline">
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
            <RouterLink v-if="accounts.length" :to="{ name: 'accounts' }" class="inline-flex items-center gap-1 text-[13px] font-medium text-accent hover:underline">
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
