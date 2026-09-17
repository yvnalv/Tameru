<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import {
  Calendar,
  Zap,
  Palette,
  User as UserIcon,
  Database,
  Check,
  Save,
  Download,
  LogOut,
  Info,
  Shield,
  Sparkles,
  ArrowRight,
  Sun,
  Moon,
  Plus,
  Trash2,
  Edit2,
  Copy,
  RefreshCw,
  Play,
  CheckCircle2,
  Eye,
  EyeOff,
  Code2,
} from 'lucide-vue-next';
import { useAuthStore } from '@/stores/auth';
import { useUiStore } from '@/stores/ui';
import { useThemeStore } from '@/stores/theme';
import { useToastStore } from '@/stores/toast';
import { useConfirmStore } from '@/stores/confirm';
import { regenerateApiToken } from '@/lib/auth';
import { listRules, createRule, updateRule, deleteRule, ingestTransaction } from '@/lib/rules';
import { listCategories } from '@/lib/categories';
import { listTransactions } from '@/lib/transactions';
import { toCsv, downloadCsv } from '@/lib/csv';
import type { Transaction, RuleDto, RuleMatchField, RuleMatchOperator, Category, IngestResultDto } from '@/types/api';
import AppCard from '@/components/ui/AppCard.vue';
import AppButton from '@/components/ui/AppButton.vue';
import AppToggle from '@/components/ui/AppToggle.vue';
import AppModal from '@/components/ui/AppModal.vue';
import AppInput from '@/components/ui/AppInput.vue';
import AppSelect, { type SelectOption } from '@/components/ui/AppSelect.vue';
import FormField from '@/components/ui/FormField.vue';
import AvatarChip from '@/components/ui/AvatarChip.vue';

const { t, locale } = useI18n();
const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const ui = useUiStore();
const themeStore = useThemeStore();
const toast = useToastStore();
const confirm = useConfirmStore();

type TabKey = 'financial-cycle' | 'rules' | 'appearance' | 'profile' | 'data';
const activeTab = ref<TabKey>('financial-cycle');

onMounted(() => {
  const queryTab = route.query.tab as string;
  if (queryTab && ['financial-cycle', 'rules', 'appearance', 'profile', 'data'].includes(queryTab)) {
    activeTab.value = queryTab as TabKey;
  }
  if (activeTab.value === 'rules') {
    void loadRulesAndCategories();
  }
});

function setTab(tab: TabKey): void {
  activeTab.value = tab;
  router.replace({ query: { ...route.query, tab } });
  if (tab === 'rules' && rules.value.length === 0) {
    void loadRulesAndCategories();
  }
}

// ----------------------------------------------------------------------------
// Tab 1: Financial Cycle
// ----------------------------------------------------------------------------
const initialCycleDay = computed(() => auth.user?.budgetCycleStartDay ?? 1);
const cycleStartDay = ref<number>(auth.user?.budgetCycleStartDay ?? 1);
const savingCycle = ref(false);

const isCycleChanged = computed(() => cycleStartDay.value !== initialCycleDay.value);

function setPresetDay(day: number): void {
  cycleStartDay.value = day;
}

// Computed date ranges for live preview
const cyclePreview = computed(() => {
  const day = Math.max(1, Math.min(28, Number(cycleStartDay.value) || 1));
  const now = new Date();

  // Determine current active cycle start & end
  let currentStartYear = now.getFullYear();
  let currentStartMonth = now.getMonth(); // 0-indexed

  if (day > 1 && now.getDate() < day) {
    // Current date is before payday, cycle began on payday of last month
    const prev = new Date(now.getFullYear(), now.getMonth() - 1, 1);
    currentStartYear = prev.getFullYear();
    currentStartMonth = prev.getMonth();
  }

  const startDate = new Date(currentStartYear, currentStartMonth, day);
  const nextMonth = new Date(currentStartYear, currentStartMonth + 1, day);
  const endDate = day === 1
    ? new Date(currentStartYear, currentStartMonth + 1, 0)
    : new Date(nextMonth.getTime() - 24 * 60 * 60 * 1000);

  const totalDays = Math.max(1, Math.round((endDate.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24)) + 1);

  // Days elapsed in current cycle
  const diffTime = now.getTime() - startDate.getTime();
  const elapsedDays = Math.max(0, Math.min(totalDays, Math.floor(diffTime / (1000 * 60 * 60 * 24)) + 1));
  const daysLeft = Math.max(0, totalDays - elapsedDays);
  const progressPct = Math.round((elapsedDays / totalDays) * 100);

  const startFormatted = startDate.toLocaleDateString(locale.value, { day: 'numeric', month: 'short', year: 'numeric' });
  const endFormatted = endDate.toLocaleDateString(locale.value, { day: 'numeric', month: 'short', year: 'numeric' });

  // Next cycle preview
  const nextStart = nextMonth;
  const nextEnd = day === 1
    ? new Date(currentStartYear, currentStartMonth + 2, 0)
    : new Date(new Date(currentStartYear, currentStartMonth + 2, day).getTime() - 24 * 60 * 60 * 1000);
  const nextStartFormatted = nextStart.toLocaleDateString(locale.value, { day: 'numeric', month: 'short', year: 'numeric' });
  const nextEndFormatted = nextEnd.toLocaleDateString(locale.value, { day: 'numeric', month: 'short', year: 'numeric' });

  return {
    day,
    startFormatted,
    endFormatted,
    totalDays,
    elapsedDays,
    daysLeft,
    progressPct,
    nextStartFormatted,
    nextEndFormatted,
  };
});

async function saveFinancialCycle(): Promise<void> {
  const day = Math.max(1, Math.min(28, Number(cycleStartDay.value) || 1));
  savingCycle.value = true;
  try {
    await auth.updateProfile({ budgetCycleStartDay: day });
    cycleStartDay.value = day;
    toast.success(t('settings.cycleSavedToast'));
  } catch {
    toast.error(t('settings.cycleSaveError'));
  } finally {
    savingCycle.value = false;
  }
}

// ----------------------------------------------------------------------------
// Tab 2: Automations & Rules
// ----------------------------------------------------------------------------
const rules = ref<RuleDto[]>([]);
const categories = ref<Category[]>([]);
const loadingRules = ref(false);
const apiToken = ref(auth.user?.apiToken || '');
const showToken = ref(false);
const regeneratingToken = ref(false);
const copiedToken = ref(false);
const copiedUrl = ref(false);

const webhookUrl = computed(() => {
  if (typeof window === 'undefined') return '/api/v1/ingest/transaction';
  return `${window.location.origin}/api/v1/ingest/transaction`;
});

async function loadRulesAndCategories(): Promise<void> {
  loadingRules.value = true;
  try {
    const [rList, cList] = await Promise.all([
      listRules(false),
      listCategories(),
    ]);
    rules.value = rList;
    categories.value = cList;
    if (!apiToken.value && auth.user?.apiToken) {
      apiToken.value = auth.user.apiToken;
    }
  } catch {
    toast.error(t('settings.rules.ruleSaveError'));
  } finally {
    loadingRules.value = false;
  }
}

