<script setup lang="ts">
import { onMounted, ref, reactive, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { Plus, Check, Undo2, Ban, ArrowRight, Download } from 'lucide-vue-next';
import {
  listTransactions, createTransaction, clearTransaction, unclearTransaction, voidTransaction,
  type TransactionFilter, type TransactionInput,
} from '@/lib/transactions';
import { listAccounts } from '@/lib/accounts';
import { listCategories, flowAccepts } from '@/lib/categories';
import type { Account, Category, Paged, Transaction } from '@/types/api';
import { errorMessage } from '@/lib/errorMessage';
import { formatShortDate } from '@/lib/format';
import { toCsv, downloadCsv } from '@/lib/csv';
import { useDensity } from '@/composables/useDensity';
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
const exporting = ref(false);

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
    window.alert(errorMessage(t, te, error));
  } finally {
    exporting.value = false;
  }
}

// --- create modal -----------------------------------------------------------
const modalOpen = ref(false);
const saving = ref(false);
const formError = ref('');
const today = new Date().toISOString().slice(0, 10);

const form = reactive({
  type: 'Expense',
  date: today,
  title: '',
  amount: 0,
  accountId: '',
  toAccountId: '',
  budgetCategoryId: '',
  categoryId: '',
  status: 'Uncleared',
  description: '',
});

const accountOptions = computed(() => accounts.value.map((a) => ({ value: a.id, label: a.name })));
const budgetOptions = computed(() => [
  { value: '', label: t('transactions.noCategory') },
  ...categories.value
    .filter((c) => c.level === 'Budget' && c.isActive && flowAccepts(c.flow, form.type))
    .map((c) => ({ value: c.id, label: c.name })),
]);
const categoryOptions = computed(() => [
  { value: '', label: t('transactions.noCategory') },
  ...categories.value
    .filter((c) => c.level === 'Category' && c.isActive && flowAccepts(c.flow, form.type))
    .map((c) => ({ value: c.id, label: c.name })),
]);
const statusOptions = computed(() => [
  { value: 'Uncleared', label: t('enums.transactionStatus.Uncleared') },
  { value: 'Cleared', label: t('enums.transactionStatus.Cleared') },
]);

function openCreate(): void {
  Object.assign(form, {
    type: 'Expense', date: today, title: '', amount: 0,
    accountId: accounts.value[0]?.id ?? '', toAccountId: accounts.value[1]?.id ?? '',
    budgetCategoryId: '', categoryId: '', status: 'Uncleared', description: '',
  });
  formError.value = '';
  modalOpen.value = true;
}

function setType(type: string): void {
  form.type = type;
  form.budgetCategoryId = '';
  form.categoryId = '';
}

async function save(): Promise<void> {
  saving.value = true;
  formError.value = '';
  const input: TransactionInput = {
    type: form.type,
    date: form.date,
    title: form.title,
    amount: Number(form.amount),
    accountId: form.accountId,
    status: form.status,
    description: form.description || null,
  };
  if (form.type === 'Transfer') {
    input.toAccountId = form.toAccountId;
  } else {
    input.budgetCategoryId = form.budgetCategoryId || null;
    input.categoryId = form.categoryId || null;
  }
  try {
    await createTransaction(input);
    modalOpen.value = false;
    await Promise.all([loadPage(), loadRefs()]);
  } catch (error) {
    formError.value = errorMessage(t, te, error);
  } finally {
    saving.value = false;
  }
}

// --- row actions ------------------------------------------------------------
async function toggleClear(tx: Transaction): Promise<void> {
  try {
    if (tx.status === 'Cleared') await unclearTransaction(tx.id);
    else await clearTransaction(tx.id);
    await loadPage();
  } catch (error) {
    window.alert(errorMessage(t, te, error));
  }
}

async function remove(tx: Transaction): Promise<void> {
  if (!window.confirm(t('transactions.voidConfirm'))) return;
  try {
    await voidTransaction(tx.id);
    await Promise.all([loadPage(), loadRefs()]);
  } catch (error) {
    window.alert(errorMessage(t, te, error));
  }
}

onMounted(async () => {
  await loadRefs();
  await loadPage();
});
</script>

