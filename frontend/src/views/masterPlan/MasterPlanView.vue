<script setup lang="ts">
import { onMounted, ref, reactive, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { Plus, Pencil, Trash2, Upload, AlertCircle, CheckCircle2, TrendingUp, Calendar, Wallet } from 'lucide-vue-next';
import {
  getMasterPlan,
  createMasterPlanItem,
  updateMasterPlanItem,
  deleteMasterPlanItem,
  updateMasterPlanSection,
} from '@/lib/budgeting';
import type { MasterPlan, MasterPlanItem, MasterPlanSection } from '@/types/api';
import { errorMessage } from '@/lib/errorMessage';
import { masterPlanImportConfig } from '@/lib/importConfigs';
import { displayName } from '@/lib/seededNames';
import { spectrumColor } from '@/lib/spectrum';
import { useToastStore } from '@/stores/toast';
import { useConfirmStore } from '@/stores/confirm';
import ImportModal from '@/components/ui/ImportModal.vue';
import LoadingBlock from '@/components/ui/LoadingBlock.vue';
import AppCard from '@/components/ui/AppCard.vue';
import AppButton from '@/components/ui/AppButton.vue';
import AppModal from '@/components/ui/AppModal.vue';
import AppInput from '@/components/ui/AppInput.vue';
import MoneyInput from '@/components/ui/MoneyInput.vue';
import FormField from '@/components/ui/FormField.vue';
import IconButton from '@/components/ui/IconButton.vue';
import Money from '@/components/ui/Money.vue';

const { t, te, locale } = useI18n();
const toast = useToastStore();
const confirm = useConfirmStore();

const plan = ref<MasterPlan | null>(null);
const loading = ref(true);
const failed = ref(false);
const importOpen = ref(false);
const importConfig = computed(() =>
  masterPlanImportConfig(plan.value?.sections ?? [], locale.value),
);

async function load(): Promise<void> {
  loading.value = true;
  failed.value = false;
  try {
    plan.value = await getMasterPlan();
  } catch {
    failed.value = true;
  } finally {
    loading.value = false;
  }
}

// --- decision support metrics -----------------------------------------------
const totalItemsCount = computed(() => {
  if (!plan.value) return 0;
  return plan.value.sections.reduce((acc, s) => acc + s.items.length, 0);
});

const annualCommitment = computed(() => {
  if (!plan.value) return 0;
  return plan.value.grandTotal * 12;
});

const plannedSavingsRate = computed(() => {
  if (!plan.value || plan.value.grandTotal <= 0) return 0;
  const inv = plan.value.sections.find((s) => s.name.toLowerCase().includes('invest'));
  if (!inv) return 0;
  return Math.round((inv.total / plan.value.grandTotal) * 100);
});

interface SectionAllocation {
  id: string;
  name: string;
  total: number;
  actualPercent: number;
  targetPercent: number;
  variancePercent: number;
  color: string;
}

const sectionAllocations = computed<SectionAllocation[]>(() => {
  if (!plan.value || plan.value.grandTotal <= 0) return [];
  const grand = plan.value.grandTotal;

  return plan.value.sections.map((s, idx) => {
    const actualPercent = Math.round((s.total / grand) * 1000) / 10;
    const variancePercent = Math.round((actualPercent - s.targetPercent) * 10) / 10;
    return {
      id: s.id,
      name: displayName(s.name, locale.value),
      total: s.total,
      actualPercent,
      targetPercent: s.targetPercent,
      variancePercent,
      color: spectrumColor(idx),
    };
  });
});

// --- modal state ------------------------------------------------------------
const modalOpen = ref(false);
const mode = ref<'item' | 'target'>('item');
const editingItemId = ref<string | null>(null);
const sectionId = ref('');
const saving = ref(false);
const formError = ref('');
const form = reactive({ name: '', price: 0, frequency: 1, targetPercent: 0, sortOrder: 0 });

const modalTitle = computed(() =>
  mode.value === 'target'
    ? t('masterPlan.editTarget')
    : editingItemId.value
      ? t('masterPlan.editItem')
      : t('masterPlan.addItem'),
);

const liveItemTotal = computed(() => (form.price || 0) * (form.frequency || 1));

const totalTargetPercent = computed(() => {
  if (!plan.value) return 0;
  return plan.value.sections.reduce((acc, s) => {
    if (mode.value === 'target' && s.id === sectionId.value) {
      return acc + Number(form.targetPercent || 0);
    }
    return acc + s.targetPercent;
  }, 0);
});

function openAddItem(section: MasterPlanSection): void {
  mode.value = 'item';
  editingItemId.value = null;
  sectionId.value = section.id;
  Object.assign(form, { name: '', price: 0, frequency: 1, sortOrder: section.items.length });
  formError.value = '';
  modalOpen.value = true;
}

function openEditItem(section: MasterPlanSection, item: MasterPlanItem): void {
  mode.value = 'item';
  editingItemId.value = item.id;
  sectionId.value = section.id;
  Object.assign(form, {
    name: item.name,
    price: item.price,
    frequency: item.frequency,
    sortOrder: item.sortOrder,
  });
  formError.value = '';
  modalOpen.value = true;
}

function openEditTarget(section: MasterPlanSection): void {
  mode.value = 'target';
  sectionId.value = section.id;
  form.targetPercent = section.targetPercent;
  formError.value = '';
  modalOpen.value = true;
}

function setFrequencyPreset(freq: number): void {
  form.frequency = freq;
}

async function save(): Promise<void> {
  saving.value = true;
  formError.value = '';
  try {
    if (mode.value === 'target') {
      await updateMasterPlanSection(sectionId.value, Number(form.targetPercent));
    } else if (editingItemId.value) {
      await updateMasterPlanItem(editingItemId.value, {
        name: form.name.trim(),
        price: Number(form.price),
        frequency: Number(form.frequency),
        sortOrder: form.sortOrder,
      });
    } else {
      await createMasterPlanItem({
        sectionId: sectionId.value,
        name: form.name.trim(),
        price: Number(form.price),
        frequency: Number(form.frequency),
        sortOrder: form.sortOrder,
      });
    }
    modalOpen.value = false;
    await load();
  } catch (error) {
    formError.value = errorMessage(t, te, error);
  } finally {
    saving.value = false;
  }
}

async function removeItem(item: MasterPlanItem): Promise<void> {
  const ok = await confirm.ask({
    message: t('masterPlan.deleteConfirm'),
    confirmLabel: t('common.delete'),
    danger: true,
  });
  if (!ok) return;
  try {
    await deleteMasterPlanItem(item.id);
    toast.success(t('common.done'));
    await load();
  } catch (error) {
    toast.error(errorMessage(t, te, error));
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-4">
    <!-- Top Action Row -->
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div class="text-xs text-text-muted">
        <span v-if="plan" class="font-medium text-text">
          {{ t('masterPlan.itemCount', { count: totalItemsCount }) }}
        </span>
        <span class="mx-1.5">·</span>
        <span>50/40/10 Allocation Framework</span>
      </div>
      <div class="flex items-center gap-3">
        <AppButton variant="secondary" @click="importOpen = true">
          <Upload :size="16" /><span class="hidden sm:inline">{{ t('import.masterPlan') }}</span>
        </AppButton>
      </div>
    </div>

    <LoadingBlock v-if="loading" />
    <div v-else-if="failed" class="py-16 text-center">
      <p class="text-sm text-text-muted">{{ t('errors.network_error') }}</p>
      <AppButton class="mt-4" variant="secondary" @click="load">
        {{ t('common.retry') }}
      </AppButton>
    </div>

    <template v-else-if="plan">
      <!-- Decision Support KPI Strip -->
      <div class="grid grid-cols-1 gap-3 sm:grid-cols-3 sm:gap-4">
        <!-- Monthly Baseline -->
        <AppCard>
          <div class="flex items-center justify-between">
            <p class="text-[13px] text-text-muted">{{ t('masterPlan.monthlyBaseline') }}</p>
            <Wallet :size="16" class="text-text-muted" />
          </div>
          <p class="mt-1 text-2xl font-bold text-text tnum">
            <Money :value="plan.grandTotal" />
          </p>
          <p class="mt-1 text-xs text-text-muted">
            {{ t('masterPlan.itemCount', { count: totalItemsCount }) }}
          </p>
        </AppCard>

        <!-- Annual Commitment -->
        <AppCard>
          <div class="flex items-center justify-between">
            <p class="text-[13px] text-text-muted">{{ t('masterPlan.annualCommitment') }}</p>
            <Calendar :size="16" class="text-text-muted" />
          </div>
          <p class="mt-1 text-2xl font-bold text-text tnum">
            <Money :value="annualCommitment" />
          </p>
          <p class="mt-1 text-xs text-text-muted">
            12 × {{ t('masterPlan.monthlyBaseline').toLowerCase() }}
          </p>
        </AppCard>

        <!-- Planned Savings Rate -->
        <AppCard>
          <div class="flex items-center justify-between">
            <p class="text-[13px] text-text-muted">{{ t('masterPlan.plannedSavingsRate') }}</p>
            <TrendingUp :size="16" class="text-text-muted" />
          </div>
          <p class="mt-1 text-2xl font-bold text-accent tnum">
            {{ plannedSavingsRate }}%
          </p>
          <p class="mt-1 text-xs text-text-muted">
            {{ t('dashboard.savingsTarget') }}
          </p>
        </AppCard>
      </div>

      <!-- Strategic Allocation Visualizer Bar (50/40/10 vs Actual) -->
      <AppCard v-if="sectionAllocations.length > 0">
        <div class="space-y-3">
          <div class="flex items-center justify-between">
            <h2 class="text-sm font-semibold">{{ t('masterPlan.allocationBar') }}</h2>
            <span class="text-xs text-text-muted">50/40/10 Master Plan Formula</span>
          </div>

          <!-- Segmented Allocation Bar -->
          <div class="flex h-3.5 w-full overflow-hidden rounded-full bg-surface-2">
            <div
              v-for="sec in sectionAllocations"
              :key="sec.id"
              class="h-full transition-all duration-300"
              :style="{ width: `${sec.actualPercent}%`, backgroundColor: sec.color }"
              :title="`${sec.name}: ${sec.actualPercent}%`"
            />
          </div>

          <!-- Allocation Legend & Variance Deltas -->
          <div class="grid grid-cols-1 gap-2 pt-1 sm:grid-cols-3">
            <div
              v-for="sec in sectionAllocations"
              :key="sec.id"
              class="flex items-center justify-between rounded-control border border-border bg-surface p-2.5 text-xs"
            >
              <div class="flex items-center gap-2 truncate">
                <span
                  class="h-2.5 w-2.5 shrink-0 rounded-full"
                  :style="{ backgroundColor: sec.color }"
                />
                <span class="font-medium truncate">{{ sec.name }}</span>
              </div>
              <div class="flex items-center gap-1.5 tnum shrink-0">
                <span class="font-bold text-text">{{ sec.actualPercent }}%</span>
                <span class="text-text-muted">/ {{ sec.targetPercent }}%</span>
                <span
                  class="ml-1 font-semibold"
                  :class="
                    Math.abs(sec.variancePercent) <= 2
                      ? 'text-accent'
                      : sec.variancePercent > 0
                        ? 'text-negative'
                        : 'text-text-muted'
                  "
                >
                  {{ sec.variancePercent > 0 ? `+${sec.variancePercent}%` : `${sec.variancePercent}%` }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </AppCard>

      <!-- Sections & Items -->
      <div class="space-y-4">
        <AppCard v-for="section in plan.sections" :key="section.id" :padded="false">
          <div class="flex flex-wrap items-center gap-2 border-b border-border px-5 py-3.5">
            <span class="text-sm font-semibold uppercase tracking-wider text-text">
              {{ displayName(section.name, locale) }}
            </span>
            <button
              class="inline-flex min-h-[24px] items-center gap-1 rounded-full bg-accent-soft px-2.5 py-1 text-xs font-semibold text-accent transition-opacity hover:opacity-80"
              :title="t('masterPlan.editTarget')"
              @click="openEditTarget(section)"
            >
              {{ t('masterPlan.target') }} {{ section.targetPercent }}%
            </button>
            <span class="ml-auto tnum text-sm font-bold text-text">
              <Money :value="section.total" />
            </span>
            <AppButton variant="secondary" size="sm" @click="openAddItem(section)">
              <Plus :size="14" />
              <span class="hidden sm:inline">{{ t('masterPlan.addItem') }}</span>
            </AppButton>
          </div>

          <div class="scroll-slim overflow-x-auto">
            <table class="w-full text-sm sm:min-w-[560px]">
              <thead>
                <tr class="text-left text-[12px] uppercase text-text-muted">
                  <th class="px-5 py-2 font-medium">{{ t('masterPlan.name') }}</th>
                  <th class="hidden px-5 py-2 text-right font-medium sm:table-cell">
                    {{ t('masterPlan.price') }}
                  </th>
                  <th class="hidden px-5 py-2 text-right font-medium sm:table-cell">
                    {{ t('masterPlan.frequency') }}
                  </th>
                  <th class="px-5 py-2 text-right font-medium">{{ t('masterPlan.total') }}</th>
                  <th class="px-5 py-2"></th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="item in section.items"
                  :key="item.id"
                  class="border-t border-border hover:bg-surface-2 transition-colors"
                >
                  <td class="px-5 py-3 font-medium">{{ item.name }}</td>
                  <td class="hidden px-5 py-3 text-right tnum sm:table-cell text-text-muted">
                    <Money :value="item.price" />
                  </td>
                  <td class="hidden px-5 py-3 text-right tnum sm:table-cell text-text-muted">
                    {{ item.frequency }}× / bln
                  </td>
                  <td class="px-5 py-3 text-right font-semibold tnum text-text">
                    <Money :value="item.totalBudget" />
                  </td>
                  <td class="px-5 py-3">
                    <div class="flex items-center justify-end gap-1">
                      <IconButton
                        :icon="Pencil"
                        :label="t('common.edit')"
                        :size="14"
                        @click="openEditItem(section, item)"
                      />
                      <IconButton
                        :icon="Trash2"
                        :label="t('common.delete')"
                        :size="14"
                        danger
                        @click="removeItem(item)"
                      />
                    </div>
                  </td>
                </tr>
                <tr v-if="!section.items.length">
                  <td
                    colspan="5"
                    class="px-5 py-8 text-center text-xs text-text-muted"
                  >
                    {{ t('masterPlan.itemEmpty') }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </AppCard>
      </div>
    </template>

    <!-- Add / Edit Modal -->
    <AppModal v-if="modalOpen" :title="modalTitle" @close="modalOpen = false">
      <form class="space-y-4" @submit.prevent="save">
        <!-- Item Form Mode -->
        <template v-if="mode === 'item'">
          <FormField :label="t('masterPlan.name')" for-id="mp-name">
            <AppInput id="mp-name" v-model="form.name" required />
          </FormField>

          <!-- Price Input with MoneyInput -->
          <FormField :label="t('masterPlan.price')" for-id="mp-price">
            <MoneyInput
              id="mp-price"
              v-model="form.price"
              :show-chips="false"
              placeholder="0 (mis. 50k, 1.5jt)"
            />
          </FormField>

          <!-- Frequency with Presets -->
          <div>
            <div class="mb-1.5 flex items-center justify-between">
              <label for="mp-freq" class="block text-[13px] font-medium text-text-muted">
                {{ t('masterPlan.frequency') }}
              </label>
              <div class="flex items-center gap-1">
                <button
                  type="button"
                  class="rounded px-2 py-0.5 text-xs font-medium transition-colors"
                  :class="
                    form.frequency === 1
                      ? 'bg-accent-soft text-accent'
                      : 'text-text-muted hover:bg-surface-2'
                  "
                  @click="setFrequencyPreset(1)"
                >
                  1× / bln
                </button>
                <button
                  type="button"
                  class="rounded px-2 py-0.5 text-xs font-medium transition-colors"
                  :class="
                    form.frequency === 2
                      ? 'bg-accent-soft text-accent'
                      : 'text-text-muted hover:bg-surface-2'
                  "
                  @click="setFrequencyPreset(2)"
                >
                  2×
                </button>
                <button
                  type="button"
                  class="rounded px-2 py-0.5 text-xs font-medium transition-colors"
                  :class="
                    form.frequency === 4
                      ? 'bg-accent-soft text-accent'
                      : 'text-text-muted hover:bg-surface-2'
                  "
                  @click="setFrequencyPreset(4)"
                >
                  4× (mingguan)
                </button>
              </div>
            </div>
            <AppInput
              id="mp-freq"
              type="number"
              :model-value="String(form.frequency)"
              @update:model-value="form.frequency = Math.max(1, Number($event))"
            />
          </div>

          <!-- Total Budget Preview -->
          <div class="rounded-control border border-border bg-surface-2 p-3 flex items-center justify-between">
            <span class="text-xs text-text-muted">{{ t('masterPlan.total') }}:</span>
            <span class="text-sm font-bold text-accent tnum">
              <Money :value="liveItemTotal" />
            </span>
          </div>
        </template>

        <!-- Target % Mode -->
        <template v-else>
          <FormField :label="t('masterPlan.target') + ' %'" for-id="mp-target">
            <AppInput
              id="mp-target"
              type="number"
              :model-value="String(form.targetPercent)"
              @update:model-value="form.targetPercent = Number($event)"
            />
          </FormField>

          <!-- Live Target Sum Indicator -->
          <div
            class="flex items-center gap-2 rounded-control border p-3 text-xs"
            :class="
              totalTargetPercent === 100
                ? 'border-accent/40 bg-accent-soft text-accent'
                : 'border-border text-text-muted'
            "
          >
            <component :is="totalTargetPercent === 100 ? CheckCircle2 : AlertCircle" :size="16" />
            <span>
              {{
                totalTargetPercent === 100
                  ? t('masterPlan.targetTotal', { total: totalTargetPercent })
                  : t('masterPlan.targetWarning', { total: totalTargetPercent })
              }}
            </span>
          </div>
        </template>

        <p v-if="formError" class="text-[13px] text-negative" role="alert">{{ formError }}</p>
      </form>

      <template #footer>
        <AppButton variant="secondary" @click="modalOpen = false">
          {{ t('common.cancel') }}
        </AppButton>
        <AppButton :loading="saving" @click="save">
          {{ saving ? t('common.saving') : t('common.save') }}
        </AppButton>
      </template>
    </AppModal>

    <ImportModal
      v-if="importOpen"
      :title="t('import.masterPlan')"
      :config="importConfig"
      @close="importOpen = false"
      @done="load"
    />
  </div>
</template>
