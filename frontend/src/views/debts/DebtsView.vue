<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import {
  Plus,
  CreditCard,
  CheckCircle2,
  Calendar,
  DollarSign,
  ArrowUpRight,
  Pencil,
  Trash2,
  History,
  ShieldAlert,
} from 'lucide-vue-next';
import {
  listDebts,
  getDebtsSummary,
  createDebt,
  updateDebt,
  deleteDebt,
  getDebtPayments,
  recordDebtPayment,
  deleteDebtPayment,
} from '@/lib/debts';
import { listAccounts } from '@/lib/accounts';
import { listCategories } from '@/lib/categories';
import type {
  Liability,
  LiabilityPayment,
  DebtsSummary,
  Account,
  Category,
  CreateLiabilityInput,
  UpdateLiabilityInput,
  RecordPaymentInput,
} from '@/types/api';
import { errorMessage } from '@/lib/errorMessage';
import { formatRowDate } from '@/lib/format';
import { useToastStore } from '@/stores/toast';
import { useConfirmStore } from '@/stores/confirm';
import AppCard from '@/components/ui/AppCard.vue';
import AppButton from '@/components/ui/AppButton.vue';
import AppModal from '@/components/ui/AppModal.vue';
import MoneyInput from '@/components/ui/MoneyInput.vue';
import AppInput from '@/components/ui/AppInput.vue';
import AppSelect from '@/components/ui/AppSelect.vue';
import FormField from '@/components/ui/FormField.vue';
import Money from '@/components/ui/Money.vue';
import LoadingBlock from '@/components/ui/LoadingBlock.vue';
import IconButton from '@/components/ui/IconButton.vue';

const { t, te } = useI18n();
const toast = useToastStore();
const confirm = useConfirmStore();

const loading = ref(true);
const debts = ref<Liability[]>([]);
const summary = ref<DebtsSummary | null>(null);
const accounts = ref<Account[]>([]);
const categories = ref<Category[]>([]);
const activeTab = ref<'active' | 'paidOff' | 'receivable' | 'all'>('active');

// Modals
const liabilityModalOpen = ref(false);
const editingLiability = ref<Liability | null>(null);
const paymentModalOpen = ref(false);
const selectedForPayment = ref<Liability | null>(null);
const historyModalOpen = ref(false);
const historyPayments = ref<LiabilityPayment[]>([]);
const historyLiability = ref<Liability | null>(null);
const historyLoading = ref(false);

// Form state: Liability
const liabilityForm = ref({
  title: '',
  type: 'Debt',
  creditor: '',
  totalAmount: 0,
  initialPaidAmount: 0,
  monthlyInstallment: 0,
  dueDay: null as number | null,
  interestRate: null as number | null,
  startDate: new Date().toISOString().split('T')[0],
  dueDate: '' as string,
  notes: '',
});

// Form state: Payment
const paymentForm = ref({
  amount: 0,
  date: new Date().toISOString().split('T')[0],
  principalAmount: null as number | null,
  interestAmount: null as number | null,
  notes: '',
  postToLedger: false,
  accountId: '',
  categoryId: '',
});

const filteredDebts = computed(() => {
  if (activeTab.value === 'active') {
    return debts.value.filter((d) => d.status === 'Active' && d.type !== 'Receivable');
  }
  if (activeTab.value === 'paidOff') {
    return debts.value.filter((d) => d.status === 'PaidOff');
  }
  if (activeTab.value === 'receivable') {
    return debts.value.filter((d) => d.type === 'Receivable');
  }
  return debts.value;
});

const accountOptions = computed(() =>
  accounts.value.map((a) => ({ value: a.id, label: `${a.name} (${a.currencyCode})` }))
);

const categoryOptions = computed(() => [
  { value: '', label: t('recurring.noCategory') },
  ...categories.value
    .filter((c) => c.flow === 'Expense')
    .map((c) => ({ value: c.id, label: c.name })),
]);

async function loadData() {
  loading.value = true;
  try {
    const [dList, sData, aList, cList] = await Promise.all([
      listDebts(),
      getDebtsSummary(),
      listAccounts(false),
      listCategories(),
    ]);
    debts.value = dList;
    summary.value = sData;
    accounts.value = aList;
    categories.value = cList;
  } catch (e) {
    toast.error(errorMessage(t, te, e));
  } finally {
    loading.value = false;
  }
}

