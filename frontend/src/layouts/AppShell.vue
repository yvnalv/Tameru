<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue';
import { RouterView } from 'vue-router';
import { Plus } from 'lucide-vue-next';
import AppSidebar from '@/components/layout/AppSidebar.vue';
import AppTopbar from '@/components/layout/AppTopbar.vue';
import MobileNav from '@/components/layout/MobileNav.vue';
import ToastHost from '@/components/ui/ToastHost.vue';
import ConfirmDialog from '@/components/ui/ConfirmDialog.vue';
import TransactionModal from '@/components/transactions/TransactionModal.vue';
import CommandPalette from '@/components/ui/CommandPalette.vue';
import { useTransactionModalStore } from '@/stores/transactionModal';
import { useUiStore } from '@/stores/ui';

const transactionModal = useTransactionModalStore();
const ui = useUiStore();

function onGlobalKeyDown(e: KeyboardEvent): void {
  // Command palette shortcut: Ctrl+K or Cmd+K
  if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
    e.preventDefault();
    ui.toggleCommandPalette();
    return;
  }

  // Escape key closes command palette if active
  if (e.key === 'Escape' && ui.commandPaletteOpen) {
    e.preventDefault();
    ui.closeCommandPalette();
    return;
  }

  const target = e.target as HTMLElement | null;
  const tag = target?.tagName?.toLowerCase();
  const isEditable = target?.isContentEditable;
  if (tag === 'input' || tag === 'textarea' || tag === 'select' || isEditable) return;

  // Single key '/' opens command palette
  if (e.key === '/' && !e.ctrlKey && !e.metaKey && !e.altKey) {
    e.preventDefault();
    ui.openCommandPalette();
    return;
  }

  // Single key 'n' or 't' opens Quick Add Transaction
  if ((e.key.toLowerCase() === 'n' || e.key.toLowerCase() === 't') && !e.ctrlKey && !e.metaKey && !e.altKey) {
    e.preventDefault();
    transactionModal.openCreate();
  }
}

onMounted(() => {
  window.addEventListener('keydown', onGlobalKeyDown);
});

onUnmounted(() => {
  window.removeEventListener('keydown', onGlobalKeyDown);
});
</script>

<template>
  <div class="min-h-screen bg-bg text-text transition-colors duration-200">
    <!-- Pinned Fixed Sidebar on the Left -->
    <AppSidebar />

    <!-- Main Viewport Area (Offset by sidebar width on md+) -->
    <div
      class="flex min-w-0 min-h-screen flex-1 flex-col transition-[margin-left] duration-200"
      :class="ui.sidebarCollapsed ? 'md:ml-[72px]' : 'md:ml-[240px]'"
    >
      <AppTopbar />
      <main class="scroll-slim w-full flex-1 px-4 py-6 pb-28 md:px-8 md:pb-8">
        <RouterView />
      </main>
    </div>

    <!-- Floating Action Button (Quick Add +) -->
    <button
      type="button"
      id="global-quick-add-fab"
      class="group fixed bottom-20 right-4 sm:bottom-8 sm:right-8 z-40 flex h-14 w-14 items-center justify-center rounded-full bg-accent text-accent-contrast shadow-xl shadow-accent/25 hover:shadow-2xl hover:shadow-accent/40 hover:-translate-y-0.5 active:translate-y-0 active:scale-95 transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2"
      :title="$t('transactions.quickAdd') + ' (N)'"
      :aria-label="$t('transactions.quickAdd')"
      @click="transactionModal.openCreate()"
    >
      <Plus :size="24" :stroke-width="2.5" class="transition-transform duration-200 group-hover:rotate-90" />
    </button>

    <MobileNav />
    <ToastHost />
    <ConfirmDialog />
    <TransactionModal />
    <CommandPalette />
  </div>
</template>
