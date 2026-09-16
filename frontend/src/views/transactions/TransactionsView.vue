<script setup lang="ts">
import { onMounted, ref, reactive, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { Plus, CircleCheck, RotateCcw, Ban, ArrowRight, Download, Upload } from 'lucide-vue-next';
import {
  listTransactions, clearTransaction, unclearTransaction, voidTransaction,
  type TransactionFilter,
} from '@/lib/transactions';
import { listAccounts } from '@/lib/accounts';
import { listCategories } from '@/lib/categories';
import type { Account, Category, Paged, Transaction } from '@/types/api';
import { errorMessage } from '@/lib/errorMessage';
import { formatRowDate } from '@/lib/format';
import { toCsv, downloadCsv } from '@/lib/csv';
import { transactionsImportConfig } from '@/lib/importConfigs';
import { useDensity } from '@/composables/useDensity';
import { useToastStore } from '@/stores/toast';
import { useConfirmStore } from '@/stores/confirm';
import { useTransactionModalStore } from '@/stores/transactionModal';
import ImportModal from '@/components/ui/ImportModal.vue';
import LoadingBlock from '@/components/ui/LoadingBlock.vue';
import AppCard from '@/components/ui/AppCard.vue';
import AppButton from '@/components/ui/AppButton.vue';
import AppInput from '@/components/ui/AppInput.vue';
import AppSelect from '@/components/ui/AppSelect.vue';
import StatusChip from '@/components/ui/StatusChip.vue';
import IconButton from '@/components/ui/IconButton.vue';
import Money from '@/components/ui/Money.vue';

const { t, te, locale } = useI18n();
const { rowPad } = useDensity();
const toast = useToastStore();
const confirm = useConfirmStore();
const exporting = ref(false);
const importOpen = ref(false);

const importConfig = computed(() => transactionsImportConfig(accounts.value, categories.value, locale.value));

async function onImported(): Promise<void> {
  await Promise.all([loadPage(), loadRefs()]);
}

const page = ref<Paged<Transaction> | null>(null);
const accounts = ref<Account[]>([]);
const categories = ref<Category[]>([]);
const loading = ref(true);
const failed = ref(false);

const filters = reactive<TransactionFilter>({
  type: '', accountId: '', status: '', q: '', from: '', to: '', page: 1, pageSize: 25,
});

const TX_TYPES = ['Income', 'Expense', 'Transfer'];

// --- lookups ----------------------------------------------------------------
const accountName = (id: string | null) =>
  id ? accounts.value.find((a) => a.id === id)?.name ?? '—' : '—';
const categoryName = (id: string | null) =>
  id ? categories.value.find((c) => c.id === id)?.name ?? null : null;

// --- filter option lists ----------------------------------------------------
const typeFilterOptions = computed(() => [
  { value: '', label: t('transactions.allTypes') },
  ...TX_TYPES.map((v) => ({ value: v, label: t(`enums.transactionType.${v}`) })),
]);
const accountFilterOptions = computed(() => [
  { value: '', label: t('transactions.allAccounts') },
  ...accounts.value.map((a) => ({ value: a.id, label: a.name })),
]);
const statusFilterOptions = computed(() => [
  { value: '', label: t('transactions.allStatuses') },
  { value: 'Cleared', label: t('enums.transactionStatus.Cleared') },
  { value: 'Uncleared', label: t('enums.transactionStatus.Uncleared') },
]);

// --- data load --------------------------------------------------------------
async function loadRefs(): Promise<void> {
  [accounts.value, categories.value] = await Promise.all([listAccounts(true), listCategories()]);
}

async function loadPage(): Promise<void> {
  loading.value = true;
  failed.value = false;
  try {
    const clean: TransactionFilter = { page: filters.page, pageSize: filters.pageSize };
    if (filters.type) clean.type = filters.type;
    if (filters.accountId) clean.accountId = filters.accountId;
    if (filters.status) clean.status = filters.status;
    if (filters.q) clean.q = filters.q;
    if (filters.from) clean.from = filters.from;
    if (filters.to) clean.to = filters.to;
    page.value = await listTransactions(clean);
  } catch {
    failed.value = true;
  } finally {
    loading.value = false;
  }
}

function applyFilters(): void {
  filters.page = 1;
  loadPage();
}

// The search box applies on its own (debounced) and on Enter. Without this the typed value only
// reached the API if the user happened to touch one of the other filters afterwards.
let searchTimer: ReturnType<typeof setTimeout> | undefined;
function onSearchInput(value: string): void {
  filters.q = value;
  clearTimeout(searchTimer);
  searchTimer = setTimeout(applyFilters, 300);
}
function submitSearch(): void {
  clearTimeout(searchTimer);
  applyFilters();
}

function goTo(delta: number): void {
  filters.page = Math.max(1, (filters.page ?? 1) + delta);
  loadPage();
}

const totalPages = computed(() => page.value?.totalPages ?? 1);

// --- row rendering ----------------------------------------------------------
function signedAmount(tx: Transaction): number {
  if (tx.type === 'Expense') return -tx.amount;
  return tx.amount;
}

async function exportCsv(): Promise<void> {
  exporting.value = true;
  try {
    const clean: TransactionFilter = { page: 1, pageSize: 100000 };
    if (filters.type) clean.type = filters.type;
    if (filters.accountId) clean.accountId = filters.accountId;
    if (filters.status) clean.status = filters.status;
    if (filters.q) clean.q = filters.q;
    if (filters.from) clean.from = filters.from;
    if (filters.to) clean.to = filters.to;
    const all = await listTransactions(clean);
    const csv = toCsv(all.items, [
      { header: t('transactions.date'), value: (r) => r.date },
      { header: t('transactions.titleField'), value: (r) => r.title },
      { header: t('transactions.type'), value: (r) => t(`enums.transactionType.${r.type}`) },
      { header: t('transactions.account'), value: (r) => accountName(r.accountId) },
      { header: t('transactions.toAccount'), value: (r) => (r.toAccountId ? accountName(r.toAccountId) : '') },
      { header: t('transactions.category'), value: (r) => categoryName(r.categoryId) ?? '' },
      { header: t('transactions.amount'), value: (r) => r.amount },
      { header: t('transactions.status'), value: (r) => t(`enums.transactionStatus.${r.status}`) },
      { header: t('transactions.description'), value: (r) => r.description ?? '' },
    ]);
    downloadCsv(`tameru-transactions-${new Date().toISOString().slice(0, 10)}.csv`, csv);
  } catch (error) {
    toast.error(errorMessage(t, te, error));
  } finally {
    exporting.value = false;
  }
}

// --- modal integration ------------------------------------------------------
const transactionModal = useTransactionModalStore();

function openCreate(): void {
  transactionModal.openCreate();
}

function openEdit(tx: Transaction): void {
  transactionModal.openEdit(tx);
}

// Refresh table whenever any transaction is created or updated
watch(
  () => transactionModal.lastEventTimestamp,
  () => {
    loadPage();
    loadRefs();
  },
);

// --- row actions ------------------------------------------------------------
async function toggleClear(tx: Transaction): Promise<void> {
  try {
    if (tx.status === 'Cleared') await unclearTransaction(tx.id);
    else await clearTransaction(tx.id);
    await loadPage();
  } catch (error) {
    toast.error(errorMessage(t, te, error));
  }
}

async function remove(tx: Transaction): Promise<void> {
  const ok = await confirm.ask({
    message: t('transactions.voidConfirm'),
    confirmLabel: t('transactions.void'),
    danger: true,
  });
  if (!ok) return;
  try {
    await voidTransaction(tx.id);
    toast.success(t('common.done'));
    await Promise.all([loadPage(), loadRefs()]);
  } catch (error) {
    toast.error(errorMessage(t, te, error));
  }
}

onMounted(async () => {
  await loadRefs();
  await loadPage();
});
</script>

<template>
  <div class="space-y-4">
    <!-- The page title lives in the top bar (one <h1> per page); this row holds its actions. -->
    <div class="flex flex-wrap items-center justify-end gap-3">
      <div class="flex items-center gap-2">
        <AppButton variant="secondary" @click="importOpen = true">
          <Upload :size="16" /><span class="hidden sm:inline">{{ t('import.transactions') }}</span>
        </AppButton>
        <AppButton variant="secondary" :loading="exporting" @click="exportCsv">
          <Download :size="16" /><span class="hidden sm:inline">{{ t('common.export') }}</span>
        </AppButton>
        <AppButton @click="openCreate"><Plus :size="16" />{{ t('transactions.add') }}</AppButton>
      </div>
    </div>

    <!-- Filters -->
    <AppCard>
      <form class="grid grid-cols-2 gap-3 md:grid-cols-4 lg:grid-cols-6" @submit.prevent="submitSearch">
        <AppSelect
          :model-value="filters.type ?? ''"
          :options="typeFilterOptions"
          :aria-label="t('transactions.filterType')"
          @update:model-value="filters.type = $event; applyFilters()"
        />
        <AppSelect
          :model-value="filters.accountId ?? ''"
          :options="accountFilterOptions"
          :aria-label="t('transactions.filterAccount')"
          @update:model-value="filters.accountId = $event; applyFilters()"
        />
        <AppSelect
          :model-value="filters.status ?? ''"
          :options="statusFilterOptions"
          :aria-label="t('transactions.filterStatus')"
          @update:model-value="filters.status = $event; applyFilters()"
        />
        <AppInput
          :model-value="filters.q ?? ''"
          :placeholder="t('common.search')"
          :aria-label="t('common.search')"
          @update:model-value="onSearchInput"
        />
        <AppInput
          :model-value="filters.from ?? ''"
          type="date"
          :aria-label="t('transactions.filterFrom')"
          :title="t('transactions.filterFrom')"
          @update:model-value="filters.from = $event; applyFilters()"
        />
        <AppInput
          :model-value="filters.to ?? ''"
          type="date"
          :aria-label="t('transactions.filterTo')"
          :title="t('transactions.filterTo')"
          @update:model-value="filters.to = $event; applyFilters()"
        />
        <!-- Submitting is what Enter does; the button keeps that reachable without a mouse. -->
        <button type="submit" class="sr-only">{{ t('common.search') }}</button>
      </form>
    </AppCard>

    <LoadingBlock v-if="loading" />
    <div v-else-if="failed" class="py-16 text-center">
      <p class="text-sm text-text-muted">{{ t('errors.network_error') }}</p>
      <AppButton class="mt-4" variant="secondary" @click="loadPage">{{ t('common.retry') }}</AppButton>
    </div>

    <template v-else>
      <AppCard v-if="page && page.items.length" :padded="false">
        <ul class="divide-y divide-border">
          <li
            v-for="tx in page.items"
            :key="tx.id"
            class="flex cursor-pointer items-center gap-2 px-4 hover:bg-surface-2 sm:gap-3 sm:px-5"
            :class="rowPad"
            @click="openEdit(tx)"
          >
            <div class="shrink-0 whitespace-nowrap tnum text-[13px] text-text-muted">{{ formatRowDate(tx.date, locale) }}</div>
            <div class="min-w-0 flex-1">
              <p class="truncate text-sm font-medium">{{ tx.title }}</p>
              <p class="flex items-center gap-1 truncate text-[13px] text-text-muted">
                <span class="truncate">{{ accountName(tx.accountId) }}</span>
                <template v-if="tx.type === 'Transfer'">
                  <ArrowRight :size="12" class="shrink-0" /><span class="truncate">{{ accountName(tx.toAccountId) }}</span>
                </template>
                <template v-else-if="categoryName(tx.categoryId)"> · {{ categoryName(tx.categoryId) }}</template>
                <!-- Status shown inline on mobile (the chip is hidden there to save width) -->
                <span class="shrink-0 sm:hidden">· {{ t(`enums.transactionStatus.${tx.status}`) }}</span>
              </p>
            </div>
            <StatusChip
              v-if="tx.status !== 'Cleared'"
              :status="tx.status as 'Cleared' | 'Uncleared'"
              class="hidden sm:inline-flex"
            />
            <Money
              :value="signedAmount(tx)"
              :currency="tx.currencyCode"
              :colored="tx.type === 'Income'"
              class="shrink-0 whitespace-nowrap text-right text-sm font-medium"
            />
            <div class="flex shrink-0 items-center justify-end gap-0.5" @click.stop>
              <IconButton
                :icon="tx.status === 'Cleared' ? RotateCcw : CircleCheck"
                :label="tx.status === 'Cleared' ? t('transactions.unclear') : t('transactions.clear')"
                @click="toggleClear(tx)"
              />
              <IconButton :icon="Ban" :label="t('transactions.void')" danger @click="remove(tx)" />
            </div>
          </li>
        </ul>
      </AppCard>

      <AppCard v-else>
        <p class="py-8 text-center text-[13px] text-text-muted">{{ t('transactions.empty') }}</p>
      </AppCard>

      <div v-if="page && page.total > 0" class="flex items-center justify-between text-[13px] text-text-muted">
        <span>{{ t('transactions.showing', { count: page.items.length, total: page.total }) }}</span>
        <div class="flex items-center gap-2">
          <AppButton variant="secondary" :disabled="(filters.page ?? 1) <= 1" @click="goTo(-1)">{{ t('transactions.prev') }}</AppButton>
          <span class="tnum">{{ filters.page }} / {{ totalPages }}</span>
          <AppButton variant="secondary" :disabled="(filters.page ?? 1) >= totalPages" @click="goTo(1)">{{ t('transactions.next') }}</AppButton>
        </div>
      </div>
    </template>

    <ImportModal
      v-if="importOpen"
      :title="t('import.transactions')"
      :config="importConfig"
      @close="importOpen = false"
      @done="onImported"
    />
  </div>
</template>
