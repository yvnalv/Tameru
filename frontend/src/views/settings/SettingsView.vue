<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import {
  Calendar,
  Palette,
  User as UserIcon,
  Database,
  Check,
  Save,
  Download,
  LogOut,
  Info,
  Shield,
  Sparkles,
  ArrowRight,
  Sun,
  Moon,
} from 'lucide-vue-next';
import { useAuthStore } from '@/stores/auth';
import { useUiStore } from '@/stores/ui';
import { useThemeStore } from '@/stores/theme';
import { useToastStore } from '@/stores/toast';
import { useConfirmStore } from '@/stores/confirm';
import { listTransactions } from '@/lib/transactions';
import { toCsv, downloadCsv } from '@/lib/csv';
import type { Transaction } from '@/types/api';
import AppCard from '@/components/ui/AppCard.vue';
import AppButton from '@/components/ui/AppButton.vue';
import AppToggle from '@/components/ui/AppToggle.vue';
import FormField from '@/components/ui/FormField.vue';
import AvatarChip from '@/components/ui/AvatarChip.vue';

const { t, locale } = useI18n();
const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const ui = useUiStore();
const themeStore = useThemeStore();
const toast = useToastStore();
const confirm = useConfirmStore();

type TabKey = 'financial-cycle' | 'appearance' | 'profile' | 'data';
const activeTab = ref<TabKey>('financial-cycle');

onMounted(() => {
  const queryTab = route.query.tab as string;
  if (queryTab && ['financial-cycle', 'appearance', 'profile', 'data'].includes(queryTab)) {
    activeTab.value = queryTab as TabKey;
  }
});

function setTab(tab: TabKey): void {
  activeTab.value = tab;
  router.replace({ query: { ...route.query, tab } });
}

// ----------------------------------------------------------------------------
// Tab 1: Financial Cycle
// ----------------------------------------------------------------------------
const initialCycleDay = computed(() => auth.user?.budgetCycleStartDay ?? 1);
const cycleStartDay = ref<number>(auth.user?.budgetCycleStartDay ?? 1);
const savingCycle = ref(false);

const isCycleChanged = computed(() => cycleStartDay.value !== initialCycleDay.value);

function setPresetDay(day: number): void {
  cycleStartDay.value = day;
}

// Computed date ranges for live preview
const cyclePreview = computed(() => {
  const day = Math.max(1, Math.min(28, Number(cycleStartDay.value) || 1));
  const now = new Date();

  // Determine current active cycle start & end
  let currentStartYear = now.getFullYear();
  let currentStartMonth = now.getMonth(); // 0-indexed

  if (day > 1 && now.getDate() < day) {
    // Current date is before payday, cycle began on payday of last month
    const prev = new Date(now.getFullYear(), now.getMonth() - 1, 1);
    currentStartYear = prev.getFullYear();
    currentStartMonth = prev.getMonth();
  }

  const startDate = new Date(currentStartYear, currentStartMonth, day);
  const nextMonth = new Date(currentStartYear, currentStartMonth + 1, day);
  const endDate = day === 1
    ? new Date(currentStartYear, currentStartMonth + 1, 0)
    : new Date(nextMonth.getTime() - 24 * 60 * 60 * 1000);

  const totalDays = Math.max(1, Math.round((endDate.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24)) + 1);

  // Days elapsed in current cycle
  const diffTime = now.getTime() - startDate.getTime();
  const elapsedDays = Math.max(0, Math.min(totalDays, Math.floor(diffTime / (1000 * 60 * 60 * 24)) + 1));
  const daysLeft = Math.max(0, totalDays - elapsedDays);
  const progressPct = Math.round((elapsedDays / totalDays) * 100);

  const startFormatted = startDate.toLocaleDateString(locale.value, { day: 'numeric', month: 'short', year: 'numeric' });
  const endFormatted = endDate.toLocaleDateString(locale.value, { day: 'numeric', month: 'short', year: 'numeric' });

  // Next cycle preview
  const nextStart = nextMonth;
  const nextEnd = day === 1
    ? new Date(currentStartYear, currentStartMonth + 2, 0)
    : new Date(new Date(currentStartYear, currentStartMonth + 2, day).getTime() - 24 * 60 * 60 * 1000);
  const nextStartFormatted = nextStart.toLocaleDateString(locale.value, { day: 'numeric', month: 'short', year: 'numeric' });
  const nextEndFormatted = nextEnd.toLocaleDateString(locale.value, { day: 'numeric', month: 'short', year: 'numeric' });

  return {
    day,
    startFormatted,
    endFormatted,
    totalDays,
    elapsedDays,
    daysLeft,
    progressPct,
    nextStartFormatted,
    nextEndFormatted,
  };
});