async function handleRegenerateToken(): Promise<void> {
  const ok = await confirm.ask({
    title: t('settings.rules.tokenRegenerate'),
    message: t('settings.rules.tokenRegenerateConfirm'),
    confirmLabel: t('settings.rules.tokenRegenerate'),
    danger: true,
  });
  if (!ok) return;

  regeneratingToken.value = true;
  try {
    const res = await regenerateApiToken();
    apiToken.value = res.apiToken;
    if (auth.user) {
      auth.user.apiToken = res.apiToken;
    }
    toast.success(t('settings.rules.tokenRegeneratedToast'));
  } catch {
    toast.error(t('settings.rules.tokenRegenerateError'));
  } finally {
    regeneratingToken.value = false;
  }
}

function copyText(text: string, isUrl = false): void {
  if (!text) return;
  navigator.clipboard.writeText(text);
  if (isUrl) {
    copiedUrl.value = true;
    setTimeout(() => (copiedUrl.value = false), 2000);
  } else {
    copiedToken.value = true;
    setTimeout(() => (copiedToken.value = false), 2000);
  }
  toast.success(t('settings.rules.copied'));
}

// Sandbox Live Testing
const sandboxText = ref('kopi kenangan 35k gopay');
const testingIngest = ref(false);
const sandboxResult = ref<IngestResultDto | null>(null);

function setSandboxPreset(val: string): void {
  sandboxText.value = val;
}

async function testIngest(): Promise<void> {
  if (!sandboxText.value.trim()) return;
  testingIngest.value = true;
  sandboxResult.value = null;
  try {
    const res = await ingestTransaction({ text: sandboxText.value.trim() });
    sandboxResult.value = res;
    toast.success(t('settings.rules.sandboxSuccess'));
  } catch (e: any) {
    toast.error(e?.message || t('errors.generic'));
  } finally {
    testingIngest.value = false;
  }
}

// Rule Modal & CRUD
const showRuleModal = ref(false);
const editingRuleId = ref<string | null>(null);
const ruleName = ref('');
const rulePattern = ref('');
const ruleMatchField = ref<RuleMatchField>('Payee');
const ruleMatchOperator = ref<RuleMatchOperator>('Contains');
const ruleTargetCategoryId = ref<string>('');
const rulePriority = ref<number>(100);
const ruleIsActive = ref(true);
const savingRule = ref(false);

const categoryOptions = computed<SelectOption[]>(() => {
  return categories.value.map((c) => ({
    value: c.id,
    label: `${c.name} (${c.flow})`,
  }));
});

const matchFieldOptions = computed<SelectOption[]>(() => [
  { value: 'Payee', label: t('settings.rules.fields.Payee') },
  { value: 'Description', label: t('settings.rules.fields.Description') },
]);

const matchOperatorOptions = computed<SelectOption[]>(() => [
  { value: 'Contains', label: t('settings.rules.operators.Contains') },
  { value: 'Equals', label: t('settings.rules.operators.Equals') },
  { value: 'StartsWith', label: t('settings.rules.operators.StartsWith') },
  { value: 'Regex', label: t('settings.rules.operators.Regex') },
]);

function openAddRuleModal(): void {
  editingRuleId.value = null;
  ruleName.value = '';
  rulePattern.value = '';
  ruleMatchField.value = 'Payee';
  ruleMatchOperator.value = 'Contains';
  ruleTargetCategoryId.value = categories.value[0]?.id || '';
  rulePriority.value = 100;
  ruleIsActive.value = true;
  showRuleModal.value = true;
}

function openEditRuleModal(rule: RuleDto): void {
  editingRuleId.value = rule.id;
  ruleName.value = rule.name;
  rulePattern.value = rule.pattern;
  ruleMatchField.value = rule.matchField;
  ruleMatchOperator.value = rule.matchOperator;
  ruleTargetCategoryId.value = rule.targetCategoryId || '';
  rulePriority.value = rule.priority;
  ruleIsActive.value = rule.isActive;
  showRuleModal.value = true;
}

async function saveRule(): Promise<void> {
  if (!ruleName.value.trim() || !rulePattern.value.trim()) {
    toast.error(t('errors.validation_error'));
    return;
  }
  savingRule.value = true;
  try {
    if (editingRuleId.value) {
      const updated = await updateRule(editingRuleId.value, {
        name: ruleName.value.trim(),
        pattern: rulePattern.value.trim(),
        matchField: ruleMatchField.value,
        matchOperator: ruleMatchOperator.value,
        targetCategoryId: ruleTargetCategoryId.value || null,
        targetBudgetCategoryId: null,
        targetSubCategoryId: null,
        targetStatus: null,
        priority: Number(rulePriority.value) || 100,
        isActive: ruleIsActive.value,
      });
      const idx = rules.value.findIndex((r) => r.id === editingRuleId.value);
      if (idx !== -1) rules.value[idx] = updated;
    } else {
      const created = await createRule({
        name: ruleName.value.trim(),
        pattern: rulePattern.value.trim(),
        matchField: ruleMatchField.value,
        matchOperator: ruleMatchOperator.value,
        targetCategoryId: ruleTargetCategoryId.value || null,
        targetBudgetCategoryId: null,
        targetSubCategoryId: null,
        targetStatus: null,
        priority: Number(rulePriority.value) || 100,
        isActive: ruleIsActive.value,
      });
      rules.value.unshift(created);
    }
    toast.success(t('settings.rules.ruleSavedToast'));
    showRuleModal.value = false;
  } catch {
    toast.error(t('settings.rules.ruleSaveError'));
  } finally {
    savingRule.value = false;
  }
}

async function handleDeleteRule(rule: RuleDto): Promise<void> {
  const ok = await confirm.ask({
    title: t('common.delete'),
    message: t('settings.rules.deleteRuleConfirm', { name: rule.name }),
    confirmLabel: t('common.delete'),
    danger: true,
  });
  if (!ok) return;

  try {
    await deleteRule(rule.id);
    rules.value = rules.value.filter((r) => r.id !== rule.id);
    toast.success(t('settings.rules.ruleDeletedToast'));
  } catch {
    toast.error(t('errors.generic'));
  }
}

async function toggleRuleActive(rule: RuleDto): Promise<void> {
  try {
    const updated = await updateRule(rule.id, {
      name: rule.name,
      pattern: rule.pattern,
      matchField: rule.matchField,
      matchOperator: rule.matchOperator,
      targetCategoryId: rule.targetCategoryId,
      targetBudgetCategoryId: rule.targetBudgetCategoryId,
      targetSubCategoryId: rule.targetSubCategoryId,
      targetStatus: rule.targetStatus,
      priority: rule.priority,
      isActive: !rule.isActive,
    });
    const idx = rules.value.findIndex((r) => r.id === rule.id);
    if (idx !== -1) rules.value[idx] = updated;
  } catch {
    toast.error(t('errors.generic'));
  }
}

