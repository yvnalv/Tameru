<script setup lang="ts">
import { onMounted, ref, computed, reactive } from 'vue';
import { useI18n } from 'vue-i18n';
import { Plus, Pencil, Ban, Upload } from 'lucide-vue-next';
import {
  listAccounts, listAccountGroups, createAccount, updateAccount, deactivateAccount,
  type AccountInput,
} from '@/lib/accounts';
import type { Account, AccountGroup } from '@/types/api';
import { errorMessage } from '@/lib/errorMessage';
import { accountsImportConfig } from '@/lib/importConfigs';
import { useDensity } from '@/composables/useDensity';
import ImportModal from '@/components/ui/ImportModal.vue';
import AppCard from '@/components/ui/AppCard.vue';
import AppButton from '@/components/ui/AppButton.vue';
import AppModal from '@/components/ui/AppModal.vue';
import AppInput from '@/components/ui/AppInput.vue';
import AppSelect from '@/components/ui/AppSelect.vue';
import FormField from '@/components/ui/FormField.vue';
import Money from '@/components/ui/Money.vue';

const { t, te } = useI18n();
const { rowPad } = useDensity();
const importOpen = ref(false);
const importConfig = computed(() => accountsImportConfig(groups.value));

async function onImported(): Promise<void> {
  await load();
}

const accounts = ref<Account[]>([]);
const groups = ref<AccountGroup[]>([]);
const loading = ref(true);
const failed = ref(false);

const modalOpen = ref(false);
const editingId = ref<string | null>(null);
const saving = ref(false);
const formError = ref('');

const ACCOUNT_TYPES = ['Cash', 'Bank', 'EWallet', 'Investment', 'Blocked'];

const form = reactive<AccountInput>({
  name: '',
  type: 'Bank',
  openingBalance: 0,
  groupId: null,
  currencyCode: 'IDR',
  sortOrder: 0,
});

const typeOptions = computed(() =>
  ACCOUNT_TYPES.map((v) => ({ value: v, label: t(`enums.accountType.${v}`) })),
);
const groupOptions = computed(() => [
  { value: '', label: t('accounts.ungrouped') },
  ...groups.value.map((g) => ({ value: g.id, label: g.name })),
]);

const total = computed(() =>
  accounts.value.filter((a) => a.isActive).reduce((sum, a) => sum + a.balance, 0),
);

async function load(): Promise<void> {
  loading.value = true;
  failed.value = false;
  try {
    [accounts.value, groups.value] = await Promise.all([listAccounts(true), listAccountGroups()]);
  } catch {
    failed.value = true;
  } finally {
    loading.value = false;
  }
}

function openCreate(): void {
  editingId.value = null;
  Object.assign(form, { name: '', type: 'Bank', openingBalance: 0, groupId: null, currencyCode: 'IDR', sortOrder: accounts.value.length });
  formError.value = '';
  modalOpen.value = true;
}

function openEdit(a: Account): void {
  editingId.value = a.id;
  Object.assign(form, {
    name: a.name, type: a.type, openingBalance: a.openingBalance,
    groupId: a.groupId, currencyCode: a.currencyCode, sortOrder: a.sortOrder,
  });
  formError.value = '';
  modalOpen.value = true;
}

async function save(): Promise<void> {
  saving.value = true;
  formError.value = '';
  const payload: AccountInput = { ...form, groupId: form.groupId || null, openingBalance: Number(form.openingBalance) };
  try {
    if (editingId.value) {
      await updateAccount(editingId.value, payload);
    } else {
      await createAccount(payload);
    }
    modalOpen.value = false;
    await load();
  } catch (error) {
    formError.value = errorMessage(t, te, error);
  } finally {
    saving.value = false;
  }
}

