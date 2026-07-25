<script setup lang="ts">
import { onMounted, ref, reactive, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { ChevronLeft, ChevronRight, Plus } from 'lucide-vue-next';
import { getBudgetPeriod, createBudgetPeriod, upsertBudgetLines } from '@/lib/budgeting';
import { listCategories } from '@/lib/categories';
import { ApiClientError } from '@/lib/api';
import type { BudgetPeriod, Category } from '@/types/api';
import { errorMessage } from '@/lib/errorMessage';
import { displayName } from '@/lib/seededNames';
import AppCard from '@/components/ui/AppCard.vue';
import AppButton from '@/components/ui/AppButton.vue';
import AppInput from '@/components/ui/AppInput.vue';
import Money from '@/components/ui/Money.vue';

const { t, te, locale } = useI18n();

const now = new Date();
const year = ref(now.getFullYear());
const month = ref(now.getMonth() + 1);

const period = ref<BudgetPeriod | null>(null);
const categories = ref<Category[]>([]);
const loading = ref(true);
const failed = ref(false);
const editing = ref(false);
const saving = ref(false);
const planDraft = reactive<Record<string, number>>({});

const monthLabel = computed(() =>
  new Date(year.value, month.value - 1, 1).toLocaleString(locale.value, { month: 'long', year: 'numeric' }),
);
const expenseCats = computed(() =>
  categories.value.filter((c) => c.level === 'Category' && c.isActive && (c.flow === 'Any' || c.flow === 'Expense')),
);

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

function categoryName(id: string, fallback: string | null): string {
  const c = categories.value.find((x) => x.id === id);
  return displayName(c?.name ?? fallback, locale.value);
}

async function createPeriod(): Promise<void> {
  try {
    period.value = await createBudgetPeriod(year.value, month.value);
    startEdit();
  } catch (error) {
    window.alert(errorMessage(t, te, error));
  }
}

function startEdit(): void {
  for (const c of expenseCats.value) planDraft[c.id] = 0;
  for (const line of period.value?.lines ?? []) planDraft[line.categoryId] = line.plan;
  editing.value = true;
}

async function savePlans(): Promise<void> {
  saving.value = true;
  try {
    if (!period.value) period.value = await createBudgetPeriod(year.value, month.value);
    const lines = expenseCats.value.map((c) => ({ categoryId: c.id, planAmount: Number(planDraft[c.id] || 0) }));
    period.value = await upsertBudgetLines(period.value.id, lines);
    editing.value = false;
  } catch (error) {
    window.alert(errorMessage(t, te, error));
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
    <div class="flex flex-wrap items-center justify-between gap-3">
      <h1 class="text-lg font-semibold">{{ t('budget.title') }}</h1>
      <div class="flex items-center gap-2">
        <button class="rounded-control border border-border p-2 text-text-muted hover:bg-surface-2 hover:text-text" @click="changeMonth(-1)"><ChevronLeft :size="16" /></button>
        <span class="min-w-[9rem] text-center text-sm font-medium">{{ monthLabel }}</span>
        <button class="rounded-control border border-border p-2 text-text-muted hover:bg-surface-2 hover:text-text" @click="changeMonth(1)"><ChevronRight :size="16" /></button>
      </div>
    </div>

    <div v-if="loading" class="py-16 text-center text-sm text-text-muted">{{ t('common.loading') }}</div>
    <div v-else-if="failed" class="py-16 text-center">
      <p class="text-sm text-text-muted">{{ t('errors.network_error') }}</p>
      <AppButton class="mt-4" variant="secondary" @click="loadPeriod">{{ t('common.retry') }}</AppButton>
    </div>

    <!-- No period yet -->
    <AppCard v-else-if="!period" >
      <div class="py-8 text-center">
        <p class="text-[13px] text-text-muted">{{ t('budget.noPeriod') }}</p>
        <AppButton class="mt-4" @click="createPeriod"><Plus :size="16" />{{ t('budget.createPeriod') }}</AppButton>
      </div>
    </AppCard>

    <template v-else>
      <!-- Totals -->
      <div class="grid grid-cols-1 gap-3 sm:grid-cols-3 sm:gap-4">
        <AppCard>
          <p class="text-[13px] text-text-muted">{{ t('budget.plan') }}</p>
          <p class="mt-1 text-xl font-semibold"><Money :value="period.totalPlan" /></p>
        </AppCard>
        <AppCard>
          <p class="text-[13px] text-text-muted">{{ t('budget.actual') }}</p>
          <p class="mt-1 text-xl font-semibold"><Money :value="period.totalActual" /></p>
        </AppCard>
        <AppCard>
          <p class="text-[13px] text-text-muted">{{ t('budget.leftover') }}</p>
          <p class="mt-1 text-xl font-semibold"><Money :value="period.totalLeftover" colored /></p>
        </AppCard>
      </div>

      <AppCard :padded="false">
        <div class="flex items-center justify-between border-b border-border px-5 py-3">
          <h2 class="text-sm font-semibold">{{ t('budget.category') }}</h2>
          <AppButton v-if="!editing" variant="secondary" @click="startEdit">{{ t('budget.editPlans') }}</AppButton>
          <AppButton v-else :loading="saving" @click="savePlans">{{ saving ? t('common.saving') : t('budget.savePlans') }}</AppButton>
        </div>

        <!-- View mode -->
        <div v-if="!editing" class="scroll-slim overflow-x-auto">
        <table class="w-full min-w-[520px] text-sm">
          <thead>
            <tr class="text-left text-[12px] uppercase text-text-muted">
              <th class="px-5 py-2 font-medium">{{ t('budget.category') }}</th>
              <th class="px-5 py-2 text-right font-medium">{{ t('budget.plan') }}</th>
              <th class="px-5 py-2 text-right font-medium">{{ t('budget.actual') }}</th>
              <th class="px-5 py-2 text-right font-medium">{{ t('budget.leftover') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="line in period.lines" :key="line.categoryId" class="border-t border-border">
              <td class="px-5 py-2.5">{{ categoryName(line.categoryId, line.categoryName) }}</td>
              <td class="px-5 py-2.5 text-right"><Money :value="line.plan" /></td>
              <td class="px-5 py-2.5 text-right"><Money :value="line.actual" /></td>
              <td class="px-5 py-2.5 text-right"><Money :value="line.leftover" colored /></td>
            </tr>
            <tr v-if="!period.lines.length">
              <td colspan="4" class="px-5 py-6 text-center text-[13px] text-text-muted">{{ t('budget.empty') }}</td>
            </tr>
          </tbody>
        </table>
        </div>

        <!-- Edit mode -->
        <div v-else class="scroll-slim overflow-x-auto">
        <table class="w-full min-w-[360px] text-sm">
          <thead>
            <tr class="text-left text-[12px] uppercase text-text-muted">
              <th class="px-5 py-2 font-medium">{{ t('budget.category') }}</th>
              <th class="px-5 py-2 text-right font-medium">{{ t('budget.plan') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in expenseCats" :key="c.id" class="border-t border-border">
              <td class="px-5 py-2">{{ displayName(c.name, locale) }}</td>
              <td class="px-5 py-2">
                <AppInput
                  type="number"
                  :model-value="String(planDraft[c.id] ?? 0)"
                  @update:model-value="planDraft[c.id] = Number($event)"
                />
              </td>
            </tr>
          </tbody>
        </table>
        </div>
      </AppCard>
    </template>
  </div>
</template>
