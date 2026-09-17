<script setup lang="ts">
import { onMounted, ref, computed, reactive, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import {
  Plus, Pencil, Ban, Upload, ArrowLeft, ArrowUpRight, ArrowDownLeft, ArrowLeftRight,
  Landmark, Smartphone, Wallet, TrendingUp, ShieldAlert, CheckCircle2, CircleCheck,
  RotateCcw, ChevronLeft, ChevronRight, Filter,
} from 'lucide-vue-next';
import {
  listAccounts, listAccountGroups, createAccount, updateAccount, deactivateAccount,
  type AccountInput,
} from '@/lib/accounts';
import {
  listTransactions, clearTransaction, unclearTransaction, voidTransaction,
} from '@/lib/transactions';
import { listCategories } from '@/lib/categories';
import type { Account, AccountGroup, Category, Paged, Transaction } from '@/types/api';
import { errorMessage } from '@/lib/errorMessage';
import { formatRowDate } from '@/lib/format';
import { displayName } from '@/lib/seededNames';
import { accountsImportConfig } from '@/lib/importConfigs';
import { useDensity } from '@/composables/useDensity';
import { useToastStore } from '@/stores/toast';
import { useConfirmStore } from '@/stores/confirm';
import { useTransactionModalStore } from '@/stores/transactionModal';
import ImportModal from '@/components/ui/ImportModal.vue';
import IconButton from '@/components/ui/IconButton.vue';
import LoadingBlock from '@/components/ui/LoadingBlock.vue';
import AppCard from '@/components/ui/AppCard.vue';
import AppButton from '@/components/ui/AppButton.vue';
import AppModal from '@/components/ui/AppModal.vue';
import AppInput from '@/components/ui/AppInput.vue';
import AppSelect from '@/components/ui/AppSelect.vue';
import FormField from '@/components/ui/FormField.vue';
import StatusChip from '@/components/ui/StatusChip.vue';
import Money from '@/components/ui/Money.vue';

const { t, te, locale } = useI18n();
const { rowPad } = useDensity();
const route = useRoute();
const router = useRouter();
const toast = useToastStore();
const confirm = useConfirmStore();
const transactionModalStore = useTransactionModalStore();

const importOpen = ref(false);
const importConfig = computed(() => accountsImportConfig(groups.value));

async function onImported(): Promise<void> {
  await load();
  if (selectedAccountId.value) {
    await loadAccountData();
  }
}

const accounts = ref<Account[]>([]);
const groups = ref<AccountGroup[]>([]);
const categories = ref<Category[]>([]);
const loading = ref(true);
const failed = ref(false);

const modalOpen = ref(false);
const editingId = ref<string | null>(null);
const saving = ref(false);
const formError = ref('');

// --- Drill-down / Selection State --------------------------------------------
const selectedAccountId = ref<string | null>((route.query.accountId as string) || null);

const selectedAccount = computed(() =>
  selectedAccountId.value ? accounts.value.find((a) => a.id === selectedAccountId.value) ?? null : null,
);

function selectAccount(id: string | null): void {
  selectedAccountId.value = id;
  const query = { ...route.query };
  if (id) {
    query.accountId = id;
  } else {
    delete query.accountId;
  }
  router.replace({ query });
  if (id) {
    accountTxFilters.page = 1;
    loadAccountData();
  }
}

watch(
  () => route.query.accountId,
  (newId) => {
    if (newId && typeof newId === 'string' && newId !== selectedAccountId.value) {
      selectedAccountId.value = newId;
      accountTxFilters.page = 1;
      loadAccountData();
    } else if (!newId && selectedAccountId.value) {
      selectedAccountId.value = null;
    }
  },
);

// --- Account Detail Insights & Ledger State ---------------------------------
const accountTxLoading = ref(false);
const accountTxPage = ref<Paged<Transaction> | null>(null);
const accountTxFilters = reactive({
  page: 1,
  pageSize: 15,
  q: '',
  status: '',
  type: '',
});

const accountInsights = reactive({
  inflow30d: 0,
  outflow30d: 0,
  net30d: 0,
  clearedCount: 0,
  totalRecent: 0,
  clearedRatio: 100,
});

const statusFilterOptions = computed(() => [
  { value: '', label: t('transactions.allStatuses') },
  { value: 'Cleared', label: t('enums.transactionStatus.Cleared') },
  { value: 'Uncleared', label: t('enums.transactionStatus.Uncleared') },
]);

const typeFilterOptions = computed(() => [
  { value: '', label: t('transactions.allTypes') },
  { value: 'Expense', label: t('enums.transactionType.Expense') },
  { value: 'Income', label: t('enums.transactionType.Income') },
  { value: 'Transfer', label: t('enums.transactionType.Transfer') },
]);

const categoryName = (id: string | null) =>
  id ? displayName(categories.value.find((c) => c.id === id)?.name ?? null, locale.value) : null;

async function loadAccountData(): Promise<void> {
  if (!selectedAccountId.value) return;
  accountTxLoading.value = true;
  try {
    const accId = selectedAccountId.value;
    const thirtyDaysAgo = new Date(Date.now() - 30 * 86400000).toISOString().slice(0, 10);

    const [txPage, recentData] = await Promise.all([
      listTransactions({
        accountId: accId,
        page: accountTxFilters.page,
        pageSize: accountTxFilters.pageSize,
        q: accountTxFilters.q || undefined,
        status: accountTxFilters.status || undefined,
        type: accountTxFilters.type || undefined,
      }),
      listTransactions({
        accountId: accId,
        page: 1,
        pageSize: 1000,
        from: thirtyDaysAgo,
      }),
    ]);

    accountTxPage.value = txPage;

    let inflow = 0;
    let outflow = 0;
    let cleared = 0;
    for (const tx of recentData.items) {
      if (tx.status === 'Cleared') cleared++;
      if (tx.type === 'Income' || (tx.type === 'Transfer' && tx.toAccountId === accId)) {
        inflow += tx.amount;
      } else if (tx.type === 'Expense' || (tx.type === 'Transfer' && tx.accountId === accId)) {
        outflow += tx.amount;
      }
    }
    accountInsights.inflow30d = inflow;
    accountInsights.outflow30d = outflow;
    accountInsights.net30d = inflow - outflow;
    accountInsights.clearedCount = cleared;
    accountInsights.totalRecent = recentData.items.length;
    accountInsights.clearedRatio = recentData.items.length > 0
      ? Math.round((cleared / recentData.items.length) * 100)
      : 100;
  } catch (err) {
    toast.error(errorMessage(t, te, err));
  } finally {
    accountTxLoading.value = false;
  }
}

let accountSearchTimer: ReturnType<typeof setTimeout> | undefined;
function onAccountSearchInput(value: string): void {
  accountTxFilters.q = value;
  clearTimeout(accountSearchTimer);
  accountSearchTimer = setTimeout(() => {
    accountTxFilters.page = 1;
    loadAccountData();
  }, 300);
}

function applyAccountFilters(): void {
  accountTxFilters.page = 1;
  loadAccountData();
}

const accountTotalPages = computed(() => accountTxPage.value?.totalPages ?? 1);

const accountDisplayedPages = computed(() => {
  const total = accountTotalPages.value;
  const current = accountTxFilters.page;
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
  const pages: (number | string)[] = [1];
  if (current > 3) pages.push('...');
  const start = Math.max(2, current - 1);
  const end = Math.min(total - 1, current + 1);
  for (let i = start; i <= end; i++) pages.push(i);
  if (current < total - 2) pages.push('...');
  pages.push(total);
  return pages;
});

function setAccountPage(p: number): void {
  if (p < 1 || p > accountTotalPages.value || p === accountTxFilters.page) return;
  accountTxFilters.page = p;
  loadAccountData();
}

function setAccountPageSize(size: number): void {
  accountTxFilters.pageSize = size;
  accountTxFilters.page = 1;
  loadAccountData();
}

function openCreateTxForAccount(): void {
  transactionModalStore.openCreate({ accountId: selectedAccountId.value ?? undefined });
}

function openEditTx(tx: Transaction): void {
  transactionModalStore.openEdit(tx);
}

async function toggleAccountTxClear(tx: Transaction): Promise<void> {
  try {
    const updated = tx.status === 'Cleared'
      ? await unclearTransaction(tx.id)
      : await clearTransaction(tx.id);
    const i = accountTxPage.value?.items.findIndex((x) => x.id === tx.id) ?? -1;
    if (i >= 0 && accountTxPage.value) accountTxPage.value.items[i] = updated;
    toast.success(t('common.done'));
  } catch (error) {
    toast.error(errorMessage(t, te, error));
  }
}

async function removeAccountTx(tx: Transaction): Promise<void> {
  const ok = await confirm.ask({
    message: t('transactions.voidConfirm'),
    confirmLabel: t('transactions.void'),
    danger: true,
  });
  if (!ok) return;
  try {
    await voidTransaction(tx.id);
    toast.success(t('common.done'));
    await Promise.all([load(), loadAccountData()]);
  } catch (error) {
    toast.error(errorMessage(t, te, error));
  }
}

watch(
  () => transactionModalStore.lastEventTimestamp,
  async () => {
    await load();
    if (selectedAccountId.value) {
      await loadAccountData();
    }
  },
);

// --- Account Form & Types ---------------------------------------------------
const ACCOUNT_TYPES = ['Cash', 'Bank', 'EWallet', 'Investment', 'Blocked'];

const form = reactive<AccountInput>({
  name: '',
  type: 'Bank',
  openingBalance: 0,
  groupId: null,
  currencyCode: 'IDR',
  sortOrder: 0,
});

const typeOptions = computed(() =>
  ACCOUNT_TYPES.map((v) => ({ value: v, label: t(`enums.accountType.${v}`) })),
);
const groupOptions = computed(() => [
  { value: '', label: t('accounts.ungrouped') },
  ...groups.value.map((g) => ({ value: g.id, label: g.name })),
]);

const total = computed(() =>
  accounts.value.filter((a) => a.isActive).reduce((sum, a) => sum + a.balance, 0),
);
const activeCount = computed(() => accounts.value.filter((a) => a.isActive).length);
const inactiveCount = computed(() => accounts.value.filter((a) => !a.isActive).length);

function getAccountIcon(type: string) {
  switch (type) {
    case 'Bank': return Landmark;
    case 'EWallet': return Smartphone;
    case 'Cash': return Wallet;
    case 'Investment': return TrendingUp;
    default: return ShieldAlert;
  }
}

function getAccountColor(type: string): string {
  switch (type) {
    case 'Bank': return 'bg-accent-soft text-accent';
    case 'EWallet': return 'bg-info-soft text-info';
    case 'Cash': return 'bg-positive-soft text-positive';
    case 'Investment': return 'bg-cat-7/15 text-cat-7';
    default: return 'bg-negative-soft text-negative';
  }
}

async function load(): Promise<void> {
  loading.value = true;
  failed.value = false;
  try {
    [accounts.value, groups.value, categories.value] = await Promise.all([
      listAccounts(true),
      listAccountGroups(),
      listCategories(),
    ]);
    if (selectedAccountId.value) {
      await loadAccountData();
    }
  } catch {
    failed.value = true;
  } finally {
    loading.value = false;
  }
}

function openCreate(): void {
  editingId.value = null;
  Object.assign(form, { name: '', type: 'Bank', openingBalance: 0, groupId: null, currencyCode: 'IDR', sortOrder: accounts.value.length });
  formError.value = '';
  modalOpen.value = true;
}

function openEdit(a: Account): void {
  editingId.value = a.id;
  Object.assign(form, {
    name: a.name, type: a.type, openingBalance: a.openingBalance,
    groupId: a.groupId, currencyCode: a.currencyCode, sortOrder: a.sortOrder,
  });
  formError.value = '';
  modalOpen.value = true;
}

async function save(): Promise<void> {
  saving.value = true;
  formError.value = '';
  const payload: AccountInput = { ...form, groupId: form.groupId || null, openingBalance: Number(form.openingBalance) };
  try {
    if (editingId.value) {
      await updateAccount(editingId.value, payload);
    } else {
      await createAccount(payload);
    }
    modalOpen.value = false;
    await load();
  } catch (error) {
    formError.value = errorMessage(t, te, error);
  } finally {
    saving.value = false;
  }
}

async function deactivate(a: Account): Promise<void> {
  const ok = await confirm.ask({
    message: t('accounts.deactivateConfirm'),
    confirmLabel: t('accounts.deactivate'),
    danger: true,
  });
  if (!ok) return;
  try {
    await deactivateAccount(a.id);
    toast.success(t('common.done'));
    await load();
  } catch (error) {
    toast.error(errorMessage(t, te, error));
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-4">
    <!-- Top Action Row (when in all accounts list) -->
    <div v-if="!selectedAccount" class="flex flex-wrap items-center justify-end gap-3">
      <div class="flex items-center gap-2">
        <AppButton variant="secondary" @click="importOpen = true">
          <Upload :size="16" /><span class="hidden sm:inline">{{ t('import.accounts') }}</span>
        </AppButton>
        <AppButton @click="openCreate"><Plus :size="16" />{{ t('accounts.add') }}</AppButton>
      </div>
    </div>

    <LoadingBlock v-if="loading" />
    <div v-else-if="failed" class="py-16 text-center">
      <p class="text-sm text-text-muted">{{ t('errors.network_error') }}</p>
      <AppButton class="mt-4" variant="secondary" @click="load">{{ t('common.retry') }}</AppButton>
    </div>

    <!-- DETAIL MODE: Account selected -->
    <template v-else-if="selectedAccount">
      <!-- Detail Header Navigation & Actions -->
      <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div class="flex flex-wrap items-center gap-3">
          <AppButton variant="secondary" size="sm" @click="selectAccount(null)">
            <ArrowLeft :size="16" />
            <span>{{ t('accounts.backToAccounts') }}</span>
          </AppButton>
          <div class="hidden h-5 w-px bg-border sm:block"></div>
          <div class="flex items-center gap-2.5">
            <div
              class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl"
              :class="getAccountColor(selectedAccount.type)"
            >
              <component :is="getAccountIcon(selectedAccount.type)" :size="18" />
            </div>
            <div>
              <div class="flex items-center gap-2">
                <h2 class="text-base font-bold text-text">{{ selectedAccount.name }}</h2>
                <span
                  class="rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider"
                  :class="selectedAccount.isActive ? 'bg-positive-soft text-positive' : 'bg-surface-2 text-text-muted'"
                >
                  {{ selectedAccount.isActive ? t('accounts.activeStatus') : t('accounts.archivedStatus') }}
                </span>
              </div>
              <p class="text-xs text-text-muted">
                {{ t(`enums.accountType.${selectedAccount.type}`) }}
                <span v-if="selectedAccount.groupName"> · {{ selectedAccount.groupName }}</span>
              </p>
            </div>
          </div>
        </div>

        <div class="flex items-center gap-2">
          <AppButton variant="secondary" size="sm" @click="openEdit(selectedAccount)">
            <Pencil :size="15" />
            {{ t('common.edit') }}
          </AppButton>
          <AppButton size="sm" @click="openCreateTxForAccount">
            <Plus :size="15" />
            {{ t('accounts.addForAccount') }}
          </AppButton>
        </div>
      </div>

      <!-- Rich Insights Cards Grid -->
      <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
        <!-- Balance -->
        <AppCard class="relative overflow-hidden">
          <p class="text-xs font-medium text-text-muted">{{ t('accounts.balance') }}</p>
          <p class="mt-1 text-xl font-bold text-text tnum">
            <Money :value="selectedAccount.balance" :currency="selectedAccount.currencyCode" />
          </p>
          <p class="mt-1 text-[11px] text-text-muted">{{ selectedAccount.currencyCode }}</p>
        </AppCard>

        <!-- 30d Inflow -->
        <AppCard>
          <div class="flex items-center justify-between">
            <p class="text-xs font-medium text-text-muted">{{ t('accounts.inflow30d') }}</p>
            <div class="flex h-5 w-5 items-center justify-center rounded-md bg-positive-soft text-positive">
              <ArrowUpRight :size="14" />
            </div>
          </div>
          <p class="mt-1 text-xl font-bold text-positive tnum">
            <Money :value="accountInsights.inflow30d" :currency="selectedAccount.currencyCode" />
          </p>
          <p class="mt-1 text-[11px] text-text-muted">Last 30 days</p>
        </AppCard>

        <!-- 30d Outflow -->
        <AppCard>
          <div class="flex items-center justify-between">
            <p class="text-xs font-medium text-text-muted">{{ t('accounts.outflow30d') }}</p>
            <div class="flex h-5 w-5 items-center justify-center rounded-md bg-negative-soft text-negative">
              <ArrowDownLeft :size="14" />
            </div>
          </div>
          <p class="mt-1 text-xl font-bold text-negative tnum">
            <Money :value="accountInsights.outflow30d" :currency="selectedAccount.currencyCode" />
          </p>
          <p class="mt-1 text-[11px] text-text-muted">Last 30 days</p>
        </AppCard>

        <!-- 30d Net -->
        <AppCard>
          <div class="flex items-center justify-between">
            <p class="text-xs font-medium text-text-muted">{{ t('accounts.net30d') }}</p>
            <div
              class="flex h-5 w-5 items-center justify-center rounded-md"
              :class="accountInsights.net30d >= 0 ? 'bg-positive-soft text-positive' : 'bg-negative-soft text-negative'"
            >
              <TrendingUp :size="14" />
            </div>
          </div>
          <p
            class="mt-1 text-xl font-bold tnum"
            :class="accountInsights.net30d >= 0 ? 'text-positive' : 'text-negative'"
          >
            <Money :value="accountInsights.net30d" :currency="selectedAccount.currencyCode" />
          </p>
          <p class="mt-1 text-[11px] text-text-muted">
            {{ accountInsights.net30d >= 0 ? 'Net positive' : 'Net negative' }}
          </p>
        </AppCard>

        <!-- Cleared ratio & Tx count -->
        <AppCard class="col-span-2 sm:col-span-1">
          <div class="flex items-center justify-between">
            <p class="text-xs font-medium text-text-muted">{{ t('accounts.clearedRatio') }}</p>
            <div class="flex h-5 w-5 items-center justify-center rounded-md bg-primary-soft text-primary">
              <CheckCircle2 :size="14" />
            </div>
          </div>
          <div class="mt-1 flex items-baseline gap-2">
            <span class="text-xl font-bold text-text tnum">{{ accountInsights.clearedRatio }}%</span>
            <span class="text-[11px] text-text-muted">({{ accountInsights.clearedCount }}/{{ accountInsights.totalRecent }})</span>
          </div>
          <p class="mt-1 text-[11px] text-text-muted">
            {{ t('accounts.totalTransactions') }}: <span class="font-medium text-text">{{ accountTxPage?.total ?? 0 }}</span>
          </p>
        </AppCard>
      </div>

      <!-- Account Ledger Filter & Table -->
      <div class="space-y-3">
        <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <h3 class="text-sm font-bold text-text">{{ t('accounts.accountLedger') }}</h3>
          <div class="flex flex-wrap items-center gap-2">
            <div class="w-full sm:w-60">
              <AppInput
                :model-value="accountTxFilters.q"
                :placeholder="t('common.search')"
                @update:model-value="onAccountSearchInput"
              />
            </div>
            <div class="w-36">
              <AppSelect
                :model-value="accountTxFilters.status"
                :options="statusFilterOptions"
                @update:model-value="accountTxFilters.status = $event; applyAccountFilters()"
              />
            </div>
            <div class="w-36">
              <AppSelect
                :model-value="accountTxFilters.type"
                :options="typeFilterOptions"
                @update:model-value="accountTxFilters.type = $event; applyAccountFilters()"
              />
            </div>
          </div>
        </div>

        <LoadingBlock v-if="accountTxLoading" />
        <template v-else>
          <AppCard v-if="accountTxPage && accountTxPage.items.length" :padded="false">
            <ul class="divide-y divide-border">
              <li
                v-for="tx in accountTxPage.items"
                :key="tx.id"
                class="group flex cursor-pointer items-center gap-3 px-4 py-3 transition-colors hover:bg-surface-2/60 sm:gap-4 sm:px-5"
                :class="rowPad"
                @click="openEditTx(tx)"
              >
                <!-- Indicator -->
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

                <!-- Title & Category -->
                <div class="min-w-0 flex-1">
                  <p class="truncate text-sm font-semibold text-text group-hover:text-primary transition-colors">
                    {{ tx.title }}
                  </p>
                  <p class="mt-0.5 truncate text-xs text-text-muted">
                    <span v-if="categoryName(tx.categoryId)">{{ categoryName(tx.categoryId) }}</span>
                    <span v-else class="italic">{{ t('transactions.noCategory') }}</span>
                    <span v-if="tx.description"> · {{ tx.description }}</span>
                  </p>
                </div>

                <!-- Status Chip -->
                <div class="hidden sm:block shrink-0">
                  <StatusChip :status="tx.status as 'Cleared' | 'Uncleared'" />
                </div>

                <!-- Amount -->
                <div class="shrink-0 text-right">
                  <span
                    class="block text-sm font-semibold tnum"
                    :class="{
                      'text-positive font-bold': tx.type === 'Income',
                      'text-negative font-bold': tx.type === 'Expense',
                      'text-accent font-bold': tx.type === 'Transfer'
                    }"
                  >
                    {{ tx.type === 'Expense' ? '-' : (tx.type === 'Income' ? '+' : '') }}<Money :value="tx.amount" />
                  </span>
                </div>

                <!-- Actions -->
                <div class="flex items-center gap-1" @click.stop>
                  <IconButton
                    :icon="tx.status === 'Cleared' ? RotateCcw : CircleCheck"
                    :label="tx.status === 'Cleared' ? t('transactions.markUncleared') : t('transactions.markCleared')"
                    @click="toggleAccountTxClear(tx)"
                  />
                  <IconButton :icon="Ban" :label="t('transactions.void')" danger @click="removeAccountTx(tx)" />
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
              <AppButton size="sm" class="mt-4" @click="openCreateTxForAccount">
                <Plus :size="14" />
                {{ t('accounts.addForAccount') }}
              </AppButton>
            </div>
          </AppCard>

          <!-- Ledger Pagination -->
          <div v-if="accountTxPage && accountTxPage.total > 0" class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between text-xs text-text-muted">
            <div class="flex items-center gap-3">
              <span>{{ t('transactions.showing', { count: accountTxPage.items.length, total: accountTxPage.total }) }}</span>
              <div class="hidden h-3.5 w-px bg-border sm:block"></div>
              <div class="hidden items-center gap-1.5 sm:flex">
                <span class="text-text-muted">{{ t('transactions.itemsPerPage') }}:</span>
                <div class="inline-flex rounded-lg border border-border bg-surface p-0.5">
                  <button
                    v-for="size in [15, 25, 50]"
                    :key="size"
                    type="button"
                    class="rounded-md px-2 py-0.5 text-xs font-medium transition-colors"
                    :class="accountTxFilters.pageSize === size ? 'bg-primary text-white font-semibold shadow-xs' : 'text-text-muted hover:text-text'"
                    @click="setAccountPageSize(size)"
                  >
                    {{ size }}
                  </button>
                </div>
              </div>
            </div>

            <!-- Numbered Pagination -->
            <div class="flex items-center gap-1.5 self-center sm:self-auto">
              <AppButton
                variant="secondary"
                size="sm"
                :disabled="accountTxFilters.page <= 1"
                @click="setAccountPage(accountTxFilters.page - 1)"
              >
                <ChevronLeft :size="14" />
                <span class="hidden sm:inline">{{ t('transactions.prev') }}</span>
              </AppButton>

              <template v-for="(p, idx) in accountDisplayedPages" :key="idx">
                <span v-if="p === '...'" class="px-1.5 text-text-muted select-none">...</span>
                <button
                  v-else
                  type="button"
                  class="h-8 min-w-[2rem] rounded-lg px-2 text-xs font-semibold transition-all"
                  :class="accountTxFilters.page === p
                    ? 'bg-primary text-white shadow-xs'
                    : 'bg-surface hover:bg-surface-2 text-text border border-border'"
                  @click="setAccountPage(Number(p))"
                >
                  {{ p }}
                </button>
              </template>

              <AppButton
                variant="secondary"
                size="sm"
                :disabled="accountTxFilters.page >= accountTotalPages"
                @click="setAccountPage(accountTxFilters.page + 1)"
              >
                <span class="hidden sm:inline">{{ t('transactions.next') }}</span>
                <ChevronRight :size="14" />
              </AppButton>
            </div>
          </div>
        </template>
      </div>
    </template>

    <!-- LIST MODE: All accounts overview -->
    <template v-else>
      <!-- Summary KPI Row -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <AppCard class="relative overflow-hidden">
          <p class="text-xs font-medium text-text-muted">{{ t('accounts.totalNetWorth') }}</p>
          <p class="mt-1 text-2xl font-bold text-text tnum">
            <Money :value="total" />
          </p>
        </AppCard>

        <AppCard>
          <p class="text-xs font-medium text-text-muted">{{ t('accounts.activeAccounts') }}</p>
          <div class="mt-1 flex items-baseline gap-2">
            <span class="text-2xl font-bold text-text tnum">{{ activeCount }}</span>
            <span class="text-xs text-text-muted">{{ t('accounts.activeStatus') }}</span>
          </div>
        </AppCard>

        <AppCard>
          <p class="text-xs font-medium text-text-muted">{{ t('accounts.inactiveAccounts') }}</p>
          <div class="mt-1 flex items-baseline gap-2">
            <span class="text-2xl font-bold text-text-muted tnum">{{ inactiveCount }}</span>
            <span class="text-xs text-text-muted">{{ t('accounts.archivedStatus') }}</span>
          </div>
        </AppCard>
      </div>

      <!-- Accounts List -->
      <AppCard v-if="accounts.length" :padded="false">
        <ul class="divide-y divide-border">
          <li
            v-for="a in accounts"
            :key="a.id"
            class="group flex cursor-pointer items-center gap-3 px-5 py-3.5 transition-colors hover:bg-surface-2/60 sm:gap-4"
            :class="[rowPad, { 'opacity-60': !a.isActive }]"
            @click="selectAccount(a.id)"
          >
            <!-- Account Type Avatar Icon -->
            <div
              class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl transition-transform group-hover:scale-105"
              :class="getAccountColor(a.type)"
            >
              <component :is="getAccountIcon(a.type)" :size="20" />
            </div>

            <!-- Account Details -->
            <div class="min-w-0 flex-1">
              <div class="flex items-center gap-2">
                <p class="truncate text-sm font-semibold text-text group-hover:text-primary transition-colors">
                  {{ a.name }}
                </p>
                <span
                  v-if="!a.isActive"
                  class="rounded-full bg-surface-2 border border-border px-2 py-0.5 text-[10px] font-medium text-text-muted"
                >
                  {{ t('common.inactive') }}
                </span>
              </div>
              <p class="mt-0.5 flex items-center gap-1.5 text-xs text-text-muted">
                <span>{{ t(`enums.accountType.${a.type}`) }}</span>
                <template v-if="a.groupName">
                  <span>·</span>
                  <span class="truncate">{{ a.groupName }}</span>
                </template>
              </p>
            </div>

            <!-- Balance -->
            <Money
              :value="a.balance"
              :currency="a.currencyCode"
              class="shrink-0 text-right text-base font-semibold tnum"
            />

            <!-- Actions -->
            <div class="flex items-center gap-1" @click.stop>
              <IconButton :icon="Pencil" :label="t('common.edit')" @click="openEdit(a)" />
              <IconButton v-if="a.isActive" :icon="Ban" :label="t('accounts.deactivate')" danger @click="deactivate(a)" />
            </div>
          </li>
        </ul>
      </AppCard>

      <AppCard v-else>
        <div class="py-12 text-center">
          <div class="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-2xl bg-surface-2 text-text-muted">
            <Landmark :size="24" />
          </div>
          <p class="text-sm font-medium text-text">{{ t('accounts.empty') }}</p>
        </div>
      </AppCard>
    </template>

    <AppModal v-if="modalOpen" :title="editingId ? t('accounts.edit') : t('accounts.add')" @close="modalOpen = false">
      <form class="space-y-4" @submit.prevent="save">
        <FormField :label="t('accounts.name')" for-id="acc-name">
          <AppInput id="acc-name" v-model="form.name" required />
        </FormField>
        <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <FormField :label="t('accounts.type')" for-id="acc-type">
            <AppSelect id="acc-type" v-model="form.type" :options="typeOptions" />
          </FormField>
          <FormField :label="t('accounts.group')" for-id="acc-group">
            <AppSelect
              id="acc-group"
              :model-value="form.groupId ?? ''"
              :options="groupOptions"
              @update:model-value="form.groupId = $event || null"
            />
          </FormField>
        </div>
        <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <FormField :label="t('accounts.openingBalance')" for-id="acc-opening">
            <AppInput
              id="acc-opening"
              type="number"
              :model-value="String(form.openingBalance)"
              @update:model-value="form.openingBalance = Number($event)"
            />
          </FormField>
          <FormField :label="t('accounts.currency')" for-id="acc-currency">
            <AppInput
              id="acc-currency"
              :model-value="form.currencyCode ?? 'IDR'"
              @update:model-value="form.currencyCode = $event"
            />
          </FormField>
        </div>
        <p v-if="formError" class="text-[13px] text-negative" role="alert">{{ formError }}</p>
      </form>

      <template #footer>
        <AppButton variant="secondary" @click="modalOpen = false">{{ t('common.cancel') }}</AppButton>
        <AppButton :loading="saving" @click="save">{{ saving ? t('common.saving') : t('common.save') }}</AppButton>
      </template>
    </AppModal>

    <ImportModal
      v-if="importOpen"
      :title="t('import.accounts')"
      :config="importConfig"
      @close="importOpen = false"
      @done="onImported"
    />
  </div>
</template>
