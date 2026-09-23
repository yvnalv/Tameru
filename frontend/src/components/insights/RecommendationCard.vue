<script setup lang="ts">
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import {
  TrendingUp,
  Info,
  Target,
  Gauge,
  Shield,
  ArrowRight,
  PiggyBank,
} from 'lucide-vue-next';
import type { RecommendationDto } from '@/types/api';
import Money from '@/components/ui/Money.vue';

const props = defineProps<{
  recommendation: RecommendationDto;
}>();

const router = useRouter();

function getStrategyIcon(strategy: string) {
  switch (strategy) {
    case 'WeekendPacing': return Gauge;
    case 'SavingsRate': return PiggyBank;
    case 'CategorySurge': return TrendingUp;
    case 'CashRunway': return Shield;
    case 'AllocationRebalance': return Target;
    default: return Info;
  }
}

const severityClasses = computed(() => {
  switch (props.recommendation.severity) {
    case 'critical':
      return {
        card: 'border-negative/40 bg-negative/5 hover:border-negative/60',
        badge: 'bg-negative/15 text-negative border-negative/30',
        icon: 'text-negative',
      };
    case 'warning':
      return {
        card: 'border-amber-500/40 bg-amber-500/5 hover:border-amber-500/60',
        badge: 'bg-amber-500/15 text-amber-400 border-amber-500/30',
        icon: 'text-amber-400',
      };
    default:
      return {
        card: 'border-accent/30 bg-accent/5 hover:border-accent/50',
        badge: 'bg-accent/15 text-accent border-accent/30',
        icon: 'text-accent',
      };
  }
});

function handleAction(): void {
  if (props.recommendation.actionRoute) {
    void router.push({ name: props.recommendation.actionRoute });
  }
}
</script>

<template>
  <div
    class="flex flex-col justify-between rounded-card border p-4.5 transition-all duration-150 shadow-xs"
    :class="severityClasses.card"
  >
    <div>
      <div class="flex items-start justify-between gap-3">
        <div class="flex items-center gap-2.5">
          <div class="flex h-8 w-8 shrink-0 items-center justify-center rounded-control bg-surface-2" :class="severityClasses.icon">
            <component :is="getStrategyIcon(recommendation.strategy)" :size="18" :stroke-width="2" />
          </div>
          <div>
            <span
              class="inline-block rounded-full border px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider"
              :class="severityClasses.badge"
            >
              {{ $t(`recommendations.strategy.${recommendation.strategy}`) }}
            </span>
            <h4 class="mt-0.5 text-sm font-bold text-text">
              {{ $t(recommendation.title) }}
            </h4>
          </div>
        </div>

        <div v-if="recommendation.estimatedMonthlySavings" class="text-right">
          <span class="text-[10px] font-medium text-text-muted block uppercase tracking-wide">
            {{ $t('recommendations.potentialSavings') }}
          </span>
          <span class="text-xs font-bold text-accent tnum">
            +<Money :value="recommendation.estimatedMonthlySavings" />/{{ $t('common.month') }}
          </span>
        </div>
      </div>

      <p class="mt-3 text-xs leading-relaxed text-text-muted">
        {{ recommendation.summary }}
      </p>
    </div>

    <div class="mt-4 pt-3 border-t border-border/50 flex items-center justify-between">
      <span class="text-[11px] text-text-muted">
        {{ $t(`recommendations.severity.${recommendation.severity}`) }}
      </span>
      <button
        type="button"
        class="inline-flex items-center gap-1.5 rounded-control px-3 py-1.5 text-xs font-semibold bg-surface-2 text-text hover:bg-surface-3 transition-colors active:scale-98"
        @click="handleAction"
      >
        <span>{{ $t(recommendation.actionText) }}</span>
        <ArrowRight :size="13" />
      </button>
    </div>
  </div>
</template>
