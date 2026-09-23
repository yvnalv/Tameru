<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import {
  Plus,
  Repeat,
  AlertTriangle,
  Calendar,
  CheckCircle2,
  Clock,
  Pencil,
  Trash2,
  DollarSign,
  Zap,
} from 'lucide-vue-next';
import {
  listRecurringBills,
  getRecurringSummary,
  createRecurringBill,
  updateRecurringBill,
  deleteRecurringBill,
  payRecurringBill,
} from '@/lib/recurring';
import { listAccounts } from '@/lib/accounts';
import { listCategories } from '@/lib/categories';
import type {
  RecurringBill,
  RecurringBillsSummary,
  Account,
  Category,
  CreateRecurringBillInput,
  UpdateRecurringBillInput,
  PayRecurringBillInput,
} from '@/types/api';
import { errorMessage } from '@/lib/errorMessage';
import { formatRowDate } from '@/lib/format';
import { useToastStore } from '@/stores/toast';
import { useConfirmStore } from '@/stores/confirm';
import AppCard from '@/components/ui/AppCard.vue';
import AppButton from '@/components/ui/AppButton.vue';
import AppModal from '@/components/ui/AppModal.vue';
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
const bills = ref<RecurringBill[]>([]);
const summary = ref<RecurringBillsSummary | null>(null);
const accounts = ref<Account[]>([]);
const categories = ref<Category[]>([]);
const activeTab = ref<'due' | 'active' | 'paid' | 'all'>('due');

// Modals
const billModalOpen = ref(false);
const editingBill = ref<RecurringBill | null>(null);
const payModalOpen = ref(false);
const payingBill = ref<RecurringBill | null>(null);

// Form state: Bill
const billForm = ref({
  title: '',
  amount: 0,
  billingCycle: 'Monthly',
  dueDay: 1,
  accountId: '',
  categoryId: '',
  autoDebit: false,
  remindDaysBefore: 3,
  notes: '',
});

// Form state: Pay
const payForm = ref({
  date: new Date().toISOString().split('T')[0],
  amount: 0,
  accountId: '',
  categoryId: '',
  notes: '',
});

const filteredBills = computed(() => {
  if (activeTab.value === 'due') {
    return bills.value.filter((b) => (b.isOverdue || b.isDueSoon) && !b.isPaidThisCycle);
  }
  if (activeTab.value === 'active') {
    return bills.value.filter((b) => b.isActive);
  }
  if (activeTab.value === 'paid') {
    return bills.value.filter((b) => b.isPaidThisCycle);
  }
  return bills.value;
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
    const [bList, sData, aList, cList] = await Promise.all([
      listRecurringBills(),
      getRecurringSummary(),
      listAccounts(false),
      listCategories(),
    ]);
    bills.value = bList;
    summary.value = sData;
    accounts.value = aList;
    categories.value = cList;
  } catch (e) {
    toast.error(errorMessage(t, te, e));
  } finally {
    loading.value = false;
  }
}

function openCreateBill() {
  editingBill.value = null;
  billForm.value = {
    title: '',
    amount: 0,
    billingCycle: 'Monthly',
    dueDay: 1,
    accountId: accounts.value.length > 0 ? accounts.value[0].id : '',
    categoryId: '',
    autoDebit: false,
    remindDaysBefore: 3,
    notes: '',
  };
  billModalOpen.value = true;
}

function openEditBill(b: RecurringBill) {
  editingBill.value = b;
  billForm.value = {
    title: b.title,
    amount: b.amount,
    billingCycle: b.billingCycle,
    dueDay: b.dueDay,
    accountId: b.accountId,
    categoryId: b.categoryId || '',
    autoDebit: b.autoDebit,
    remindDaysBefore: b.remindDaysBefore,
    notes: b.notes || '',
  };
  billModalOpen.value = true;
}

