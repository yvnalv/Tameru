<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import {
  Sparkles,
  ChevronLeft,
  ChevronRight,
  Gauge,
  PiggyBank,
  CheckCircle2,
  AlertTriangle,
  Layers,
  ShieldCheck,
} from 'lucide-vue-next';
import { getDeepAnalysis, getRecommendations } from '@/lib/insights';
import { listCategories } from '@/lib/categories';
import type { DeepAnalysisDto, RecommendationDto, Category } from '@/types/api';
import { displayName } from '@/lib/seededNames';
import AppCard from '@/components/ui/AppCard.vue';
import AppButton from '@/components/ui/AppButton.vue';
import Money from '@/components/ui/Money.vue';
import LoadingBlock from '@/components/ui/LoadingBlock.vue';
import SpendBar from '@/components/ui/SpendBar.vue';
import WeekdayVelocityChart from '@/components/insights/WeekdayVelocityChart.vue';
import RecommendationCard from '@/components/insights/RecommendationCard.vue';

type Tab = 'overview' | 'expense' | 'income';

const { locale } = useI18n();

const activeTab = ref<Tab>('overview');
const loading = ref(true);
const failed = ref(false);

const now = new Date();
const currentYear = ref(now.getFullYear());
const currentMonth = ref(now.getMonth() + 1);

const analysis = ref<DeepAnalysisDto | null>(null);
const recommendations = ref<RecommendationDto[]>([]);
const categories = ref<Category[]>([]);

const monthLabel = computed(() => {
  const d = new Date(currentYear.value, currentMonth.value - 1, 1);
  return d.toLocaleDateString(locale.value, { month: 'long', year: 'numeric' });
});

const catName = (id: string | null) =>
  id ? displayName(categories.value.find((c) => c.id === id)?.name ?? null, locale.value) || id.slice(0, 6) : '—';

async function fetchData(): Promise<void> {
  loading.value = true;
  failed.value = false;
  try {
    const [anRes, recRes, catList] = await Promise.all([
      getDeepAnalysis(currentYear.value, currentMonth.value),
      getRecommendations(currentYear.value, currentMonth.value),
      listCategories({ includeInactive: true }),
    ]);
    analysis.value = anRes;
    recommendations.value = recRes;
    categories.value = catList;
  } catch {
    failed.value = true;
  } finally {
    loading.value = false;
  }
}

function prevMonth(): void {
  if (currentMonth.value === 1) {
    currentYear.value -= 1;
    currentMonth.value = 12;
  } else {
    currentMonth.value -= 1;
  }
  void fetchData();
}

function nextMonth(): void {
  if (currentMonth.value === 12) {
    currentYear.value += 1;
    currentMonth.value = 1;
  } else {
    currentMonth.value += 1;
  }
  void fetchData();
}

onMounted(() => {
  void fetchData();
});
</script>