function getCategoryName(id: string | null): string {
  if (!id) return '-';
  return categories.value.find((c) => c.id === id)?.name ?? '-';
}

// ----------------------------------------------------------------------------
// Tab 3: Appearance & Display
// ----------------------------------------------------------------------------
function selectTheme(mode: 'light' | 'dark'): void {
  themeStore.setTheme(mode);
}

// ----------------------------------------------------------------------------
// Tab 3: Account & Profile
// ----------------------------------------------------------------------------
const displayNameInput = ref(auth.user?.displayName || '');
const savingProfile = ref(false);

const isProfileChanged = computed(
  () => displayNameInput.value.trim() !== (auth.user?.displayName || '').trim(),
);

async function saveProfile(): Promise<void> {
  if (!displayNameInput.value.trim()) return;
  savingProfile.value = true;
  try {
    await auth.updateProfile({ displayName: displayNameInput.value.trim() });
    toast.success(t('settings.profileSavedToast'));
  } catch {
    toast.error(t('settings.profileSaveError'));
  } finally {
    savingProfile.value = false;
  }
}

async function handleSignOut(): Promise<void> {
  const ok = await confirm.ask({
    title: t('common.signOut'),
    message: t('settings.signOutConfirmMessage'),
    confirmLabel: t('common.signOut'),
    danger: true,
  });
  if (ok) {
    await auth.logout();
    router.push({ name: 'login' });
  }
}

// ----------------------------------------------------------------------------
// Tab 4: Data & Backup
// ----------------------------------------------------------------------------
const exportingCsv = ref(false);

async function exportTransactionsCsv(): Promise<void> {
  exportingCsv.value = true;
  try {
    const res = await listTransactions({ page: 1, pageSize: 10000 });
    const rows = res.items;
    const csv = toCsv<Transaction>(rows, [
      { header: 'Date', value: (t) => t.date },
      { header: 'Title', value: (t) => t.title },
      { header: 'Type', value: (t) => t.type },
      { header: 'Amount', value: (t) => t.amount },
      { header: 'Currency', value: (t) => t.currencyCode },
      { header: 'Status', value: (t) => t.status },
      { header: 'Description', value: (t) => t.description ?? '' },
    ]);
    const dateStr = new Date().toISOString().slice(0, 10);
    downloadCsv(`tameru-transactions-backup-${dateStr}.csv`, csv);
    toast.success(t('settings.exportSuccessToast'));
  } catch {
    toast.error(t('settings.exportErrorToast'));
  } finally {
    exportingCsv.value = false;
  }
}
</script>