async function saveBill() {
  if (!billForm.value.title.trim()) {
    toast.error(t('recurring.validationTitleRequired'));
    return;
  }
  if (billForm.value.amount <= 0) {
    toast.error(t('recurring.validationAmountPositive'));
    return;
  }
  if (!billForm.value.accountId) {
    toast.error(t('recurring.validationAccountRequired'));
    return;
  }

  try {
    if (editingBill.value) {
      const input: UpdateRecurringBillInput = {
        title: billForm.value.title,
        amount: billForm.value.amount,
        billingCycle: billForm.value.billingCycle,
        dueDay: billForm.value.dueDay,
        accountId: billForm.value.accountId,
        categoryId: billForm.value.categoryId || null,
        autoDebit: billForm.value.autoDebit,
        remindDaysBefore: billForm.value.remindDaysBefore,
        notes: billForm.value.notes || null,
      };
      await updateRecurringBill(editingBill.value.id, input);
      toast.success(t('recurring.savedSuccess'));
    } else {
      const input: CreateRecurringBillInput = {
        title: billForm.value.title,
        amount: billForm.value.amount,
        billingCycle: billForm.value.billingCycle,
        dueDay: billForm.value.dueDay,
        accountId: billForm.value.accountId,
        categoryId: billForm.value.categoryId || null,
        autoDebit: billForm.value.autoDebit,
        remindDaysBefore: billForm.value.remindDaysBefore,
        notes: billForm.value.notes || null,
      };
      await createRecurringBill(input);
      toast.success(t('recurring.createdSuccess'));
    }
    billModalOpen.value = false;
    await loadData();
  } catch (e) {
    toast.error(errorMessage(t, te, e));
  }
}

async function confirmDeleteBill(b: RecurringBill) {
  const ok = await confirm.ask({
    title: t('recurring.deleteTitle'),
    message: t('recurring.deleteConfirm', { title: b.title }),
    confirmLabel: t('common.delete'),
    danger: true,
  });
  if (!ok) return;

  try {
    await deleteRecurringBill(b.id);
    toast.success(t('recurring.deletedSuccess'));
    await loadData();
  } catch (e) {
    toast.error(errorMessage(t, te, e));
  }
}

function openPayModal(b: RecurringBill) {
  payingBill.value = b;
  payForm.value = {
    date: new Date().toISOString().split('T')[0],
    amount: b.amount,
    accountId: b.accountId,
    categoryId: b.categoryId || '',
    notes: `Recurring payment: ${b.title}`,
  };
  payModalOpen.value = true;
}

