<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import AppModal from '@/components/ui/AppModal.vue';
import { formatMoney } from '@/lib/format';
import { getSafeToSpend, simulatePurchase } from '@/lib/decision';
import { listCategories } from '@/lib/categories';
import { useTransactionModalStore } from '@/stores/transactionModal';
import type { SafeToSpendDto, SimulatePurchaseResultDto, Category } from '@/types/api';
import {
  ShieldCheck,
  AlertTriangle,
  AlertOctagon,
  Wallet,
  ArrowRight,
  Sparkles,
  HelpCircle,
  PlusCircle,
} from 'lucide-vue-next';

const emit = defineEmits<{ close: [] }>();

const { t } = useI18n();
const transactionModal = useTransactionModalStore();

const amount = ref<number | null>(null);
const categoryId = ref<string>('');
const description = ref('');

const safeToSpend = ref<SafeToSpendDto | null>(null);
const categories = ref<Category[]>([]);
const simulation = ref<SimulatePurchaseResultDto | null>(null);
const loading = ref(false);
const simulating = ref(false);

const quickAmounts = [50_000, 100_000, 250_000, 500_000, 1_000_000, 2_000_000];

async function loadData(): Promise<void> {
  loading.value = true;
  try {
    const [safeData, catData] = await Promise.all([
      getSafeToSpend(),
      listCategories({ flow: 'Expense' }),
    ]);
    safeToSpend.value = safeData;
    categories.value = catData.filter(c => c.isActive && c.level !== 'Budget');
  } catch (err) {
    console.error('Failed to load simulator data:', err);
  } finally {
    loading.value = false;
  }
}

let debounceTimer: ReturnType<typeof setTimeout> | null = null;
watch([amount, categoryId], () => {
  if (debounceTimer) clearTimeout(debounceTimer);
  if (!amount.value || amount.value <= 0) {
    simulation.value = null;
    return;
  }
  debounceTimer = setTimeout(() => {
    void runSimulation();
  }, 250);
});

async function runSimulation(): Promise<void> {
  if (!amount.value || amount.value <= 0) return;
  simulating.value = true;
  try {
    const result = await simulatePurchase({
      amount: amount.value,
      categoryId: categoryId.value || null,
      description: description.value.trim() || undefined,
    });
    simulation.value = result;
  } catch (err) {
    console.error('Simulation error:', err);
  } finally {
    simulating.value = false;
  }
}

function addQuickAmount(val: number): void {
  amount.value = (amount.value || 0) + val;
}

function handleRecordTransaction(): void {
  if (!amount.value || amount.value <= 0) return;
  emit('close');
  transactionModal.openCreate({
    type: 'Expense',
    amount: amount.value,
    categoryId: categoryId.value || undefined,
    description: description.value.trim() || undefined,
  });
}

onMounted(() => {
  void loadData();
});
</script>

