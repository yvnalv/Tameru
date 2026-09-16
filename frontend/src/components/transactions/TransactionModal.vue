<script setup lang="ts">
import { ref, reactive, computed, watch, nextTick } from 'vue';
import { useI18n } from 'vue-i18n';
import { Sparkles, Check } from 'lucide-vue-next';
import { useTransactionModalStore } from '@/stores/transactionModal';
import { useToastStore } from '@/stores/toast';
import {
  createTransaction,
  updateTransaction,
  listTransactions,
  type TransactionInput,
} from '@/lib/transactions';
import { listAccounts } from '@/lib/accounts';
import { listCategories, flowAccepts } from '@/lib/categories';
import type { Account, Category, Transaction } from '@/types/api';
import { errorMessage } from '@/lib/errorMessage';
import AppModal from '@/components/ui/AppModal.vue';
import AppButton from '@/components/ui/AppButton.vue';
import AppInput from '@/components/ui/AppInput.vue';
import AppSelect from '@/components/ui/AppSelect.vue';
import FormField from '@/components/ui/FormField.vue';
import MoneyInput from '@/components/ui/MoneyInput.vue';
import PayeeInput, { type HistoricalPayee } from '@/components/ui/PayeeInput.vue';

const { t, te } = useI18n();
const store = useTransactionModalStore();
const toast = useToastStore();

const accounts = ref<Account[]>([]);
const categories = ref<Category[]>([]);
const recentTxs = ref<Transaction[]>([]);
const loadingRefs = ref(false);
const saving = ref(false);
const formError = ref('');
const autoFilledNote = ref('');

const moneyInputRef = ref<InstanceType<typeof MoneyInput> | null>(null);
const payeeInputRef = ref<InstanceType<typeof PayeeInput> | null>(null);

const TX_TYPES = ['Expense', 'Income', 'Transfer'];

function getTodayStr(): string {
  return new Date().toISOString().slice(0, 10);
}

function getYesterdayStr(): string {
  const d = new Date();
  d.setDate(d.getDate() - 1);
  return d.toISOString().slice(0, 10);
}

const todayStr = getTodayStr();
const yesterdayStr = getYesterdayStr();

const form = reactive({
  type: 'Expense',
  date: todayStr,
  title: '',
  amount: 0,
  accountId: '',
  toAccountId: '',
  budgetCategoryId: '',
  categoryId: '',
  status: 'Uncleared',
  description: '',
});

const formSnapshot = ref('');
const formDirty = computed(() => JSON.stringify(form) !== formSnapshot.value);

const amountCurrency = computed(
  () => accounts.value.find((a) => a.id === form.accountId)?.currencyCode ?? 'IDR',
);

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

// Payee history mapping for smart autocomplete
const payeeHistory = computed<HistoricalPayee[]>(() => {
  return recentTxs.value.map((tx) => ({
    title: tx.title,
    type: tx.type,
    categoryId: tx.categoryId,
    budgetCategoryId: tx.budgetCategoryId,
    accountId: tx.accountId,
  }));
});

// Top 5 frequently used categories for the current transaction type
const frequentCategories = computed(() => {
  if (form.type === 'Transfer') return [];
  const counts = new Map<string, number>();
  for (const tx of recentTxs.value) {
    if (tx.type === form.type && tx.categoryId) {
      counts.set(tx.categoryId, (counts.get(tx.categoryId) ?? 0) + 1);
    }
  }
  const sorted = Array.from(counts.entries())
    .sort((a, b) => b[1] - a[1])
    .slice(0, 5)
    .map(([id]) => categories.value.find((c) => c.id === id))
    .filter((c): c is Category => !!c && c.isActive);

  return sorted;
});

async function loadData(): Promise<void> {
  loadingRefs.value = true;
  try {
    const [accs, cats, txPage] = await Promise.all([
      listAccounts(true),
      listCategories(),
      listTransactions({ pageSize: 100 }),
    ]);
    accounts.value = accs;
    categories.value = cats;
    recentTxs.value = txPage.items;
  } catch (err) {
    console.error('Failed to load transaction modal refs', err);
  } finally {
    loadingRefs.value = false;
  }
}

watch(
  () => store.isOpen,
  async (open) => {
    if (open) {
      if (accounts.value.length === 0) {
        await loadData();
      }
      initForm();
    }
  },
  { immediate: true },
);

