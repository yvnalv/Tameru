<script setup lang="ts">
import { ref, computed, watch, nextTick, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import {
  Search, Plus, Eye, EyeOff, Sun, Moon, Languages,
  LayoutDashboard, ReceiptText, Landmark, PieChart, BarChart3,
  Layers, Compass, Sparkles, CornerDownLeft, X, Wallet, Smartphone, TrendingUp, ShieldAlert,
  Settings, Calendar, Bot,
} from 'lucide-vue-next';
import { useUiStore } from '@/stores/ui';
import { useThemeStore } from '@/stores/theme';
import { useTransactionModalStore } from '@/stores/transactionModal';
import { useAssistantStore } from '@/stores/assistant';
import { listAccounts } from '@/lib/accounts';
import type { Account } from '@/types/api';

interface CommandItem {
  id: string;
  category: 'quickActions' | 'navigation' | 'jumpToAccount';
  title: string;
  description?: string;
  icon: any;
  badge?: string;
  action: () => void;
}

const { t, locale } = useI18n();
const router = useRouter();
const ui = useUiStore();
const themeStore = useThemeStore();
const transactionModal = useTransactionModalStore();
const assistant = useAssistantStore();

const query = ref('');
const activeIndex = ref(0);
const accounts = ref<Account[]>([]);
const searchInputRef = ref<HTMLInputElement | null>(null);

async function loadAccounts(): Promise<void> {
  try {
    accounts.value = await listAccounts(true);
  } catch {
    // Ignore error if network fails
  }
}

onMounted(() => {
  loadAccounts();
});

watch(
  () => ui.commandPaletteOpen,
  (open) => {
    if (open) {
      query.value = '';
      activeIndex.value = 0;
      loadAccounts();
      nextTick(() => {
        searchInputRef.value?.focus();
      });
    }
  },
);

function getAccountIcon(type: string) {
  switch (type) {
    case 'Bank': return Landmark;
    case 'EWallet': return Smartphone;
    case 'Cash': return Wallet;
    case 'Investment': return TrendingUp;
    default: return ShieldAlert;
  }
}

const allItems = computed<CommandItem[]>(() => {
  const items: CommandItem[] = [];

  // Quick Actions
  items.push({
    id: 'action-new-tx',
    category: 'quickActions',
    title: t('transactions.add'),
    description: t('commandPalette.quickActions'),
    icon: Plus,
    badge: 'N',
    action: () => {
      ui.closeCommandPalette();
      transactionModal.openCreate();
    },
  });

  items.push({
    id: 'action-simulate-purchase',
    category: 'quickActions',
    title: t('decision.simulatorTitle'),
    description: t('decision.simulatorPrompt'),
    icon: Sparkles,
    badge: 'S',
    action: () => {
      ui.closeCommandPalette();
      ui.openSimulator();
    },
  });

  items.push({
    id: 'action-open-assistant',
    category: 'quickActions',
    title: t('assistant.title'),
    description: t('assistant.subtitle'),
    icon: Bot,
    badge: 'Ctrl+J',
    action: () => {
      ui.closeCommandPalette();
      assistant.openAssistant();
    },
  });

  items.push({
    id: 'action-toggle-amounts',
    category: 'quickActions',
    title: ui.amountsHidden ? t('common.showAmounts') : t('common.hideAmounts'),
    description: t('commandPalette.quickActions'),
    icon: ui.amountsHidden ? Eye : EyeOff,
    badge: 'H',
    action: () => {
      ui.toggleAmounts();
      ui.closeCommandPalette();
    },
  });

  items.push({
    id: 'action-toggle-theme',
    category: 'quickActions',
    title: themeStore.isDark ? 'Switch to Light Mode' : 'Switch to Dark Mode',
    description: t('commandPalette.quickActions'),
    icon: themeStore.isDark ? Sun : Moon,
    action: () => {
      themeStore.toggleTheme();
      ui.closeCommandPalette();
    },
  });

  items.push({
    id: 'action-toggle-locale',
    category: 'quickActions',
    title: locale.value === 'en' ? 'Ganti Bahasa (Bahasa Indonesia)' : 'Switch Language (English)',
    description: t('commandPalette.quickActions'),
    icon: Languages,
    badge: ui.locale.toUpperCase(),
    action: () => {
      ui.toggleLocale();
      ui.closeCommandPalette();
    },
  });

  items.push({
    id: 'action-settings-cycle',
    category: 'quickActions',
    title: t('settings.setCycleQuickAction'),
    description: t('settings.tabs.cycle'),
    icon: Calendar,
    action: () => {
      ui.closeCommandPalette();
      router.push('/settings?tab=financial-cycle');
    },
  });

  // Navigation Items
  const navItems = [
    { name: 'dashboard', route: '/dashboard', label: t('nav.dashboard'), icon: LayoutDashboard },
    { name: 'transactions', route: '/transactions', label: t('nav.transactions'), icon: ReceiptText },
    { name: 'accounts', route: '/accounts', label: t('nav.accounts'), icon: Landmark },
    { name: 'budget', route: '/budget', label: t('nav.budget'), icon: PieChart },
    { name: 'reports', route: '/reports', label: t('nav.reports'), icon: BarChart3 },
    { name: 'categories', route: '/categories', label: t('nav.categories'), icon: Layers },
    { name: 'masterPlan', route: '/master-plan', label: t('nav.masterPlan'), icon: Compass },
    { name: 'settings', route: '/settings', label: t('nav.settings'), icon: Settings },
    { name: 'designSystem', route: '/design-system', label: 'Design System', icon: Sparkles },
  ];

  for (const nav of navItems) {
    items.push({
      id: `nav-${nav.name}`,
      category: 'navigation',
      title: nav.label,
      description: nav.route,
      icon: nav.icon,
      action: () => {
        ui.closeCommandPalette();
        router.push(nav.route);
      },
    });
  }

  // Account Jumps
  for (const acc of accounts.value) {
    items.push({
      id: `acc-${acc.id}`,
      category: 'jumpToAccount',
      title: acc.name,
      description: `${t(`enums.accountType.${acc.type}`)}${acc.groupName ? ' · ' + acc.groupName : ''}`,
      icon: getAccountIcon(acc.type),
      action: () => {
        ui.closeCommandPalette();
        router.push({ path: '/accounts', query: { accountId: acc.id } });
      },
    });
  }

  return items;
});

const filteredItems = computed<CommandItem[]>(() => {
  const q = query.value.trim().toLowerCase();
  if (!q) return allItems.value;
  return allItems.value.filter((item) =>
    item.title.toLowerCase().includes(q) ||
    item.description?.toLowerCase().includes(q) ||
    item.badge?.toLowerCase().includes(q),
  );
});

// Reset index when query changes
watch(query, () => {
  activeIndex.value = 0;
});

function onKeydown(e: KeyboardEvent): void {
  if (e.key === 'ArrowDown') {
    e.preventDefault();
    if (filteredItems.value.length > 0) {
      activeIndex.value = (activeIndex.value + 1) % filteredItems.value.length;
      scrollActiveIntoView();
    }
  } else if (e.key === 'ArrowUp') {
    e.preventDefault();
    if (filteredItems.value.length > 0) {
      activeIndex.value = (activeIndex.value - 1 + filteredItems.value.length) % filteredItems.value.length;
      scrollActiveIntoView();
    }
  } else if (e.key === 'Enter') {
    e.preventDefault();
    const item = filteredItems.value[activeIndex.value];
    if (item) {
      item.action();
    }
  } else if (e.key === 'Escape') {
    e.preventDefault();
    ui.closeCommandPalette();
  }
}

function scrollActiveIntoView(): void {
  nextTick(() => {
    const el = document.getElementById(`cmd-item-${activeIndex.value}`);
    el?.scrollIntoView({ block: 'nearest' });
  });
}
</script>

<template>
  <div
    v-if="ui.commandPaletteOpen"
    class="fixed inset-0 z-50 flex items-start justify-center px-4 pt-16 sm:pt-24 bg-black/60 backdrop-blur-xs transition-opacity duration-150"
    @click.self="ui.closeCommandPalette()"
  >
    <div
      class="w-full max-w-xl overflow-hidden rounded-2xl border border-border bg-surface shadow-2xl transition-transform duration-150"
      @keydown="onKeydown"
    >
      <!-- Top Search Input Bar -->
      <div class="relative flex items-center border-b border-border px-4 py-3">
        <Search :size="18" class="shrink-0 text-text-muted" />
        <input
          ref="searchInputRef"
          v-model="query"
          type="text"
          class="ml-3 flex-1 bg-transparent text-sm text-text placeholder-text-muted focus:outline-none"
          :placeholder="t('commandPalette.searchAnything') + '...'"
          autocomplete="off"
          spellcheck="false"
        />
        <button
          v-if="query"
          type="button"
          class="mr-2 text-text-muted hover:text-text"
          @click="query = ''"
        >
          <X :size="15" />
        </button>
        <kbd class="hidden sm:inline-flex items-center rounded border border-border bg-surface-2 px-1.5 py-0.5 text-[11px] font-semibold text-text-muted">
          ESC
        </kbd>
      </div>

      <!-- Command Items List -->
      <div class="scroll-slim max-h-[60vh] overflow-y-auto p-2">
        <div v-if="filteredItems.length === 0" class="py-12 text-center text-xs text-text-muted">
          <p>{{ t('transactions.empty') }}</p>
        </div>

        <ul v-else class="space-y-1">
          <li
            v-for="(item, idx) in filteredItems"
            :id="`cmd-item-${idx}`"
            :key="item.id"
            class="group flex cursor-pointer items-center justify-between rounded-xl px-3.5 py-2.5 transition-colors"
            :class="idx === activeIndex
              ? 'bg-accent text-accent-contrast shadow-sm font-medium'
              : 'text-text hover:bg-surface-2'"
            @click="item.action()"
            @mouseenter="activeIndex = idx"
          >
            <div class="flex items-center gap-3 min-w-0">
              <div
                class="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg"
                :class="idx === activeIndex
                  ? 'bg-white/20 text-white'
                  : 'bg-surface-2 text-text-muted group-hover:text-text'"
              >
                <component :is="item.icon" :size="16" />
              </div>
              <div class="min-w-0">
                <p class="truncate text-xs font-semibold">
                  {{ item.title }}
                </p>
                <p
                  v-if="item.description"
                  class="truncate text-[11px]"
                  :class="idx === activeIndex ? 'text-white/80' : 'text-text-muted'"
                >
                  {{ item.description }}
                </p>
              </div>
            </div>

            <div class="flex items-center gap-2 shrink-0">
              <span
                v-if="item.badge"
                class="rounded border px-1.5 py-0.5 text-[10px] font-bold"
                :class="idx === activeIndex
                  ? 'border-white/30 bg-white/20 text-white'
                  : 'border-border bg-surface text-text-muted'"
              >
                {{ item.badge }}
              </span>
              <CornerDownLeft
                v-if="idx === activeIndex"
                :size="14"
                class="text-white/90"
              />
            </div>
          </li>
        </ul>
      </div>

      <!-- Footer Hints -->
      <div class="flex items-center justify-between border-t border-border bg-surface-2/40 px-4 py-2 text-[11px] text-text-muted">
        <div class="flex items-center gap-3">
          <span class="inline-flex items-center gap-1">
            <kbd class="rounded border border-border bg-surface px-1 py-0.2 text-[10px]">↑</kbd>
            <kbd class="rounded border border-border bg-surface px-1 py-0.2 text-[10px]">↓</kbd>
            <span>navigate</span>
          </span>
          <span class="inline-flex items-center gap-1">
            <kbd class="rounded border border-border bg-surface px-1.5 py-0.2 text-[10px]">↵</kbd>
            <span>select</span>
          </span>
        </div>
        <span class="inline-flex items-center gap-1">
          <kbd class="rounded border border-border bg-surface px-1 py-0.2 text-[10px]">esc</kbd>
          <span>close</span>
        </span>
      </div>
    </div>
  </div>
</template>