<template>
  <div class="space-y-4">
    <!-- Action Bar & Categorized Segmented Tabs (matches Design System) -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
      <div class="inline-flex rounded-xl bg-surface border border-border p-1 shadow-sm overflow-x-auto max-w-full">
        <button
          type="button"
          class="flex items-center gap-2 rounded-lg px-3.5 py-1.5 text-xs font-semibold transition-all whitespace-nowrap"
          :class="
            activeTab === 'financial-cycle'
              ? 'bg-accent text-accent-contrast shadow-sm font-bold'
              : 'text-text-muted hover:text-text'
          "
          @click="setTab('financial-cycle')"
        >
          <Calendar :size="15" />
          {{ t('settings.tabs.cycle') }}
        </button>

        <button
          type="button"
          class="flex items-center gap-2 rounded-lg px-3.5 py-1.5 text-xs font-semibold transition-all whitespace-nowrap"
          :class="
            activeTab === 'rules'
              ? 'bg-accent text-accent-contrast shadow-sm font-bold'
              : 'text-text-muted hover:text-text'
          "
          @click="setTab('rules')"
        >
          <Zap :size="15" />
          {{ t('settings.tabs.rules') }}
        </button>

        <button
          type="button"
          class="flex items-center gap-2 rounded-lg px-3.5 py-1.5 text-xs font-semibold transition-all whitespace-nowrap"
          :class="
            activeTab === 'appearance'
              ? 'bg-accent text-accent-contrast shadow-sm font-bold'
              : 'text-text-muted hover:text-text'
          "
          @click="setTab('appearance')"
        >
          <Palette :size="15" />
          {{ t('settings.tabs.appearance') }}
        </button>

        <button
          type="button"
          class="flex items-center gap-2 rounded-lg px-3.5 py-1.5 text-xs font-semibold transition-all whitespace-nowrap"
          :class="
            activeTab === 'profile'
              ? 'bg-accent text-accent-contrast shadow-sm font-bold'
              : 'text-text-muted hover:text-text'
          "
          @click="setTab('profile')"
        >
          <UserIcon :size="15" />
          {{ t('settings.tabs.profile') }}
        </button>

        <button
          type="button"
          class="flex items-center gap-2 rounded-lg px-3.5 py-1.5 text-xs font-semibold transition-all whitespace-nowrap"
          :class="
            activeTab === 'data'
              ? 'bg-accent text-accent-contrast shadow-sm font-bold'
              : 'text-text-muted hover:text-text'
          "
          @click="setTab('data')"
        >
          <Database :size="15" />
          {{ t('settings.tabs.data') }}
        </button>
      </div>

      <!-- Quick Status Badges -->
      <div class="flex items-center gap-2 shrink-0">
        <span class="inline-flex items-center gap-1.5 rounded-full bg-accent-soft px-3 py-1 text-xs font-semibold text-accent">
          <Sparkles :size="13" />
          {{ t('settings.cycleActiveBadge', { day: auth.user?.budgetCycleStartDay ?? 1 }) }}
        </span>
        <span class="inline-flex items-center rounded-full bg-surface-2 px-2.5 py-1 text-xs font-medium text-text-muted border border-border">
          IDR (Rp)
        </span>
      </div>
    </div>

    <!-- ===================================================================== -->
    <!-- TAB 1: Financial Cycle & Starting Day                                  -->
    <!-- ===================================================================== -->
    <div v-if="activeTab === 'financial-cycle'" class="space-y-4">
      <!-- Main Starting Day Card -->
      <AppCard>
        <div class="flex items-start justify-between gap-4">
          <div>
            <h2 class="text-base font-bold text-text">
              {{ t('settings.cycleHeading') }}
            </h2>
            <p class="text-xs text-text-muted mt-1 max-w-2xl">
              {{ t('settings.cycleDescription') }}
            </p>
          </div>
          <span class="rounded-control bg-accent-soft p-2 text-accent">
            <Calendar :size="20" />
          </span>
        </div>

        <!-- Explanatory Banner -->
        <div class="mt-4 flex items-start gap-3 rounded-control border border-accent/25 bg-accent-soft/40 p-3.5 text-xs text-text">
          <Info :size="18" class="text-accent shrink-0 mt-0.5" />
          <div class="space-y-1">
            <p class="font-semibold text-accent">
              {{ t('settings.cycleHowItWorksTitle') }}
            </p>
            <p class="text-text-muted leading-relaxed">
              {{ t('settings.cycleHowItWorksBody', { day: cyclePreview.day }) }}
            </p>
          </div>
        </div>

        <!-- Starting Day Input & Quick Presets -->
        <div class="mt-6 grid grid-cols-1 md:grid-cols-2 gap-6 items-end">
          <div>
            <FormField :label="t('settings.startingDayLabel')" :hint="t('settings.startingDayHint')">
              <div class="flex items-center gap-2 mt-1">
                <input
                  v-model.number="cycleStartDay"
                  type="number"
                  min="1"
                  max="28"
                  class="h-10 w-28 rounded-control border border-border bg-surface px-3 text-center text-base font-bold text-text shadow-xs focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent"
                />
                <span class="text-xs text-text-muted font-medium">
                  {{ t('settings.ofEveryMonth') }}
                </span>
              </div>
            </FormField>

            <!-- Quick Presets -->
            <div class="mt-3 flex items-center gap-2">
              <span class="text-[11px] text-text-muted font-medium">{{ t('settings.presets') }}:</span>
              <button
                type="button"
                class="rounded-control border px-2.5 py-1 text-xs font-semibold transition-all"
                :class="
                  cycleStartDay === 1
                    ? 'border-accent bg-accent text-accent-contrast shadow-xs'
                    : 'border-border bg-surface-2 text-text-muted hover:border-border-strong hover:text-text'
                "
                @click="setPresetDay(1)"
              >
                {{ t('settings.presetCalendarMonth') }}
              </button>
              <button
                type="button"
                class="rounded-control border px-2.5 py-1 text-xs font-semibold transition-all"
                :class="
                  cycleStartDay === 25
                    ? 'border-accent bg-accent text-accent-contrast shadow-xs'
                    : 'border-border bg-surface-2 text-text-muted hover:border-border-strong hover:text-text'
                "
                @click="setPresetDay(25)"
              >
                {{ t('settings.presetPayday25') }}
              </button>
            </div>
          </div>

          <!-- Save Button -->
          <div class="flex justify-start md:justify-end">
            <AppButton
              variant="primary"
              :loading="savingCycle"
              :disabled="!isCycleChanged && !savingCycle"
              class="w-full sm:w-auto"
              @click="saveFinancialCycle"
            >
              <Save :size="16" />
              {{ t('settings.saveCycleButton') }}
            </AppButton>
          </div>
        </div>

        <!-- Live Dynamic Cycle Preview Box -->
        <div class="mt-6 rounded-container border border-border bg-surface-2/60 p-4 space-y-4">
          <div class="flex items-center justify-between">
            <span class="text-xs font-bold uppercase tracking-wider text-text-muted">
              {{ t('settings.livePreviewTitle') }}
            </span>
            <span class="text-xs font-semibold text-accent">
              {{ t('settings.daysInCycle', { count: cyclePreview.totalDays }) }}
            </span>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <!-- Current Cycle Box -->
            <div class="rounded-control border border-border bg-surface p-3 space-y-1.5 shadow-xs">
              <span class="text-[11px] font-semibold text-text-muted uppercase">
                {{ t('settings.activeCurrentCycle') }}
              </span>
              <p class="text-sm font-bold text-text flex items-center gap-1.5">
                <span>{{ cyclePreview.startFormatted }}</span>
                <ArrowRight :size="14" class="text-text-muted shrink-0" />
                <span>{{ cyclePreview.endFormatted }}</span>
              </p>
              <div class="pt-1">
                <div class="flex items-center justify-between text-[11px] text-text-muted">
                  <span>{{ t('settings.cycleElapsed', { day: cyclePreview.elapsedDays, total: cyclePreview.totalDays }) }}</span>
                  <span class="font-semibold text-accent">{{ cyclePreview.daysLeft }} {{ t('settings.daysLeft') }}</span>
                </div>
                <div class="mt-1.5 h-1.5 w-full rounded-full bg-surface-2 overflow-hidden">
                  <div class="h-full bg-accent rounded-full transition-all duration-300" :style="{ width: `${cyclePreview.progressPct}%` }" />
                </div>
              </div>
            </div>

            <!-- Next Cycle Box -->
            <div class="rounded-control border border-border bg-surface p-3 space-y-1.5 shadow-xs">
              <span class="text-[11px] font-semibold text-text-muted uppercase">
                {{ t('settings.nextCycle') }}
              </span>
              <p class="text-sm font-bold text-text flex items-center gap-1.5">
                <span>{{ cyclePreview.nextStartFormatted }}</span>
                <ArrowRight :size="14" class="text-text-muted shrink-0" />
                <span>{{ cyclePreview.nextEndFormatted }}</span>
              </p>
              <p class="text-[11px] text-text-muted pt-1">
                {{ t('settings.nextCycleAutoSwitch', { day: cyclePreview.day }) }}
              </p>
            </div>
          </div>
        </div>
      </AppCard>

      <!-- Currency & Accounting Standards Card -->
      <AppCard>
        <h2 class="text-base font-bold text-text">
          {{ t('settings.currencyHeading') }}
        </h2>
        <p class="text-xs text-text-muted mt-1">
          {{ t('settings.currencyDescription') }}
        </p>

        <div class="mt-4 grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div class="rounded-control border border-border bg-surface-2 p-3">
            <span class="text-[11px] text-text-muted font-medium">{{ t('settings.functionalCurrency') }}</span>
            <p class="text-sm font-bold text-text mt-0.5">IDR - Indonesian Rupiah</p>
            <span class="text-xs text-text-muted">Rp (Rupiah)</span>
          </div>

          <div class="rounded-control border border-border bg-surface-2 p-3">
            <span class="text-[11px] text-text-muted font-medium">{{ t('settings.currencyFormat') }}</span>
            <p class="text-sm font-bold text-text mt-0.5">Rp 1.250.000,00</p>
            <span class="text-xs text-text-muted">id-ID locale</span>
          </div>

          <div class="rounded-control border border-border bg-surface-2 p-3">
            <span class="text-[11px] text-text-muted font-medium">{{ t('settings.roundingMode') }}</span>
            <p class="text-sm font-bold text-text mt-0.5">Two Decimals (IDR standard)</p>
            <span class="text-xs text-text-muted">Banker's Rounding</span>
          </div>
        </div>
      </AppCard>
    </div>

    <!-- ===================================================================== -->
    <!-- TAB 2: Automations & Rules                                             -->
    <!-- ===================================================================== -->
    <div v-else-if="activeTab === 'rules'" class="space-y-4">
      <!-- 1. Ingestion Webhook & Token API Card -->
      <AppCard>
        <div class="flex items-start justify-between gap-4">
          <div>
            <h2 class="text-base font-bold text-text">
              {{ t('settings.rules.webhookHeading') }}
            </h2>
            <p class="text-xs text-text-muted mt-1 max-w-2xl">
              {{ t('settings.rules.webhookDescription') }}
            </p>
          </div>
          <span class="rounded-control bg-accent-soft p-2 text-accent">
            <Zap :size="20" />
          </span>
        </div>

        <div class="mt-6 grid grid-cols-1 md:grid-cols-2 gap-6">
          <!-- Personal API Token -->
          <div class="space-y-2">
            <div class="flex items-center justify-between">
              <label class="text-xs font-semibold text-text">
                {{ t('settings.rules.personalApiToken') }}
              </label>
              <button
                type="button"
                :disabled="regeneratingToken"
                class="inline-flex items-center gap-1 text-[11px] font-semibold text-accent hover:underline disabled:opacity-50"
                @click="handleRegenerateToken"
              >
                <RefreshCw :size="12" :class="{ 'animate-spin': regeneratingToken }" />
                {{ t('settings.rules.tokenRegenerate') }}
              </button>
            </div>
            <div class="flex items-center gap-2">
              <div class="relative flex-1">
                <input
                  :type="showToken ? 'text' : 'password'"
                  :value="apiToken || 'tmr_xxxxxxxxxxxxxxxxxxxxxxxx'"
                  readonly
                  class="h-10 w-full rounded-control border border-border bg-surface-2 px-3 pr-10 font-mono text-xs text-text shadow-xs focus:outline-none"
                />
                <button
                  type="button"
                  class="absolute right-2.5 top-1/2 -translate-y-1/2 text-text-muted hover:text-text"
                  @click="showToken = !showToken"
                >
                  <EyeOff v-if="showToken" :size="15" />
                  <Eye v-else :size="15" />
                </button>
              </div>
              <AppButton
                variant="secondary"
                size="sm"
                class="h-10 shrink-0"
                @click="copyText(apiToken, false)"
              >
                <Check v-if="copiedToken" :size="14" class="text-positive" />
                <Copy v-else :size="14" />
                <span>{{ copiedToken ? t('settings.rules.copied') : t('settings.rules.copy') }}</span>
              </AppButton>
            </div>
            <p class="text-[11px] text-text-muted">
              Header: <code class="rounded bg-surface-2 px-1 py-0.5 font-mono text-accent">X-Tameru-Token: &lt;token&gt;</code>
            </p>
          </div>

          <!-- Webhook Endpoint URL -->
          <div class="space-y-2">
            <label class="text-xs font-semibold text-text">
              {{ t('settings.rules.webhookUrl') }}
            </label>
            <div class="flex items-center gap-2">
              <input
                type="text"
                :value="webhookUrl"
                readonly
                class="h-10 w-full rounded-control border border-border bg-surface-2 px-3 font-mono text-xs text-text shadow-xs focus:outline-none"
              />
              <AppButton
                variant="secondary"
                size="sm"
                class="h-10 shrink-0"
                @click="copyText(webhookUrl, true)"
              >
                <Check v-if="copiedUrl" :size="14" class="text-positive" />
                <Copy v-else :size="14" />
                <span>{{ copiedUrl ? t('settings.rules.copied') : t('settings.rules.copy') }}</span>
              </AppButton>
            </div>
            <p class="text-[11px] text-text-muted">
              Method: <span class="font-bold text-positive">POST</span> • Format: <code class="rounded bg-surface-2 px-1 py-0.5 font-mono text-accent">application/json</code>
            </p>
          </div>
        </div>

        <!-- Integration Guide accordion / banner -->
        <div class="mt-6 rounded-control border border-border bg-surface-2 p-4 text-xs space-y-3">
          <div class="flex items-center gap-2 font-bold text-text">
            <Code2 :size="16" class="text-accent" />
            <span>{{ t('settings.rules.howToUse') }}</span>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <!-- cURL snippet -->
            <div class="space-y-1.5">
              <span class="text-[11px] font-semibold text-text-muted">{{ t('settings.rules.curlGuide') }}</span>
              <pre class="rounded-control bg-surface border border-border p-2.5 font-mono text-[11px] text-text-muted overflow-x-auto select-all">curl -X POST "{{ webhookUrl }}" \
  -H "X-Tameru-Token: {{ apiToken ? (showToken ? apiToken : 'tmr_your_token_here') : 'tmr_your_token_here' }}" \
  -H "Content-Type: application/json" \
  -d '{"text": "kopi kenangan 35k gopay"}'</pre>
            </div>

            <!-- Telegram / iOS Shortcuts summary -->
            <div class="space-y-1.5">
              <span class="text-[11px] font-semibold text-text-muted">{{ t('settings.rules.telegramGuide') }}</span>
              <p class="text-[11px] text-text-muted leading-relaxed">
                {{ t('settings.rules.telegramGuideBody') }}
              </p>
              <div class="pt-1 flex flex-wrap gap-1.5">
                <span class="rounded bg-accent-soft px-2 py-0.5 font-mono text-[10px] text-accent font-semibold">kopi 35k gopay</span>
                <span class="rounded bg-accent-soft px-2 py-0.5 font-mono text-[10px] text-accent font-semibold">makan siang 45rb bca</span>
                <span class="rounded bg-accent-soft px-2 py-0.5 font-mono text-[10px] text-accent font-semibold">gaji 15jt bca</span>
              </div>
            </div>
          </div>
        </div>
      </AppCard>

      <!-- 2. Interactive Sandbox Tester Card -->
      <AppCard>
        <div class="flex items-start justify-between gap-4">
          <div>
            <h2 class="text-base font-bold text-text">
              {{ t('settings.rules.sandboxHeading') }}
            </h2>
            <p class="text-xs text-text-muted mt-1 max-w-2xl">
              {{ t('settings.rules.sandboxDescription') }}
            </p>
          </div>
          <span class="rounded-control bg-accent-soft p-2 text-accent">
            <Play :size="20" />
          </span>
        </div>

        <div class="mt-4 space-y-3">
          <!-- Text Input & Submit Button -->
          <div class="flex flex-col sm:flex-row items-stretch sm:items-center gap-2">
            <input
              v-model="sandboxText"
              type="text"
              :placeholder="t('settings.rules.sandboxPlaceholder')"
              class="h-10 flex-1 rounded-control border border-border bg-surface px-3 text-sm text-text placeholder:text-text-muted shadow-xs focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent"
              @keydown.enter="testIngest"
            />
            <AppButton
              variant="primary"
              :loading="testingIngest"
              class="h-10 shrink-0"
              @click="testIngest"
            >
              <Play :size="14" />
              <span>{{ t('settings.rules.sandboxSubmit') }}</span>
            </AppButton>
          </div>

          <!-- Quick presets chips -->
          <div class="flex items-center gap-2 flex-wrap">
            <span class="text-[11px] text-text-muted font-medium">{{ t('settings.presets') }}:</span>
            <button
              type="button"
              class="rounded-full border border-border bg-surface-2 px-2.5 py-0.5 text-xs text-text-muted hover:border-accent hover:text-accent transition-all font-mono"
              @click="setSandboxPreset('kopi kenangan 35k gopay')"
            >
              kopi kenangan 35k gopay
            </button>
            <button
              type="button"
              class="rounded-full border border-border bg-surface-2 px-2.5 py-0.5 text-xs text-text-muted hover:border-accent hover:text-accent transition-all font-mono"
              @click="setSandboxPreset('makan siang 45rb bca')"
            >
              makan siang 45rb bca
            </button>
            <button
              type="button"
              class="rounded-full border border-border bg-surface-2 px-2.5 py-0.5 text-xs text-text-muted hover:border-accent hover:text-accent transition-all font-mono"
              @click="setSandboxPreset('gaji bulanan 15jt bca')"
            >
              gaji bulanan 15jt bca
            </button>
            <button
              type="button"
              class="rounded-full border border-border bg-surface-2 px-2.5 py-0.5 text-xs text-text-muted hover:border-accent hover:text-accent transition-all font-mono"
              @click="setSandboxPreset('grab ride 25k')"
            >
              grab ride 25k
            </button>
          </div>

          <!-- Live Result Banner -->
          <div
            v-if="sandboxResult"
            class="mt-4 rounded-control border border-positive/30 bg-positive-soft/40 p-4 space-y-2 animate-in fade-in duration-200"
          >
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2 text-positive font-bold text-sm">
                <CheckCircle2 :size="18" />
                <span>{{ t('settings.rules.sandboxSuccess') }}</span>
              </div>
              <span
                v-if="sandboxResult.appliedRuleName"
                class="rounded-full bg-accent-soft px-2.5 py-0.5 text-xs font-semibold text-accent border border-accent/30"
              >
                {{ t('settings.rules.sandboxAppliedRule', { rule: sandboxResult.appliedRuleName }) }}
              </span>
            </div>

            <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs pt-1">
              <div class="rounded-control bg-surface border border-border p-2">
                <span class="text-[10px] text-text-muted font-medium">Title / Payee</span>
                <p class="font-bold text-text truncate mt-0.5">{{ sandboxResult.transaction.title }}</p>
              </div>
              <div class="rounded-control bg-surface border border-border p-2">
                <span class="text-[10px] text-text-muted font-medium">Amount & Type</span>
                <p class="font-bold text-text truncate mt-0.5">
                  Rp {{ sandboxResult.transaction.amount.toLocaleString(locale) }}
                  <span class="text-[10px] font-semibold ml-1" :class="sandboxResult.transaction.type === 'Income' ? 'text-positive' : 'text-negative'">
                    ({{ sandboxResult.transaction.type }})
                  </span>
                </p>
              </div>
              <div class="rounded-control bg-surface border border-border p-2">
                <span class="text-[10px] text-text-muted font-medium">Account</span>
                <p class="font-bold text-text truncate mt-0.5">{{ sandboxResult.transaction.accountName || '-' }}</p>
              </div>
              <div class="rounded-control bg-surface border border-border p-2">
                <span class="text-[10px] text-text-muted font-medium">Category</span>
                <p class="font-bold text-text truncate mt-0.5">{{ sandboxResult.transaction.categoryName || '-' }}</p>
              </div>
            </div>
          </div>
        </div>
      </AppCard>

      <!-- 3. Categorization Rules Table Card -->
      <AppCard>
        <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
          <div>
            <h2 class="text-base font-bold text-text">
              {{ t('settings.rules.rulesHeading') }}
            </h2>
            <p class="text-xs text-text-muted mt-1 max-w-2xl">
              {{ t('settings.rules.rulesDescription') }}
            </p>
          </div>
          <AppButton
            variant="primary"
            size="sm"
            class="shrink-0"
            @click="openAddRuleModal"
          >
            <Plus :size="14" />
            <span>{{ t('settings.rules.addRule') }}</span>
          </AppButton>
        </div>

        <!-- Rules Table -->
        <div class="mt-4 overflow-x-auto rounded-control border border-border">
          <table class="w-full text-left text-xs text-text">
            <thead class="bg-surface-2 text-[11px] font-semibold text-text-muted border-b border-border uppercase">
              <tr>
                <th class="px-3 py-2.5">{{ t('settings.rules.priority') }}</th>
                <th class="px-3 py-2.5">{{ t('settings.rules.ruleName') }}</th>
                <th class="px-3 py-2.5">{{ t('settings.rules.matchField') }}</th>
                <th class="px-3 py-2.5">{{ t('settings.rules.matchOperator') }}</th>
                <th class="px-3 py-2.5">{{ t('settings.rules.pattern') }}</th>
                <th class="px-3 py-2.5">{{ t('settings.rules.targetCategory') }}</th>
                <th class="px-3 py-2.5 text-center">{{ t('settings.rules.active') }}</th>
                <th class="px-3 py-2.5 text-right">{{ t('settings.rules.actions') }}</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-border bg-surface">
              <tr v-if="rules.length === 0">
                <td colspan="8" class="px-4 py-8 text-center text-text-muted">
                  {{ t('settings.rules.emptyRules') }}
                </td>
              </tr>
              <tr
                v-for="rule in rules"
                :key="rule.id"
                class="hover:bg-surface-2/60 transition-colors"
              >
                <td class="px-3 py-2.5 font-mono text-text-muted font-bold">
                  {{ rule.priority }}
                </td>
                <td class="px-3 py-2.5 font-semibold text-text">
                  {{ rule.name }}
                </td>
                <td class="px-3 py-2.5 text-text-muted">
                  {{ t(`settings.rules.fields.${rule.matchField}`) }}
                </td>
                <td class="px-3 py-2.5">
                  <span class="rounded bg-surface-2 px-1.5 py-0.5 font-mono text-[10px] text-text-muted border border-border">
                    {{ t(`settings.rules.operators.${rule.matchOperator}`) }}
                  </span>
                </td>
                <td class="px-3 py-2.5 font-mono text-accent font-semibold">
                  {{ rule.pattern }}
                </td>
                <td class="px-3 py-2.5">
                  <span class="rounded-full bg-accent-soft px-2.5 py-0.5 text-xs font-semibold text-accent">
                    {{ getCategoryName(rule.targetCategoryId) }}
                  </span>
                </td>
                <td class="px-3 py-2.5 text-center">
                  <button
                    type="button"
                    class="inline-flex h-5 w-9 items-center rounded-full transition-colors focus:outline-none"
                    :class="rule.isActive ? 'bg-accent' : 'bg-surface-3 border border-border'"
                    @click="toggleRuleActive(rule)"
                  >
                    <span
                      class="inline-block h-3.5 w-3.5 transform rounded-full bg-white transition-transform"
                      :class="rule.isActive ? 'translate-x-4' : 'translate-x-1'"
                    />
                  </button>
                </td>
                <td class="px-3 py-2.5 text-right space-x-1">
                  <button
                    type="button"
                    class="rounded-control p-1 text-text-muted hover:bg-surface-2 hover:text-text transition-colors"
                    :title="t('settings.rules.editRule')"
                    @click="openEditRuleModal(rule)"
                  >
                    <Edit2 :size="14" />
                  </button>
                  <button
                    type="button"
                    class="rounded-control p-1 text-text-muted hover:bg-surface-2 hover:text-negative transition-colors"
                    :title="t('common.delete')"
                    @click="handleDeleteRule(rule)"
                  >
                    <Trash2 :size="14" />
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </AppCard>
    </div>

    <!-- ===================================================================== -->
    <!-- TAB 3: Appearance & Display                                            -->
    <!-- ===================================================================== -->
    <div v-else-if="activeTab === 'appearance'" class="space-y-4">
      <AppCard>
        <h2 class="text-base font-bold text-text">
          {{ t('settings.themeHeading') }}
        </h2>
        <p class="text-xs text-text-muted mt-1">
          {{ t('settings.themeDescription') }}
        </p>

        <!-- Modern Theme Selector Cards -->
        <div class="mt-4 grid grid-cols-1 sm:grid-cols-2 gap-3">
          <button
            type="button"
            class="flex items-center gap-3 rounded-container border p-4 text-left transition-all"
            :class="
              themeStore.theme === 'dark'
                ? 'border-accent bg-accent-soft/40 shadow-xs ring-1 ring-accent'
                : 'border-border bg-surface hover:border-border-strong hover:bg-surface-2'
            "
            @click="selectTheme('dark')"
          >
            <div class="rounded-control bg-surface-2 p-2.5 text-accent">
              <Moon :size="18" />
            </div>
            <div>
              <p class="text-sm font-bold text-text">{{ t('settings.themeDark') }}</p>
              <p class="text-xs text-text-muted">{{ t('settings.themeDarkDesc') }}</p>
            </div>
            <Check v-if="themeStore.theme === 'dark'" :size="18" class="ml-auto text-accent shrink-0" />
          </button>

          <button
            type="button"
            class="flex items-center gap-3 rounded-container border p-4 text-left transition-all"
            :class="
              themeStore.theme === 'light'
                ? 'border-accent bg-accent-soft/40 shadow-xs ring-1 ring-accent'
                : 'border-border bg-surface hover:border-border-strong hover:bg-surface-2'
            "
            @click="selectTheme('light')"
          >
            <div class="rounded-control bg-surface-2 p-2.5 text-accent">
              <Sun :size="18" />
            </div>
            <div>
              <p class="text-sm font-bold text-text">{{ t('settings.themeLight') }}</p>
              <p class="text-xs text-text-muted">{{ t('settings.themeLightDesc') }}</p>
            </div>
            <Check v-if="themeStore.theme === 'light'" :size="18" class="ml-auto text-accent shrink-0" />
          </button>
        </div>
      </AppCard>

      <!-- Display Preferences Card -->
      <AppCard>
        <h2 class="text-base font-bold text-text">
          {{ t('settings.displayHeading') }}
        </h2>
        <p class="text-xs text-text-muted mt-1">
          {{ t('settings.displayDescription') }}
        </p>

        <div class="mt-5 space-y-4 divide-y divide-border">
          <!-- Density Toggle -->
          <div class="flex items-center justify-between pt-3 first:pt-0">
            <div>
              <p class="text-sm font-bold text-text">{{ t('settings.densityTitle') }}</p>
              <p class="text-xs text-text-muted">{{ t('settings.densityDesc') }}</p>
            </div>
            <div class="flex items-center gap-1.5 rounded-xl border border-border bg-surface-2 p-1">
              <button
                type="button"
                class="rounded-lg px-3 py-1.5 text-xs font-semibold transition-all"
                :class="ui.density === 'comfortable' ? 'bg-accent text-accent-contrast shadow-xs font-bold' : 'text-text-muted hover:text-text'"
                @click="ui.setDensity('comfortable')"
              >
                {{ t('settings.densityComfortable') }}
              </button>
              <button
                type="button"
                class="rounded-lg px-3 py-1.5 text-xs font-semibold transition-all"
                :class="ui.density === 'compact' ? 'bg-accent text-accent-contrast shadow-xs font-bold' : 'text-text-muted hover:text-text'"
                @click="ui.setDensity('compact')"
              >
                {{ t('settings.densityCompact') }}
              </button>
            </div>
          </div>

          <!-- Mask Numbers / Privacy Toggle -->
          <div class="flex items-center justify-between pt-3">
            <div>
              <p class="text-sm font-bold text-text">{{ t('settings.privacyTitle') }}</p>
              <p class="text-xs text-text-muted">{{ t('settings.privacyDesc') }}</p>
            </div>
            <AppToggle :model-value="ui.amountsHidden" @update:model-value="ui.toggleAmounts()" />
          </div>

          <!-- Language Selector -->
          <div class="flex items-center justify-between pt-3">
            <div>
              <p class="text-sm font-bold text-text">{{ t('settings.languageTitle') }}</p>
              <p class="text-xs text-text-muted">{{ t('settings.languageDesc') }}</p>
            </div>
            <div class="flex items-center gap-1.5 rounded-xl border border-border bg-surface-2 p-1">
              <button
                type="button"
                class="rounded-lg px-3 py-1.5 text-xs font-semibold transition-all"
                :class="ui.locale === 'en' ? 'bg-accent text-accent-contrast shadow-xs' : 'text-text-muted hover:text-text'"
                @click="ui.changeLocale('en')"
              >
                English (EN)
              </button>
              <button
                type="button"
                class="rounded-lg px-3 py-1.5 text-xs font-semibold transition-all"
                :class="ui.locale === 'id' ? 'bg-accent text-accent-contrast shadow-xs' : 'text-text-muted hover:text-text'"
                @click="ui.changeLocale('id')"
              >
                Bahasa Indonesia (ID)
              </button>
            </div>
          </div>
        </div>
      </AppCard>
    </div>

    <!-- ===================================================================== -->
    <!-- TAB 3: Account & Profile                                               -->
    <!-- ===================================================================== -->
    <div v-else-if="activeTab === 'profile'" class="space-y-4">
      <AppCard>
        <div class="flex items-center gap-4">
          <AvatarChip :name="auth.user?.displayName || auth.user?.email || 'User'" />
          <div>
            <h2 class="text-base font-bold text-text">
              {{ auth.user?.displayName || auth.user?.email }}
            </h2>
            <div class="flex items-center gap-2 mt-0.5">
              <span class="text-xs text-text-muted font-mono">{{ auth.user?.email }}</span>
              <span class="rounded-full bg-accent-soft px-2 py-0.5 text-[10px] font-semibold text-accent">
                {{ t('settings.ownerRole') }}
              </span>
            </div>
          </div>
        </div>

        <div class="mt-6 border-t border-border pt-5 space-y-4 max-w-md">
          <FormField :label="t('settings.displayNameLabel')" :hint="t('settings.displayNameHint')">
            <input
              v-model="displayNameInput"
              type="text"
              class="mt-1 h-10 w-full rounded-control border border-border bg-surface px-3 text-sm text-text shadow-xs focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent"
              :placeholder="t('settings.displayNamePlaceholder')"
            />
          </FormField>

          <AppButton
            variant="primary"
            :loading="savingProfile"
            :disabled="!isProfileChanged && !savingProfile"
            @click="saveProfile"
          >
            <Save :size="16" />
            {{ t('settings.saveProfileButton') }}
          </AppButton>
        </div>
      </AppCard>

      <!-- Security & Session Card -->
      <AppCard>
        <div class="flex items-start justify-between">
          <div>
            <h2 class="text-base font-bold text-text">
              {{ t('settings.securityHeading') }}
            </h2>
            <p class="text-xs text-text-muted mt-1">
              {{ t('settings.securityDescription') }}
            </p>
          </div>
          <Shield :size="20" class="text-accent" />
        </div>

        <div class="mt-4 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 rounded-control border border-border bg-surface-2 p-4">
          <div>
            <p class="text-sm font-semibold text-text">{{ t('settings.activeSessionTitle') }}</p>
            <p class="text-xs text-text-muted mt-0.5">{{ t('settings.activeSessionDesc') }}</p>
          </div>
          <AppButton variant="secondary" danger @click="handleSignOut">
            <LogOut :size="16" />
            {{ t('common.signOut') }}
          </AppButton>
        </div>
      </AppCard>
    </div>

    <!-- ===================================================================== -->
    <!-- TAB 4: Data & Backup                                                   -->
    <!-- ===================================================================== -->
    <div v-else-if="activeTab === 'data'" class="space-y-4">
      <AppCard>
        <div class="flex items-start justify-between">
          <div>
            <h2 class="text-base font-bold text-text">
              {{ t('settings.dataHeading') }}
            </h2>
            <p class="text-xs text-text-muted mt-1">
              {{ t('settings.dataDescription') }}
            </p>
          </div>
          <Database :size="20" class="text-accent" />
        </div>

        <!-- Direct CSV Download -->
        <div class="mt-5 rounded-control border border-border bg-surface-2 p-4 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <p class="text-sm font-semibold text-text">{{ t('settings.exportTransactionsTitle') }}</p>
            <p class="text-xs text-text-muted mt-0.5">{{ t('settings.exportTransactionsDesc') }}</p>
          </div>
          <AppButton
            variant="secondary"
            :loading="exportingCsv"
            class="shrink-0"
            @click="exportTransactionsCsv"
          >
            <Download :size="16" />
            {{ t('settings.exportCsvButton') }}
          </AppButton>
        </div>
      </AppCard>

      <!-- Application Version & Release Card -->
      <AppCard>
        <div class="flex items-start justify-between">
          <div>
            <h2 class="text-base font-bold text-text">
              {{ t('settings.aboutHeading') }}
            </h2>
            <p class="text-xs text-text-muted mt-1">
              {{ t('settings.aboutDescription') }}
            </p>
          </div>
          <span class="rounded-control bg-accent-soft p-2 text-accent">
            <Sparkles :size="20" />
          </span>
        </div>

        <div class="mt-4 grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div class="rounded-control border border-border bg-surface-2 p-3.5 space-y-1">
            <span class="text-[11px] text-text-muted font-medium">{{ t('settings.currentVersion') }}</span>
            <p class="text-sm font-bold text-text">Tameru v0.1.0</p>
            <span class="text-xs text-text-muted font-mono">Build 2026.09</span>
          </div>

          <div class="rounded-control border border-border bg-surface-2 p-3.5 space-y-1">
            <span class="text-[11px] text-text-muted font-medium">{{ t('settings.latestVersion') }}</span>
            <p class="text-sm font-bold text-text">v0.1.0</p>
            <span class="inline-flex items-center gap-1 text-xs text-positive font-semibold">
              <Check :size="13" />
              {{ t('settings.releaseStatus') }}
            </span>
          </div>

          <div class="rounded-control border border-border bg-surface-2 p-3.5 space-y-1">
            <span class="text-[11px] text-text-muted font-medium">{{ t('settings.releaseChannel') }}</span>
            <p class="text-sm font-bold text-text">Stable</p>
            <span class="text-xs text-text-muted">Self-Hosted Community</span>
          </div>
        </div>
      </AppCard>
    </div>

    <!-- Add / Edit Rule Modal -->
    <AppModal
      v-if="showRuleModal"
      :title="editingRuleId ? t('settings.rules.editRule') : t('settings.rules.addRule')"
      @close="showRuleModal = false"
    >
      <form class="space-y-4" @submit.prevent="saveRule">
        <FormField :label="t('settings.rules.ruleName')" required>
          <AppInput
            v-model="ruleName"
            placeholder="e.g. Starbucks to Coffee"
            required
          />
        </FormField>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FormField :label="t('settings.rules.matchField')">
            <AppSelect
              v-model="ruleMatchField"
              :options="matchFieldOptions"
            />
          </FormField>

          <FormField :label="t('settings.rules.matchOperator')">
            <AppSelect
              v-model="ruleMatchOperator"
              :options="matchOperatorOptions"
            />
          </FormField>
        </div>

        <FormField :label="t('settings.rules.pattern')" required>
          <AppInput
            v-model="rulePattern"
            placeholder="e.g. starbucks, kopi, grab"
            required
          />
        </FormField>

        <FormField :label="t('settings.rules.targetCategory')" required>
          <AppSelect
            v-model="ruleTargetCategoryId"
            :options="categoryOptions"
            placeholder="Select target category..."
          />
        </FormField>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 items-center">
          <FormField :label="t('settings.rules.priority')">
            <input
              v-model.number="rulePriority"
              type="number"
              min="1"
              max="999"
              class="h-10 w-full rounded-control border border-border bg-surface px-3 text-sm text-text shadow-xs focus:border-accent focus:outline-none"
            />
          </FormField>

          <div class="pt-5 flex items-center gap-2">
            <input
              id="ruleIsActiveCheckbox"
              v-model="ruleIsActive"
              type="checkbox"
              class="h-4 w-4 rounded border-border text-accent focus:ring-accent"
            />
            <label for="ruleIsActiveCheckbox" class="text-xs font-semibold text-text cursor-pointer">
              {{ t('settings.rules.active') }}
            </label>
          </div>
        </div>

        <div class="mt-6 flex items-center justify-end gap-2 pt-2 border-t border-border">
          <AppButton
            variant="secondary"
            type="button"
            @click="showRuleModal = false"
          >
            {{ t('common.cancel') }}
          </AppButton>
          <AppButton
            variant="primary"
            type="submit"
            :loading="savingRule"
          >
            <Save :size="14" />
            <span>{{ t('common.save') }}</span>
          </AppButton>
        </div>
      </form>
    </AppModal>
  </div>
</template>