<template>
  <div class="space-y-4">
    <div class="flex items-center justify-between">
      <h1 class="text-lg font-semibold">{{ t('transactions.title') }}</h1>
      <div class="flex items-center gap-2">
        <AppButton variant="secondary" :loading="exporting" @click="exportCsv">
          <Download :size="16" />{{ t('common.export') }}
        </AppButton>
        <AppButton @click="openCreate"><Plus :size="16" />{{ t('transactions.add') }}</AppButton>
      </div>
    </div>

    <!-- Filters -->
    <AppCard>
      <div class="grid grid-cols-2 gap-3 md:grid-cols-4 lg:grid-cols-6">
        <AppSelect :model-value="filters.type ?? ''" :options="typeFilterOptions" @update:model-value="filters.type = $event; applyFilters()" />
        <AppSelect :model-value="filters.accountId ?? ''" :options="accountFilterOptions" @update:model-value="filters.accountId = $event; applyFilters()" />
        <AppSelect :model-value="filters.status ?? ''" :options="statusFilterOptions" @update:model-value="filters.status = $event; applyFilters()" />
        <AppInput :model-value="filters.q ?? ''" :placeholder="t('common.search')" @update:model-value="filters.q = $event" />
        <AppInput :model-value="filters.from ?? ''" type="date" @update:model-value="filters.from = $event; applyFilters()" />
        <AppInput :model-value="filters.to ?? ''" type="date" @update:model-value="filters.to = $event; applyFilters()" />
      </div>
    </AppCard>

    <div v-if="loading" class="py-16 text-center text-sm text-text-muted">{{ t('common.loading') }}</div>
    <div v-else-if="failed" class="py-16 text-center">
      <p class="text-sm text-text-muted">{{ t('errors.network_error') }}</p>
      <AppButton class="mt-4" variant="secondary" @click="loadPage">{{ t('common.retry') }}</AppButton>
    </div>

    <template v-else>
      <AppCard v-if="page && page.items.length" :padded="false">
        <ul class="divide-y divide-border">
          <li v-for="tx in page.items" :key="tx.id" class="flex items-center gap-3 px-5" :class="rowPad">
            <div class="w-14 shrink-0 tnum text-[13px] text-text-muted">{{ formatShortDate(tx.date, locale) }}</div>
            <div class="min-w-0 flex-1">
              <p class="truncate text-sm font-medium">{{ tx.title }}</p>
              <p class="flex items-center gap-1 truncate text-[13px] text-text-muted">
                <span>{{ accountName(tx.accountId) }}</span>
                <template v-if="tx.type === 'Transfer'">
                  <ArrowRight :size="12" /><span>{{ accountName(tx.toAccountId) }}</span>
                </template>
                <template v-else-if="categoryName(tx.categoryId)"> · {{ categoryName(tx.categoryId) }}</template>
              </p>
            </div>
            <StatusChip :status="tx.status as 'Cleared' | 'Uncleared'" />
            <Money
              :value="signedAmount(tx)"
              :currency="tx.currencyCode"
              :signed="tx.type !== 'Transfer'"
              class="w-32 shrink-0 text-right text-sm font-medium"
            />
            <div class="flex w-16 shrink-0 items-center justify-end gap-1">
              <button
                class="rounded-control p-1.5 text-text-muted hover:bg-surface-2 hover:text-text"
                :title="tx.status === 'Cleared' ? t('transactions.unclear') : t('transactions.clear')"
                @click="toggleClear(tx)"
              >
                <Undo2 v-if="tx.status === 'Cleared'" :size="15" />
                <Check v-else :size="15" />
              </button>
              <button class="rounded-control p-1.5 text-text-muted hover:bg-surface-2 hover:text-negative" :title="t('transactions.void')" @click="remove(tx)">
                <Ban :size="15" />
              </button>
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

    <!-- Create modal -->
    <AppModal v-if="modalOpen" :title="t('transactions.add')" @close="modalOpen = false">
      <form class="space-y-4" @submit.prevent="save">
        <div class="grid grid-cols-3 gap-2">
          <button
            v-for="ty in TX_TYPES"
            :key="ty"
            type="button"
            class="rounded-control border px-3 py-2 text-sm font-medium"
            :class="form.type === ty ? 'border-accent bg-accent-soft text-accent' : 'border-border text-text-muted hover:bg-surface-2'"
            @click="setType(ty)"
          >
            {{ t(`enums.transactionType.${ty}`) }}
          </button>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <FormField :label="t('transactions.date')" for-id="tx-date">
            <AppInput id="tx-date" v-model="form.date" type="date" />
          </FormField>
          <FormField :label="t('transactions.amount')" for-id="tx-amount">
            <AppInput id="tx-amount" type="number" :model-value="String(form.amount)" @update:model-value="form.amount = Number($event)" />
          </FormField>
        </div>

        <FormField :label="t('transactions.titleField')" for-id="tx-title">
          <AppInput id="tx-title" v-model="form.title" required />
        </FormField>

        <div class="grid grid-cols-2 gap-4">
          <FormField :label="t('transactions.account')" for-id="tx-account">
            <AppSelect id="tx-account" v-model="form.accountId" :options="accountOptions" />
          </FormField>
          <FormField v-if="form.type === 'Transfer'" :label="t('transactions.toAccount')" for-id="tx-to">
            <AppSelect id="tx-to" v-model="form.toAccountId" :options="accountOptions" />
          </FormField>
          <FormField v-else :label="t('transactions.status')" for-id="tx-status">
            <AppSelect id="tx-status" v-model="form.status" :options="statusOptions" />
          </FormField>
        </div>

        <div v-if="form.type !== 'Transfer'" class="grid grid-cols-2 gap-4">
          <FormField :label="t('transactions.budget')" for-id="tx-budget">
            <AppSelect id="tx-budget" v-model="form.budgetCategoryId" :options="budgetOptions" />
          </FormField>
          <FormField :label="t('transactions.category')" for-id="tx-category">
            <AppSelect id="tx-category" v-model="form.categoryId" :options="categoryOptions" />
          </FormField>
        </div>

        <FormField v-if="form.type === 'Transfer'" :label="t('transactions.status')" for-id="tx-status2">
          <AppSelect id="tx-status2" v-model="form.status" :options="statusOptions" />
        </FormField>

        <p v-if="formError" class="text-[13px] text-negative" role="alert">{{ formError }}</p>
      </form>

      <template #footer>
        <AppButton variant="secondary" @click="modalOpen = false">{{ t('common.cancel') }}</AppButton>
        <AppButton :loading="saving" @click="save">{{ saving ? t('common.saving') : t('common.save') }}</AppButton>
      </template>
    </AppModal>
  </div>
</template>