async function saveFinancialCycle(): Promise<void> {
  const day = Math.max(1, Math.min(28, Number(cycleStartDay.value) || 1));
  savingCycle.value = true;
  try {
    await auth.updateProfile({ budgetCycleStartDay: day });
    cycleStartDay.value = day;
    toast.success(t('settings.cycleSavedToast'));
  } catch {
    toast.error(t('settings.cycleSaveError'));
  } finally {
    savingCycle.value = false;
  }
}

// ----------------------------------------------------------------------------
// Tab 2: Appearance & Display
// ----------------------------------------------------------------------------
function selectTheme(mode: 'light' | 'dark'): void {
  themeStore.setTheme(mode);
}

// ----------------------------------------------------------------------------
// Tab 3: Account & Profile
// ----------------------------------------------------------------------------
const displayNameInput = ref(auth.user?.displayName || '');
const savingProfile = ref(false);

const isProfileChanged = computed(
  () => displayNameInput.value.trim() !== (auth.user?.displayName || '').trim(),
);

async function saveProfile(): Promise<void> {
  if (!displayNameInput.value.trim()) return;
  savingProfile.value = true;
  try {
    await auth.updateProfile({ displayName: displayNameInput.value.trim() });
    toast.success(t('settings.profileSavedToast'));
  } catch {
    toast.error(t('settings.profileSaveError'));
  } finally {
    savingProfile.value = false;
  }
}

async function handleSignOut(): Promise<void> {
  const ok = await confirm.ask({
    title: t('common.signOut'),
    message: t('settings.signOutConfirmMessage'),
    confirmLabel: t('common.signOut'),
    danger: true,
  });
  if (ok) {
    await auth.logout();
    router.push({ name: 'login' });
  }
}

// ----------------------------------------------------------------------------
// Tab 4: Data & Backup
// ----------------------------------------------------------------------------
const exportingCsv = ref(false);

async function exportTransactionsCsv(): Promise<void> {
  exportingCsv.value = true;
  try {
    const res = await listTransactions({ page: 1, pageSize: 10000 });
    const rows = res.items;
    const csv = toCsv<Transaction>(rows, [
      { header: 'Date', value: (t) => t.date },
      { header: 'Title', value: (t) => t.title },
      { header: 'Type', value: (t) => t.type },
      { header: 'Amount', value: (t) => t.amount },
      { header: 'Currency', value: (t) => t.currencyCode },
      { header: 'Status', value: (t) => t.status },
      { header: 'Description', value: (t) => t.description ?? '' },
    ]);
    const dateStr = new Date().toISOString().slice(0, 10);
    downloadCsv(`tameru-transactions-backup-${dateStr}.csv`, csv);
    toast.success(t('settings.exportSuccessToast'));
  } catch {
    toast.error(t('settings.exportErrorToast'));
  } finally {
    exportingCsv.value = false;
  }
}
</script>

