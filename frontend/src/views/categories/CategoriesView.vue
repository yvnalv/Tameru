<script setup lang="ts">
import { onMounted, ref, reactive, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { Plus, Pencil, Ban } from 'lucide-vue-next';
import {
  listCategories, createCategory, updateCategory, deactivateCategory,
} from '@/lib/categories';
import type { Category } from '@/types/api';
import { errorMessage } from '@/lib/errorMessage';
import { displayName } from '@/lib/seededNames';
import AppCard from '@/components/ui/AppCard.vue';
import AppButton from '@/components/ui/AppButton.vue';
import AppModal from '@/components/ui/AppModal.vue';
import AppInput from '@/components/ui/AppInput.vue';
import AppSelect from '@/components/ui/AppSelect.vue';
import FormField from '@/components/ui/FormField.vue';

const { t, te, locale } = useI18n();

const cats = ref<Category[]>([]);
const loading = ref(true);
const failed = ref(false);

const byOrder = (a: Category, b: Category) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name);
const budgets = computed(() => cats.value.filter((c) => c.level === 'Budget').sort(byOrder));
const childrenOf = (id: string) => cats.value.filter((c) => c.parentId === id).sort(byOrder);

const FLOWS = ['Any', 'Income', 'Expense', 'Transfer'];
const flowOptions = computed(() => FLOWS.map((f) => ({ value: f, label: t(`enums.categoryFlow.${f}`) })));

async function load(): Promise<void> {
  loading.value = true;
  failed.value = false;
  try {
    cats.value = await listCategories({ includeInactive: true });
  } catch {
    failed.value = true;
  } finally {
    loading.value = false;
  }
}

// --- modal ------------------------------------------------------------------
const modalOpen = ref(false);
const editingId = ref<string | null>(null);
const saving = ref(false);
const formError = ref('');

const form = reactive({
  name: '',
  level: 'Category',
  parentId: null as string | null,
  parentLabel: '',
  flow: 'Any',
  sortOrder: 0,
});

function openAdd(level: string, parent: Category | null): void {
  editingId.value = null;
  form.name = '';
  form.level = level;
  form.parentId = parent?.id ?? null;
  form.parentLabel = parent ? displayName(parent.name, locale.value) : '';
  form.flow = parent ? parent.flow : 'Any';
  form.sortOrder = parent ? childrenOf(parent.id).length : budgets.value.length;
  formError.value = '';
  modalOpen.value = true;
}

function openEdit(c: Category): void {
  editingId.value = c.id;
  form.name = c.name;
  form.level = c.level;
  form.parentId = c.parentId;
  form.parentLabel = '';
  form.flow = c.flow;
  form.sortOrder = c.sortOrder;
  formError.value = '';
  modalOpen.value = true;
}

