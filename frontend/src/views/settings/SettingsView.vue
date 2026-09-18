<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue';
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
  Sliders,
  History,
  BookOpen,
  Clock,
  XCircle,
  Bot,
  AlertCircle,
} from 'lucide-vue-next';
import { useAuthStore } from '@/stores/auth';
import { useUiStore } from '@/stores/ui';
import { useThemeStore } from '@/stores/theme';
import { useToastStore } from '@/stores/toast';
import { useConfirmStore } from '@/stores/confirm';
import { regenerateApiToken } from '@/lib/auth';
import {
  getStoredAiConfig,
  setStoredAiConfig,
  testAiConnection,
} from '@/lib/assistant';
import {
  listRules,
  createRule,
  updateRule,
  deleteRule,
  dryRunRules,
  listAuditLog,
  listTemplates,
  createFromTemplate,
} from '@/lib/rules';
import { listCategories } from '@/lib/categories';
import { listAccounts } from '@/lib/accounts';
import { listTransactions } from '@/lib/transactions';
import { toCsv, downloadCsv } from '@/lib/csv';
import type {
  Transaction,
  RuleDto,
  RuleMatchField,
  RuleMatchOperator,
  Category,
  Account,
  TransactionType,
  DryRunResultDto,
  RuleAuditLogDto,
} from '@/types/api';
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

type TabKey = 'financial-cycle' | 'rules' | 'ai-provider' | 'appearance' | 'profile' | 'data';
const activeTab = ref<TabKey>('financial-cycle');

// ----------------------------------------------------------------------------
// Tab 2b: AI Provider Settings
// ----------------------------------------------------------------------------
interface AiPreset {
  id: string;
  name: string;
  provider: string;
  baseUrl: string;
  model: string;
  needsKey: boolean;
  hint: string;
}

const AI_PRESETS: AiPreset[] = [
  {
    id: 'openai',
    name: 'OpenAI',
    provider: 'OpenAI',
    baseUrl: 'https://api.openai.com/v1/',
    model: 'gpt-4o-mini',
    needsKey: true,
    hint: 'Official OpenAI GPT-4o / GPT-4o-mini models',
  },
  {
    id: 'groq',
    name: 'Groq (Fast)',
    provider: 'Groq',
    baseUrl: 'https://api.groq.com/openai/v1/',
    model: 'llama-3.3-70b-versatile',
    needsKey: true,
    hint: 'Ultra-fast Llama 3 inference with free tier',
  },
  {
    id: 'openrouter',
    name: 'OpenRouter',
    provider: 'OpenRouter',
    baseUrl: 'https://openrouter.ai/api/v1/',
    model: 'deepseek/deepseek-chat',
    needsKey: true,
    hint: 'Access DeepSeek, Claude, Llama 3, and more',
  },
  {
    id: 'ollama',
    name: 'Ollama (Local)',
    provider: 'Ollama',
    baseUrl: 'http://localhost:11434/v1/',
    model: 'llama3.2',
    needsKey: false,
    hint: '100% private and offline on localhost',
  },
  {
    id: 'custom',
    name: 'Custom',
    provider: 'Custom',
    baseUrl: '',
    model: '',
    needsKey: true,
    hint: 'Any OpenAI-compatible API endpoint',
  },
];

const aiForm = reactive<{
  provider: string;
  baseUrl: string;
  apiKey: string;
  model: string;
}>({
  provider: 'OpenAI',
  baseUrl: 'https://api.openai.com/v1/',
  apiKey: '',
  model: 'gpt-4o-mini',
});
const showAiKey = ref(false);
const testingAi = ref(false);
const testResult = ref<{ success: boolean; message: string; model?: string | null } | null>(null);
const selectedPresetId = ref('openai');

function initAiSettings(): void {
  const stored = getStoredAiConfig();
  if (stored) {
    aiForm.provider = stored.provider || 'OpenAI';
    aiForm.baseUrl = stored.baseUrl || 'https://api.openai.com/v1/';
    aiForm.apiKey = stored.apiKey || '';
    aiForm.model = stored.model || 'gpt-4o-mini';
    const match = AI_PRESETS.find((p) => p.baseUrl === aiForm.baseUrl);
    selectedPresetId.value = match ? match.id : 'custom';
  }
}

function selectAiPreset(preset: AiPreset): void {
  selectedPresetId.value = preset.id;
  aiForm.provider = preset.provider;
  if (preset.baseUrl) aiForm.baseUrl = preset.baseUrl;
  if (preset.model) aiForm.model = preset.model;
  testResult.value = null;
}

async function handleTestAi(): Promise<void> {
  testingAi.value = true;
  testResult.value = null;
  try {
    const res = await testAiConnection(aiForm);
    testResult.value = res;
    if (res.success) {
      toast.success(res.message);
    } else {
      toast.error(res.message);
    }
  } catch (err: any) {
    const msg = err?.response?.data?.message || err?.message || 'Connection test failed';
    testResult.value = { success: false, message: msg };
    toast.error(msg);
  } finally {
    testingAi.value = false;
  }
}

function handleSaveAi(): void {
  setStoredAiConfig(aiForm);
  toast.success(t('settings.aiProvider.savedToast'));
}

async function handleResetAi(): Promise<void> {
  const ok = await confirm.ask({
    message: t('settings.aiProvider.resetConfirm'),
    confirmLabel: t('common.reset'),
    danger: true,
  });
  if (!ok) return;

  setStoredAiConfig(null);
  selectedPresetId.value = 'openai';
  aiForm.provider = 'OpenAI';
  aiForm.baseUrl = 'https://api.openai.com/v1/';
  aiForm.apiKey = '';
  aiForm.model = 'gpt-4o-mini';
  testResult.value = null;
  toast.success(t('settings.aiProvider.resetToast'));
}