<template>
  <div class="space-y-6">
    <!-- Header: Title, Month Selector & Tab Switcher -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <div class="flex items-center gap-2">
          <div class="flex h-7 w-7 items-center justify-center rounded-control bg-accent/15 text-accent">
            <Sparkles :size="16" />
          </div>
          <h2 class="text-xl font-bold tracking-tight text-text">
            {{ $t('insights.title') }}
          </h2>
        </div>
        <p class="mt-0.5 text-xs text-text-muted">
          {{ $t('insights.subtitle') }}
        </p>
      </div>

      <!-- Month Selector Controls -->
      <div class="flex items-center gap-2">
        <div class="flex items-center rounded-control border border-border bg-surface p-1 shadow-xs">
          <button
            type="button"
            class="rounded-control p-1 text-text-muted hover:bg-surface-2 hover:text-text transition-colors"
            :title="$t('common.prev')"
            @click="prevMonth"
          >
            <ChevronLeft :size="16" />
          </button>
          <span class="px-3 text-xs font-semibold text-text min-w-[120px] text-center">
            {{ monthLabel }}
          </span>
          <button
            type="button"
            class="rounded-control p-1 text-text-muted hover:bg-surface-2 hover:text-text transition-colors"
            :title="$t('common.next')"
            @click="nextMonth"
          >
            <ChevronRight :size="16" />
          </button>
        </div>

        <!-- Segmented Tab Switcher -->
        <div class="inline-flex rounded-control border border-border bg-surface p-1 shadow-xs">
          <button
            type="button"
            class="rounded-control px-3 py-1 text-xs font-semibold transition-all"
            :class="activeTab === 'overview' ? 'bg-accent text-accent-contrast shadow-xs' : 'text-text-muted hover:text-text'"
            @click="activeTab = 'overview'"
          >
            {{ $t('insights.tabOverview') }}
          </button>
          <button
            type="button"
            class="rounded-control px-3 py-1 text-xs font-semibold transition-all"
            :class="activeTab === 'expense' ? 'bg-accent text-accent-contrast shadow-xs' : 'text-text-muted hover:text-text'"
            @click="activeTab = 'expense'"
          >
            {{ $t('insights.tabExpense') }}
          </button>
          <button
            type="button"
            class="rounded-control px-3 py-1 text-xs font-semibold transition-all"
            :class="activeTab === 'income' ? 'bg-accent text-accent-contrast shadow-xs' : 'text-text-muted hover:text-text'"
            @click="activeTab = 'income'"
          >
            {{ $t('insights.tabIncome') }}
          </button>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <LoadingBlock v-if="loading" :message="$t('insights.loading')" class="h-64" />

    <!-- Error State -->
    <div v-else-if="failed" class="rounded-card border border-negative/30 bg-negative/5 p-8 text-center text-sm text-negative">
      <AlertTriangle :size="24" class="mx-auto mb-2" />
      <p class="font-semibold">{{ $t('insights.loadFailed') }}</p>
      <AppButton variant="secondary" size="sm" class="mt-4" @click="fetchData">
        {{ $t('common.retry') }}
      </AppButton>
    </div>

    <!-- Active View Content -->
    <div v-else-if="analysis" class="space-y-6">
      <!-- KPI Executive Strip -->
      <div class="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <!-- Retention Rate Tile -->
        <div class="rounded-card border border-border bg-surface p-4 shadow-xs">
          <div class="flex items-center justify-between text-xs text-text-muted font-medium">
            <span>{{ $t('insights.retentionRate') }}</span>
            <PiggyBank :size="15" class="text-accent" />
          </div>
          <div class="mt-2 text-xl font-bold tracking-tight text-text tnum">
            {{ analysis.income.retentionRate }}%
          </div>
          <div class="mt-1 text-[11px] text-text-muted flex items-center gap-1">
            <span :class="analysis.income.retentionRate >= 20 ? 'text-accent font-semibold' : 'text-amber-400'">
              {{ analysis.income.retentionRate >= 20 ? $t('insights.retentionHealthy') : $t('insights.retentionBelowTarget') }}
            </span>
            <span class="opacity-60">· Target 20%</span>
          </div>
        </div>

        <!-- Weekend Burn Pace Tile -->
        <div class="rounded-card border border-border bg-surface p-4 shadow-xs">
          <div class="flex items-center justify-between text-xs text-text-muted font-medium">
            <span>{{ $t('insights.weekendVelocity') }}</span>
            <Gauge :size="15" class="text-accent" />
          </div>
          <div class="mt-2 text-xl font-bold tracking-tight text-text tnum">
            {{ analysis.expense.weekdayVsWeekend.weekendVelocityRatio }}×
          </div>
          <div class="mt-1 text-[11px] text-text-muted">
            <span>vs. Weekday burn pace</span>
          </div>
        </div>

        <!-- Fixed Cost Ratio Tile -->
        <div class="rounded-card border border-border bg-surface p-4 shadow-xs">
          <div class="flex items-center justify-between text-xs text-text-muted font-medium">
            <span>{{ $t('insights.fixedCostBurden') }}</span>
            <Layers :size="15" class="text-accent" />
          </div>
          <div class="mt-2 text-xl font-bold tracking-tight text-text tnum">
            {{ analysis.expense.fixedVsVariable.fixedPercentage }}%
          </div>
          <div class="mt-1 text-[11px] text-text-muted">
            <span>{{ $t('insights.committedNeeds') }}</span>
          </div>
        </div>

        <!-- Stability Score Tile -->
        <div class="rounded-card border border-border bg-surface p-4 shadow-xs">
          <div class="flex items-center justify-between text-xs text-text-muted font-medium">
            <span>{{ $t('insights.incomeStability') }}</span>
            <ShieldCheck :size="15" class="text-accent" />
          </div>
          <div class="mt-2 text-xl font-bold tracking-tight text-text tnum">
            {{ analysis.income.stabilityScore }} / 100
          </div>
          <div class="mt-1 text-[11px] text-text-muted">
            <span>{{ analysis.income.stabilityScore >= 80 ? $t('insights.stabilityHigh') : $t('insights.stabilityModerate') }}</span>
          </div>
        </div>
      </div>

      <!-- TAB 1: OVERVIEW & ACTIONABLE RECOMMENDATIONS -->
      <div v-if="activeTab === 'overview'" class="space-y-6">
        <div>
          <div class="flex items-center justify-between mb-3">
            <h3 class="text-sm font-bold uppercase tracking-wider text-text-muted">
              {{ $t('insights.actionableRecommendations') }} ({{ recommendations.length }})
            </h3>
            <span class="text-xs text-text-muted">
              {{ $t('insights.ruleBasedAdvice') }}
            </span>
          </div>

          <div v-if="recommendations.length" class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <RecommendationCard
              v-for="rec in recommendations"
              :key="rec.id"
              :recommendation="rec"
            />
          </div>

          <div
            v-else
            class="rounded-card border border-accent/30 bg-accent/5 p-8 text-center"
          >
            <CheckCircle2 :size="28" class="mx-auto mb-2 text-accent" />
            <h4 class="text-sm font-bold text-text">{{ $t('insights.allClearTitle') }}</h4>
            <p class="mt-1 text-xs text-text-muted max-w-md mx-auto">
              {{ $t('insights.allClearMessage') }}
            </p>
          </div>
        </div>

        <!-- Side-by-side Highlights -->
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <!-- Weekend vs Weekday Card Preview -->
          <AppCard>
            <template #title>
              <div class="flex items-center justify-between">
                <span>{{ $t('insights.burnVelocityHeading') }}</span>
                <span class="text-xs font-semibold text-accent">
                  {{ analysis.expense.weekdayVsWeekend.weekendVelocityRatio }}× Weekend Ratio
                </span>
              </div>
            </template>
            <div class="pt-2">
              <WeekdayVelocityChart :data="analysis.expense.weekdayVsWeekend.dayOfWeekBreakdown" />
            </div>
          </AppCard>

          <!-- Top Payees Preview -->
          <AppCard>
            <template #title>
              <span>{{ $t('insights.topPayeesHeading') }}</span>
            </template>
            <div class="space-y-3 pt-2">
              <div
                v-for="payee in analysis.expense.topPayees"
                :key="payee.payee"
                class="flex items-center justify-between rounded-control border border-border/60 bg-surface-2/40 px-3 py-2.5"
              >
                <div>
                  <div class="text-xs font-bold text-text">{{ payee.payee }}</div>
                  <div class="text-[11px] text-text-muted">{{ payee.transactionCount }} transactions</div>
                </div>
                <div class="text-right">
                  <div class="text-xs font-bold text-text tnum">
                    <Money :value="payee.amount" />
                  </div>
                  <div class="text-[10px] font-semibold text-accent tnum">{{ payee.percentage }}%</div>
                </div>
              </div>
            </div>
          </AppCard>
        </div>
      </div>

      <!-- TAB 2: EXPENSE DIAGNOSTICS -->
      <div v-else-if="activeTab === 'expense'" class="space-y-6">
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <!-- Weekday vs Weekend Spending Details -->
          <AppCard>
            <template #title>
              <div class="flex items-center justify-between">
                <span>{{ $t('insights.burnVelocityHeading') }}</span>
                <span class="text-xs font-semibold text-accent">
                  {{ analysis.expense.weekdayVsWeekend.weekendVelocityRatio }}× Weekend Pace
                </span>
              </div>
            </template>
            <p class="text-xs text-text-muted mb-4">
              {{ $t('insights.burnVelocityExplainer') }}
            </p>
            <WeekdayVelocityChart :data="analysis.expense.weekdayVsWeekend.dayOfWeekBreakdown" />

            <div class="mt-4 grid grid-cols-2 gap-3 border-t border-border pt-4">
              <div class="rounded-control bg-surface-2 p-2.5">
                <span class="text-[10px] uppercase font-bold text-text-muted">{{ $t('insights.weekdayDailyAvg') }}</span>
                <div class="text-sm font-bold text-text tnum mt-0.5">
                  <Money :value="analysis.expense.weekdayVsWeekend.weekdayDailyAverage" />
                </div>
              </div>
              <div class="rounded-control bg-surface-2 p-2.5">
                <span class="text-[10px] uppercase font-bold text-accent">{{ $t('insights.weekendDailyAvg') }}</span>
                <div class="text-sm font-bold text-accent tnum mt-0.5">
                  <Money :value="analysis.expense.weekdayVsWeekend.weekendDailyAverage" />
                </div>
              </div>
            </div>
          </AppCard>

          <!-- Fixed vs Variable Breakdown -->
          <AppCard>
            <template #title>
              <span>{{ $t('insights.fixedVsVariableHeading') }}</span>
            </template>
            <p class="text-xs text-text-muted mb-4">
              {{ $t('insights.fixedVsVariableExplainer') }}
            </p>

            <div class="space-y-4">
              <!-- SpendBar comparing fixed vs variable -->
              <SpendBar
                :segments="[
                  { label: $t('insights.fixedCommitted'), value: analysis.expense.fixedVsVariable.fixedAmount },
                  { label: $t('insights.variableDiscretionary'), value: analysis.expense.fixedVsVariable.variableAmount },
                ]"
              />

              <div class="grid grid-cols-2 gap-3 pt-2">
                <div class="rounded-control border border-border p-3">
                  <div class="text-[11px] font-semibold text-text-muted">{{ $t('insights.fixedCommitted') }}</div>
                  <div class="text-base font-bold text-text tnum mt-1">
                    <Money :value="analysis.expense.fixedVsVariable.fixedAmount" />
                  </div>
                  <span class="text-xs font-semibold text-text-muted tnum">{{ analysis.expense.fixedVsVariable.fixedPercentage }}%</span>
                </div>
                <div class="rounded-control border border-border p-3">
                  <div class="text-[11px] font-semibold text-accent">{{ $t('insights.variableDiscretionary') }}</div>
                  <div class="text-base font-bold text-accent tnum mt-1">
                    <Money :value="analysis.expense.fixedVsVariable.variableAmount" />
                  </div>
                  <span class="text-xs font-semibold text-accent tnum">{{ analysis.expense.fixedVsVariable.variablePercentage }}%</span>
                </div>
              </div>
            </div>

            <!-- Category Momentum & Spending Surge -->
            <div class="mt-6 border-t border-border pt-4">
              <h4 class="text-xs font-bold uppercase tracking-wider text-text-muted mb-3">
                {{ $t('insights.categorySurgeHeading') }}
              </h4>
              <div class="space-y-2">
                <div
                  v-for="item in analysis.expense.categoryMomentum"
                  :key="item.categoryId"
                  class="flex items-center justify-between text-xs py-1.5 border-b border-border/40 last:border-0"
                >
                  <span class="font-medium text-text">{{ catName(item.categoryId) }}</span>
                  <div class="flex items-center gap-3">
                    <span class="tnum text-text-muted"><Money :value="item.currentAmount" /></span>
                    <span
                      class="rounded-full px-1.5 py-0.5 text-[10px] font-bold tnum"
                      :class="item.growthPercent > 20 ? 'bg-amber-500/15 text-amber-400' : 'bg-surface-2 text-text-muted'"
                    >
                      {{ item.growthPercent > 0 ? '+' : '' }}{{ item.growthPercent }}%
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </AppCard>
        </div>
      </div>

      <!-- TAB 3: INCOME DIAGNOSTICS -->
      <div v-else-if="activeTab === 'income'" class="space-y-6">
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <!-- Income Retention Rate -->
          <AppCard>
            <template #title>
              <span>{{ $t('insights.retentionHeroHeading') }}</span>
            </template>
            <p class="text-xs text-text-muted mb-4">
              {{ $t('insights.retentionExplainer') }}
            </p>

            <div class="rounded-control bg-surface-2/60 p-4 border border-border">
              <div class="flex items-baseline justify-between">
                <span class="text-xs font-semibold text-text-muted">{{ $t('insights.currentRetention') }}</span>
                <span class="text-2xl font-black tracking-tight text-accent tnum">{{ analysis.income.retentionRate }}%</span>
              </div>
              <!-- Progress bar with 20% benchmark marker -->
              <div class="relative mt-3 h-3 w-full rounded-full bg-surface-3 overflow-hidden">
                <div
                  class="h-full rounded-full transition-all duration-300"
                  :class="analysis.income.retentionRate >= 20 ? 'bg-accent' : 'bg-amber-400'"
                  :style="{ width: `${Math.min(100, Math.max(0, analysis.income.retentionRate))}%` }"
                />
              </div>
              <div class="mt-2 flex justify-between text-[10px] font-semibold text-text-muted">
                <span>0% (Breakeven)</span>
                <span class="text-accent">20% (Target)</span>
                <span>50% (Master Plan)</span>
              </div>
            </div>

            <div class="mt-4 grid grid-cols-2 gap-3">
              <div class="rounded-control border border-border p-3">
                <span class="text-[11px] text-text-muted font-medium">{{ $t('insights.totalIncomeLabel') }}</span>
                <div class="text-base font-bold text-text tnum mt-0.5">
                  <Money :value="analysis.income.totalIncome" />
                </div>
              </div>
              <div class="rounded-control border border-border p-3">
                <span class="text-[11px] text-text-muted font-medium">{{ $t('insights.netSavedLabel') }}</span>
                <div class="text-base font-bold text-accent tnum mt-0.5">
                  <Money :value="analysis.income.totalIncome - analysis.expense.totalExpense" />
                </div>
              </div>
            </div>
          </AppCard>

          <!-- Income Sources & Diversity -->
          <AppCard>
            <template #title>
              <span>{{ $t('insights.incomeSourcesHeading') }}</span>
            </template>
            <p class="text-xs text-text-muted mb-4">
              {{ $t('insights.incomeSourcesExplainer') }}
            </p>

            <div class="space-y-3">
              <div
                v-for="cat in analysis.income.categories"
                :key="cat.categoryId"
                class="flex items-center justify-between rounded-control border border-border/60 bg-surface-2/40 px-3 py-2.5"
              >
                <div class="flex items-center gap-2">
                  <div class="h-2 w-2 rounded-full bg-accent" />
                  <span class="text-xs font-bold text-text">{{ catName(cat.categoryId) }}</span>
                </div>
                <div class="text-right">
                  <span class="text-xs font-bold text-text tnum"><Money :value="cat.amount" /></span>
                  <span class="ml-2 text-[10px] font-semibold text-accent tnum">{{ cat.percentage }}%</span>
                </div>
              </div>
            </div>

            <div class="mt-6 rounded-control border border-border bg-surface-2/40 p-3">
              <div class="flex items-center justify-between text-xs">
                <span class="font-semibold text-text-muted">{{ $t('insights.stabilityRating') }}</span>
                <span class="font-bold text-text tnum">{{ analysis.income.stabilityScore }} / 100</span>
              </div>
            </div>
          </AppCard>
        </div>
      </div>
    </div>
  </div>
</template>