function initForm(): void {
  autoFilledNote.value = '';
  formError.value = '';

  if (store.editingTx) {
    const tx = store.editingTx;
    Object.assign(form, {
      type: tx.type,
      date: tx.date,
      title: tx.title,
      amount: tx.amount,
      accountId: tx.accountId,
      toAccountId: tx.toAccountId ?? '',
      budgetCategoryId: tx.budgetCategoryId ?? '',
      categoryId: tx.categoryId ?? '',
      status: tx.status,
      description: tx.description ?? '',
    });
  } else {
    // New transaction
    const initial = store.prefill || {};
    const defaultAcc = accounts.value[0]?.id ?? '';
    const defaultToAcc = accounts.value[1]?.id ?? '';

    Object.assign(form, {
      type: initial.type || 'Expense',
      date: initial.date || todayStr,
      title: initial.title || '',
      amount: initial.amount || 0,
      accountId: initial.accountId || defaultAcc,
      toAccountId: initial.toAccountId || defaultToAcc,
      budgetCategoryId: initial.budgetCategoryId || '',
      categoryId: initial.categoryId || '',
      status: initial.status || 'Uncleared',
      description: initial.description || '',
    });
  }

  formSnapshot.value = JSON.stringify(form);
  nextTick(() => {
    if (!form.amount) {
      moneyInputRef.value?.focus();
    } else {
      payeeInputRef.value?.focus();
    }
  });
}

function setType(type: string): void {
  if (store.editingTx) return;
  form.type = type;
  form.budgetCategoryId = '';
  form.categoryId = '';
  autoFilledNote.value = '';
}

function setDatePreset(d: string): void {
  form.date = d;
}

function onSelectPayee(payee: HistoricalPayee): void {
  let notes: string[] = [];

  if (payee.type && !store.editingTx) {
    form.type = payee.type;
  }
  if (payee.accountId && accounts.value.some((a) => a.id === payee.accountId)) {
    form.accountId = payee.accountId;
    const acc = accounts.value.find((a) => a.id === payee.accountId);
    if (acc) notes.push(acc.name);
  }
  if (payee.categoryId && categories.value.some((c) => c.id === payee.categoryId)) {
    form.categoryId = payee.categoryId;
    const cat = categories.value.find((c) => c.id === payee.categoryId);
    if (cat) {
      notes.push(cat.name);
      if (cat.parentId && !form.budgetCategoryId) {
        form.budgetCategoryId = cat.parentId;
      }
    }
  }
  if (payee.budgetCategoryId && categories.value.some((c) => c.id === payee.budgetCategoryId)) {
    form.budgetCategoryId = payee.budgetCategoryId;
  }

  if (notes.length > 0) {
    autoFilledNote.value = notes.join(' · ');
  }
}

function selectFrequentCategory(cat: Category): void {
  form.categoryId = cat.id;
  if (cat.parentId) {
    form.budgetCategoryId = cat.parentId;
  }
}

function buildInput(): TransactionInput {
  const input: TransactionInput = {
    type: form.type,
    date: form.date,
    title: form.title.trim(),
    amount: Number(form.amount),
    accountId: form.accountId,
    status: form.status,
    description: form.description.trim() || null,
  };
  if (form.type === 'Transfer') {
    input.toAccountId = form.toAccountId;
  } else {
    input.budgetCategoryId = form.budgetCategoryId || null;
    input.categoryId = form.categoryId || null;
  }
  return input;
}

function validateInput(input: TransactionInput): boolean {
  if (!input.title) {
    formError.value = t('errors.validation_title_required');
    payeeInputRef.value?.focus();
    return false;
  }
  if (!input.amount || input.amount <= 0) {
    formError.value = t('errors.validation_amount_positive');
    moneyInputRef.value?.focus();
    return false;
  }
  if (!input.accountId) {
    formError.value = t('errors.validation_account_required');
    return false;
  }
  if (input.type === 'Transfer' && input.accountId === input.toAccountId) {
    formError.value = t('errors.validation_transfer_same_account');
    return false;
  }
  return true;
}