async function save(): Promise<void> {
  saving.value = true;
  formError.value = '';
  try {
    if (editingId.value) {
      await updateCategory(editingId.value, { name: form.name, flow: form.flow, sortOrder: form.sortOrder });
    } else {
      await createCategory({
        name: form.name, level: form.level, parentId: form.parentId,
        flow: form.flow, sortOrder: form.sortOrder,
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

async function deactivate(c: Category): Promise<void> {
  if (!window.confirm(t('categories.deactivateConfirm'))) return;
  try {
    await deactivateCategory(c.id);
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
      <h1 class="text-lg font-semibold">{{ t('categories.title') }}</h1>
      <AppButton @click="openAdd('Budget', null)"><Plus :size="16" />{{ t('categories.add') }}</AppButton>
    </div>

    <div v-if="loading" class="py-16 text-center text-sm text-text-muted">{{ t('common.loading') }}</div>
    <div v-else-if="failed" class="py-16 text-center">
      <p class="text-sm text-text-muted">{{ t('errors.network_error') }}</p>
      <AppButton class="mt-4" variant="secondary" @click="load">{{ t('common.retry') }}</AppButton>
    </div>

    <div v-else class="space-y-4">
      <AppCard v-for="b in budgets" :key="b.id" :padded="false">
        <!-- Budget header -->
        <div class="flex items-center gap-2 border-b border-border px-5 py-3">
          <span class="text-sm font-semibold" :class="{ 'opacity-50': !b.isActive }">{{ displayName(b.name, locale) }}</span>
          <span class="rounded-full bg-surface-2 px-1.5 py-0.5 text-[10px] text-text-muted">{{ t(`enums.categoryFlow.${b.flow}`) }}</span>
          <span v-if="b.isSystem" class="rounded-full bg-surface-2 px-1.5 py-0.5 text-[10px] text-text-muted">{{ t('common.system') }}</span>
          <div class="ml-auto flex items-center gap-1">
            <button class="rounded-control p-1.5 text-text-muted hover:bg-surface-2 hover:text-text" :title="t('common.edit')" @click="openEdit(b)"><Pencil :size="15" /></button>
            <button class="rounded-control p-1.5 text-text-muted hover:bg-surface-2 hover:text-text" :title="t('categories.addChild')" @click="openAdd('Category', b)"><Plus :size="15" /></button>
          </div>
        </div>

        <!-- Categories -->
        <ul class="divide-y divide-border">
          <li v-for="c in childrenOf(b.id)" :key="c.id" class="px-5">
            <div class="flex items-center gap-2 py-2.5" :class="{ 'opacity-50': !c.isActive }">
              <span class="text-sm">{{ displayName(c.name, locale) }}</span>
              <div class="ml-auto flex items-center gap-1">
                <button class="rounded-control p-1.5 text-text-muted hover:bg-surface-2 hover:text-text" :title="t('categories.addChild')" @click="openAdd('Sub', c)"><Plus :size="14" /></button>
                <button class="rounded-control p-1.5 text-text-muted hover:bg-surface-2 hover:text-text" :title="t('common.edit')" @click="openEdit(c)"><Pencil :size="14" /></button>
                <button v-if="!c.isSystem && c.isActive" class="rounded-control p-1.5 text-text-muted hover:bg-surface-2 hover:text-negative" :title="t('categories.deactivate')" @click="deactivate(c)"><Ban :size="14" /></button>
              </div>
            </div>
            <!-- Subs -->
            <ul v-if="childrenOf(c.id).length" class="border-l border-border pb-1 pl-4">
              <li v-for="s in childrenOf(c.id)" :key="s.id" class="flex items-center gap-2 py-2 text-text-muted" :class="{ 'opacity-50': !s.isActive }">
                <span class="text-[13px]">{{ displayName(s.name, locale) }}</span>
                <div class="ml-auto flex items-center gap-1">
                  <button class="rounded-control p-1.5 hover:bg-surface-2 hover:text-text" :title="t('common.edit')" @click="openEdit(s)"><Pencil :size="13" /></button>
                  <button v-if="!s.isSystem && s.isActive" class="rounded-control p-1.5 hover:bg-surface-2 hover:text-negative" :title="t('categories.deactivate')" @click="deactivate(s)"><Ban :size="13" /></button>
                </div>
              </li>
            </ul>
          </li>
          <li v-if="!childrenOf(b.id).length" class="px-5 py-3 text-[13px] text-text-muted">{{ t('categories.empty') }}</li>
        </ul>
      </AppCard>
    </div>

    <AppModal v-if="modalOpen" :title="editingId ? t('categories.edit') : t('categories.add')" @close="modalOpen = false">
      <form class="space-y-4" @submit.prevent="save">
        <p v-if="form.parentLabel" class="text-[13px] text-text-muted">
          {{ t('categories.parent') }}: <span class="text-text">{{ form.parentLabel }}</span>
        </p>
        <FormField :label="t('categories.name')" for-id="cat-name">
          <AppInput id="cat-name" v-model="form.name" required />
        </FormField>
        <FormField :label="t('categories.flow')" for-id="cat-flow">
          <AppSelect id="cat-flow" v-model="form.flow" :options="flowOptions" />
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
