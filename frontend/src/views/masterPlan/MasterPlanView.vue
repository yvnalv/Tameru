<script setup lang="ts">
import { onMounted, ref, reactive, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { Plus, Pencil, Trash2 } from 'lucide-vue-next';
import {
  getMasterPlan, createMasterPlanItem, updateMasterPlanItem, deleteMasterPlanItem, updateMasterPlanSection,
} from '@/lib/budgeting';
import type { MasterPlan, MasterPlanItem, MasterPlanSection } from '@/types/api';
import { errorMessage } from '@/lib/errorMessage';
import { displayName } from '@/lib/seededNames';
import AppCard from '@/components/ui/AppCard.vue';
import AppButton from '@/components/ui/AppButton.vue';
import AppModal from '@/components/ui/AppModal.vue';
import AppInput from '@/components/ui/AppInput.vue';
import FormField from '@/components/ui/FormField.vue';
import IconButton from '@/components/ui/IconButton.vue';
import Money from '@/components/ui/Money.vue';

const { t, te, locale } = useI18n();

const plan = ref<MasterPlan | null>(null);
const loading = ref(true);
const failed = ref(false);

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

// --- modal ------------------------------------------------------------------
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
    : editingItemId.value ? t('masterPlan.editItem') : t('masterPlan.addItem'),
);

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
  Object.assign(form, { name: item.name, price: item.price, frequency: item.frequency, sortOrder: item.sortOrder });
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

async function save(): Promise<void> {
  saving.value = true;
  formError.value = '';
  try {
    if (mode.value === 'target') {
      await updateMasterPlanSection(sectionId.value, Number(form.targetPercent));
    } else if (editingItemId.value) {
      await updateMasterPlanItem(editingItemId.value, {
        name: form.name, price: Number(form.price), frequency: Number(form.frequency), sortOrder: form.sortOrder,
      });
    } else {
      await createMasterPlanItem({
        sectionId: sectionId.value, name: form.name,
        price: Number(form.price), frequency: Number(form.frequency), sortOrder: form.sortOrder,
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
  if (!window.confirm(t('masterPlan.deleteConfirm'))) return;
  try {
    await deleteMasterPlanItem(item.id);
    await load();
  } catch (error) {
    window.alert(errorMessage(t, te, error));
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-4">
    <div class="flex items-center justify-between">
      <h1 class="text-lg font-semibold">{{ t('masterPlan.title') }}</h1>
      <div v-if="plan" class="text-right">
        <p class="text-[13px] text-text-muted">{{ t('masterPlan.grandTotal') }}</p>
        <p class="text-lg font-semibold"><Money :value="plan.grandTotal" /></p>
      </div>
    </div>

    <div v-if="loading" class="py-16 text-center text-sm text-text-muted">{{ t('common.loading') }}</div>
    <div v-else-if="failed" class="py-16 text-center">
      <p class="text-sm text-text-muted">{{ t('errors.network_error') }}</p>
      <AppButton class="mt-4" variant="secondary" @click="load">{{ t('common.retry') }}</AppButton>
    </div>

    <div v-else-if="plan" class="space-y-4">
      <AppCard v-for="section in plan.sections" :key="section.id" :padded="false">
        <div class="flex items-center gap-2 border-b border-border px-5 py-3">
          <span class="text-sm font-semibold">{{ displayName(section.name, locale) }}</span>
          <button
            class="inline-flex items-center gap-1 rounded-full bg-accent-soft px-2 py-0.5 text-[11px] font-medium text-accent hover:opacity-80"
            :title="t('masterPlan.editTarget')"
            @click="openEditTarget(section)"
          >
            {{ t('masterPlan.target') }} {{ section.targetPercent }}%
          </button>
          <span class="ml-auto tnum text-sm font-medium"><Money :value="section.total" /></span>
          <AppButton class="ml-3" variant="secondary" @click="openAddItem(section)"><Plus :size="15" />{{ t('masterPlan.addItem') }}</AppButton>
        </div>

        <div class="scroll-slim overflow-x-auto">
        <table class="w-full min-w-[560px] text-sm">
          <thead>
            <tr class="text-left text-[12px] uppercase text-text-muted">
              <th class="px-5 py-2 font-medium">{{ t('masterPlan.name') }}</th>
              <th class="px-5 py-2 text-right font-medium">{{ t('masterPlan.price') }}</th>
              <th class="px-5 py-2 text-right font-medium">{{ t('masterPlan.frequency') }}</th>
              <th class="px-5 py-2 text-right font-medium">{{ t('masterPlan.total') }}</th>
              <th class="px-5 py-2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in section.items" :key="item.id" class="border-t border-border">
              <td class="px-5 py-2.5">{{ item.name }}</td>
              <td class="px-5 py-2.5 text-right"><Money :value="item.price" /></td>
              <td class="px-5 py-2.5 text-right tnum">{{ item.frequency }}</td>
              <td class="px-5 py-2.5 text-right font-medium"><Money :value="item.totalBudget" /></td>
              <td class="px-5 py-2.5">
                <div class="flex items-center justify-end gap-0.5">
                  <IconButton :icon="Pencil" :label="t('common.edit')" :size="14" @click="openEditItem(section, item)" />
                  <IconButton :icon="Trash2" :label="t('common.delete')" :size="14" danger @click="removeItem(item)" />
                </div>
              </td>
            </tr>
            <tr v-if="!section.items.length">
              <td colspan="5" class="px-5 py-6 text-center text-[13px] text-text-muted">{{ t('masterPlan.itemEmpty') }}</td>
            </tr>
          </tbody>
        </table>
        </div>
      </AppCard>
    </div>

    <AppModal v-if="modalOpen" :title="modalTitle" @close="modalOpen = false">
      <form class="space-y-4" @submit.prevent="save">
        <template v-if="mode === 'item'">
          <FormField :label="t('masterPlan.name')" for-id="mp-name">
            <AppInput id="mp-name" v-model="form.name" required />
          </FormField>
          <div class="grid grid-cols-2 gap-4">
            <FormField :label="t('masterPlan.price')" for-id="mp-price">
              <AppInput id="mp-price" type="number" :model-value="String(form.price)" @update:model-value="form.price = Number($event)" />
            </FormField>
            <FormField :label="t('masterPlan.frequency')" for-id="mp-freq">
              <AppInput id="mp-freq" type="number" :model-value="String(form.frequency)" @update:model-value="form.frequency = Number($event)" />
            </FormField>
          </div>
        </template>
        <FormField v-else :label="t('masterPlan.target') + ' %'" for-id="mp-target">
          <AppInput id="mp-target" type="number" :model-value="String(form.targetPercent)" @update:model-value="form.targetPercent = Number($event)" />
        </FormField>
        <p v-if="formError" class="text-[13px] text-negative" role="alert">{{ formError }}</p>
      </form>
      <template #footer>
        <AppButton variant="secondary" @click="modalOpen = false">{{ t('common.cancel') }}</AppButton>
        <AppButton :loading="saving" @click="save">{{ saving ? t('common.saving') : t('common.save') }}</AppButton>
      </template>
    </AppModal>
  </div>
</template>