<template>
  <AppModal
    :title="t('decision.simulatorTitle')"
    @close="emit('close')"
  >
    <div class="space-y-6 text-sm">
      <!-- Uncommitted Liquidity Overview Header -->
      <div
        v-if="safeToSpend"
        class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 p-4 rounded-xl bg-surface-2 border border-border"
      >
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-xl bg-primary/10 flex items-center justify-center text-primary shrink-0">
            <Wallet :size="20" />
          </div>
          <div>
            <div class="text-xs text-text-muted font-medium">{{ t('decision.currentSafeToSpend') }}</div>
            <div class="text-lg font-bold text-text tabular-nums">{{ formatMoney(safeToSpend.safeToSpend) }}</div>
          </div>
        </div>

        <div class="flex items-center gap-4 text-xs text-text-muted border-t sm:border-t-0 sm:border-l border-border pt-2 sm:pt-0 sm:pl-4">
          <div>
            <span class="block text-text font-semibold tabular-nums">{{ formatMoney(safeToSpend.dailyAllowance) }}</span>
            <span>{{ t('decision.dailyPacing') }}</span>
          </div>
          <div class="h-7 w-px bg-border hidden sm:block"></div>
          <div>
            <span class="block text-text font-semibold tabular-nums">{{ safeToSpend.daysRemaining }} {{ t('decision.days') }}</span>
            <span>{{ t('decision.untilPayday') }}</span>
          </div>
        </div>
      </div>

      <!-- Purchase Input Form -->
      <div class="space-y-4">
        <div>
          <label class="block text-xs font-semibold text-text-muted uppercase tracking-wider mb-1.5">
            {{ t('decision.purchaseAmount') }} <span class="text-danger">*</span>
          </label>
          <div class="relative">
            <span class="absolute left-3.5 top-1/2 -translate-y-1/2 text-text-muted font-semibold">Rp</span>
            <input
              v-model.number="amount"
              type="number"
              min="1"
              placeholder="0"
              class="w-full pl-11 pr-4 py-2.5 rounded-xl bg-surface border border-border text-base font-bold text-text focus:outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 tabular-nums transition-all"
            />
          </div>

          <!-- Quick Increment Chips -->
          <div class="flex flex-wrap gap-1.5 mt-2">
            <button
              v-for="amt in quickAmounts"
              :key="amt"
              type="button"
              class="px-2.5 py-1 text-xs font-medium rounded-lg bg-surface-2 hover:bg-surface-3 text-text-muted hover:text-text border border-border/60 transition-colors"
              @click="addQuickAmount(amt)"
            >
              +{{ formatMoney(amt).replace('Rp', '').trim() }}
            </button>
            <button
              v-if="amount && amount > 0"
              type="button"
              class="px-2 py-1 text-xs text-text-muted hover:text-danger ml-auto transition-colors"
              @click="amount = null"
            >
              {{ t('common.reset') }}
            </button>
          </div>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label class="block text-xs font-semibold text-text-muted uppercase tracking-wider mb-1.5">
              {{ t('decision.targetCategory') }}
            </label>
            <select
              v-model="categoryId"
              class="w-full px-3 py-2 rounded-xl bg-surface border border-border text-sm text-text focus:outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 transition-all"
            >
              <option value="">{{ t('decision.anyOrGeneral') }}</option>
              <option
                v-for="cat in categories"
                :key="cat.id"
                :value="cat.id"
              >
                {{ cat.name }}
              </option>
            </select>
          </div>

          <div>
            <label class="block text-xs font-semibold text-text-muted uppercase tracking-wider mb-1.5">
              {{ t('decision.itemName') }}
            </label>
            <input
              v-model="description"
              type="text"
              :placeholder="t('decision.itemNamePlaceholder')"
              class="w-full px-3 py-2 rounded-xl bg-surface border border-border text-sm text-text focus:outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 transition-all"
            />
          </div>
        </div>
      </div>

      <!-- Live Simulation Verdict & Impact Breakdown -->
      <div v-if="simulation" class="space-y-4 pt-2">
        <!-- Verdict Banner -->
        <div
          class="p-4 rounded-xl border transition-all"
          :class="{
            'bg-success/10 border-success/30 text-success': simulation.verdict === 'Safe',
            'bg-warning/10 border-warning/30 text-warning': simulation.verdict === 'Warning',
            'bg-danger/10 border-danger/30 text-danger': simulation.verdict === 'Risky',
          }"
        >
          <div class="flex items-start gap-3">
            <div class="p-2 rounded-lg bg-surface/60 shrink-0">
              <ShieldCheck v-if="simulation.verdict === 'Safe'" :size="22" class="text-success" />
              <AlertTriangle v-else-if="simulation.verdict === 'Warning'" :size="22" class="text-warning" />
              <AlertOctagon v-else :size="22" class="text-danger" />
            </div>
            <div class="space-y-1">
              <div class="flex items-center gap-2">
                <span class="text-base font-bold">
                  {{ simulation.verdict === 'Safe' ? t('decision.verdictSafe') : simulation.verdict === 'Warning' ? t('decision.verdictWarning') : t('decision.verdictRisky') }}
                </span>
                <span class="text-xs px-2 py-0.5 rounded-full font-semibold bg-surface/80 border border-current">
                  {{ simulation.verdict.toUpperCase() }}
                </span>
              </div>
              <p class="text-xs font-medium text-text-muted leading-relaxed">
                {{ simulation.impactSummary }}
              </p>
            </div>
          </div>
        </div>

        <!-- Pacing & Budget Impact Grid -->
        <div class="grid grid-cols-2 sm:grid-cols-3 gap-3">
          <div class="p-3 rounded-xl bg-surface-2 border border-border">
            <div class="text-xs text-text-muted font-medium mb-1">{{ t('decision.safeToSpendRemaining') }}</div>
            <div class="flex items-baseline gap-1.5 flex-wrap">
              <span class="text-sm font-bold text-text tabular-nums">{{ formatMoney(simulation.newSafeToSpend) }}</span>
              <span class="text-xs text-danger tabular-nums font-medium">-{{ formatMoney(simulation.amount) }}</span>
            </div>
          </div>

          <div class="p-3 rounded-xl bg-surface-2 border border-border">
            <div class="text-xs text-text-muted font-medium mb-1">{{ t('decision.dailyPacingAfter') }}</div>
            <div class="flex items-baseline gap-1.5 flex-wrap">
              <span class="text-sm font-bold text-text tabular-nums">{{ formatMoney(simulation.newDailyAllowance) }}</span>
              <span class="text-xs text-text-muted">/{{ t('decision.day') }}</span>
            </div>
          </div>

          <div class="p-3 rounded-xl bg-surface-2 border border-border col-span-2 sm:col-span-1">
            <div class="text-xs text-text-muted font-medium mb-1">{{ t('decision.nextPayday') }}</div>
            <div class="text-sm font-bold text-text">
              {{ safeToSpend?.nextPayday ? new Date(safeToSpend.nextPayday).toLocaleDateString(undefined, { day: 'numeric', month: 'short' }) : '-' }}
            </div>
            <div class="text-xs text-text-muted">{{ simulation.daysRemaining }} {{ t('decision.daysRemaining') }}</div>
          </div>
        </div>

        <!-- Category Leftover Impact (if category selected) -->
        <div
          v-if="simulation.categoryId && simulation.categoryLeftover !== null"
          class="p-3 rounded-xl bg-surface-2 border border-border flex items-center justify-between"
        >
          <div>
            <span class="text-xs text-text-muted">{{ simulation.categoryName }} {{ t('decision.categoryLeftover') }}</span>
            <div class="text-sm font-semibold text-text tabular-nums">
              {{ formatMoney(simulation.categoryLeftover) }}
              <ArrowRight :size="14" class="inline mx-1 text-text-muted" />
              <span :class="simulation.categoryLeftoverAfterPurchase! < 0 ? 'text-danger font-bold' : 'text-success'">
                {{ formatMoney(simulation.categoryLeftoverAfterPurchase!) }}
              </span>
            </div>
          </div>
        </div>

        <!-- Surplus Categories Reallocation Recommendations -->
        <div
          v-if="simulation.verdict === 'Warning' && simulation.surplusCategories && simulation.surplusCategories.length > 0"
          class="p-4 rounded-xl bg-surface border border-warning/30 space-y-2.5"
        >
          <div class="flex items-center gap-2 text-xs font-bold text-warning uppercase tracking-wider">
            <Sparkles :size="15" />
            <span>{{ t('decision.reallocationTip') }}</span>
          </div>
          <p class="text-xs text-text-muted">
            {{ t('decision.reallocationExplanation') }}
          </p>
          <div class="flex flex-wrap gap-2 pt-1">
            <div
              v-for="surplus in simulation.surplusCategories"
              :key="surplus.categoryId"
              class="flex items-center gap-2 px-2.5 py-1.5 rounded-lg bg-surface-2 border border-border text-xs"
            >
              <span class="font-medium text-text">{{ surplus.categoryName }}</span>
              <span class="font-bold text-success tabular-nums">+{{ formatMoney(surplus.availableSurplus) }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Empty state when no amount entered -->
      <div
        v-else-if="!loading"
        class="p-6 text-center rounded-xl bg-surface-2 border border-dashed border-border text-text-muted space-y-2"
      >
        <HelpCircle :size="28" class="mx-auto text-text-muted/60" />
        <p class="text-xs max-w-sm mx-auto">
          {{ t('decision.simulatorEmptyPrompt') }}
        </p>
      </div>
    </div>

    <template #footer>
      <div class="flex items-center justify-between gap-3">
        <button
          type="button"
          class="btn-secondary"
          @click="emit('close')"
        >
          {{ t('common.close') }}
        </button>

        <button
          type="button"
          class="btn-primary flex items-center gap-2"
          :disabled="!amount || amount <= 0"
          @click="handleRecordTransaction"
        >
          <PlusCircle :size="16" />
          <span>{{ t('decision.recordTransactionNow') }}</span>
        </button>
      </div>
    </template>
  </AppModal>
</template>
