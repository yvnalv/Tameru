<script setup lang="ts">
import { onMounted, ref, reactive, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import {
  ChevronLeft,
  ChevronRight,
  Plus,
  RotateCcw,
  Copy,
  Layers,
  List,
  AlertTriangle,
  Flame,
  CheckCircle2,
} from 'lucide-vue-next';
import { getBudgetPeriod, createBudgetPeriod, upsertBudgetLines } from '@/lib/budgeting';
import { listCategories } from '@/lib/categories';
import { ApiClientError } from '@/lib/api';
import type { BudgetLine, BudgetPeriod, Category } from '@/types/api';
import { errorMessage } from '@/lib/errorMessage';
import { displayName } from '@/lib/seededNames';
import { useToastStore } from '@/stores/toast';
import AppCard from '@/components/ui/AppCard.vue';
import IconButton from '@/components/ui/IconButton.vue';
import LoadingBlock from '@/components/ui/LoadingBlock.vue';
import AppButton from '@/components/ui/AppButton.vue';
import MoneyInput from '@/components/ui/MoneyInput.vue';
import Money from '@/components/ui/Money.vue';

const { t, te, locale } = useI18n();
const toast = useToastStore();

const now = new Date();
const year = ref(now.getFullYear());
const month = ref(now.getMonth() + 1);

const period = ref<BudgetPeriod | null>(null);
const categories = ref<Category[]>([]);
const loading = ref(true);
const failed = ref(false);
const editing = ref(false);
const saving = ref(false);
const copying = ref(false);
const planDraft = reactive<Record<string, number>>({});

type RiskFilter = 'all' | 'over' | 'warning' | 'safe';
const riskFilter = ref<RiskFilter>('all');
const viewMode = ref<'grouped' | 'flat'>('grouped');

const monthLabel = computed(() =>
  new Date(year.value, month.value - 1, 1).toLocaleString(locale.value, {
    month: 'long',
    year: 'numeric',
  }),
);

const expenseCats = computed(() =>
  categories.value.filter(
    (c) => c.level === 'Category' && c.isActive && (c.flow === 'Any' || c.flow === 'Expense'),
  ),
);

// Pacing calculations
const daysInMonth = computed(() => new Date(year.value, month.value, 0).getDate());
const isCurrentMonth = computed(() => {
  const n = new Date();
  return year.value === n.getFullYear() && month.value === n.getMonth() + 1;
});
const currentDay = computed(() => {
  if (isCurrentMonth.value) {
    return Math.min(daysInMonth.value, new Date().getDate());
  }
  const periodDate = new Date(year.value, month.value - 1, 1);
  const nowDate = new Date(now.getFullYear(), now.getMonth(), 1);
  return periodDate < nowDate ? daysInMonth.value : 0;
});
const daysRemaining = computed(() => Math.max(1, daysInMonth.value - currentDay.value));
const monthPacingPercent = computed(() => Math.round((currentDay.value / daysInMonth.value) * 100));

const dailyAllowance = computed(() => {
  if (!period.value) return 0;
  return Math.round(period.value.totalLeftover / daysRemaining.value);
});

const spendPercent = computed(() => {
  if (!period.value || period.value.totalPlan <= 0) return 0;
  return Math.round((period.value.totalActual / period.value.totalPlan) * 100);
});

async function loadPeriod(): Promise<void> {
  loading.value = true;
  failed.value = false;
  editing.value = false;
  try {
    period.value = await getBudgetPeriod(year.value, month.value);
  } catch (error) {
    if (error instanceof ApiClientError && error.code === 'not_found') {
      period.value = null;
    } else {
      failed.value = true;
    }
  } finally {
    loading.value = false;
  }
}

function changeMonth(delta: number): void {
  const d = new Date(year.value, month.value - 1 + delta, 1);
  year.value = d.getFullYear();
  month.value = d.getMonth() + 1;
  loadPeriod();
}

function barMax(plan: number, actual: number): number {
  return Math.max(plan, actual, 1);
}
function greenWidth(plan: number, actual: number): number {
  return (Math.min(actual, plan) / barMax(plan, actual)) * 100;
}
function redWidth(plan: number, actual: number): number {
  return (Math.max(0, actual - plan) / barMax(plan, actual)) * 100;
}
function usedPct(plan: number, actual: number): number | null {
  return plan > 0 ? Math.round((actual / plan) * 100) : null;
}

function categoryName(id: string, fallback: string | null): string {
  const c = categories.value.find((x) => x.id === id);
  return displayName(c?.name ?? fallback, locale.value);
}

function getLineRisk(line: BudgetLine): 'over' | 'warning' | 'safe' {
  if (line.actual > line.plan) return 'over';
  if (line.plan > 0 && line.actual / line.plan >= 0.8) return 'warning';
  return 'safe';
}

const riskCounts = computed(() => {
  let over = 0;
  let warning = 0;
  let safe = 0;
  for (const line of period.value?.lines ?? []) {
    const r = getLineRisk(line);
    if (r === 'over') over++;
    else if (r === 'warning') warning++;
    else safe++;
  }
  return { all: period.value?.lines.length ?? 0, over, warning, safe };
});

function lineMatchesFilter(line: BudgetLine): boolean {
  if (riskFilter.value === 'all') return true;
  return getLineRisk(line) === riskFilter.value;
}

// Group lines by parent envelope (Needs, Wants, Investment, Other)
interface EnvelopeGroup {
  id: string;
  name: string;
  totalPlan: number;
  totalActual: number;
  totalLeftover: number;
  lines: BudgetLine[];
}

const envelopeGroups = computed<EnvelopeGroup[]>(() => {
  if (!period.value) return [];
  const map = new Map<string, EnvelopeGroup>();

  const budgetEnvelopes = categories.value.filter((c) => c.level === 'Budget');

  for (const env of budgetEnvelopes) {
    map.set(env.id, {
      id: env.id,
      name: displayName(env.name, locale.value),
      totalPlan: 0,
      totalActual: 0,
      totalLeftover: 0,
      lines: [],
    });
  }
  map.set('other', {
    id: 'other',
    name: t('dashboard.others'),
    totalPlan: 0,
    totalActual: 0,
    totalLeftover: 0,
    lines: [],
  });

  for (const line of period.value.lines) {
    if (!lineMatchesFilter(line)) continue;
    const cat = categories.value.find((c) => c.id === line.categoryId);
    const parentId = cat?.parentId || 'other';
    const group = map.get(parentId) ?? map.get('other')!;

    group.lines.push(line);
    group.totalPlan += line.plan;
    group.totalActual += line.actual;
    group.totalLeftover += line.leftover;
  }

  return Array.from(map.values()).filter((g) => g.lines.length > 0);
});

const filteredLines = computed(() => {
  if (!period.value) return [];
  return period.value.lines.filter(lineMatchesFilter);
});

const totalDraftPlan = computed(() => {
  let sum = 0;
  for (const c of expenseCats.value) {
    sum += Number(planDraft[c.id] || 0);
  }
  return sum;
});

async function createPeriod(): Promise<void> {
  try {
    period.value = await createBudgetPeriod(year.value, month.value);
    startEdit();
  } catch (error) {
    toast.error(errorMessage(t, te, error));
  }
}

function startEdit(): void {
  for (const c of expenseCats.value) planDraft[c.id] = 0;
  for (const line of period.value?.lines ?? []) planDraft[line.categoryId] = line.plan;
  editing.value = true;
}

async function copyFromLastMonth(): Promise<void> {
  copying.value = true;
  const d = new Date(year.value, month.value - 2, 1);
  const prevYear = d.getFullYear();
  const prevMonth = d.getMonth() + 1;
  try {
    const prev = await getBudgetPeriod(prevYear, prevMonth);
    if (!prev || !prev.lines.length) {
      toast.error(t('budget.noPreviousBudget'));
      return;
    }
    for (const line of prev.lines) {
      planDraft[line.categoryId] = line.plan;
    }
    toast.success(t('budget.copySuccess'));
  } catch {
    toast.error(t('budget.noPreviousBudget'));
  } finally {
    copying.value = false;
  }
}

function resetAllDrafts(): void {
  for (const c of expenseCats.value) {
    planDraft[c.id] = 0;
  }
}

async function savePlans(): Promise<void> {
  saving.value = true;
  try {
    if (!period.value) period.value = await createBudgetPeriod(year.value, month.value);
    const lines = expenseCats.value.map((c) => ({
      categoryId: c.id,
      planAmount: Number(planDraft[c.id] || 0),
    }));
    period.value = await upsertBudgetLines(period.value.id, lines);
    editing.value = false;
    toast.success(t('common.done'));
  } catch (error) {
    toast.error(errorMessage(t, te, error));
  } finally {
    saving.value = false;
  }
}

onMounted(async () => {
  categories.value = await listCategories({ includeInactive: false });
  await loadPeriod();
});
</script>

<template>
  <div class="space-y-4">
    <!-- Month Navigation & View Actions -->
    <div class="flex flex-wrap items-center justify-between gap-3">
      <!-- Month switcher -->
      <div class="flex items-center gap-2">
        <IconButton
          :icon="ChevronLeft"
          :label="t('budget.previousMonth')"
          :size="16"
          @click="changeMonth(-1)"
        />
        <span class="min-w-[9rem] text-center text-sm font-semibold">{{ monthLabel }}</span>
        <IconButton
          :icon="ChevronRight"
          :label="t('budget.nextMonth')"
          :size="16"
          @click="changeMonth(1)"
        />
      </div>

      <!-- View layout toggle (Grouped vs Flat) when viewing -->
      <div v-if="period && !editing" class="flex items-center gap-2">
        <div class="inline-flex rounded-xl bg-surface border border-border p-1 shadow-sm">
          <button
            type="button"
            class="flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-semibold transition-all"
            :class="
              viewMode === 'grouped'
                ? 'bg-accent text-accent-contrast shadow-sm'
                : 'text-text-muted hover:text-text'
            "
            @click="viewMode = 'grouped'"
          >
            <Layers :size="14" />
            <span class="hidden sm:inline">{{ t('budget.viewModeGrouped') }}</span>
          </button>
          <button
            type="button"
            class="flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-semibold transition-all"
            :class="
              viewMode === 'flat'
                ? 'bg-accent text-accent-contrast shadow-sm'
                : 'text-text-muted hover:text-text'
            "
            @click="viewMode = 'flat'"
          >
            <List :size="14" />
            <span class="hidden sm:inline">{{ t('budget.viewModeFlat') }}</span>
          </button>
        </div>
      </div>
    </div>

    <LoadingBlock v-if="loading" />
    <div v-else-if="failed" class="py-16 text-center">
      <p class="text-sm text-text-muted">{{ t('errors.network_error') }}</p>
      <AppButton class="mt-4" variant="secondary" @click="loadPeriod">
        {{ t('common.retry') }}
      </AppButton>
    </div>

    <!-- Empty period -->
    <AppCard v-else-if="!period">
      <div class="py-12 text-center">
        <p class="text-sm text-text-muted">{{ t('budget.noPeriod') }}</p>
        <AppButton class="mt-4" @click="createPeriod">
          <Plus :size="16" />{{ t('budget.createPeriod') }}
        </AppButton>
      </div>
    </AppCard>

    <template v-else>
      <!-- Decision Support KPI & Pacing Strip -->
      <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <!-- Plan Card -->
        <AppCard>
          <p class="text-[13px] text-text-muted">{{ t('budget.plan') }}</p>
          <p class="mt-1 text-xl font-semibold tnum">
            <Money :value="period.totalPlan" />
          </p>
          <p class="mt-1 text-xs text-text-muted">
            {{ t('budget.category') }}: {{ period.lines.length }}
          </p>
        </AppCard>

        <!-- Actual Card with Pacing -->
        <AppCard>
          <div class="flex items-center justify-between">
            <p class="text-[13px] text-text-muted">{{ t('budget.actual') }}</p>
            <span
              v-if="period.totalPlan > 0"
              class="rounded-full px-2 py-0.5 text-xs font-semibold tnum"
              :class="
                spendPercent > monthPacingPercent
                  ? 'bg-negative/10 text-negative'
                  : 'bg-accent-soft text-accent'
              "
            >
              {{ spendPercent }}%
            </span>
          </div>
          <p class="mt-1 text-xl font-semibold tnum">
            <Money :value="period.totalActual" />
          </p>
          <p class="mt-1 text-xs text-text-muted truncate">
            {{
              t('budget.daysElapsed', {
                day: currentDay,
                total: daysInMonth,
                percent: monthPacingPercent,
              })
            }}
          </p>
        </AppCard>

        <!-- Leftover Card -->
        <AppCard>
          <p class="text-[13px] text-text-muted">{{ t('budget.leftover') }}</p>
          <p class="mt-1 text-xl font-semibold tnum">
            <Money :value="period.totalLeftover" colored />
          </p>
          <p
            class="mt-1 text-xs font-medium"
            :class="period.totalLeftover < 0 ? 'text-negative' : 'text-accent'"
          >
            {{
              period.totalLeftover < 0
                ? t('budget.filterOver')
                : t('budget.daysRemaining', { days: daysRemaining })
            }}
          </p>
        </AppCard>

        <!-- Daily Burn Allowance Card -->
        <AppCard>
          <p class="text-[13px] text-text-muted">{{ t('budget.dailyAllowance') }}</p>
          <p class="mt-1 text-xl font-semibold tnum">
            <Money :value="dailyAllowance" :colored="dailyAllowance >= 0" />
            <span class="text-xs font-normal text-text-muted"> / {{ t('reports.daily').toLowerCase() }}</span>
          </p>
          <p class="mt-1 text-xs text-text-muted">
            {{ t('budget.daysRemaining', { days: daysRemaining }) }}
          </p>
        </AppCard>
      </div>

      <!-- Main Budget Card -->
      <AppCard :padded="false">
        <div class="flex flex-wrap items-center justify-between gap-3 border-b border-border px-5 py-3.5">
          <!-- View Mode: Risk Filter Chips -->
          <div v-if="!editing" class="flex flex-wrap items-center gap-1.5">
            <button
              type="button"
              class="rounded-lg border px-3 py-1 text-xs font-semibold transition-all"
              :class="
                riskFilter === 'all'
                  ? 'border-accent bg-accent text-accent-contrast shadow-sm'
                  : 'border-border bg-surface text-text-muted hover:border-border-strong hover:text-text'
              "
              @click="riskFilter = 'all'"
            >
              {{ t('budget.filterAll') }} ({{ riskCounts.all }})
            </button>
            <button
              v-if="riskCounts.over > 0"
              type="button"
              class="flex items-center gap-1.5 rounded-lg border px-3 py-1 text-xs font-semibold transition-all"
              :class="
                riskFilter === 'over'
                  ? 'border-negative bg-negative text-negative-contrast shadow-sm'
                  : 'border-negative/30 bg-negative-soft text-negative hover:border-negative'
              "
              @click="riskFilter = 'over'"
            >
              <AlertTriangle :size="13" />
              <span>{{ t('budget.filterOver') }} ({{ riskCounts.over }})</span>
            </button>
            <button
              v-if="riskCounts.warning > 0"
              type="button"
              class="flex items-center gap-1.5 rounded-lg border px-3 py-1 text-xs font-semibold transition-all"
              :class="
                riskFilter === 'warning'
                  ? 'border-warning bg-warning text-warning-contrast shadow-sm'
                  : 'border-warning/30 bg-warning-soft text-warning hover:border-warning'
              "
              @click="riskFilter = 'warning'"
            >
              <Flame :size="13" />
              <span>{{ t('budget.filterWarning') }} ({{ riskCounts.warning }})</span>
            </button>
            <button
              v-if="riskCounts.safe > 0"
              type="button"
              class="flex items-center gap-1.5 rounded-lg border px-3 py-1 text-xs font-semibold transition-all"
              :class="
                riskFilter === 'safe'
                  ? 'border-positive bg-positive text-positive-contrast shadow-sm'
                  : 'border-positive/30 bg-positive-soft text-positive hover:border-positive'
              "
              @click="riskFilter = 'safe'"
            >
              <CheckCircle2 :size="13" />
              <span>{{ t('budget.filterSafe') }} ({{ riskCounts.safe }})</span>
            </button>
          </div>

          <!-- Edit Mode: Tools Bar (Copy from Last Month & Reset) -->
          <div v-else class="flex flex-wrap items-center gap-2">
            <AppButton
              variant="secondary"
              size="sm"
              :loading="copying"
              @click="copyFromLastMonth"
            >
              <Copy :size="14" />
              <span>{{ t('budget.copyLastMonth') }}</span>
            </AppButton>
            <AppButton variant="secondary" size="sm" @click="resetAllDrafts">
              <RotateCcw :size="14" />
              <span>{{ t('budget.resetAll') }}</span>
            </AppButton>
          </div>

          <!-- Edit / Save Buttons -->
          <div class="flex items-center gap-2 ml-auto">
            <AppButton v-if="!editing" variant="secondary" @click="startEdit">
              {{ t('budget.editPlans') }}
            </AppButton>
            <template v-else>
              <AppButton variant="secondary" @click="editing = false">
                {{ t('common.cancel') }}
              </AppButton>
              <AppButton :loading="saving" @click="savePlans">
                {{ saving ? t('common.saving') : t('budget.savePlans') }}
              </AppButton>
            </template>
          </div>
        </div>

        <!-- View Mode: Grouped by Envelope -->
        <div v-if="!editing && viewMode === 'grouped'" class="divide-y divide-border">
          <div v-for="group in envelopeGroups" :key="group.id" class="p-5 space-y-4">
            <!-- Envelope Header with Subtotal & Envelope Spend Bar -->
            <div class="flex flex-wrap items-center justify-between gap-2">
              <div>
                <span class="text-sm font-semibold uppercase tracking-wider text-text">
                  {{ group.name }}
                </span>
                <span class="ml-2 text-xs text-text-muted">
                  ({{ group.lines.length }} {{ t('budget.category').toLowerCase() }})
                </span>
              </div>
              <div class="flex items-center gap-3 tnum text-xs">
                <span class="text-text-muted">{{ t('budget.envelopeSubtotal') }}:</span>
                <span class="font-medium text-text"><Money :value="group.totalActual" /></span>
                <span class="text-text-muted">/</span>
                <span class="text-text-muted"><Money :value="group.totalPlan" /></span>
                <span
                  class="font-semibold ml-1"
                  :class="group.totalActual > group.totalPlan ? 'text-negative' : 'text-accent'"
                >
                  <Money :value="group.totalLeftover" colored />
                </span>
              </div>
            </div>

            <!-- Envelope Overall Progress Bar -->
            <div class="relative h-2.5 w-full overflow-hidden rounded-full bg-surface-3">
              <div
                class="absolute inset-y-0 left-0 bg-positive transition-all duration-300"
                :style="{ width: `${greenWidth(group.totalPlan, group.totalActual)}%` }"
              />
              <div
                class="absolute inset-y-0 bg-negative transition-all duration-300"
                :style="{
                  left: `${greenWidth(group.totalPlan, group.totalActual)}%`,
                  width: `${redWidth(group.totalPlan, group.totalActual)}%`,
                }"
              />
            </div>

            <!-- Category Lines inside Envelope -->
            <div class="space-y-3.5 pt-1 pl-1 sm:pl-3">
              <div v-for="line in group.lines" :key="line.categoryId" class="space-y-1.5">
                <div class="flex items-center justify-between gap-3 text-sm">
                  <div class="flex items-center gap-2 truncate">
                    <span class="truncate font-medium text-text">
                      {{ categoryName(line.categoryId, line.categoryName) }}
                    </span>
                    <span
                      v-if="line.actual > line.plan"
                      class="rounded-md bg-negative-soft border border-negative/20 px-2 py-0.5 text-[10px] font-semibold text-negative"
                    >
                      {{ t('budget.filterOver') }}
                    </span>
                  </div>
                  <div class="tnum flex shrink-0 items-center gap-1.5 text-xs">
                    <span :class="line.actual > line.plan ? 'text-negative font-semibold' : 'text-text font-medium'">
                      <Money :value="line.actual" />
                    </span>
                    <span class="text-text-muted">/</span>
                    <Money :value="line.plan" class="text-text-muted font-medium" />
                    <span
                      v-if="usedPct(line.plan, line.actual) !== null"
                      class="ml-1 font-semibold"
                      :class="line.actual > line.plan ? 'text-negative' : 'text-text-muted'"
                    >
                      {{ usedPct(line.plan, line.actual) }}%
                    </span>
                  </div>
                </div>

                <div class="relative h-2 w-full overflow-hidden rounded-full bg-surface-3">
                  <div
                    class="absolute inset-y-0 left-0 bg-positive transition-all duration-300"
                    :style="{ width: `${greenWidth(line.plan, line.actual)}%` }"
                  />
                  <div
                    class="absolute inset-y-0 bg-negative transition-all duration-300"
                    :style="{
                      left: `${greenWidth(line.plan, line.actual)}%`,
                      width: `${redWidth(line.plan, line.actual)}%`,
                    }"
                  />
                </div>

                <div class="flex justify-between text-[11px] text-text-muted">
                  <span>
                    {{
                      line.plan > 0 && line.actual < line.plan
                        ? `${Math.round(line.plan - line.actual)} sisa`
                        : ''
                    }}
                  </span>
                  <span>
                    {{ t('budget.leftover') }}:
                    <Money :value="line.leftover" colored class="ml-1 font-medium" />
                  </span>
                </div>
              </div>
            </div>
          </div>
          <p
            v-if="!envelopeGroups.length"
            class="py-12 text-center text-sm text-text-muted"
          >
            {{ t('budget.empty') }}
          </p>
        </div>

        <!-- View Mode: Flat List -->
        <div v-else-if="!editing && viewMode === 'flat'" class="space-y-4 p-5">
          <div v-for="line in filteredLines" :key="line.categoryId" class="space-y-1.5">
            <div class="flex items-center justify-between gap-3 text-sm">
              <div class="flex items-center gap-2 truncate">
                <span class="truncate font-medium">
                  {{ categoryName(line.categoryId, line.categoryName) }}
                </span>
                <span
                  v-if="line.actual > line.plan"
                  class="rounded bg-negative/15 px-1.5 py-0.5 text-[10px] font-semibold text-negative"
                >
                  {{ t('budget.filterOver') }}
                </span>
              </div>
              <div class="tnum flex shrink-0 items-center gap-1.5 text-[13px]">
                <span :class="line.actual > line.plan ? 'text-negative font-semibold' : 'text-text'">
                  <Money :value="line.actual" />
                </span>
                <span class="text-text-muted">/</span>
                <Money :value="line.plan" class="text-text-muted" />
                <span
                  v-if="usedPct(line.plan, line.actual) !== null"
                  class="ml-1 font-semibold"
                  :class="line.actual > line.plan ? 'text-negative' : 'text-text-muted'"
                >
                  {{ usedPct(line.plan, line.actual) }}%
                </span>
              </div>
            </div>

            <div class="relative h-2.5 w-full overflow-hidden rounded-full bg-surface-3">
              <div
                class="absolute inset-y-0 left-0 bg-positive transition-all duration-300"
                :style="{ width: `${greenWidth(line.plan, line.actual)}%` }"
              />
              <div
                class="absolute inset-y-0 bg-negative transition-all duration-300"
                :style="{
                  left: `${greenWidth(line.plan, line.actual)}%`,
                  width: `${redWidth(line.plan, line.actual)}%`,
                }"
              />
            </div>

            <div class="flex justify-end text-[11px] text-text-muted">
              {{ t('budget.leftover') }}: <Money :value="line.leftover" colored class="ml-1 font-medium" />
            </div>
          </div>
          <p v-if="!filteredLines.length" class="py-8 text-center text-sm text-text-muted">
            {{ t('budget.empty') }}
          </p>
        </div>

        <!-- Edit Mode: High-speed Form with MoneyInput -->
        <div v-else class="p-5 space-y-4">
          <!-- Sticky Live Summary Banner -->
          <div class="flex flex-wrap items-center justify-between gap-3 rounded-control border border-border bg-surface-2 p-3.5">
            <div>
              <p class="text-xs text-text-muted">{{ t('budget.draftTotal') }}</p>
              <p class="text-lg font-bold text-accent tnum">
                <Money :value="totalDraftPlan" />
              </p>
            </div>
            <div class="text-right text-xs text-text-muted">
              <p>{{ t('budget.category') }}: {{ expenseCats.length }}</p>
              <p v-if="period.totalPlan > 0">
                vs Sebelumnya: <Money :value="period.totalPlan" />
              </p>
            </div>
          </div>

          <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <div
              v-for="c in expenseCats"
              :key="c.id"
              class="rounded-control border border-border bg-surface p-3 space-y-1.5"
            >
              <div class="flex items-center justify-between">
                <label :for="'plan-' + c.id" class="text-xs font-semibold truncate">
                  {{ displayName(c.name, locale) }}
                </label>
                <span v-if="period?.lines.find(l => l.categoryId === c.id)" class="text-[10px] text-text-muted tnum">
                  Aktual: <Money :value="period.lines.find(l => l.categoryId === c.id)?.actual ?? 0" />
                </span>
              </div>
              <MoneyInput
                :id="'plan-' + c.id"
                v-model="planDraft[c.id]"
                :show-chips="false"
                placeholder="0 (mis. 500k, 1.5jt)"
              />
            </div>
          </div>
        </div>
      </AppCard>
    </template>
  </div>
</template>
