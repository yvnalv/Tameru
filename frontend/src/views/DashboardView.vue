<script setup lang="ts">
import { onMounted, ref, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { getCashflow, getNetWorth } from '@/lib/reports';
import type { CashflowReport, NetWorthReport } from '@/types/api';
import BalanceCard from '@/components/ui/BalanceCard.vue';
import StatTile from '@/components/ui/StatTile.vue';
import AppCard from '@/components/ui/AppCard.vue';
import SpendBar from '@/components/ui/SpendBar.vue';
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

const segments = computed(
  () => netWorth.value?.accounts.map((a) => ({ label: a.name, value: Math.max(0, a.balance) })) ?? [],
);

onMounted(load);
</script>

<template>
  <div class="space-y-5">
    <div v-if="loading" class="py-16 text-center text-sm text-text-muted">
      {{ t('common.loading') }}
    </div>

    <div v-else-if="failed" class="py-16 text-center">
      <p class="text-sm text-text-muted">{{ t('errors.network_error') }}</p>
      <AppButton class="mt-4" variant="secondary" @click="load">{{ t('common.retry') }}</AppButton>
    </div>

    <template v-else>
      <BalanceCard
        :label="t('dashboard.netWorth')"
        :value="netWorth?.total ?? 0"
        :currency="netWorth?.currencyCode ?? 'IDR'"
        :caption="t('dashboard.acrossAccounts', { count: netWorth?.accounts.length ?? 0 })"
      >
        <template #footer>
          <SpendBar v-if="segments.length" :segments="segments" />
        </template>
      </BalanceCard>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <StatTile :label="t('dashboard.monthIncome')" :value="cashflow?.income ?? 0" colored />
        <StatTile :label="t('dashboard.monthExpense')" :value="-(cashflow?.expense ?? 0)" colored />
        <StatTile :label="t('dashboard.monthNet')" :value="cashflow?.net ?? 0" colored />
      </div>

      <AppCard>
        <h2 class="text-sm font-semibold">{{ t('dashboard.accounts') }}</h2>
        <p v-if="!netWorth?.accounts.length" class="mt-3 text-[13px] text-text-muted">
          {{ t('dashboard.noAccounts') }}
        </p>
        <ul v-else class="mt-2 divide-y divide-border">
          <li
            v-for="account in netWorth?.accounts"
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
      </AppCard>

      <p class="px-1 text-[13px] text-text-muted">{{ t('dashboard.placeholderNote') }}</p>
    </template>
  </div>
</template>