onMounted(() => {
  initAiSettings();
  const queryTab = route.query.tab as string;
  if (queryTab && ['financial-cycle', 'rules', 'ai-provider', 'appearance', 'profile', 'data'].includes(queryTab)) {
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
    const [rList, cList, aList] = await Promise.all([
      listRules(false),
      listCategories(),
      listAccounts(true),
    ]);
    rules.value = rList;
    categories.value = cList;
    accounts.value = aList;
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


// Advanced Rules Sub-Tabs
type RulesSubTab = 'rules' | 'dry-run' | 'templates' | 'audit';
const rulesSubTab = ref<RulesSubTab>('rules');

function handleRulesSubTabChange(subTab: RulesSubTab): void {
  rulesSubTab.value = subTab;
  if (subTab === 'templates' && templates.value.length === 0) {
    void loadTemplates();
  } else if (subTab === 'audit') {
    void loadAuditLogs();
  }
}

// Dry-Run Simulator State
const dryRunText = ref('starbucks 65000 bca');
const dryRunPayee = ref('Starbucks Coffee');
const dryRunAmount = ref<number | null>(65000);
const dryRunAccountId = ref<string>('');
const dryRunType = ref<TransactionType | ''>('Expense');
const testingDryRun = ref(false);
const dryRunResult = ref<DryRunResultDto | null>(null);

async function runDryRun(): Promise<void> {
  testingDryRun.value = true;
  dryRunResult.value = null;
  try {
    const res = await dryRunRules({
      text: dryRunText.value.trim() || undefined,
      payee: dryRunPayee.value.trim() || undefined,
      amount: dryRunAmount.value && dryRunAmount.value > 0 ? dryRunAmount.value : undefined,
      accountId: dryRunAccountId.value || undefined,
      transactionType: (dryRunType.value as any) || undefined,
    });
    dryRunResult.value = res;
    toast.success(t('settings.rules.dryRun.heading'));
  } catch (e: any) {
    toast.error(e?.message || t('errors.generic'));
  } finally {
    testingDryRun.value = false;
  }
}

// Templates State
const templates = ref<RuleDto[]>([]);
const loadingTemplates = ref(false);
const applyingTemplateId = ref<string | null>(null);

async function loadTemplates(): Promise<void> {
  loadingTemplates.value = true;
  try {
    templates.value = await listTemplates();
  } catch {
    toast.error(t('settings.rules.templates.templatesLoadError'));
  } finally {
    loadingTemplates.value = false;
  }
}

async function handleUseTemplate(template: RuleDto): Promise<void> {
  applyingTemplateId.value = template.id;
  try {
    const targetCat = categories.value[0]?.id || null;
    const created = await createFromTemplate(template.id, {
      name: template.name,
      pattern: template.pattern,
      targetCategoryId: targetCat,
      tags: template.tags,
      groupId: template.groupId,
      scheduleExpression: template.scheduleExpression,
    });
    rules.value.unshift(created);
    toast.success(t('settings.rules.templates.templateCreatedSuccess', { name: created.name }));
    rulesSubTab.value = 'rules';
  } catch {
    toast.error(t('settings.rules.templates.templateCreateError'));
  } finally {
    applyingTemplateId.value = null;
  }
}

// Audit Log State
const auditLogs = ref<RuleAuditLogDto[]>([]);
const loadingAuditLogs = ref(false);
const selectedRuleAuditFilter = ref<string>('');

async function loadAuditLogs(): Promise<void> {
  loadingAuditLogs.value = true;
  try {
    auditLogs.value = await listAuditLog(selectedRuleAuditFilter.value || undefined, 50);
  } catch {
    toast.error(t('settings.rules.audit.auditLogLoadError'));
  } finally {
    loadingAuditLogs.value = false;
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

// Advanced Rules Criteria, Grouping, Scheduling & Transformations
const ruleMinAmount = ref<number | null>(null);
const ruleMaxAmount = ref<number | null>(null);
const ruleAccountId = ref<string>('');
const ruleTransactionType = ref<TransactionType | ''>('');
const ruleReplaceTitle = ref<string>('');
const ruleGroupId = ref<string>('');
const ruleScheduleExpression = ref<string>('');
const ruleTags = ref<string>('');
const showAdvancedRuleFields = ref(false);
const accounts = ref<Account[]>([]);

const accountOptions = computed<SelectOption[]>(() => [
  { value: '', label: t('settings.rules.anyAccount') },
  ...accounts.value.map((a) => ({
    value: a.id,
    label: a.name,
  })),
]);

const transactionTypeOptions = computed<SelectOption[]>(() => [
  { value: '', label: t('settings.rules.anyType') },
  { value: 'Expense', label: t('transactions.types.expense') },
  { value: 'Income', label: t('transactions.types.income') },
  { value: 'Transfer', label: t('transactions.types.transfer') },
]);

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
  ruleMinAmount.value = null;
  ruleMaxAmount.value = null;
  ruleAccountId.value = '';
  ruleTransactionType.value = '';
  ruleReplaceTitle.value = '';
  ruleGroupId.value = '';
  ruleScheduleExpression.value = '';
  ruleTags.value = '';
  showAdvancedRuleFields.value = false;
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
  ruleMinAmount.value = rule.minAmount ?? null;
  ruleMaxAmount.value = rule.maxAmount ?? null;
  ruleAccountId.value = rule.accountId ?? '';
  ruleTransactionType.value = rule.transactionType ?? '';
  ruleReplaceTitle.value = rule.replaceTitle ?? '';
  ruleGroupId.value = rule.groupId ?? '';
  ruleScheduleExpression.value = rule.scheduleExpression ?? '';
  ruleTags.value = rule.tags ?? '';
  showAdvancedRuleFields.value = !!(
    rule.minAmount ||
    rule.maxAmount ||
    rule.accountId ||
    rule.transactionType ||
    rule.replaceTitle ||
    rule.groupId ||
    rule.scheduleExpression ||
    rule.tags
  );
  showRuleModal.value = true;
}

async function saveRule(): Promise<void> {
  if (!ruleName.value.trim() || !rulePattern.value.trim()) {
    toast.error(t('errors.validation_error'));
    return;
  }
  savingRule.value = true;
  try {
    const payload = {
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
      minAmount: ruleMinAmount.value && ruleMinAmount.value > 0 ? ruleMinAmount.value : null,
      maxAmount: ruleMaxAmount.value && ruleMaxAmount.value > 0 ? ruleMaxAmount.value : null,
      accountId: ruleAccountId.value || null,
      transactionType: (ruleTransactionType.value as any) || null,
      replaceTitle: ruleReplaceTitle.value.trim() || null,
      groupId: ruleGroupId.value.trim() || null,
      scheduleExpression: ruleScheduleExpression.value.trim() || null,
      isTemplate: false,
      tags: ruleTags.value.trim() || null,
    };

    if (editingRuleId.value) {
      const updated = await updateRule(editingRuleId.value, payload);
      const idx = rules.value.findIndex((r) => r.id === editingRuleId.value);
      if (idx !== -1) rules.value[idx] = updated;
    } else {
      const created = await createRule(payload);
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

function getAccountName(id: string | null | undefined): string {
  if (!id) return '';
  const a = accounts.value.find((acc) => acc.id === id);
  return a ? a.name : '';
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
      minAmount: rule.minAmount,
      maxAmount: rule.maxAmount,
      accountId: rule.accountId,
      transactionType: rule.transactionType,
      replaceTitle: rule.replaceTitle,
      groupId: rule.groupId,
      scheduleExpression: rule.scheduleExpression,
      isTemplate: rule.isTemplate,
      tags: rule.tags,
    });
    const idx = rules.value.findIndex((r) => r.id === rule.id);
    if (idx !== -1) rules.value[idx] = updated;
  } catch {
    toast.error(t('errors.generic'));
  }
}

function getCategoryName(id?: string | null): string {
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
            activeTab === 'ai-provider'
              ? 'bg-accent text-accent-contrast shadow-sm font-bold'
              : 'text-text-muted hover:text-text'
          "
          @click="setTab('ai-provider')"
        >
          <Bot :size="15" />
          {{ t('settings.tabs.aiProvider') }}
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
      <!-- Sub-Navigation for Automations & Rules -->
      <div class="flex items-center gap-1.5 p-1 bg-surface-2 rounded-control border border-border w-fit text-xs font-semibold overflow-x-auto">
        <button
          type="button"
          class="px-3 py-1.5 rounded-control transition-all"
          :class="rulesSubTab === 'rules' ? 'bg-surface text-text shadow-xs font-bold border border-border/60' : 'text-text-muted hover:text-text'"
          @click="handleRulesSubTabChange('rules')"
        >
          <div class="flex items-center gap-1.5">
            <Sliders :size="13" />
            <span>{{ t('settings.rules.subTabs.activeRules') }}</span>
            <span class="rounded-full bg-accent-soft px-1.5 py-0.2 text-[10px] text-accent font-bold">{{ rules.length }}</span>
          </div>
        </button>
        <button
          type="button"
          class="px-3 py-1.5 rounded-control transition-all"
          :class="rulesSubTab === 'dry-run' ? 'bg-surface text-text shadow-xs font-bold border border-border/60' : 'text-text-muted hover:text-text'"
          @click="handleRulesSubTabChange('dry-run')"
        >
          <div class="flex items-center gap-1.5">
            <Play :size="13" />
            <span>{{ t('settings.rules.subTabs.dryRunTester') }}</span>
          </div>
        </button>
        <button
          type="button"
          class="px-3 py-1.5 rounded-control transition-all"
          :class="rulesSubTab === 'templates' ? 'bg-surface text-text shadow-xs font-bold border border-border/60' : 'text-text-muted hover:text-text'"
          @click="handleRulesSubTabChange('templates')"
        >
          <div class="flex items-center gap-1.5">
            <BookOpen :size="13" />
            <span>{{ t('settings.rules.subTabs.templateLibrary') }}</span>
          </div>
        </button>
        <button
          type="button"
          class="px-3 py-1.5 rounded-control transition-all"
          :class="rulesSubTab === 'audit' ? 'bg-surface text-text shadow-xs font-bold border border-border/60' : 'text-text-muted hover:text-text'"
          @click="handleRulesSubTabChange('audit')"
        >
          <div class="flex items-center gap-1.5">
            <History :size="13" />
            <span>{{ t('settings.rules.subTabs.auditLog') }}</span>
          </div>
        </button>
      </div>

      <!-- SUB-TAB 1: Active Rules & Webhook -->
      <div v-if="rulesSubTab === 'rules'" class="space-y-4">
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

          <!-- Integration Guide snippet -->
          <div class="mt-6 rounded-control border border-border bg-surface-2 p-4 text-xs space-y-3">
            <div class="flex items-center gap-2 font-bold text-text">
              <Code2 :size="16" class="text-accent" />
              <span>{{ t('settings.rules.howToUse') }}</span>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div class="space-y-1.5">
                <span class="text-[11px] font-semibold text-text-muted">{{ t('settings.rules.curlGuide') }}</span>
                <pre class="rounded-control bg-surface border border-border p-2.5 font-mono text-[11px] text-text-muted overflow-x-auto select-all">curl -X POST "{{ webhookUrl }}" \
  -H "X-Tameru-Token: {{ apiToken ? (showToken ? apiToken : 'tmr_your_token_here') : 'tmr_your_token_here' }}" \
  -H "Content-Type: application/json" \
  -d '{"text": "kopi kenangan 35k gopay"}'</pre>
              </div>

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

        <!-- 2. Categorization Rules Table Card -->
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
                    <div>{{ rule.name }}</div>
                    <div class="flex flex-wrap gap-1 mt-1">
                      <!-- Group Badge -->
                      <span
                        v-if="rule.groupId"
                        class="rounded bg-accent/15 px-1.5 py-0.2 text-[9px] font-bold text-accent border border-accent/30"
                      >
                        AND: {{ rule.groupId }}
                      </span>
                      <!-- Schedule Badge -->
                      <span
                        v-if="rule.scheduleExpression"
                        class="rounded bg-primary/10 px-1.5 py-0.2 text-[9px] font-bold text-primary border border-primary/20 flex items-center gap-1"
                      >
                        <Clock :size="10" /> {{ rule.scheduleExpression }}
                      </span>
                      <span
                        v-if="rule.transactionType"
                        class="rounded bg-surface-2 px-1.5 py-0.2 text-[9px] font-semibold text-primary border border-border"
                      >
                        {{ rule.transactionType }}
                      </span>
                      <span
                        v-if="rule.accountId"
                        class="rounded bg-surface-2 px-1.5 py-0.2 text-[9px] font-semibold text-accent border border-border"
                      >
                        {{ getAccountName(rule.accountId) }}
                      </span>
                      <span
                        v-if="rule.minAmount || rule.maxAmount"
                        class="rounded bg-surface-2 px-1.5 py-0.2 text-[9px] font-semibold text-text-muted border border-border tabular-nums"
                      >
                        {{ rule.minAmount ? `≥ ${rule.minAmount.toLocaleString()}` : '' }}
                        {{ rule.minAmount && rule.maxAmount ? ' · ' : '' }}
                        {{ rule.maxAmount ? `≤ ${rule.maxAmount.toLocaleString()}` : '' }}
                      </span>
                      <span
                        v-if="rule.replaceTitle"
                        class="rounded bg-primary/10 px-1.5 py-0.2 text-[9px] font-semibold text-primary border border-primary/20"
                      >
                        ➔ {{ rule.replaceTitle }}
                      </span>
                      <!-- Tags -->
                      <span
                        v-for="tag in (rule.tags ? rule.tags.split(',') : [])"
                        :key="tag"
                        class="rounded bg-surface-2 px-1.5 py-0.2 text-[9px] text-text-muted border border-border"
                      >
                        #{{ tag.trim() }}
                      </span>
                    </div>
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

      <!-- SUB-TAB 2: Dry-Run Rule Tester -->
      <div v-else-if="rulesSubTab === 'dry-run'" class="space-y-4">
        <AppCard>
          <div class="flex items-start justify-between gap-4">
            <div>
              <h2 class="text-base font-bold text-text">
                {{ t('settings.rules.dryRun.heading') }}
              </h2>
              <p class="text-xs text-text-muted mt-1 max-w-2xl">
                {{ t('settings.rules.dryRun.description') }}
              </p>
            </div>
            <span class="rounded-control bg-accent-soft p-2 text-accent">
              <Play :size="20" />
            </span>
          </div>

          <div class="mt-4 space-y-4">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <FormField :label="t('settings.rules.dryRun.textPrompt')">
                <AppInput
                  v-model="dryRunText"
                  placeholder="e.g. Starbucks 65000 BCA"
                />
              </FormField>

              <FormField :label="t('settings.rules.dryRun.amountPrompt')">
                <input
                  v-model.number="dryRunAmount"
                  type="number"
                  min="0"
                  placeholder="e.g. 65000"
                  class="h-10 w-full rounded-control border border-border bg-surface px-3 text-sm text-text shadow-xs focus:border-accent focus:outline-none tabular-nums"
                />
              </FormField>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <FormField :label="t('settings.rules.accountConstraint')">
                <AppSelect
                  v-model="dryRunAccountId"
                  :options="accountOptions"
                />
              </FormField>

              <FormField :label="t('settings.rules.transactionTypeConstraint')">
                <AppSelect
                  v-model="dryRunType"
                  :options="transactionTypeOptions"
                />
              </FormField>
            </div>

            <div class="flex items-center justify-between pt-2">
              <div class="flex items-center gap-1.5 flex-wrap">
                <span class="text-[11px] text-text-muted font-medium">{{ t('settings.presets') }}:</span>
                <button
                  type="button"
                  class="rounded-full border border-border bg-surface-2 px-2.5 py-0.5 text-xs text-text-muted hover:border-accent hover:text-accent transition-all font-mono"
                  @click="dryRunText = 'Starbucks Coffee 65k'; dryRunAmount = 65000; dryRunType = 'Expense'"
                >
                  Starbucks Coffee 65k
                </button>
                <button
                  type="button"
                  class="rounded-full border border-border bg-surface-2 px-2.5 py-0.5 text-xs text-text-muted hover:border-accent hover:text-accent transition-all font-mono"
                  @click="dryRunText = 'Gojek Ride 22k'; dryRunAmount = 22000; dryRunType = 'Expense'"
                >
                  Gojek Ride 22k
                </button>
                <button
                  type="button"
                  class="rounded-full border border-border bg-surface-2 px-2.5 py-0.5 text-xs text-text-muted hover:border-accent hover:text-accent transition-all font-mono"
                  @click="dryRunText = 'Gaji Bulanan PT Tech'; dryRunAmount = 15000000; dryRunType = 'Income'"
                >
                  Gaji Bulanan 15jt
                </button>
              </div>

              <AppButton
                variant="primary"
                :loading="testingDryRun"
                @click="runDryRun"
              >
                <Play :size="14" />
                <span>{{ t('settings.rules.dryRun.runTest') }}</span>
              </AppButton>
            </div>
          </div>

          <!-- Dry-Run Result Overview Banner -->
          <div v-if="dryRunResult" class="mt-6 space-y-4 animate-in fade-in duration-200">
            <div
              class="rounded-control border p-4 space-y-2"
              :class="dryRunResult.hasMatch ? 'border-positive/40 bg-positive-soft/30' : 'border-warning/40 bg-warning-soft/30'"
            >
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-2 font-bold text-sm" :class="dryRunResult.hasMatch ? 'text-positive' : 'text-warning'">
                  <CheckCircle2 v-if="dryRunResult.hasMatch" :size="18" />
                  <XCircle v-else :size="18" />
                  <span>{{ dryRunResult.hasMatch ? t('settings.rules.dryRun.winningRule') : t('settings.rules.dryRun.noWinningRule') }}</span>
                </div>
                <span class="text-xs text-text-muted font-medium">
                  {{ t('settings.rules.dryRun.evaluatedCount', { count: dryRunResult.allEvaluations.length }) }}
                </span>
              </div>

              <div v-if="dryRunResult.winningMatch" class="grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs pt-1">
                <div class="rounded-control bg-surface border border-border p-2.5">
                  <span class="text-[10px] text-text-muted font-medium">{{ t('settings.rules.ruleName') }}</span>
                  <p class="font-bold text-text truncate mt-0.5">{{ dryRunResult.winningMatch.rule.name }}</p>
                </div>
                <div class="rounded-control bg-surface border border-border p-2.5">
                  <span class="text-[10px] text-text-muted font-medium">{{ t('settings.rules.targetCategory') }}</span>
                  <p class="font-bold text-text truncate mt-0.5">{{ getCategoryName(dryRunResult.winningMatch.projectedCategoryId) }}</p>
                </div>
                <div class="rounded-control bg-surface border border-border p-2.5">
                  <span class="text-[10px] text-text-muted font-medium">Projected Title</span>
                  <p class="font-bold text-text truncate mt-0.5">{{ dryRunResult.winningMatch.projectedTitle || dryRunText }}</p>
                </div>
                <div class="rounded-control bg-surface border border-border p-2.5">
                  <span class="text-[10px] text-text-muted font-medium">Status</span>
                  <p class="font-bold text-text truncate mt-0.5">{{ dryRunResult.winningMatch.projectedStatus || 'Cleared' }}</p>
                </div>
              </div>
            </div>

            <!-- Detailed Evaluation Table -->
            <div class="rounded-control border border-border overflow-x-auto">
              <table class="w-full text-left text-xs text-text">
                <thead class="bg-surface-2 text-[11px] font-semibold text-text-muted border-b border-border uppercase">
                  <tr>
                    <th class="px-3 py-2.5">{{ t('settings.rules.priority') }}</th>
                    <th class="px-3 py-2.5">{{ t('settings.rules.ruleName') }}</th>
                    <th class="px-3 py-2.5 text-center">Match Status</th>
                    <th class="px-3 py-2.5">Reason / Logic</th>
                    <th class="px-3 py-2.5">{{ t('settings.rules.targetCategory') }}</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-border bg-surface">
                  <tr
                    v-for="item in dryRunResult.allEvaluations"
                    :key="item.rule.id"
                    :class="item.matched ? 'bg-positive-soft/20 font-medium' : 'hover:bg-surface-2/40'"
                  >
                    <td class="px-3 py-2.5 font-mono text-text-muted font-bold">{{ item.rule.priority }}</td>
                    <td class="px-3 py-2.5 font-semibold text-text">
                      <span>{{ item.rule.name }}</span>
                      <span v-if="item.rule.groupId" class="ml-1.5 rounded bg-accent-soft px-1.5 py-0.2 text-[9px] font-bold text-accent border border-accent/20">
                        {{ item.rule.groupId }}
                      </span>
                    </td>
                    <td class="px-3 py-2.5 text-center">
                      <span
                        class="rounded-full px-2 py-0.5 text-[10px] font-bold"
                        :class="item.matched ? 'bg-positive-soft text-positive border border-positive/30' : 'bg-surface-2 text-text-muted border border-border'"
                      >
                        {{ item.matched ? t('settings.rules.dryRun.ruleMatchPassed') : t('settings.rules.dryRun.ruleMatchSkipped') }}
                      </span>
                    </td>
                    <td class="px-3 py-2.5 text-text-muted font-mono text-[11px]">{{ item.matchedReason }}</td>
                    <td class="px-3 py-2.5">{{ item.matched ? getCategoryName(item.projectedCategoryId) : '-' }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </AppCard>
      </div>

      <!-- SUB-TAB 3: Templates Library -->
      <div v-else-if="rulesSubTab === 'templates'" class="space-y-4">
        <AppCard>
          <div class="flex items-start justify-between gap-4">
            <div>
              <h2 class="text-base font-bold text-text">
                {{ t('settings.rules.templates.heading') }}
              </h2>
              <p class="text-xs text-text-muted mt-1 max-w-2xl">
                {{ t('settings.rules.templates.description') }}
              </p>
            </div>
            <span class="rounded-control bg-accent-soft p-2 text-accent">
              <BookOpen :size="20" />
            </span>
          </div>

          <div v-if="loadingTemplates" class="mt-6 py-8 text-center text-xs text-text-muted">
            <RefreshCw :size="20" class="animate-spin mx-auto mb-2 text-accent" />
            <span>{{ t('common.loading') }}</span>
          </div>

          <div v-else class="mt-6 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <div
              v-for="tpl in templates"
              :key="tpl.id"
              class="rounded-container border border-border bg-surface p-4 flex flex-col justify-between hover:border-accent/50 transition-all shadow-xs"
            >
              <div class="space-y-2">
                <div class="flex items-start justify-between gap-2">
                  <h3 class="font-bold text-sm text-text">{{ tpl.name }}</h3>
                  <span class="rounded bg-surface-2 px-1.5 py-0.5 font-mono text-[10px] text-accent border border-border">
                    {{ tpl.matchOperator }}
                  </span>
                </div>

                <div class="rounded-control bg-surface-2 p-2 border border-border font-mono text-xs text-text truncate">
                  {{ tpl.pattern }}
                </div>

                <div class="flex flex-wrap gap-1 pt-1">
                  <span
                    v-if="tpl.transactionType"
                    class="rounded bg-accent-soft px-1.5 py-0.2 text-[9px] font-bold text-accent border border-accent/20"
                  >
                    {{ tpl.transactionType }}
                  </span>
                  <span
                    v-if="tpl.scheduleExpression"
                    class="rounded bg-primary/10 px-1.5 py-0.2 text-[9px] font-bold text-primary border border-primary/20 flex items-center gap-1"
                  >
                    <Clock :size="10" /> {{ tpl.scheduleExpression }}
                  </span>
                  <span
                    v-for="tag in (tpl.tags ? tpl.tags.split(',') : [])"
                    :key="tag"
                    class="rounded bg-surface-2 px-1.5 py-0.2 text-[9px] text-text-muted border border-border"
                  >
                    #{{ tag.trim() }}
                  </span>
                </div>
              </div>

              <div class="mt-4 pt-3 border-t border-border flex items-center justify-between">
                <span class="text-[11px] text-text-muted">Priority: {{ tpl.priority }}</span>
                <AppButton
                  variant="primary"
                  size="sm"
                  :loading="applyingTemplateId === tpl.id"
                  @click="handleUseTemplate(tpl)"
                >
                  <Plus :size="12" />
                  <span>{{ t('settings.rules.templates.useTemplate') }}</span>
                </AppButton>
              </div>
            </div>
          </div>
        </AppCard>
      </div>

      <!-- SUB-TAB 4: Audit Log History -->
      <div v-else-if="rulesSubTab === 'audit'" class="space-y-4">
        <AppCard>
          <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
            <div>
              <h2 class="text-base font-bold text-text">
                {{ t('settings.rules.audit.heading') }}
              </h2>
              <p class="text-xs text-text-muted mt-1 max-w-2xl">
                {{ t('settings.rules.audit.description') }}
              </p>
            </div>
            <div class="flex items-center gap-2">
              <AppButton
                variant="secondary"
                size="sm"
                :loading="loadingAuditLogs"
                @click="loadAuditLogs"
              >
                <RefreshCw :size="13" />
                <span>{{ t('common.refresh') }}</span>
              </AppButton>
            </div>
          </div>

          <!-- Audit Log Table -->
          <div class="mt-4 overflow-x-auto rounded-control border border-border">
            <table class="w-full text-left text-xs text-text">
              <thead class="bg-surface-2 text-[11px] font-semibold text-text-muted border-b border-border uppercase">
                <tr>
                  <th class="px-3 py-2.5">{{ t('settings.rules.audit.timestamp') }}</th>
                  <th class="px-3 py-2.5">{{ t('settings.rules.ruleName') }}</th>
                  <th class="px-3 py-2.5">Transaction Title</th>
                  <th class="px-3 py-2.5">Amount</th>
                  <th class="px-3 py-2.5">{{ t('settings.rules.audit.matchedField') }}</th>
                  <th class="px-3 py-2.5">{{ t('settings.rules.audit.matchedValue') }}</th>
                  <th class="px-3 py-2.5 text-center">{{ t('settings.rules.audit.wasApplied') }}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-border bg-surface">
                <tr v-if="auditLogs.length === 0">
                  <td colspan="7" class="px-4 py-8 text-center text-text-muted">
                    {{ t('settings.rules.audit.emptyAudit') }}
                  </td>
                </tr>
                <tr
                  v-for="log in auditLogs"
                  :key="log.id"
                  class="hover:bg-surface-2/60 transition-colors"
                >
                  <td class="px-3 py-2.5 font-mono text-text-muted text-[11px] whitespace-nowrap">
                    {{ new Date(log.evaluatedAt).toLocaleString(locale) }}
                  </td>
                  <td class="px-3 py-2.5 font-semibold text-text">
                    {{ log.ruleName }}
                  </td>
                  <td class="px-3 py-2.5 text-text truncate max-w-[160px]">
                    {{ log.transactionTitle }}
                  </td>
                  <td class="px-3 py-2.5 font-mono text-text tabular-nums whitespace-nowrap">
                    {{ log.amount ? `Rp ${log.amount.toLocaleString(locale)}` : '-' }}
                  </td>
                  <td class="px-3 py-2.5 text-text-muted">
                    {{ log.matchedField }}
                  </td>
                  <td class="px-3 py-2.5 font-mono text-accent font-semibold text-[11px] truncate max-w-[140px]">
                    {{ log.matchedValue }}
                  </td>
                  <td class="px-3 py-2.5 text-center">
                    <span
                      class="rounded-full px-2 py-0.5 text-[10px] font-bold"
                      :class="log.wasApplied ? 'bg-positive-soft text-positive border border-positive/30' : 'bg-surface-2 text-text-muted border border-border'"
                    >
                      {{ log.wasApplied ? t('settings.rules.active') : t('settings.rules.inactive') }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </AppCard>
      </div>
    </div>

    <!-- ===================================================================== -->
    <!-- TAB 2b: AI Provider & LLM Connection                                  -->
    <!-- ===================================================================== -->
    <div v-else-if="activeTab === 'ai-provider'" class="space-y-4">
      <!-- Overview & Status Banner -->
      <AppCard>
        <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
          <div>
            <div class="flex items-center gap-2">
              <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-accent-soft text-accent">
                <Bot :size="18" />
              </div>
              <h2 class="text-base font-bold text-text">
                {{ t('settings.aiProvider.heading') }}
              </h2>
            </div>
            <p class="text-xs text-text-muted mt-1.5 max-w-2xl">
              {{ t('settings.aiProvider.description') }}
            </p>
          </div>
          <div class="flex items-center gap-2 shrink-0">
            <span
              v-if="aiForm.apiKey || aiForm.baseUrl?.includes('localhost') || aiForm.baseUrl?.includes('127.0.0.1')"
              class="inline-flex items-center gap-1.5 rounded-full bg-positive-soft px-3 py-1 text-xs font-semibold text-positive border border-positive/20"
            >
              <CheckCircle2 :size="13" />
              {{ t('settings.aiProvider.activeStatus', { model: aiForm.model || 'Configured' }) }}
            </span>
            <span
              v-else
              class="inline-flex items-center gap-1.5 rounded-full bg-surface-2 px-3 py-1 text-xs font-medium text-text-muted border border-border"
            >
              <Info :size="13" />
              {{ t('settings.aiProvider.offlineStatus') }}
            </span>
          </div>
        </div>
      </AppCard>

      <!-- Provider Selection & Configuration Form -->
      <AppCard>
        <h3 class="text-sm font-bold text-text mb-1">
          {{ t('settings.aiProvider.presets') }}
        </h3>
        <p class="text-xs text-text-muted mb-4">
          Select a provider to auto-fill recommended API endpoints and model identifiers.
        </p>

        <!-- Preset Cards Grid -->
        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-2.5 mb-6">
          <button
            v-for="preset in AI_PRESETS"
            :key="preset.id"
            type="button"
            class="flex flex-col items-start p-3 rounded-container border text-left transition-all"
            :class="
              selectedPresetId === preset.id
                ? 'border-accent bg-accent-soft/40 ring-1 ring-accent text-text'
                : 'border-border bg-surface-2/40 hover:bg-surface-2 text-text-muted hover:text-text'
            "
            @click="selectAiPreset(preset)"
          >
            <span class="text-xs font-bold text-text">{{ preset.name }}</span>
            <span class="text-[10px] text-text-muted mt-1 leading-tight line-clamp-2">{{ preset.hint }}</span>
          </button>
        </div>

        <!-- Connection Settings Form -->
        <form class="space-y-4" @submit.prevent="handleSaveAi">
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField :label="t('settings.aiProvider.baseUrlLabel')" for-id="ai-base-url" required>
              <AppInput
                id="ai-base-url"
                v-model="aiForm.baseUrl"
                :placeholder="t('settings.aiProvider.baseUrlPlaceholder')"
                required
              />
            </FormField>

            <FormField :label="t('settings.aiProvider.modelLabel')" for-id="ai-model" required>
              <AppInput
                id="ai-model"
                v-model="aiForm.model"
                :placeholder="t('settings.aiProvider.modelPlaceholder')"
                required
              />
            </FormField>
          </div>

          <FormField :label="t('settings.aiProvider.apiKeyLabel')" for-id="ai-key">
            <div class="relative flex items-center">
              <AppInput
                id="ai-key"
                v-model="aiForm.apiKey"
                :type="showAiKey ? 'text' : 'password'"
                :placeholder="t('settings.aiProvider.apiKeyPlaceholder')"
                class="pr-10 w-full"
              />
              <button
                type="button"
                class="absolute right-2.5 p-1 rounded-md text-text-muted hover:text-text hover:bg-surface-2 transition-colors"
                @click="showAiKey = !showAiKey"
                :title="showAiKey ? 'Hide key' : 'Show key'"
              >
                <EyeOff v-if="showAiKey" :size="15" />
                <Eye v-else :size="15" />
              </button>
            </div>
            <p class="text-[11px] text-text-muted mt-1">
              {{ t('settings.aiProvider.apiKeyNote') }}
            </p>
          </FormField>

          <!-- Live Test Feedback Card -->
          <div
            v-if="testResult"
            class="p-3.5 rounded-control border text-xs flex items-start gap-2.5 transition-all"
            :class="
              testResult.success
                ? 'bg-positive-soft/60 border-positive/30 text-positive'
                : 'bg-negative-soft/60 border-negative/30 text-negative'
            "
          >
            <CheckCircle2 v-if="testResult.success" :size="16" class="shrink-0 mt-0.5" />
            <AlertCircle v-else :size="16" class="shrink-0 mt-0.5" />
            <div class="min-w-0 flex-1">
              <div class="font-bold">
                {{ testResult.success ? t('settings.aiProvider.testSuccess') : 'Connection Test Failed' }}
              </div>
              <div class="mt-0.5 opacity-90 break-words font-mono text-[11px]">
                {{ testResult.message }}
              </div>
            </div>
          </div>

          <!-- Buttons Bar -->
          <div class="flex flex-wrap items-center justify-between gap-3 pt-2 border-t border-border">
            <AppButton
              type="button"
              variant="secondary"
              size="sm"
              :loading="testingAi"
              @click="handleTestAi"
            >
              <Sparkles :size="14" />
              <span>{{ testingAi ? t('settings.aiProvider.testingConnection') : t('settings.aiProvider.testConnection') }}</span>
            </AppButton>

            <div class="flex items-center gap-2">
              <AppButton
                type="button"
                variant="secondary"
                size="sm"
                @click="handleResetAi"
              >
                {{ t('settings.aiProvider.resetSettings') }}
              </AppButton>
              <AppButton
                type="submit"
                variant="primary"
                size="sm"
              >
                <Save :size="14" />
                <span>{{ t('settings.aiProvider.saveSettings') }}</span>
              </AppButton>
            </div>
          </div>
        </form>
      </AppCard>

      <!-- Provider Guides & Capabilities -->
      <AppCard>
        <h3 class="text-sm font-bold text-text mb-3">
          {{ t('settings.aiProvider.helpTitle') }}
        </h3>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs text-text-muted">
          <div class="rounded-control border border-border bg-surface-2 p-3 space-y-1">
            <span class="font-bold text-text flex items-center gap-1.5">
              <span>OpenAI</span>
            </span>
            <p>{{ t('settings.aiProvider.helpOpenAi') }}</p>
          </div>
          <div class="rounded-control border border-border bg-surface-2 p-3 space-y-1">
            <span class="font-bold text-text flex items-center gap-1.5">
              <span>Groq</span>
              <span class="text-[10px] bg-positive-soft text-positive px-1.5 py-0.5 rounded font-semibold">Recommended</span>
            </span>
            <p>{{ t('settings.aiProvider.helpGroq') }}</p>
          </div>
          <div class="rounded-control border border-border bg-surface-2 p-3 space-y-1">
            <span class="font-bold text-text flex items-center gap-1.5">
              <span>OpenRouter</span>
            </span>
            <p>{{ t('settings.aiProvider.helpOpenRouter') }}</p>
          </div>
          <div class="rounded-control border border-border bg-surface-2 p-3 space-y-1">
            <span class="font-bold text-text flex items-center gap-1.5">
              <span>Ollama</span>
              <span class="text-[10px] bg-accent-soft text-accent px-1.5 py-0.5 rounded font-semibold">Offline & Private</span>
            </span>
            <p>{{ t('settings.aiProvider.helpOllama') }}</p>
          </div>
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

        <!-- Advanced Constraints Collapsible -->
        <div class="rounded-xl border border-border/70 bg-surface-2/40 p-3 space-y-3">
          <button
            type="button"
            class="w-full flex items-center justify-between text-xs font-bold text-text-muted hover:text-text transition-colors"
            @click="showAdvancedRuleFields = !showAdvancedRuleFields"
          >
            <div class="flex items-center gap-1.5">
              <Sliders :size="13" class="text-accent" />
              <span>{{ t('settings.rules.advancedScenarios') }}</span>
              <span
                v-if="ruleMinAmount || ruleMaxAmount || ruleAccountId || ruleTransactionType || ruleReplaceTitle"
                class="rounded-full bg-accent/20 px-1.5 py-0.2 text-[9px] font-bold text-accent"
              >
                {{ t('common.active') }}
              </span>
            </div>
            <span class="text-xs">{{ showAdvancedRuleFields ? '▲' : '▼' }}</span>
          </button>

          <div v-show="showAdvancedRuleFields" class="space-y-3 pt-2 border-t border-border/60">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <FormField :label="t('settings.rules.transactionTypeConstraint')">
                <AppSelect
                  v-model="ruleTransactionType"
                  :options="transactionTypeOptions"
                />
              </FormField>

              <FormField :label="t('settings.rules.accountConstraint')">
                <AppSelect
                  v-model="ruleAccountId"
                  :options="accountOptions"
                />
              </FormField>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <FormField :label="t('settings.rules.minAmountThreshold')">
                <input
                  v-model.number="ruleMinAmount"
                  type="number"
                  min="0"
                  placeholder="e.g. 100000"
                  class="h-10 w-full rounded-control border border-border bg-surface px-3 text-sm text-text shadow-xs focus:border-accent focus:outline-none tabular-nums"
                />
              </FormField>

              <FormField :label="t('settings.rules.maxAmountThreshold')">
                <input
                  v-model.number="ruleMaxAmount"
                  type="number"
                  min="0"
                  placeholder="e.g. 500000"
                  class="h-10 w-full rounded-control border border-border bg-surface px-3 text-sm text-text shadow-xs focus:border-accent focus:outline-none tabular-nums"
                />
              </FormField>
            </div>

            <FormField :label="t('settings.rules.replaceTitleTransform')">
              <AppInput
                v-model="ruleReplaceTitle"
                :placeholder="t('settings.rules.replaceTitlePlaceholder')"
              />
            </FormField>

            <!-- Group ID (AND-combined rules) -->
            <FormField :label="t('settings.rules.groupId')">
              <AppInput
                v-model="ruleGroupId"
                :placeholder="t('settings.rules.groupIdPlaceholder')"
              />
              <p class="text-[10px] text-text-muted mt-1">
                {{ t('settings.rules.groupIdHelp') }}
              </p>
            </FormField>

            <!-- Schedule Expression -->
            <FormField :label="t('settings.rules.scheduleExpression')">
              <AppInput
                v-model="ruleScheduleExpression"
                :placeholder="t('settings.rules.schedulePlaceholder')"
              />
              <div class="flex items-center gap-1.5 mt-1.5 flex-wrap">
                <span class="text-[10px] text-text-muted font-medium">{{ t('settings.presets') }}:</span>
                <button
                  type="button"
                  class="rounded bg-surface px-1.5 py-0.5 text-[10px] text-accent border border-border hover:border-accent font-mono"
                  @click="ruleScheduleExpression = 'WEEKDAYS'"
                >
                  WEEKDAYS
                </button>
                <button
                  type="button"
                  class="rounded bg-surface px-1.5 py-0.5 text-[10px] text-accent border border-border hover:border-accent font-mono"
                  @click="ruleScheduleExpression = 'WEEKENDS'"
                >
                  WEEKENDS
                </button>
                <button
                  type="button"
                  class="rounded bg-surface px-1.5 py-0.5 text-[10px] text-accent border border-border hover:border-accent font-mono"
                  @click="ruleScheduleExpression = 'DOM:25-31'"
                >
                  DOM:25-31
                </button>
              </div>
            </FormField>

            <!-- Tags -->
            <FormField :label="t('settings.rules.tags')">
              <AppInput
                v-model="ruleTags"
                :placeholder="t('settings.rules.tagsPlaceholder')"
              />
            </FormField>
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