async function save(): Promise<void> {
  const input = buildInput();
  if (!validateInput(input)) return;

  saving.value = true;
  formError.value = '';
  try {
    if (store.editingTx) {
      await updateTransaction(store.editingTx.id, {
        date: input.date,
        title: input.title,
        amount: input.amount,
        accountId: input.accountId,
        toAccountId: input.toAccountId ?? null,
        budgetCategoryId: input.budgetCategoryId ?? null,
        categoryId: input.categoryId ?? null,
        status: input.status,
        description: input.description ?? null,
      });
      toast.success(t('common.done'));
      store.notifyCreated(store.editingTx);
    } else {
      const created = await createTransaction(input);
      toast.success(t('transactions.saved'));
      store.notifyCreated(created);
    }
    store.close();
  } catch (err) {
    formError.value = errorMessage(t, te, err);
  } finally {
    saving.value = false;
  }
}

async function saveAndAnother(): Promise<void> {
  if (store.editingTx) {
    await save();
    return;
  }

  const input = buildInput();
  if (!validateInput(input)) return;

  saving.value = true;
  formError.value = '';
  try {
    const created = await createTransaction(input);
    toast.success(t('transactions.saved'));
    store.notifyCreated(created);

    // Keep date, accountId, and type. Clear title, amount, description, category.
    form.title = '';
    form.amount = 0;
    form.description = '';
    form.categoryId = '';
    form.budgetCategoryId = '';
    autoFilledNote.value = '';
    formSnapshot.value = JSON.stringify(form);

    nextTick(() => {
      moneyInputRef.value?.focus();
    });
  } catch (err) {
    formError.value = errorMessage(t, te, err);
  } finally {
    saving.value = false;
  }
}

function onFormKeyDown(e: KeyboardEvent): void {
  // Ctrl+Enter or Cmd+Enter triggers Save & Add Another (or Save in edit mode)
  if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
    e.preventDefault();
    if (store.editingTx) {
      save();
    } else {
      saveAndAnother();
    }
  }
}
</script>

