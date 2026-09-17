<script setup lang="ts">
import { onMounted, ref, reactive, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import {
  Plus, CircleCheck, RotateCcw, Ban, ArrowRight, Download, Upload,
  ArrowUpRight, ArrowDownLeft, ArrowLeftRight, Filter, X, ChevronLeft, ChevronRight,
} from 'lucide-vue-next';
import { displayName } from '@/lib/seededNames';
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
  type: '',
  accountId: '',
  budgetCategoryId: '',
  categoryId: '',
  status: '',
  q: '',
  from: '',
  to: '',
  page: 1,
  pageSize: 15,
});

const TX_TYPES = ['Income', 'Expense', 'Transfer'];

// --- lookups ----------------------------------------------------------------
const accountName = (id: string | null) =>
  id ? accounts.value.find((a) => a.id === id)?.name ?? '—' : '—';
const categoryName = (id: string | null) =>
  id ? displayName(categories.value.find((c) => c.id === id)?.name ?? null, locale.value) : null;

// --- filter option lists ----------------------------------------------------
const accountFilterOptions = computed(() => [
  { value: '', label: t('transactions.allAccounts') },
  ...accounts.value.map((a) => ({ value: a.id, label: a.name })),
]);

const budgets = computed(() => categories.value.filter((c) => c.level === 'Budget'));
const budgetFilterOptions = computed(() => [
  { value: '', label: t('transactions.allBudgets') },
  ...budgets.value.map((b) => ({ value: b.id, label: displayName(b.name, locale.value) })),
]);

const availableCategories = computed(() => {
  if (!filters.budgetCategoryId) {
    return categories.value.filter((c) => c.level === 'Category' && c.isActive);
  }
  return categories.value.filter(
    (c) => c.level === 'Category' && c.isActive && c.parentId === filters.budgetCategoryId,
  );
});

const categoryFilterOptions = computed(() => [
  { value: '', label: t('transactions.allCategories') },
  ...availableCategories.value.map((c) => ({ value: c.id, label: displayName(c.name, locale.value) })),
]);

const statusFilterOptions = computed(() => [
  { value: '', label: t('transactions.allStatuses') },
  { value: 'Cleared', label: t('enums.transactionStatus.Cleared') },
  { value: 'Uncleared', label: t('enums.transactionStatus.Uncleared') },
]);

function onBudgetChange(val: string): void {
  filters.budgetCategoryId = val;
  if (filters.categoryId && val) {
    const cat = categories.value.find((c) => c.id === filters.categoryId);
    if (cat?.parentId !== val) {
      filters.categoryId = '';
    }
  }
  applyFilters();
}

const hasActiveFilters = computed(() =>
  Boolean(
    filters.type ||
    filters.accountId ||
    filters.budgetCategoryId ||
    filters.categoryId ||
    filters.status ||
    filters.q ||
    filters.from ||
    filters.to,
  ),
);

function resetFilters(): void {
  filters.type = '';
  filters.accountId = '';
  filters.budgetCategoryId = '';
  filters.categoryId = '';
  filters.status = '';
  filters.q = '';
  filters.from = '';
  filters.to = '';
  filters.page = 1;
  loadPage();
}

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
    if (filters.budgetCategoryId) clean.budgetCategoryId = filters.budgetCategoryId;
    if (filters.categoryId) clean.categoryId = filters.categoryId;
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

// The search box applies on its own (debounced) and on Enter.
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

const totalPages = computed(() => page.value?.totalPages ?? 1);

const displayedPages = computed(() => {
  const total = totalPages.value;
  const current = filters.page ?? 1;
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }
  const pages: (number | string)[] = [1];
  if (current > 3) pages.push('...');
  const start = Math.max(2, current - 1);
  const end = Math.min(total - 1, current + 1);
  for (let i = start; i <= end; i++) pages.push(i);
  if (current < total - 2) pages.push('...');
  pages.push(total);
  return pages;
});

function setPage(p: number): void {
  if (p < 1 || p > totalPages.value || p === filters.page) return;
  filters.page = p;
  loadPage();
}