async function deactivate(a: Account): Promise<void> {
  if (!window.confirm(t('accounts.deactivateConfirm'))) return;
  try {
    await deactivateAccount(a.id);
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
      <h1 class="text-lg font-semibold">{{ t('accounts.title') }}</h1>
      <div class="flex items-center gap-2">
        <AppButton variant="secondary" @click="importOpen = true">
          <Upload :size="16" /><span class="hidden sm:inline">{{ t('import.accounts') }}</span>
        </AppButton>
        <AppButton @click="openCreate"><Plus :size="16" />{{ t('accounts.add') }}</AppButton>
      </div>
    </div>

    <div v-if="loading" class="py-16 text-center text-sm text-text-muted">{{ t('common.loading') }}</div>
    <div v-else-if="failed" class="py-16 text-center">
      <p class="text-sm text-text-muted">{{ t('errors.network_error') }}</p>
      <AppButton class="mt-4" variant="secondary" @click="load">{{ t('common.retry') }}</AppButton>
    </div>

    <template v-else>
      <AppCard>
        <p class="text-[13px] text-text-muted">{{ t('accounts.totalNetWorth') }}</p>
        <p class="mt-1 text-2xl font-semibold"><Money :value="total" /></p>
      </AppCard>

      <AppCard v-if="accounts.length" :padded="false">
        <ul class="divide-y divide-border">
          <li
            v-for="a in accounts"
            :key="a.id"
            class="flex items-center gap-3 px-5"
            :class="[rowPad, { 'opacity-50': !a.isActive }]"
          >
            <div class="min-w-0 flex-1">
              <div class="flex items-center gap-2">
                <p class="truncate text-sm font-medium">{{ a.name }}</p>
                <span v-if="!a.isActive" class="rounded-full bg-surface-2 px-1.5 py-0.5 text-[10px] text-text-muted">
                  {{ t('common.inactive') }}
                </span>
              </div>
              <p class="text-[13px] text-text-muted">
                {{ t(`enums.accountType.${a.type}`) }}
                <span v-if="a.groupName"> · {{ a.groupName }}</span>
              </p>
            </div>
            <Money :value="a.balance" :currency="a.currencyCode" class="text-sm font-medium" />
            <div class="flex items-center gap-1">
              <button class="rounded-control p-1.5 text-text-muted hover:bg-surface-2 hover:text-text" :title="t('common.edit')" @click="openEdit(a)">
                <Pencil :size="16" />
              </button>
              <button
                v-if="a.isActive"
                class="rounded-control p-1.5 text-text-muted hover:bg-surface-2 hover:text-negative"
                :title="t('accounts.deactivate')"
                @click="deactivate(a)"
              >
                <Ban :size="16" />
              </button>
            </div>
          </li>
        </ul>
      </AppCard>

      <AppCard v-else>
        <p class="py-8 text-center text-[13px] text-text-muted">{{ t('accounts.empty') }}</p>
      </AppCard>
    </template>

    <AppModal v-if="modalOpen" :title="editingId ? t('accounts.edit') : t('accounts.add')" @close="modalOpen = false">
      <form class="space-y-4" @submit.prevent="save">
        <FormField :label="t('accounts.name')" for-id="acc-name">
          <AppInput id="acc-name" v-model="form.name" required />
        </FormField>
        <div class="grid grid-cols-2 gap-4">
          <FormField :label="t('accounts.type')" for-id="acc-type">
            <AppSelect id="acc-type" v-model="form.type" :options="typeOptions" />
          </FormField>
          <FormField :label="t('accounts.group')" for-id="acc-group">
            <AppSelect
              id="acc-group"
              :model-value="form.groupId ?? ''"
              :options="groupOptions"
              @update:model-value="form.groupId = $event || null"
            />
          </FormField>
        </div>
        <div class="grid grid-cols-2 gap-4">
          <FormField :label="t('accounts.openingBalance')" for-id="acc-opening">
            <AppInput
              id="acc-opening"
              type="number"
              :model-value="String(form.openingBalance)"
              @update:model-value="form.openingBalance = Number($event)"
            />
          </FormField>
          <FormField :label="t('accounts.currency')" for-id="acc-currency">
            <AppInput
              id="acc-currency"
              :model-value="form.currencyCode ?? 'IDR'"
              @update:model-value="form.currencyCode = $event"
            />
          </FormField>
        </div>
        <p v-if="formError" class="text-[13px] text-negative" role="alert">{{ formError }}</p>
      </form>

      <template #footer>
        <AppButton variant="secondary" @click="modalOpen = false">{{ t('common.cancel') }}</AppButton>
        <AppButton :loading="saving" @click="save">{{ saving ? t('common.saving') : t('common.save') }}</AppButton>
      </template>
    </AppModal>

    <ImportModal
      v-if="importOpen"
      :title="t('import.accounts')"
      :config="importConfig"
      @close="importOpen = false"
      @done="onImported"
    />
  </div>
</template>
