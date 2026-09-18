<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { getInsights } from '@/lib/insights';
import type { InsightDto } from '@/types/api';
import {
  Zap,
  TrendingUp,
  AlertTriangle,
  AlertOctagon,
  Info,
  Calendar,
  Target,
  Gauge,
  Shield,
  ChevronDown,
  ChevronUp,
  RefreshCw,
  Sparkles,
  ArrowRight,
} from 'lucide-vue-next';

const { t } = useI18n();
const router = useRouter();

const insights = ref<InsightDto[]>([]);
const loading = ref(true);
const error = ref(false);
const expanded = ref(false);
const refreshing = ref(false);

const MAX_COLLAPSED = 3;
let refreshTimer: ReturnType<typeof setInterval> | null = null;

const visibleInsights = computed(() =>
  expanded.value ? insights.value : insights.value.slice(0, MAX_COLLAPSED),
);

const hasMore = computed(() => insights.value.length > MAX_COLLAPSED);

async function fetchInsights(silent = false): Promise<void> {
  if (!silent) loading.value = true;
  else refreshing.value = true;
  error.value = false;
  try {
    insights.value = await getInsights({ maxResults: 10 });
  } catch {
    error.value = true;
  } finally {
    loading.value = false;
    refreshing.value = false;
  }
}

function navigateTo(route: string): void {
  void router.push(route);
}

function getIcon(type: string) {
  switch (type) {
    case 'payday': return Calendar;
    case 'budget_pacing': return Target;
    case 'spending_velocity': return Gauge;
    case 'savings_trend': return TrendingUp;
    case 'runway': return Shield;
    case 'anomaly': return AlertOctagon;
    default: return Zap;
  }
}

function getSeverityClasses(severity: string) {
  switch (severity) {
    case 'critical': return {
      card: 'insight-card--critical',
      icon: 'insight-icon--critical',
      badge: 'insight-badge--critical',
    };
    case 'warning': return {
      card: 'insight-card--warning',
      icon: 'insight-icon--warning',
      badge: 'insight-badge--warning',
    };
    default: return {
      card: 'insight-card--info',
      icon: 'insight-icon--info',
      badge: 'insight-badge--info',
    };
  }
}

onMounted(() => {
  void fetchInsights();
  // Auto-refresh every 5 minutes
  refreshTimer = setInterval(() => void fetchInsights(true), 5 * 60 * 1000);
});

onUnmounted(() => {
  if (refreshTimer) clearInterval(refreshTimer);
});
</script>

<template>
  <div class="insights-panel">
    <!-- Header -->
    <div class="insights-header">
      <div class="insights-title-group">
        <div class="insights-title-icon">
          <Sparkles :size="18" />
        </div>
        <h3 class="insights-title">{{ t('insights.title') }}</h3>
        <span v-if="insights.length > 0" class="insights-count">{{ insights.length }}</span>
      </div>
      <button
        type="button"
        class="insights-refresh-btn"
        :disabled="refreshing"
        :title="t('insights.refresh')"
        @click="fetchInsights(true)"
      >
        <RefreshCw :size="14" :class="{ 'animate-spin': refreshing }" />
      </button>
    </div>

    <!-- Loading Skeleton -->
    <div v-if="loading" class="insights-skeleton-list">
      <div v-for="i in 3" :key="i" class="insight-skeleton">
        <div class="insight-skeleton-icon" />
        <div class="insight-skeleton-content">
          <div class="insight-skeleton-title" />
          <div class="insight-skeleton-text" />
        </div>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="insights-empty">
      <AlertTriangle :size="20" class="text-warning" />
      <p>{{ t('insights.errorLoading') }}</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="insights.length === 0" class="insights-empty">
      <Info :size="20" />
      <p>{{ t('insights.noInsights') }}</p>
    </div>

    <!-- Insights List -->
    <div v-else class="insights-list">
      <TransitionGroup name="insight-fade">
        <div
          v-for="(insight, idx) in visibleInsights"
          :key="insight.id"
          class="insight-card"
          :class="getSeverityClasses(insight.severity).card"
          :style="{ animationDelay: `${idx * 60}ms` }"
        >
          <div class="insight-card-inner">
            <div class="insight-icon-wrap" :class="getSeverityClasses(insight.severity).icon">
              <component :is="getIcon(insight.type)" :size="16" />
            </div>

            <div class="insight-body">
              <div class="insight-top-row">
                <span class="insight-card-title">{{ insight.title }}</span>
                <span
                  class="insight-badge"
                  :class="getSeverityClasses(insight.severity).badge"
                >
                  {{ insight.severity }}
                </span>
              </div>
              <p class="insight-message">{{ insight.message }}</p>
            </div>
          </div>

          <button
            v-if="insight.actionRoute"
            type="button"
            class="insight-action-btn"
            @click="navigateTo(insight.actionRoute)"
          >
            <span>{{ t('insights.viewDetails') }}</span>
            <ArrowRight :size="12" />
          </button>
        </div>
      </TransitionGroup>

      <!-- Show More / Less -->
      <button
        v-if="hasMore"
        type="button"
        class="insights-toggle-btn"
        @click="expanded = !expanded"
      >
        <component :is="expanded ? ChevronUp : ChevronDown" :size="14" />
        <span>{{ expanded ? t('insights.showLess') : t('insights.showAll', { count: insights.length }) }}</span>
      </button>
    </div>
  </div>
</template>