<template>
  <div class="space-y-6 max-w-5xl mx-auto">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 border-b border-border pb-5">
      <div>
        <h1 class="text-2xl font-bold tracking-tight text-text">
          {{ t('settings.title') }}
        </h1>
        <p class="text-xs text-text-muted mt-1">
          {{ t('settings.subtitle') }}
        </p>
      </div>

      <!-- Quick Status Badges -->
      <div class="flex items-center gap-2">
        <span class="inline-flex items-center gap-1.5 rounded-full bg-accent-soft px-3 py-1 text-xs font-semibold text-accent">
          <Sparkles :size="13" />
          {{ t('settings.cycleActiveBadge', { day: auth.user?.budgetCycleStartDay ?? 1 }) }}
        </span>
        <span class="inline-flex items-center rounded-full bg-surface-2 px-2.5 py-1 text-xs font-medium text-text-muted border border-border">
          IDR (Rp)
        </span>
      </div>
    </div>

    <!-- Tab Navigation Bar -->
    <div class="flex flex-wrap gap-1.5 border-b border-border pb-1">
      <button
        type="button"
        class="flex items-center gap-2 rounded-t-lg border-b-2 px-4 py-2.5 text-xs font-semibold transition-all"
        :class="
          activeTab === 'financial-cycle'
            ? 'border-accent bg-surface text-accent shadow-xs'
            : 'border-transparent text-text-muted hover:bg-surface-2 hover:text-text'
        "
        @click="setTab('financial-cycle')"
      >
        <Calendar :size="15" />
        {{ t('settings.tabs.cycle') }}
      </button>

      <button
        type="button"
        class="flex items-center gap-2 rounded-t-lg border-b-2 px-4 py-2.5 text-xs font-semibold transition-all"
        :class="
          activeTab === 'appearance'
            ? 'border-accent bg-surface text-accent shadow-xs'
            : 'border-transparent text-text-muted hover:bg-surface-2 hover:text-text'
        "
        @click="setTab('appearance')"
      >
        <Palette :size="15" />
        {{ t('settings.tabs.appearance') }}
      </button>

      <button
        type="button"
        class="flex items-center gap-2 rounded-t-lg border-b-2 px-4 py-2.5 text-xs font-semibold transition-all"
        :class="
          activeTab === 'profile'
            ? 'border-accent bg-surface text-accent shadow-xs'
            : 'border-transparent text-text-muted hover:bg-surface-2 hover:text-text'
        "
        @click="setTab('profile')"
      >
        <UserIcon :size="15" />
        {{ t('settings.tabs.profile') }}
      </button>

      <button
        type="button"
        class="flex items-center gap-2 rounded-t-lg border-b-2 px-4 py-2.5 text-xs font-semibold transition-all"
        :class="
          activeTab === 'data'
            ? 'border-accent bg-surface text-accent shadow-xs'
            : 'border-transparent text-text-muted hover:bg-surface-2 hover:text-text'
        "
        @click="setTab('data')"
      >
        <Database :size="15" />
        {{ t('settings.tabs.data') }}
      </button>
    </div>

    <!-- ===================================================================== -->
    <!-- TAB 1: Financial Cycle & Starting Day                                  -->
    <!-- ===================================================================== -->
    <div v-if="activeTab === 'financial-cycle'" class="space-y-6">
      <!-- Main Starting Day Card -->
      <AppCard>
        <div class="flex items-start justify-between gap-4">
          <div>
            <h2 class="text-base font-bold text-text">
              {{ t('settings.cycleHeading') }}
            </h2>
            <p class="text-xs text-text-muted mt-1 max-w-2xl">
              {{ t('settings.cycleDescription') }}
            </p>
          </div>
          <span class="rounded-control bg-accent-soft p-2 text-accent">
            <Calendar :size="20" />
          </span>
        </div>

        <!-- Explanatory Banner -->
        <div class="mt-4 flex items-start gap-3 rounded-control border border-accent/25 bg-accent-soft/40 p-3.5 text-xs text-text">
          <Info :size="18" class="text-accent shrink-0 mt-0.5" />
          <div class="space-y-1">
            <p class="font-semibold text-accent">
              {{ t('settings.cycleHowItWorksTitle') }}
            </p>
            <p class="text-text-muted leading-relaxed">
              {{ t('settings.cycleHowItWorksBody', { day: cyclePreview.day }) }}
            </p>
          </div>
        </div>

        <!-- Starting Day Input & Quick Presets -->
        <div class="mt-6 grid grid-cols-1 md:grid-cols-2 gap-6 items-end">
          <div>
            <FormField :label="t('settings.startingDayLabel')" :hint="t('settings.startingDayHint')">
              <div class="flex items-center gap-2 mt-1">
                <input
                  v-model.number="cycleStartDay"
                  type="number"
                  min="1"
                  max="28"
                  class="h-10 w-28 rounded-control border border-border bg-surface px-3 text-center text-base font-bold text-text shadow-xs focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent"
                />
                <span class="text-xs text-text-muted font-medium">
                  {{ t('settings.ofEveryMonth') }}
                </span>
              </div>
            </FormField>

            <!-- Quick Presets -->
            <div class="mt-3 flex items-center gap-2">
              <span class="text-[11px] text-text-muted font-medium">{{ t('settings.presets') }}:</span>
              <button
                type="button"
                class="rounded-control border px-2.5 py-1 text-xs font-semibold transition-all"
                :class="
                  cycleStartDay === 1
                    ? 'border-accent bg-accent text-accent-contrast shadow-xs'
                    : 'border-border bg-surface-2 text-text-muted hover:border-border-strong hover:text-text'
                "
                @click="setPresetDay(1)"
              >
                {{ t('settings.presetCalendarMonth') }}
              </button>
              <button
                type="button"
                class="rounded-control border px-2.5 py-1 text-xs font-semibold transition-all"
                :class="
                  cycleStartDay === 25
                    ? 'border-accent bg-accent text-accent-contrast shadow-xs'
                    : 'border-border bg-surface-2 text-text-muted hover:border-border-strong hover:text-text'
                "
                @click="setPresetDay(25)"
              >
                {{ t('settings.presetPayday25') }}
              </button>
            </div>
          </div>

          <!-- Save Button -->
          <div class="flex justify-start md:justify-end">
            <AppButton
              variant="primary"
              :loading="savingCycle"
              :disabled="!isCycleChanged && !savingCycle"
              class="w-full sm:w-auto"
              @click="saveFinancialCycle"
            >
              <Save :size="16" />
              {{ t('settings.saveCycleButton') }}
            </AppButton>
          </div>
        </div>

        <!-- Live Dynamic Cycle Preview Box -->
        <div class="mt-6 rounded-container border border-border bg-surface-2/60 p-4 space-y-4">
          <div class="flex items-center justify-between">
            <span class="text-xs font-bold uppercase tracking-wider text-text-muted">
              {{ t('settings.livePreviewTitle') }}
            </span>
            <span class="text-xs font-semibold text-accent">
              {{ t('settings.daysInCycle', { count: cyclePreview.totalDays }) }}
            </span>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <!-- Current Cycle Box -->
            <div class="rounded-control border border-border bg-surface p-3 space-y-1.5 shadow-xs">
              <span class="text-[11px] font-semibold text-text-muted uppercase">
                {{ t('settings.activeCurrentCycle') }}
              </span>
              <p class="text-sm font-bold text-text flex items-center gap-1.5">
                <span>{{ cyclePreview.startFormatted }}</span>
                <ArrowRight :size="14" class="text-text-muted shrink-0" />
                <span>{{ cyclePreview.endFormatted }}</span>
              </p>
              <div class="pt-1">
                <div class="flex items-center justify-between text-[11px] text-text-muted">
                  <span>{{ t('settings.cycleElapsed', { day: cyclePreview.elapsedDays, total: cyclePreview.totalDays }) }}</span>
                  <span class="font-semibold text-accent">{{ cyclePreview.daysLeft }} {{ t('settings.daysLeft') }}</span>
                </div>
                <div class="mt-1.5 h-1.5 w-full rounded-full bg-surface-2 overflow-hidden">
                  <div class="h-full bg-accent rounded-full transition-all duration-300" :style="{ width: `${cyclePreview.progressPct}%` }" />
                </div>
              </div>
            </div>

            <!-- Next Cycle Box -->
            <div class="rounded-control border border-border bg-surface p-3 space-y-1.5 shadow-xs">
              <span class="text-[11px] font-semibold text-text-muted uppercase">
                {{ t('settings.nextCycle') }}
              </span>
              <p class="text-sm font-bold text-text flex items-center gap-1.5">
                <span>{{ cyclePreview.nextStartFormatted }}</span>
                <ArrowRight :size="14" class="text-text-muted shrink-0" />
                <span>{{ cyclePreview.nextEndFormatted }}</span>
              </p>
              <p class="text-[11px] text-text-muted pt-1">
                {{ t('settings.nextCycleAutoSwitch', { day: cyclePreview.day }) }}
              </p>
            </div>
          </div>
        </div>
      </AppCard>

      <!-- Currency & Accounting Standards Card -->
      <AppCard>
        <h2 class="text-base font-bold text-text">
          {{ t('settings.currencyHeading') }}
        </h2>
        <p class="text-xs text-text-muted mt-1">
          {{ t('settings.currencyDescription') }}
        </p>

        <div class="mt-4 grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div class="rounded-control border border-border bg-surface-2 p-3">
            <span class="text-[11px] text-text-muted font-medium">{{ t('settings.functionalCurrency') }}</span>
            <p class="text-sm font-bold text-text mt-0.5">IDR - Indonesian Rupiah</p>
            <span class="text-xs text-text-muted">Rp (Rupiah)</span>
          </div>

          <div class="rounded-control border border-border bg-surface-2 p-3">
            <span class="text-[11px] text-text-muted font-medium">{{ t('settings.currencyFormat') }}</span>
            <p class="text-sm font-bold text-text mt-0.5">Rp 1.250.000,00</p>
            <span class="text-xs text-text-muted">id-ID locale</span>
          </div>

          <div class="rounded-control border border-border bg-surface-2 p-3">
            <span class="text-[11px] text-text-muted font-medium">{{ t('settings.roundingMode') }}</span>
            <p class="text-sm font-bold text-text mt-0.5">Two Decimals (IDR standard)</p>
            <span class="text-xs text-text-muted">Banker's Rounding</span>
          </div>
        </div>
      </AppCard>
    </div>

    <!-- ===================================================================== -->
    <!-- TAB 2: Appearance & Display                                            -->
    <!-- ===================================================================== -->
    <div v-else-if="activeTab === 'appearance'" class="space-y-6">
      <AppCard>
        <h2 class="text-base font-bold text-text">
          {{ t('settings.themeHeading') }}
        </h2>
        <p class="text-xs text-text-muted mt-1">
          {{ t('settings.themeDescription') }}
        </p>

        <!-- Modern Theme Selector Cards -->
        <div class="mt-4 grid grid-cols-1 sm:grid-cols-2 gap-3">
          <button
            type="button"
            class="flex items-center gap-3 rounded-container border p-4 text-left transition-all"
            :class="
              themeStore.theme === 'dark'
                ? 'border-accent bg-accent-soft/40 shadow-xs ring-1 ring-accent'
                : 'border-border bg-surface hover:border-border-strong hover:bg-surface-2'
            "
            @click="selectTheme('dark')"
          >
            <div class="rounded-control bg-surface-2 p-2.5 text-accent">
              <Moon :size="18" />
            </div>
            <div>
              <p class="text-sm font-bold text-text">{{ t('settings.themeDark') }}</p>
              <p class="text-xs text-text-muted">{{ t('settings.themeDarkDesc') }}</p>
            </div>
            <Check v-if="themeStore.theme === 'dark'" :size="18" class="ml-auto text-accent shrink-0" />
          </button>

          <button
            type="button"
            class="flex items-center gap-3 rounded-container border p-4 text-left transition-all"
            :class="
              themeStore.theme === 'light'
                ? 'border-accent bg-accent-soft/40 shadow-xs ring-1 ring-accent'
                : 'border-border bg-surface hover:border-border-strong hover:bg-surface-2'
            "
            @click="selectTheme('light')"
          >
            <div class="rounded-control bg-surface-2 p-2.5 text-accent">
              <Sun :size="18" />
            </div>
            <div>
              <p class="text-sm font-bold text-text">{{ t('settings.themeLight') }}</p>
              <p class="text-xs text-text-muted">{{ t('settings.themeLightDesc') }}</p>
            </div>
            <Check v-if="themeStore.theme === 'light'" :size="18" class="ml-auto text-accent shrink-0" />
          </button>
        </div>
      </AppCard>

      <!-- Display Preferences Card -->
      <AppCard>
        <h2 class="text-base font-bold text-text">
          {{ t('settings.displayHeading') }}
        </h2>
        <p class="text-xs text-text-muted mt-1">
          {{ t('settings.displayDescription') }}
        </p>

        <div class="mt-5 space-y-4 divide-y divide-border">
          <!-- Density Toggle -->
          <div class="flex items-center justify-between pt-3 first:pt-0">
            <div>
              <p class="text-sm font-bold text-text">{{ t('settings.densityTitle') }}</p>
              <p class="text-xs text-text-muted">{{ t('settings.densityDesc') }}</p>
            </div>
            <div class="flex items-center gap-1.5 rounded-xl border border-border bg-surface-2 p-1">
              <button
                type="button"
                class="rounded-lg px-3 py-1.5 text-xs font-semibold transition-all"
                :class="ui.density === 'comfortable' ? 'bg-accent text-accent-contrast shadow-xs' : 'text-text-muted hover:text-text'"
                @click="ui.setDensity('comfortable')"
              >
                {{ t('density.comfortable') }}
              </button>
              <button
                type="button"
                class="rounded-lg px-3 py-1.5 text-xs font-semibold transition-all"
                :class="ui.density === 'compact' ? 'bg-accent text-accent-contrast shadow-xs' : 'text-text-muted hover:text-text'"
                @click="ui.setDensity('compact')"
              >
                {{ t('density.compact') }}
              </button>
            </div>
          </div>

          <!-- Mask Numbers / Privacy Toggle -->
          <div class="flex items-center justify-between pt-3">
            <div>
              <p class="text-sm font-bold text-text">{{ t('settings.privacyTitle') }}</p>
              <p class="text-xs text-text-muted">{{ t('settings.privacyDesc') }}</p>
            </div>
            <AppToggle :model-value="ui.amountsHidden" @update:model-value="ui.toggleAmounts()" />
          </div>

          <!-- Language Selector -->
          <div class="flex items-center justify-between pt-3">
            <div>
              <p class="text-sm font-bold text-text">{{ t('settings.languageTitle') }}</p>
              <p class="text-xs text-text-muted">{{ t('settings.languageDesc') }}</p>
            </div>
            <div class="flex items-center gap-1.5 rounded-xl border border-border bg-surface-2 p-1">
              <button
                type="button"
                class="rounded-lg px-3 py-1.5 text-xs font-semibold transition-all"
                :class="ui.locale === 'en' ? 'bg-accent text-accent-contrast shadow-xs' : 'text-text-muted hover:text-text'"
                @click="ui.changeLocale('en')"
              >
                English (EN)
              </button>
              <button
                type="button"
                class="rounded-lg px-3 py-1.5 text-xs font-semibold transition-all"
                :class="ui.locale === 'id' ? 'bg-accent text-accent-contrast shadow-xs' : 'text-text-muted hover:text-text'"
                @click="ui.changeLocale('id')"
              >
                Bahasa Indonesia (ID)
              </button>
            </div>
          </div>
        </div>
      </AppCard>
    </div>

    <!-- ===================================================================== -->
    <!-- TAB 3: Account & Profile                                               -->
    <!-- ===================================================================== -->
    <div v-else-if="activeTab === 'profile'" class="space-y-6">
      <AppCard>
        <div class="flex items-center gap-4">
          <AvatarChip :name="auth.user?.displayName || auth.user?.email || 'User'" />
          <div>
            <h2 class="text-base font-bold text-text">
              {{ auth.user?.displayName || auth.user?.email }}
            </h2>
            <div class="flex items-center gap-2 mt-0.5">
              <span class="text-xs text-text-muted font-mono">{{ auth.user?.email }}</span>
              <span class="rounded-full bg-accent-soft px-2 py-0.5 text-[10px] font-semibold text-accent">
                {{ t('settings.ownerRole') }}
              </span>
            </div>
          </div>
        </div>

        <div class="mt-6 border-t border-border pt-5 space-y-4 max-w-md">
          <FormField :label="t('settings.displayNameLabel')" :hint="t('settings.displayNameHint')">
            <input
              v-model="displayNameInput"
              type="text"
              class="mt-1 h-10 w-full rounded-control border border-border bg-surface px-3 text-sm text-text shadow-xs focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent"
              :placeholder="t('settings.displayNamePlaceholder')"
            />
          </FormField>

          <AppButton
            variant="primary"
            :loading="savingProfile"
            :disabled="!isProfileChanged && !savingProfile"
            @click="saveProfile"
          >
            <Save :size="16" />
            {{ t('settings.saveProfileButton') }}
          </AppButton>
        </div>
      </AppCard>

      <!-- Security & Session Card -->
      <AppCard>
        <div class="flex items-start justify-between">
          <div>
            <h2 class="text-base font-bold text-text">
              {{ t('settings.securityHeading') }}
            </h2>
            <p class="text-xs text-text-muted mt-1">
              {{ t('settings.securityDescription') }}
            </p>
          </div>
          <Shield :size="20" class="text-accent" />
        </div>

        <div class="mt-4 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 rounded-control border border-border bg-surface-2 p-4">
          <div>
            <p class="text-sm font-semibold text-text">{{ t('settings.activeSessionTitle') }}</p>
            <p class="text-xs text-text-muted mt-0.5">{{ t('settings.activeSessionDesc') }}</p>
          </div>
          <AppButton variant="secondary" danger @click="handleSignOut">
            <LogOut :size="16" />
            {{ t('common.signOut') }}
          </AppButton>
        </div>
      </AppCard>
    </div>

    <!-- ===================================================================== -->
    <!-- TAB 4: Data & Backup                                                   -->
    <!-- ===================================================================== -->
    <div v-else-if="activeTab === 'data'" class="space-y-6">
      <AppCard>
        <div class="flex items-start justify-between">
          <div>
            <h2 class="text-base font-bold text-text">
              {{ t('settings.dataHeading') }}
            </h2>
            <p class="text-xs text-text-muted mt-1">
              {{ t('settings.dataDescription') }}
            </p>
          </div>
          <Database :size="20" class="text-accent" />
        </div>

        <!-- Direct CSV Download -->
        <div class="mt-5 rounded-control border border-border bg-surface-2 p-4 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <p class="text-sm font-semibold text-text">{{ t('settings.exportTransactionsTitle') }}</p>
            <p class="text-xs text-text-muted mt-0.5">{{ t('settings.exportTransactionsDesc') }}</p>
          </div>
          <AppButton
            variant="secondary"
            :loading="exportingCsv"
            class="shrink-0"
            @click="exportTransactionsCsv"
          >
            <Download :size="16" />
            {{ t('settings.exportCsvButton') }}
          </AppButton>
        </div>
      </AppCard>

      <!-- System Architecture & Environment Card -->
      <AppCard>
        <h2 class="text-base font-bold text-text">
          {{ t('settings.systemHeading') }}
        </h2>
        <p class="text-xs text-text-muted mt-1">
          {{ t('settings.systemDescription') }}
        </p>

        <div class="mt-4 grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-3">
          <div class="rounded-control border border-border bg-surface-2 p-3">
            <span class="text-[11px] text-text-muted font-medium">{{ t('settings.appVersion') }}</span>
            <p class="text-sm font-bold text-text mt-0.5">Tameru v0.1</p>
            <span class="text-xs text-accent font-medium">{{ t('settings.upToDate') }}</span>
          </div>

          <div class="rounded-control border border-border bg-surface-2 p-3">
            <span class="text-[11px] text-text-muted font-medium">{{ t('settings.backendStack') }}</span>
            <p class="text-sm font-bold text-text mt-0.5">.NET 8 Web API</p>
            <span class="text-xs text-text-muted">Modular Monolith</span>
          </div>

          <div class="rounded-control border border-border bg-surface-2 p-3">
            <span class="text-[11px] text-text-muted font-medium">{{ t('settings.databaseEngine') }}</span>
            <p class="text-sm font-bold text-text mt-0.5">PostgreSQL 16</p>
            <span class="text-xs text-text-muted">Self-Hosted</span>
          </div>

          <div class="rounded-control border border-border bg-surface-2 p-3">
            <span class="text-[11px] text-text-muted font-medium">{{ t('settings.frontendStack') }}</span>
            <p class="text-sm font-bold text-text mt-0.5">Vue 3 + Vite</p>
            <span class="text-xs text-text-muted">Modern Cobalt DS</span>
          </div>
        </div>
      </AppCard>
    </div>
  </div>
</template>