function openCreateLiability() {
  editingLiability.value = null;
  liabilityForm.value = {
    title: '',
    type: 'Debt',
    creditor: '',
    totalAmount: 0,
    initialPaidAmount: 0,
    monthlyInstallment: 0,
    dueDay: null,
    interestRate: null,
    startDate: new Date().toISOString().split('T')[0],
    dueDate: '',
    notes: '',
  };
  liabilityModalOpen.value = true;
}

function openEditLiability(l: Liability) {
  editingLiability.value = l;
  liabilityForm.value = {
    title: l.title,
    type: l.type,
    creditor: l.creditor,
    totalAmount: l.totalAmount,
    initialPaidAmount: 0,
    monthlyInstallment: l.monthlyInstallment,
    dueDay: l.dueDay,
    interestRate: l.interestRate,
    startDate: l.startDate,
    dueDate: l.dueDate || '',
    notes: l.notes || '',
  };
  liabilityModalOpen.value = true;
}

async function saveLiability() {
  if (!liabilityForm.value.title.trim()) {
    toast.error(t('debts.validationTitleRequired'));
    return;
  }
  if (liabilityForm.value.totalAmount <= 0) {
    toast.error(t('debts.validationAmountPositive'));
    return;
  }

  try {
    if (editingLiability.value) {
      const input: UpdateLiabilityInput = {
        title: liabilityForm.value.title,
        type: liabilityForm.value.type,
        creditor: liabilityForm.value.creditor,
        totalAmount: liabilityForm.value.totalAmount,
        monthlyInstallment: liabilityForm.value.monthlyInstallment,
        dueDay: liabilityForm.value.dueDay || null,
        interestRate: liabilityForm.value.interestRate || null,
        startDate: liabilityForm.value.startDate,
        dueDate: liabilityForm.value.dueDate || null,
        notes: liabilityForm.value.notes || null,
      };
      await updateDebt(editingLiability.value.id, input);
      toast.success(t('debts.savedSuccess'));
    } else {
      const input: CreateLiabilityInput = {
        title: liabilityForm.value.title,
        type: liabilityForm.value.type,
        creditor: liabilityForm.value.creditor,
        totalAmount: liabilityForm.value.totalAmount,
        initialPaidAmount: liabilityForm.value.initialPaidAmount || 0,
        monthlyInstallment: liabilityForm.value.monthlyInstallment || 0,
        dueDay: liabilityForm.value.dueDay || null,
        interestRate: liabilityForm.value.interestRate || null,
        startDate: liabilityForm.value.startDate || null,
        dueDate: liabilityForm.value.dueDate || null,
        notes: liabilityForm.value.notes || null,
      };
      await createDebt(input);
      toast.success(t('debts.createdSuccess'));
    }
    liabilityModalOpen.value = false;
    await loadData();
  } catch (e) {
    toast.error(errorMessage(t, te, e));
  }
}

async function confirmDeleteLiability(l: Liability) {
  const ok = await confirm.ask({
    title: t('debts.deleteTitle'),
    message: t('debts.deleteConfirm', { title: l.title }),
    confirmLabel: t('common.delete'),
    danger: true,
  });
  if (!ok) return;

  try {
    await deleteDebt(l.id);
    toast.success(t('debts.deletedSuccess'));
    await loadData();
  } catch (e) {
    toast.error(errorMessage(t, te, e));
  }
}

function openPaymentModal(l: Liability) {
  selectedForPayment.value = l;
  paymentForm.value = {
    amount: l.monthlyInstallment > 0 && l.monthlyInstallment <= l.remainingBalance ? l.monthlyInstallment : l.remainingBalance,
    date: new Date().toISOString().split('T')[0],
    principalAmount: null,
    interestAmount: null,
    notes: '',
    postToLedger: false,
    accountId: accounts.value.length > 0 ? accounts.value[0].id : '',
    categoryId: '',
  };
  paymentModalOpen.value = true;
}

async function submitPayment() {
  if (!selectedForPayment.value) return;
  if (paymentForm.value.amount <= 0) {
    toast.error(t('debts.validationPaymentPositive'));
    return;
  }

  try {
    const input: RecordPaymentInput = {
      amount: paymentForm.value.amount,
      date: paymentForm.value.date,
      principalAmount: paymentForm.value.principalAmount || undefined,
      interestAmount: paymentForm.value.interestAmount || undefined,
      notes: paymentForm.value.notes || undefined,
      postToLedger: paymentForm.value.postToLedger,
      accountId: paymentForm.value.postToLedger && paymentForm.value.accountId ? paymentForm.value.accountId : null,
      categoryId: paymentForm.value.postToLedger && paymentForm.value.categoryId ? paymentForm.value.categoryId : null,
    };

    await recordDebtPayment(selectedForPayment.value.id, input);
    toast.success(t('debts.paymentRecordedSuccess'));
    paymentModalOpen.value = false;
    await loadData();
  } catch (e) {
    toast.error(errorMessage(t, te, e));
  }
}