<style scoped>
.insights-panel {
  border-radius: 16px;
  background: var(--surface);
  border: 1px solid var(--border);
  box-shadow: var(--shadow-card);
  overflow: hidden;
}

.insights-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px 12px;
}

.insights-title-group {
  display: flex;
  align-items: center;
  gap: 8px;
}

.insights-title-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border-radius: 8px;
  background: var(--accent-soft);
  color: var(--accent);
}

.insights-title {
  font-size: 14px;
  font-weight: 700;
  color: var(--text);
  margin: 0;
  letter-spacing: -0.01em;
}

.insights-count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 20px;
  height: 20px;
  padding: 0 6px;
  border-radius: 10px;
  background: var(--accent-soft);
  color: var(--accent);
  font-size: 11px;
  font-weight: 700;
}

.insights-refresh-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border-radius: 8px;
  border: 1px solid var(--border);
  background: var(--surface-2);
  color: var(--text-muted);
  cursor: pointer;
  transition: all 0.15s ease;
}
.insights-refresh-btn:hover:not(:disabled) {
  color: var(--text);
  background: var(--surface-3);
}
.insights-refresh-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* Skeleton */
.insights-skeleton-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 0 16px 16px;
}

.insight-skeleton {
  display: flex;
  gap: 12px;
  align-items: center;
  padding: 12px;
  border-radius: 12px;
  background: var(--surface-2);
}

.insight-skeleton-icon {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  background: var(--surface-3);
  animation: pulse 1.5s ease-in-out infinite;
}

.insight-skeleton-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.insight-skeleton-title {
  height: 14px;
  width: 60%;
  border-radius: 4px;
  background: var(--surface-3);
  animation: pulse 1.5s ease-in-out infinite;
}

.insight-skeleton-text {
  height: 12px;
  width: 90%;
  border-radius: 4px;
  background: var(--surface-3);
  animation: pulse 1.5s ease-in-out 0.15s infinite;
}

/* Empty/Error */
.insights-empty {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 16px 20px 20px;
  color: var(--text-muted);
  font-size: 13px;
}

/* Insights list */
.insights-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 0 12px 12px;
}

/* Insight Card */
.insight-card {
  border-radius: 12px;
  border: 1px solid var(--border);
  overflow: hidden;
  transition: all 0.2s ease;
  animation: insightSlideIn 0.3s ease both;
}
.insight-card:hover {
  box-shadow: var(--shadow-lift);
  transform: translateY(-1px);
}

.insight-card-inner {
  display: flex;
  gap: 12px;
  padding: 12px 14px;
  align-items: flex-start;
}

.insight-icon-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  min-width: 32px;
  border-radius: 8px;
  flex-shrink: 0;
}

.insight-body {
  flex: 1;
  min-width: 0;
}

.insight-top-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 4px;
}

.insight-card-title {
  font-size: 13px;
  font-weight: 600;
  color: var(--text);
  line-height: 1.3;
}

.insight-badge {
  display: inline-flex;
  padding: 1px 6px;
  border-radius: 6px;
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  white-space: nowrap;
  flex-shrink: 0;
}

.insight-message {
  font-size: 12px;
  color: var(--text-muted);
  line-height: 1.5;
  margin: 0;
}

.insight-action-btn {
  display: flex;
  align-items: center;
  gap: 4px;
  width: 100%;
  padding: 8px 14px;
  border: none;
  border-top: 1px solid var(--border);
  background: var(--surface-2);
  color: var(--accent);
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;
}
.insight-action-btn:hover {
  background: var(--accent-soft);
}

/* Severity variants */
.insight-card--critical {
  border-color: var(--negative);
  background: var(--negative-soft);
}
.insight-card--critical .insight-action-btn {
  border-color: color-mix(in srgb, var(--negative), transparent 70%);
}
.insight-icon--critical {
  background: var(--negative-soft);
  color: var(--negative);
}
.insight-badge--critical {
  background: var(--negative-soft);
  color: var(--negative);
}

.insight-card--warning {
  border-color: color-mix(in srgb, var(--warning), transparent 50%);
  background: var(--warning-soft);
}
.insight-card--warning .insight-action-btn {
  border-color: color-mix(in srgb, var(--warning), transparent 70%);
}
.insight-icon--warning {
  background: var(--warning-soft);
  color: var(--warning);
}
.insight-badge--warning {
  background: var(--warning-soft);
  color: var(--warning);
}

.insight-card--info {
  border-color: color-mix(in srgb, var(--info), transparent 60%);
  background: var(--info-soft);
}
.insight-card--info .insight-action-btn {
  border-color: color-mix(in srgb, var(--info), transparent 70%);
}
.insight-icon--info {
  background: var(--info-soft);
  color: var(--info);
}
.insight-badge--info {
  background: var(--info-soft);
  color: var(--info);
}

/* Toggle button */
.insights-toggle-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  width: 100%;
  padding: 8px;
  border: none;
  border-radius: 8px;
  background: var(--surface-2);
  color: var(--text-muted);
  font-size: 12px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s ease;
}
.insights-toggle-btn:hover {
  background: var(--surface-3);
  color: var(--text);
}

/* Animations */
@keyframes insightSlideIn {
  from {
    opacity: 0;
    transform: translateY(8px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.4; }
}

.animate-spin {
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

/* Transition group */
.insight-fade-enter-active {
  transition: all 0.3s ease;
}
.insight-fade-leave-active {
  transition: all 0.2s ease;
}
.insight-fade-enter-from {
  opacity: 0;
  transform: translateY(8px);
}
.insight-fade-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>