function setPageSize(size: number): void {
  filters.pageSize = size;
  filters.page = 1;
  loadPage();
}

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
    if (filters.budgetCategoryId) clean.budgetCategoryId = filters.budgetCategoryId;
    if (filters.categoryId) clean.categoryId = filters.categoryId;
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
    <!-- Header Action Bar & Quick Type Filters -->
    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <!-- Quick Type Segmented Tabs with Semantic Financial Colors -->
      <div class="inline-flex rounded-xl bg-surface border border-border p-1 shadow-sm">
        <button
          type="button"
          class="rounded-lg px-3 py-1.5 text-xs font-semibold transition-all"
          :class="!filters.type ? 'bg-surface-2 text-text font-bold shadow-xs' : 'text-text-muted hover:text-text'"
          @click="filters.type = ''; applyFilters()"
        >
          {{ t('transactions.allTypes') }}
        </button>
        <button
          v-for="txType in TX_TYPES"
          :key="txType"
          type="button"
          class="rounded-lg px-3 py-1.5 text-xs font-semibold transition-all"
          :class="[
            filters.type === txType
              ? txType === 'Expense'
                ? 'bg-negative text-negative-contrast font-bold shadow-sm'
                : txType === 'Income'
                  ? 'bg-positive text-positive-contrast font-bold shadow-sm'
                  : 'bg-accent text-accent-contrast font-bold shadow-sm'
              : txType === 'Expense'
                ? 'text-text-muted hover:text-negative'
                : txType === 'Income'
                  ? 'text-text-muted hover:text-positive'
                  : 'text-text-muted hover:text-accent',
          ]"
          @click="filters.type = txType; applyFilters()"
        >
          {{ t(`enums.transactionType.${txType}`) }}
        </button>
      </div>

      <!-- Action Buttons -->
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

    <!-- Filters Bar -->
    <AppCard>
      <form class="space-y-3" @submit.prevent="submitSearch">
        <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-7">
          <AppInput
            :model-value="filters.q ?? ''"
            :placeholder="t('common.search')"
            :aria-label="t('common.search')"
            @update:model-value="onSearchInput"
          />
          <AppSelect
            :model-value="filters.accountId ?? ''"
            :options="accountFilterOptions"
            :aria-label="t('transactions.filterAccount')"
            @update:model-value="filters.accountId = $event; applyFilters()"
          />
          <AppSelect
            :model-value="filters.budgetCategoryId ?? ''"
            :options="budgetFilterOptions"
            :aria-label="t('transactions.filterBudget')"
            @update:model-value="onBudgetChange"
          />
          <AppSelect
            :model-value="filters.categoryId ?? ''"
            :options="categoryFilterOptions"
            :aria-label="t('transactions.filterCategory')"
            @update:model-value="filters.categoryId = $event; applyFilters()"
          />
          <AppSelect
            :model-value="filters.status ?? ''"
            :options="statusFilterOptions"
            :aria-label="t('transactions.filterStatus')"
            @update:model-value="filters.status = $event; applyFilters()"
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
        </div>

        <div v-if="hasActiveFilters" class="flex items-center justify-between border-t border-border pt-2 text-xs">
          <span class="text-text-muted">
            {{ t('transactions.showing', { count: page?.items.length ?? 0, total: page?.total ?? 0 }) }}
          </span>
          <button
            type="button"
            class="inline-flex items-center gap-1.5 font-medium text-negative hover:underline"
            @click="resetFilters"
          >
            <X :size="13" />
            {{ t('transactions.clearFilters') }}
          </button>
        </div>

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
            class="group flex cursor-pointer items-center gap-3 px-4 py-3 transition-colors hover:bg-surface-2/60 sm:gap-4 sm:px-5"
            :class="rowPad"
            @click="openEdit(tx)"
          >
            <!-- Transaction Type Avatar Indicator -->
            <div
              class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl"
              :class="{
                'bg-positive-soft text-positive': tx.type === 'Income',
                'bg-negative-soft text-negative': tx.type === 'Expense',
                'bg-accent-soft text-accent': tx.type === 'Transfer'
              }"
            >
              <ArrowUpRight v-if="tx.type === 'Income'" :size="18" />
              <ArrowDownLeft v-else-if="tx.type === 'Expense'" :size="18" />
              <ArrowLeftRight v-else :size="18" />
            </div>

            <!-- Date -->
            <div class="shrink-0 whitespace-nowrap tnum text-xs font-medium text-text-muted sm:w-20">
              {{ formatRowDate(tx.date, locale) }}
            </div>

            <!-- Details -->
            <div class="min-w-0 flex-1">
              <p class="truncate text-sm font-semibold text-text group-hover:text-accent transition-colors">
                {{ tx.title }}
              </p>
              <p class="flex items-center gap-1.5 truncate text-xs text-text-muted mt-0.5">
                <span class="truncate font-medium">{{ accountName(tx.accountId) }}</span>
                <template v-if="tx.type === 'Transfer'">
                  <ArrowRight :size="12" class="shrink-0 text-text-muted" />
                  <span class="truncate font-medium">{{ accountName(tx.toAccountId) }}</span>
                </template>
                <template v-else-if="categoryName(tx.categoryId)">
                  <span>·</span>
                  <span class="truncate">{{ categoryName(tx.categoryId) }}</span>
                </template>
                <!-- Status shown inline on mobile -->
                <span class="shrink-0 sm:hidden">· {{ t(`enums.transactionStatus.${tx.status}`) }}</span>
              </p>
            </div>

            <!-- Status Chip -->
            <StatusChip
              v-if="tx.status !== 'Cleared'"
              :status="tx.status as 'Cleared' | 'Uncleared'"
              class="hidden sm:inline-flex shrink-0"
            />

            <!-- Amount -->
            <Money
              :value="signedAmount(tx)"
              :currency="tx.currencyCode"
              :colored="tx.type === 'Income'"
              class="shrink-0 whitespace-nowrap text-right text-sm font-semibold tnum"
            />

            <!-- Actions -->
            <div class="flex shrink-0 items-center justify-end gap-1" @click.stop>
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
        <div class="py-12 text-center">
          <div class="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-2xl bg-surface-2 text-text-muted">
            <Filter :size="24" />
          </div>
          <p class="text-sm font-medium text-text">{{ t('transactions.empty') }}</p>
        </div>
      </AppCard>

      <!-- Pagination -->
      <div v-if="page && page.total > 0" class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between text-xs text-text-muted">
        <div class="flex items-center gap-3">
          <span>{{ t('transactions.showing', { count: page.items.length, total: page.total }) }}</span>
          <div class="hidden h-3.5 w-px bg-border sm:block"></div>
          <!-- Page size selector -->
          <div class="hidden items-center gap-1.5 sm:flex">
            <span class="text-text-muted">{{ t('transactions.itemsPerPage') }}:</span>
            <div class="inline-flex rounded-lg border border-border bg-surface p-0.5">
              <button
                v-for="size in [15, 25, 50]"
                :key="size"
                type="button"
                class="rounded-md px-2 py-0.5 text-xs font-medium transition-colors"
                :class="filters.pageSize === size ? 'bg-primary text-white font-semibold shadow-xs' : 'text-text-muted hover:text-text'"
                @click="setPageSize(size)"
              >
                {{ size }}
              </button>
            </div>
          </div>
        </div>

        <!-- Numbered pagination -->
        <div class="flex items-center gap-1.5 self-center sm:self-auto">
          <AppButton
            variant="secondary"
            size="sm"
            :disabled="(filters.page ?? 1) <= 1"
            @click="setPage((filters.page ?? 1) - 1)"
          >
            <ChevronLeft :size="14" />
            <span class="hidden sm:inline">{{ t('transactions.prev') }}</span>
          </AppButton>

          <template v-for="(p, idx) in displayedPages" :key="idx">
            <span v-if="p === '...'" class="px-1.5 text-text-muted select-none">...</span>
            <button
              v-else
              type="button"
              class="h-8 min-w-[2rem] rounded-lg px-2 text-xs font-semibold transition-all"
              :class="filters.page === p
                ? 'bg-primary text-white shadow-xs'
                : 'bg-surface hover:bg-surface-2 text-text border border-border'"
              @click="setPage(Number(p))"
            >
              {{ p }}
            </button>
          </template>

          <AppButton
            variant="secondary"
            size="sm"
            :disabled="(filters.page ?? 1) >= totalPages"
            @click="setPage((filters.page ?? 1) + 1)"
          >
            <span class="hidden sm:inline">{{ t('transactions.next') }}</span>
            <ChevronRight :size="14" />
          </AppButton>
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
