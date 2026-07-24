<script setup lang="ts">
import { onMounted, ref, computed } from 'vue';
import { RouterLink } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { Plus, ArrowRight } from 'lucide-vue-next';
import { getCashflow, getNetWorth } from '@/lib/reports';
import type { CashflowReport, NetWorthReport } from '@/types/api';
import BalanceCard from '@/components/ui/BalanceCard.vue';
import AppCard from '@/components/ui/AppCard.vue';
import SpendBar from '@/components/ui/SpendBar.vue';
import CashflowChart from '@/components/ui/CashflowChart.vue';
import Money from '@/components/ui/Money.vue';
import AppButton from '@/components/ui/AppButton.vue';

const { t } = useI18n();

const netWorth = ref<NetWorthReport | null>(null);
const cashflow = ref<CashflowReport | null>(null);
const loading = ref(true);
const failed = ref(false);

const now = new Date();

async function load(): Promise<void> {
  loading.value = true;
  failed.value = false;
  try {
    const [nw, cf] = await Promise.all([
      getNetWorth(),
      getCashflow(now.getFullYear(), now.getMonth() + 1),
    ]);
    netWorth.value = nw;
    cashflow.value = cf;
  } catch {
    failed.value = true;
  } finally {
    loading.value = false;
  }
}

const currency = computed(() => netWorth.value?.currencyCode ?? 'IDR');
const accounts = computed(() => netWorth.value?.accounts ?? []);
const segments = computed(() =>
  accounts.value.map((a) => ({ label: a.name, value: Math.max(0, a.balance) })),
);

onMounted(load);
</script>

<template>
  <div>
    <div v-if="loading" class="py-24 text-center text-sm text-text-muted">
      {{ t('common.loading') }}
    </div>

    <div v-else-if="failed" class="py-24 text-center">
      <p class="text-sm text-text-muted">{{ t('errors.network_error') }}</p>
      <AppButton class="mt-4" variant="secondary" @click="load">{{ t('common.retry') }}</AppButton>
    </div>

    <div v-else class="space-y-4">
      <!-- Net worth + this-month summary -->
      <div class="grid gap-4 lg:grid-cols-3">
        <BalanceCard
          class="lg:col-span-2"
          :label="t('dashboard.netWorth')"
          :value="netWorth?.total ?? 0"
          :currency="currency"
          :caption="t('dashboard.acrossAccounts', { count: accounts.length })"
        >
          <template #footer>
            <SpendBar v-if="segments.length" :segments="segments" />
          </template>
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

      <!-- Cashflow trend -->
      <AppCard>
        <div class="mb-2 flex items-baseline justify-between">
          <h2 class="text-sm font-semibold">{{ t('dashboard.cashflow') }}</h2>
          <span class="tnum text-[13px] text-text-muted">{{ cashflow?.year }}</span>
        </div>
        <CashflowChart :months="cashflow?.trend ?? []" :currency="currency" />
      </AppCard>

      <!-- Accounts + recent activity -->
      <div class="grid gap-4 lg:grid-cols-2">
        <AppCard>
          <div class="flex items-center justify-between">
            <h2 class="text-sm font-semibold">{{ t('dashboard.accounts') }}</h2>
            <RouterLink
              v-if="accounts.length"
              :to="{ name: 'accounts' }"
              class="inline-flex items-center gap-1 text-[13px] font-medium text-accent hover:underline"
            >
              {{ t('dashboard.viewAll') }}<ArrowRight :size="14" />
            </RouterLink>
          </div>

          <ul v-if="accounts.length" class="mt-2 divide-y divide-border">
            <li
              v-for="account in accounts"
              :key="account.accountId"
              class="flex items-center justify-between py-3"
            >
              <div class="min-w-0">
                <p class="truncate text-sm font-medium">{{ account.name }}</p>
                <p class="text-[13px] text-text-muted">
                  {{ t(`enums.accountType.${account.type}`) }}
                  <span v-if="account.groupName"> · {{ account.groupName }}</span>
                </p>
              </div>
              <Money :value="account.balance" :currency="account.currencyCode" class="text-sm font-medium" />
            </li>
          </ul>

          <div v-else class="py-8 text-center">
            <p class="text-[13px] text-text-muted">{{ t('dashboard.noAccounts') }}</p>
            <RouterLink :to="{ name: 'accounts' }" class="mt-4 inline-block">
              <AppButton variant="secondary"><Plus :size="16" />{{ t('dashboard.addAccount') }}</AppButton>
            </RouterLink>
          </div>
        </AppCard>

        <AppCard>
          <div class="flex items-center justify-between">
            <h2 class="text-sm font-semibold">{{ t('dashboard.recent') }}</h2>
            <RouterLink
              :to="{ name: 'transactions' }"
              class="inline-flex items-center gap-1 text-[13px] font-medium text-accent hover:underline"
            >
              {{ t('dashboard.viewAll') }}<ArrowRight :size="14" />
            </RouterLink>
          </div>

          <div class="py-8 text-center">
            <p class="text-[13px] text-text-muted">{{ t('dashboard.noTransactions') }}</p>
            <RouterLink :to="{ name: 'transactions' }" class="mt-4 inline-block">
              <AppButton variant="secondary"><Plus :size="16" />{{ t('dashboard.addTransaction') }}</AppButton>
            </RouterLink>
          </div>
        </AppCard>
      </div>

      <p class="px-1 text-[13px] text-text-muted">{{ t('dashboard.placeholderNote') }}</p>
    </div>
  </div>
</template>