async function submitPayment() {
  if (!payingBill.value) return;

  try {
    const input: PayRecurringBillInput = {
      date: payForm.value.date,
      amount: payForm.value.amount,
      accountId: payForm.value.accountId,
      categoryId: payForm.value.categoryId || null,
      notes: payForm.value.notes || undefined,
    };

    await payRecurringBill(payingBill.value.id, input);
    toast.success(t('recurring.loggedSuccess', { title: payingBill.value.title }));
    payModalOpen.value = false;
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
          {{ t('recurring.title') }}
        </h1>
        <p class="mt-1 text-sm text-text-muted">
          {{ t('recurring.subtitle') }}
        </p>
      </div>
      <div class="flex items-center gap-3">
        <AppButton variant="primary" @click="openCreateBill">
          <Plus class="h-4 w-4 mr-2" />
          {{ t('recurring.addBill') }}
        </AppButton>
      </div>
    </div>

    <!-- Metric Strip -->
    <div v-if="summary" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <AppCard class="p-5 flex flex-col justify-between">
        <div class="flex items-center justify-between text-text-muted">
          <span class="text-xs font-semibold uppercase tracking-wider">{{ t('recurring.monthlyCommitment') }}</span>
          <Calendar class="h-4 w-4 text-accent" />
        </div>
        <div class="mt-3">
          <span class="text-2xl font-bold font-mono tracking-tight text-text">
            <Money :value="summary.totalMonthlyCommitment" currency="IDR" />
          </span>
          <p class="text-xs text-text-muted mt-1">
            {{ summary.totalActiveBills }} {{ t('recurring.activeSubscriptions') }}
          </p>
        </div>
      </AppCard>

      <AppCard class="p-5 flex flex-col justify-between" :class="{ 'border-danger/40': summary.overdueCount > 0 }">
        <div class="flex items-center justify-between text-text-muted">
          <span class="text-xs font-semibold uppercase tracking-wider">{{ t('recurring.overdue') }}</span>
          <AlertTriangle class="h-4 w-4" :class="summary.overdueCount > 0 ? 'text-danger' : 'text-text-muted'" />
        </div>
        <div class="mt-3">
          <span
            class="text-2xl font-bold font-mono tracking-tight"
            :class="summary.overdueCount > 0 ? 'text-danger' : 'text-text'"
          >
            <Money :value="summary.overdueAmount" currency="IDR" />
          </span>
          <p class="text-xs mt-1" :class="summary.overdueCount > 0 ? 'text-danger' : 'text-text-muted'">
            {{ summary.overdueCount }} {{ t('recurring.billsPastDue') }}
          </p>
        </div>
      </AppCard>

      <AppCard class="p-5 flex flex-col justify-between" :class="{ 'border-warning/40': summary.dueSoonCount > 0 }">
        <div class="flex items-center justify-between text-text-muted">
          <span class="text-xs font-semibold uppercase tracking-wider">{{ t('recurring.dueSoon') }}</span>
          <Clock class="h-4 w-4 text-warning" />
        </div>
        <div class="mt-3">
          <span class="text-2xl font-bold font-mono tracking-tight text-warning">
            <Money :value="summary.dueSoonAmount" currency="IDR" />
          </span>
          <p class="text-xs text-text-muted mt-1">
            {{ summary.dueSoonCount }} {{ t('recurring.dueInSevenDays') }}
          </p>
        </div>
      </AppCard>

      <AppCard class="p-5 flex flex-col justify-between">
        <div class="flex items-center justify-between text-text-muted">
          <span class="text-xs font-semibold uppercase tracking-wider">{{ t('recurring.autoDebit') }}</span>
          <Zap class="h-4 w-4 text-accent" />
        </div>
        <div class="mt-3">
          <span class="text-2xl font-bold font-mono tracking-tight text-text">
            {{ bills.filter(b => b.autoDebit && b.isActive).length }}
          </span>
          <p class="text-xs text-text-muted mt-1">
            {{ t('recurring.automatedDeductions') }}
          </p>
        </div>
      </AppCard>
    </div>

    <!-- Filter Tabs -->
    <div class="flex items-center gap-2 border-b border-border pb-3 overflow-x-auto">
      <button
        v-for="tab in [
          { id: 'due', label: `${t('recurring.tabDueSoon')} (${(summary?.dueSoonCount || 0) + (summary?.overdueCount || 0)})` },
          { id: 'active', label: `${t('recurring.tabActive')} (${summary?.totalActiveBills || 0})` },
          { id: 'paid', label: t('recurring.tabPaidThisMonth') },
          { id: 'all', label: t('recurring.tabAll') }
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
    <div v-else-if="filteredBills.length === 0" class="text-center py-16">
      <Repeat class="mx-auto h-12 w-12 text-text-muted/40 mb-3" />
      <p class="text-text font-medium">{{ t('recurring.noBillsFound') }}</p>
      <p class="text-xs text-text-muted mt-1">{{ t('recurring.noBillsSubtitle') }}</p>
      <AppButton variant="secondary" class="mt-4" @click="openCreateBill">
        <Plus class="h-4 w-4 mr-2" />
        {{ t('recurring.addBill') }}
      </AppButton>
    </div>

    <!-- Bills Grid -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <AppCard
        v-for="b in filteredBills"
        :key="b.id"
        class="p-5 flex flex-col justify-between hover:border-accent/40 transition-colors"
        :class="{
          'border-danger/40': b.isOverdue,
          'border-warning/30': b.isDueSoon && !b.isOverdue
        }"
      >
        <div>
          <!-- Top row -->
          <div class="flex items-start justify-between gap-3">
            <div>
              <div class="flex items-center gap-2">
                <h3 class="font-bold text-text text-base">{{ b.title }}</h3>
                <span
                  v-if="b.autoDebit"
                  class="text-[10px] px-1.5 py-0.5 rounded bg-accent/15 text-accent font-mono uppercase font-semibold flex items-center gap-1"
                >
                  <Zap class="h-2.5 w-2.5" /> Auto
                </span>
              </div>
              <p class="text-xs text-text-muted mt-0.5">
                {{ b.accountName || t('recurring.unassignedAccount') }}
                <span v-if="b.dueDay" class="font-mono ml-2">· {{ t('debts.dueDayLabel', { day: b.dueDay }) }}</span>
              </p>
            </div>

            <!-- Due Status Badge -->
            <div>
              <span
                v-if="b.isPaidThisCycle"
                class="text-[11px] px-2.5 py-1 rounded-full font-semibold bg-accent/15 text-accent flex items-center gap-1"
              >
                <CheckCircle2 class="h-3 w-3" /> {{ t('recurring.statusPaid') }}
              </span>
              <span
                v-else-if="b.isOverdue"
                class="text-[11px] px-2.5 py-1 rounded-full font-semibold bg-danger/15 text-danger flex items-center gap-1"
              >
                <AlertTriangle class="h-3 w-3" /> {{ t('recurring.statusOverdue') }}
              </span>
              <span
                v-else-if="b.isDueSoon"
                class="text-[11px] px-2.5 py-1 rounded-full font-semibold bg-warning/15 text-warning flex items-center gap-1"
              >
                <Clock class="h-3 w-3" /> {{ t('recurring.statusDueSoon', { days: b.daysUntilDue }) }}
              </span>
              <span
                v-else
                class="text-[11px] px-2.5 py-1 rounded-full font-medium bg-surface-elevated text-text-muted border border-border"
              >
                {{ formatRowDate(b.nextDueDate) }}
              </span>
            </div>
          </div>

          <!-- Nominal & Cycle -->
          <div class="mt-4 flex items-baseline justify-between">
            <div>
              <span class="text-2xl font-mono font-bold text-text">
                <Money :value="b.amount" :currency="b.currencyCode" />
              </span>
              <span class="text-xs text-text-muted ml-1.5 font-medium">/ {{ b.billingCycle.toLowerCase() }}</span>
            </div>
          </div>

          <!-- Dates & Category Strip -->
          <div class="mt-4 pt-3 border-t border-border flex items-center justify-between text-xs text-text-muted">
            <div>
              <span>{{ t('recurring.nextDue') }}: </span>
              <span class="font-mono text-text font-medium">{{ formatRowDate(b.nextDueDate) }}</span>
            </div>
            <div v-if="b.lastPaidDate">
              <span>{{ t('recurring.lastPaid') }}: </span>
              <span class="font-mono text-text-muted">{{ formatRowDate(b.lastPaidDate) }}</span>
            </div>
          </div>

          <p v-if="b.notes" class="mt-3 text-xs text-text-muted/80 italic line-clamp-1">
            "{{ b.notes }}"
          </p>
        </div>

        <!-- Footer Actions -->
        <div class="mt-5 pt-3 border-t border-border flex items-center justify-between gap-2">
          <!-- 1-Click Pay Action -->
          <div>
            <AppButton
              v-if="!b.isPaidThisCycle"
              variant="primary"
              @click="openPayModal(b)"
            >
              <DollarSign class="h-3.5 w-3.5 mr-1" />
              {{ t('recurring.logPayment') }}
            </AppButton>
            <span v-else class="text-xs text-text-muted italic flex items-center gap-1">
              <CheckCircle2 class="h-3.5 w-3.5 text-accent" /> {{ t('recurring.clearedForPeriod') }}
            </span>
          </div>

          <div class="flex items-center gap-1">
            <IconButton :icon="Pencil" :label="t('common.edit')" @click="openEditBill(b)" />
            <IconButton :icon="Trash2" :label="t('common.delete')" :danger="true" @click="confirmDeleteBill(b)" />
          </div>
        </div>
      </AppCard>
    </div>

    <!-- Add / Edit Bill Modal -->
    <AppModal
      v-if="billModalOpen"
      :title="editingBill ? t('recurring.editBill') : t('recurring.addBill')"
      @close="billModalOpen = false"
    >
      <form class="space-y-4" @submit.prevent="saveBill">
        <FormField :label="t('recurring.formTitle')" required>
          <AppInput v-model="billForm.title" :placeholder="t('recurring.placeholderTitle')" />
        </FormField>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField :label="t('recurring.formAmount')" required>
            <input
              v-model.number="billForm.amount"
              type="number"
              min="1"
              step="1000"
              class="h-10 w-full min-w-0 rounded-control border border-border-strong bg-surface px-3 text-sm text-text placeholder:text-text-muted focus:border-accent"
            />
          </FormField>
          <FormField :label="t('recurring.formBillingCycle')" required>
            <AppSelect
              v-model="billForm.billingCycle"
              :options="[
                { value: 'Monthly', label: t('recurring.cycleMonthly') },
                { value: 'Quarterly', label: t('recurring.cycleQuarterly') },
                { value: 'Yearly', label: t('recurring.cycleYearly') },
                { value: 'Weekly', label: t('recurring.cycleWeekly') }
              ]"
            />
          </FormField>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField :label="t('recurring.formAccount')" required>
            <AppSelect v-model="billForm.accountId" :options="accountOptions" />
          </FormField>
          <FormField :label="t('recurring.formCategory')">
            <AppSelect v-model="billForm.categoryId" :options="categoryOptions" />
          </FormField>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField :label="t('recurring.formDueDay')" required>
            <input
              v-model.number="billForm.dueDay"
              type="number"
              min="1"
              max="31"
              class="h-10 w-full min-w-0 rounded-control border border-border-strong bg-surface px-3 text-sm text-text placeholder:text-text-muted focus:border-accent"
            />
          </FormField>
          <FormField :label="t('recurring.formRemindDays')">
            <input
              v-model.number="billForm.remindDaysBefore"
              type="number"
              min="0"
              max="30"
              class="h-10 w-full min-w-0 rounded-control border border-border-strong bg-surface px-3 text-sm text-text placeholder:text-text-muted focus:border-accent"
            />
          </FormField>
        </div>

        <div class="pt-2">
          <label class="flex items-center gap-2 cursor-pointer text-sm font-medium text-text">
            <input
              v-model="billForm.autoDebit"
              type="checkbox"
              class="h-4 w-4 rounded border-border text-accent focus:ring-accent"
            />
            {{ t('recurring.formAutoDebit') }}
          </label>
        </div>

        <FormField :label="t('recurring.formNotes')">
          <AppInput v-model="billForm.notes" :placeholder="t('recurring.placeholderNotes')" />
        </FormField>

        <div class="flex justify-end gap-3 pt-4 border-t border-border">
          <AppButton variant="secondary" type="button" @click="billModalOpen = false">
            {{ t('common.cancel') }}
          </AppButton>
          <AppButton variant="primary" type="submit">
            {{ t('common.save') }}
          </AppButton>
        </div>
      </form>
    </AppModal>

    <!-- 1-Click Pay Confirm Modal -->
    <AppModal
      v-if="payModalOpen"
      :title="t('recurring.payConfirmTitle', { title: payingBill?.title || '' })"
      @close="payModalOpen = false"
    >
      <form class="space-y-4" @submit.prevent="submitPayment">
        <p class="text-sm text-text-muted">
          {{ t('recurring.payConfirmNotice') }}
        </p>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField :label="t('recurring.paymentDate')" required>
            <AppInput v-model="payForm.date" type="date" />
          </FormField>
          <FormField :label="t('recurring.formAmount')" required>
            <input
              v-model.number="payForm.amount"
              type="number"
              min="1"
              step="1000"
              class="h-10 w-full min-w-0 rounded-control border border-border-strong bg-surface px-3 text-sm text-text placeholder:text-text-muted focus:border-accent"
            />
          </FormField>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField :label="t('recurring.formAccount')" required>
            <AppSelect v-model="payForm.accountId" :options="accountOptions" />
          </FormField>
          <FormField :label="t('recurring.formCategory')">
            <AppSelect v-model="payForm.categoryId" :options="categoryOptions" />
          </FormField>
        </div>

        <FormField :label="t('recurring.formNotes')">
          <AppInput v-model="payForm.notes" />
        </FormField>

        <div class="flex justify-end gap-3 pt-4 border-t border-border">
          <AppButton variant="secondary" type="button" @click="payModalOpen = false">
            {{ t('common.cancel') }}
          </AppButton>
          <AppButton variant="primary" type="submit">
            {{ t('recurring.confirmAndLog') }}
          </AppButton>
        </div>
      </form>
    </AppModal>
  </div>
</template>