async function openHistoryModal(l: Liability) {
  historyLiability.value = l;
  historyModalOpen.value = true;
  historyLoading.value = true;
  try {
    historyPayments.value = await getDebtPayments(l.id);
  } catch (e) {
    toast.error(errorMessage(t, te, e));
  } finally {
    historyLoading.value = false;
  }
}

async function confirmDeletePayment(paymentId: string) {
  if (!historyLiability.value) return;
  const ok = await confirm.ask({
    title: t('debts.deletePaymentTitle'),
    message: t('debts.deletePaymentConfirm'),
    confirmLabel: t('common.delete'),
    danger: true,
  });
  if (!ok) return;

  try {
    await deleteDebtPayment(historyLiability.value.id, paymentId);
    toast.success(t('debts.paymentDeletedSuccess'));
    historyPayments.value = await getDebtPayments(historyLiability.value.id);
    await loadData();
  } catch (e) {
    toast.error(errorMessage(t, te, e));
  }
}

onMounted(loadData);
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold tracking-tight text-text">
          {{ t('debts.title') }}
        </h1>
        <p class="mt-1 text-sm text-text-muted">
          {{ t('debts.subtitle') }}
        </p>
      </div>
      <div class="flex items-center gap-3">
        <AppButton variant="primary" @click="openCreateLiability">
          <Plus class="h-4 w-4 mr-2" />
          {{ t('debts.addLiability') }}
        </AppButton>
      </div>
    </div>

    <!-- Executive Summary Cards -->
    <div v-if="summary" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <AppCard class="p-5 flex flex-col justify-between">
        <div class="flex items-center justify-between text-text-muted">
          <span class="text-xs font-semibold uppercase tracking-wider">{{ t('debts.totalRemaining') }}</span>
          <ShieldAlert class="h-4 w-4 text-warning" />
        </div>
        <div class="mt-3">
          <span class="text-2xl font-bold font-mono tracking-tight text-warning">
            <Money :value="summary.totalRemaining" currency="IDR" />
          </span>
          <p class="text-xs text-text-muted mt-1">
            {{ summary.activeCount }} {{ t('debts.activeAccounts') }}
          </p>
        </div>
      </AppCard>

      <AppCard class="p-5 flex flex-col justify-between">
        <div class="flex items-center justify-between text-text-muted">
          <span class="text-xs font-semibold uppercase tracking-wider">{{ t('debts.totalPaid') }}</span>
          <CheckCircle2 class="h-4 w-4 text-accent" />
        </div>
        <div class="mt-3">
          <span class="text-2xl font-bold font-mono tracking-tight text-accent">
            <Money :value="summary.totalPaid" currency="IDR" />
          </span>
          <p class="text-xs text-text-muted mt-1">
            {{ summary.overallProgressPercentage }}% {{ t('debts.repaid') }}
          </p>
        </div>
      </AppCard>

      <AppCard class="p-5 flex flex-col justify-between">
        <div class="flex items-center justify-between text-text-muted">
          <span class="text-xs font-semibold uppercase tracking-wider">{{ t('debts.monthlyCommitment') }}</span>
          <Calendar class="h-4 w-4 text-text-muted" />
        </div>
        <div class="mt-3">
          <span class="text-2xl font-bold font-mono tracking-tight text-text">
            <Money :value="summary.monthlyCommitment" currency="IDR" />
          </span>
          <p class="text-xs text-text-muted mt-1">
            {{ t('debts.monthlyPlannedInstallments') }}
          </p>
        </div>
      </AppCard>

      <AppCard class="p-5 flex flex-col justify-between">
        <div class="flex items-center justify-between text-text-muted">
          <span class="text-xs font-semibold uppercase tracking-wider">{{ t('debts.totalReceivables') }}</span>
          <ArrowUpRight class="h-4 w-4 text-info" />
        </div>
        <div class="mt-3">
          <span class="text-2xl font-bold font-mono tracking-tight text-info">
            <Money :value="summary.totalReceivables" currency="IDR" />
          </span>
          <p class="text-xs text-text-muted mt-1">
            {{ t('debts.moneyOwedToYou') }}
          </p>
        </div>
      </AppCard>
    </div>

    <!-- Filter Tabs -->
    <div class="flex items-center gap-2 border-b border-border pb-3 overflow-x-auto">
      <button
        v-for="tab in [
          { id: 'active', label: t('debts.tabActive') },
          { id: 'paidOff', label: t('debts.tabPaidOff') },
          { id: 'receivable', label: t('debts.tabReceivable') },
          { id: 'all', label: t('debts.tabAll') }
        ]"
        :key="tab.id"
        class="px-4 py-2 text-sm font-medium rounded-lg transition-colors whitespace-nowrap"
        :class="activeTab === tab.id
          ? 'bg-accent/15 text-accent font-semibold'
          : 'text-text-muted hover:text-text hover:bg-surface-elevated'"
        @click="activeTab = tab.id as any"
      >
        {{ tab.label }}
      </button>
    </div>

    <!-- Content -->
    <LoadingBlock v-if="loading" />
    <div v-else-if="filteredDebts.length === 0" class="text-center py-16">
      <CreditCard class="mx-auto h-12 w-12 text-text-muted/40 mb-3" />
      <p class="text-text font-medium">{{ t('debts.noDebtsFound') }}</p>
      <p class="text-xs text-text-muted mt-1">{{ t('debts.noDebtsSubtitle') }}</p>
      <AppButton variant="secondary" class="mt-4" @click="openCreateLiability">
        <Plus class="h-4 w-4 mr-2" />
        {{ t('debts.addLiability') }}
      </AppButton>
    </div>

    <!-- Liability Cards Grid -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <AppCard
        v-for="item in filteredDebts"
        :key="item.id"
        class="p-5 flex flex-col justify-between hover:border-accent/40 transition-colors"
      >
        <div>
          <!-- Header row -->
          <div class="flex items-start justify-between gap-3">
            <div>
              <div class="flex items-center gap-2">
                <h3 class="font-bold text-text text-base">{{ item.title }}</h3>
                <span
                  class="text-[11px] px-2 py-0.5 rounded font-mono uppercase font-semibold tracking-wider"
                  :class="item.type === 'Receivable' ? 'bg-info/15 text-info' : 'bg-surface-elevated text-text-muted border border-border'"
                >
                  {{ item.type }}
                </span>
              </div>
              <p class="text-xs text-text-muted mt-0.5">
                {{ item.creditor }}
                <span v-if="item.dueDay" class="ml-2 font-mono">· {{ t('debts.dueDayLabel', { day: item.dueDay }) }}</span>
              </p>
            </div>
            <span
              class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
              :class="item.status === 'PaidOff' ? 'text-positive bg-positive-soft border border-positive/20' : 'text-warning bg-warning-soft border border-warning/20'"
            >
              {{ item.status === 'PaidOff' ? t('debts.statusPaidOff') : t('debts.statusActive') }}
            </span>
          </div>

          <!-- Progress bar -->
          <div class="mt-5 space-y-1.5">
            <div class="flex items-center justify-between text-xs">
              <span class="text-text-muted">
                {{ t('debts.repaid') }}:
                <span class="font-mono text-text font-medium"><Money :value="item.paidAmount" currency="IDR" /></span>
              </span>
              <span class="font-mono font-bold text-accent">{{ item.progressPercentage }}%</span>
            </div>
            <div class="h-2 w-full bg-surface-elevated rounded-full overflow-hidden border border-border/50">
              <div
                class="h-full bg-accent transition-all duration-300"
                :style="{ width: `${Math.min(100, Math.max(0, item.progressPercentage))}%` }"
              />
            </div>
            <div class="flex items-center justify-between text-xs text-text-muted pt-0.5">
              <span>{{ t('debts.totalPrincipal') }}: <Money :value="item.totalAmount" currency="IDR" /></span>
              <span class="font-semibold text-warning">
                {{ t('debts.leftover') }}: <Money :value="item.remainingBalance" currency="IDR" />
              </span>
            </div>
          </div>

          <!-- Installment info & Dates -->
          <div class="mt-4 pt-3 border-t border-border grid grid-cols-2 gap-2 text-xs">
            <div>
              <span class="text-text-muted block">{{ t('debts.monthlyInstallment') }}</span>
              <span class="font-mono font-medium text-text mt-0.5 block">
                <Money :value="item.monthlyInstallment" currency="IDR" />
              </span>
            </div>
            <div v-if="item.dueDate">
              <span class="text-text-muted block">{{ t('debts.targetPayoff') }}</span>
              <span class="font-mono text-text mt-0.5 block">{{ formatRowDate(item.dueDate) }}</span>
            </div>
          </div>

          <p v-if="item.notes" class="mt-3 text-xs text-text-muted/80 italic line-clamp-2">
            "{{ item.notes }}"
          </p>
        </div>

        <!-- Card Footer Actions -->
        <div class="mt-5 pt-3 border-t border-border flex items-center justify-between gap-2">
          <div class="flex items-center gap-2">
            <AppButton
              v-if="item.status === 'Active'"
              variant="primary"
              @click="openPaymentModal(item)"
            >
              <DollarSign class="h-3.5 w-3.5 mr-1" />
              {{ t('debts.recordPayment') }}
            </AppButton>
            <AppButton
              variant="secondary"
              @click="openHistoryModal(item)"
            >
              <History class="h-3.5 w-3.5 mr-1" />
              {{ t('debts.history') }} ({{ item.paymentsCount }})
            </AppButton>
          </div>
          <div class="flex items-center gap-1">
            <IconButton :icon="Pencil" :label="t('common.edit')" @click="openEditLiability(item)" />
            <IconButton :icon="Trash2" :label="t('common.delete')" :danger="true" @click="confirmDeleteLiability(item)" />
          </div>
        </div>
      </AppCard>
    </div>

    <!-- Create / Edit Liability Modal -->
    <AppModal
      v-if="liabilityModalOpen"
      :title="editingLiability ? t('debts.editLiability') : t('debts.addLiability')"
      @close="liabilityModalOpen = false"
    >
      <form class="space-y-4" @submit.prevent="saveLiability">
        <FormField :label="t('debts.formTitle')" required>
          <AppInput v-model="liabilityForm.title" :placeholder="t('debts.placeholderTitle')" />
        </FormField>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField :label="t('debts.formType')" required>
            <AppSelect
              v-model="liabilityForm.type"
              :options="[
                { value: 'Debt', label: t('debts.typeDebt') },
                { value: 'Installment', label: t('debts.typeInstallment') },
                { value: 'Receivable', label: t('debts.typeReceivable') }
              ]"
            />
          </FormField>
          <FormField :label="t('debts.formCreditor')" required>
            <AppInput v-model="liabilityForm.creditor" :placeholder="t('debts.placeholderCreditor')" />
          </FormField>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField :label="t('debts.formTotalAmount')" required>
            <MoneyInput v-model="liabilityForm.totalAmount" :show-chips="false" />
          </FormField>
          <FormField v-if="!editingLiability" :label="t('debts.formInitialPaidAmount')">
            <MoneyInput v-model="liabilityForm.initialPaidAmount" :show-chips="false" />
          </FormField>
          <FormField v-else :label="t('debts.formMonthlyInstallment')">
            <MoneyInput v-model="liabilityForm.monthlyInstallment" :show-chips="false" />
          </FormField>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <FormField v-if="!editingLiability" :label="t('debts.formMonthlyInstallment')">
            <MoneyInput v-model="liabilityForm.monthlyInstallment" :show-chips="false" />
          </FormField>
          <FormField :label="t('debts.formDueDay')">
            <input
              v-model.number="liabilityForm.dueDay"
              type="number"
              min="1"
              max="31"
              placeholder="1-31"
              class="h-10 w-full min-w-0 rounded-control border border-border-strong bg-surface px-3 text-sm text-text placeholder:text-text-muted focus:border-accent"
            />
          </FormField>
          <FormField :label="t('debts.formInterestRate')">
            <input
              v-model.number="liabilityForm.interestRate"
              type="number"
              min="0"
              step="any"
              placeholder="% APR"
              class="h-10 w-full min-w-0 rounded-control border border-border-strong bg-surface px-3 text-sm text-text placeholder:text-text-muted focus:border-accent"
            />
          </FormField>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField :label="t('debts.formStartDate')">
            <AppInput v-model="liabilityForm.startDate" type="date" />
          </FormField>
          <FormField :label="t('debts.formDueDate')">
            <AppInput v-model="liabilityForm.dueDate" type="date" />
          </FormField>
        </div>

        <FormField :label="t('debts.formNotes')">
          <AppInput v-model="liabilityForm.notes" :placeholder="t('debts.placeholderNotes')" />
        </FormField>

        <div class="flex justify-end gap-3 pt-4 border-t border-border">
          <AppButton variant="secondary" type="button" @click="liabilityModalOpen = false">
            {{ t('common.cancel') }}
          </AppButton>
          <AppButton variant="primary" type="submit">
            {{ t('common.save') }}
          </AppButton>
        </div>
      </form>
    </AppModal>

    <!-- Record Payment Modal -->
    <AppModal
      v-if="paymentModalOpen"
      :title="t('debts.recordPaymentTitle', { title: selectedForPayment?.title || '' })"
      @close="paymentModalOpen = false"
    >
      <form class="space-y-4" @submit.prevent="submitPayment">
        <div v-if="selectedForPayment" class="p-3 bg-surface-elevated rounded-lg border border-border text-xs flex justify-between">
          <span class="text-text-muted">{{ t('debts.leftover') }}:</span>
          <span class="font-mono font-bold text-warning"><Money :value="selectedForPayment.remainingBalance" currency="IDR" /></span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField :label="t('debts.paymentAmount')" required>
            <MoneyInput v-model="paymentForm.amount" :show-chips="false" />
          </FormField>
          <FormField :label="t('debts.paymentDate')" required>
            <AppInput v-model="paymentForm.date" type="date" />
          </FormField>
        </div>

        <FormField :label="t('debts.formNotes')">
          <AppInput v-model="paymentForm.notes" :placeholder="t('debts.placeholderPaymentNotes')" />
        </FormField>

        <!-- Optional Ledger Transaction Posting -->
        <div class="pt-3 border-t border-border space-y-3">
          <label class="flex items-center gap-2 cursor-pointer text-sm font-medium text-text">
            <input
              v-model="paymentForm.postToLedger"
              type="checkbox"
              class="h-4 w-4 rounded border-border text-accent focus:ring-accent"
            />
            {{ t('debts.postToLedgerCheckbox') }}
          </label>

          <div v-if="paymentForm.postToLedger" class="grid grid-cols-1 sm:grid-cols-2 gap-4 pl-6">
            <FormField :label="t('debts.selectAccount')" required>
              <AppSelect v-model="paymentForm.accountId" :options="accountOptions" />
            </FormField>
            <FormField :label="t('debts.selectCategory')">
              <AppSelect v-model="paymentForm.categoryId" :options="categoryOptions" />
            </FormField>
          </div>
        </div>

        <div class="flex justify-end gap-3 pt-4 border-t border-border">
          <AppButton variant="secondary" type="button" @click="paymentModalOpen = false">
            {{ t('common.cancel') }}
          </AppButton>
          <AppButton variant="primary" type="submit">
            {{ t('debts.submitPayment') }}
          </AppButton>
        </div>
      </form>
    </AppModal>

    <!-- Payment History Modal -->
    <AppModal
      v-if="historyModalOpen"
      :title="t('debts.historyTitle', { title: historyLiability?.title || '' })"
      @close="historyModalOpen = false"
    >
      <div class="space-y-4">
        <LoadingBlock v-if="historyLoading" />
        <div v-else-if="historyPayments.length === 0" class="text-center py-8 text-text-muted text-sm">
          {{ t('debts.noPaymentsRecorded') }}
        </div>
        <div v-else class="max-h-[60vh] overflow-y-auto divide-y divide-border">
          <div
            v-for="p in historyPayments"
            :key="p.id"
            class="py-3 flex items-center justify-between text-xs"
          >
            <div>
              <span class="font-mono text-text font-bold text-sm block">
                <Money :value="p.amount" currency="IDR" />
              </span>
              <span class="text-text-muted mt-0.5 block">
                {{ formatRowDate(p.date) }} <span v-if="p.notes">· {{ p.notes }}</span>
              </span>
            </div>
            <IconButton :icon="Trash2" :label="t('common.delete')" :danger="true" @click="confirmDeletePayment(p.id)" />
          </div>
        </div>
        <div class="flex justify-end pt-3 border-t border-border">
          <AppButton variant="secondary" @click="historyModalOpen = false">
            {{ t('common.close') }}
          </AppButton>
        </div>
      </div>
    </AppModal>
  </div>
</template>