<template>
  <AppModal
    v-if="store.isOpen"
    :title="store.editingTx ? t('transactions.edit') : t('transactions.add')"
    :dirty="formDirty"
    @close="store.close()"
  >
    <form class="space-y-4" @submit.prevent="save" @keydown="onFormKeyDown">
      <!-- Transaction Type Switcher -->
      <div class="grid grid-cols-3 gap-2">
        <button
          v-for="ty in TX_TYPES"
          :key="ty"
          type="button"
          :disabled="!!store.editingTx"
          :aria-pressed="form.type === ty"
          class="rounded-control border px-3 py-2 text-sm font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-50"
          :class="
            form.type === ty
              ? 'border-accent bg-accent-soft text-accent'
              : 'border-border text-text-muted hover:bg-surface-2'
          "
          @click="setType(ty)"
        >
          {{ t(`enums.transactionType.${ty}`) }}
        </button>
      </div>

      <!-- Amount Input (Tactile, formatted, IDR shortcuts) -->
      <FormField :label="t('transactions.amount')" for-id="tx-amount">
        <MoneyInput
          id="tx-amount"
          ref="moneyInputRef"
          v-model="form.amount"
          :currency="amountCurrency"
          :placeholder="t('transactions.amountPlaceholder')"
          @submit="save"
        />
      </FormField>

      <!-- Payee / Title Input (with smart history autocomplete) -->
      <FormField :label="t('transactions.titleField')" for-id="tx-title">
        <PayeeInput
          id="tx-title"
          ref="payeeInputRef"
          v-model="form.title"
          :history="payeeHistory"
          :placeholder="t('transactions.titlePlaceholder')"
          required
          @select-payee="onSelectPayee"
        />
        <!-- Auto-fill indicator -->
        <p
          v-if="autoFilledNote"
          class="mt-1 flex items-center gap-1.5 text-xs text-accent"
        >
          <Sparkles :size="12" />
          <span>{{ t('transactions.autoFilled') }}: {{ autoFilledNote }}</span>
        </p>
      </FormField>

      <!-- Date with Quick Chips (Today / Yesterday) -->
      <div>
        <div class="mb-1.5 flex items-center justify-between">
          <label for="tx-date" class="block text-[13px] font-medium text-text-muted">
            {{ t('transactions.date') }}
          </label>
          <div class="flex items-center gap-1">
            <button
              type="button"
              class="rounded px-2 py-0.5 text-xs font-medium transition-colors"
              :class="
                form.date === todayStr
                  ? 'bg-accent-soft text-accent'
                  : 'text-text-muted hover:bg-surface-2'
              "
              @click="setDatePreset(todayStr)"
            >
              {{ t('transactions.today') }}
            </button>
            <button
              type="button"
              class="rounded px-2 py-0.5 text-xs font-medium transition-colors"
              :class="
                form.date === yesterdayStr
                  ? 'bg-accent-soft text-accent'
                  : 'text-text-muted hover:bg-surface-2'
              "
              @click="setDatePreset(yesterdayStr)"
            >
              {{ t('transactions.yesterday') }}
            </button>
          </div>
        </div>
        <AppInput id="tx-date" v-model="form.date" type="date" />
      </div>

      <!-- Account & Status / ToAccount -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <FormField :label="t('transactions.account')" for-id="tx-account">
          <AppSelect id="tx-account" v-model="form.accountId" :options="accountOptions" />
        </FormField>
        <FormField
          v-if="form.type === 'Transfer'"
          :label="t('transactions.toAccount')"
          for-id="tx-to"
        >
          <AppSelect id="tx-to" v-model="form.toAccountId" :options="accountOptions" />
        </FormField>
        <FormField v-else :label="t('transactions.status')" for-id="tx-status">
          <AppSelect id="tx-status" v-model="form.status" :options="statusOptions" />
        </FormField>
      </div>

      <!-- Category & Budget (for Expense & Income) -->
      <div v-if="form.type !== 'Transfer'" class="space-y-3">
        <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <FormField :label="t('transactions.budget')" for-id="tx-budget">
            <AppSelect id="tx-budget" v-model="form.budgetCategoryId" :options="budgetOptions" />
          </FormField>
          <FormField :label="t('transactions.category')" for-id="tx-category">
            <AppSelect id="tx-category" v-model="form.categoryId" :options="categoryOptions" />
          </FormField>
        </div>

        <!-- Frequent Category Quick Chips -->
        <div v-if="frequentCategories.length > 0" class="pt-1">
          <p class="mb-1.5 text-xs text-text-muted">{{ t('transactions.frequentCategories') }}:</p>
          <div class="flex flex-wrap gap-1.5">
            <button
              v-for="cat in frequentCategories"
              :key="cat.id"
              type="button"
              class="flex items-center gap-1 rounded-full border px-2.5 py-1 text-xs font-medium transition-colors"
              :class="
                form.categoryId === cat.id
                  ? 'border-accent bg-accent-soft text-accent'
                  : 'border-border bg-surface-2 text-text-muted hover:border-border-strong hover:text-text'
              "
              @click="selectFrequentCategory(cat)"
            >
              <Check v-if="form.categoryId === cat.id" :size="12" />
              <span>{{ cat.name }}</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Status for Transfer -->
      <FormField
        v-if="form.type === 'Transfer'"
        :label="t('transactions.status')"
        for-id="tx-status2"
      >
        <AppSelect id="tx-status2" v-model="form.status" :options="statusOptions" />
      </FormField>

      <!-- Optional Note / Description -->
      <FormField :label="t('transactions.description')" for-id="tx-desc">
        <AppInput
          id="tx-desc"
          v-model="form.description"
          :placeholder="t('transactions.descriptionPlaceholder')"
        />
      </FormField>

      <p v-if="formError" class="text-[13px] text-negative" role="alert">{{ formError }}</p>
    </form>

    <template #footer>
      <div class="flex w-full flex-wrap items-center justify-between gap-2">
        <div class="hidden text-xs text-text-muted sm:block">
          <span v-if="!store.editingTx" class="tnum">{{ t('transactions.shortcutHint') }}</span>
        </div>
        <div class="flex items-center gap-2 ml-auto">
          <AppButton variant="secondary" @click="store.close()">
            {{ t('common.cancel') }}
          </AppButton>
          <!-- Save & Add Another Button (New entries only) -->
          <AppButton
            v-if="!store.editingTx"
            variant="secondary"
            :loading="saving"
            @click="saveAndAnother"
          >
            {{ t('transactions.saveAndAnother') }}
          </AppButton>
          <!-- Primary Save Button -->
          <AppButton :loading="saving" @click="save">
            {{ saving ? t('common.saving') : t('common.save') }}
          </AppButton>
        </div>
      </div>
    </template>
  </AppModal>
</template>
