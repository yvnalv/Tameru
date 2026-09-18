<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue';
import { RouterView } from 'vue-router';
import AppSidebar from '@/components/layout/AppSidebar.vue';
import AppTopbar from '@/components/layout/AppTopbar.vue';
import MobileNav from '@/components/layout/MobileNav.vue';
import ToastHost from '@/components/ui/ToastHost.vue';
import ConfirmDialog from '@/components/ui/ConfirmDialog.vue';
import TransactionModal from '@/components/transactions/TransactionModal.vue';
import CommandPalette from '@/components/ui/CommandPalette.vue';
import PurchaseSimulatorModal from '@/components/decision/PurchaseSimulatorModal.vue';
import AssistantChat from '@/components/assistant/AssistantChat.vue';
import TameruAssistantIcon from '@/components/brand/TameruAssistantIcon.vue';
import { useTransactionModalStore } from '@/stores/transactionModal';
import { useUiStore } from '@/stores/ui';
import { useAssistantStore } from '@/stores/assistant';

const transactionModal = useTransactionModalStore();
const ui = useUiStore();
const assistant = useAssistantStore();

function onGlobalKeyDown(e: KeyboardEvent): void {
  // Command palette shortcut: Ctrl+K or Cmd+K
  if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
    e.preventDefault();
    ui.toggleCommandPalette();
    return;
  }

  // AI Assistant shortcut: Ctrl+J or Cmd+J
  if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'j') {
    e.preventDefault();
    assistant.toggleAssistant();
    return;
  }

  // Escape key closes command palette or assistant if active
  if (e.key === 'Escape' && ui.commandPaletteOpen) {
    e.preventDefault();
    ui.closeCommandPalette();
    return;
  }

  if (e.key === 'Escape' && assistant.isOpen) {
    e.preventDefault();
    assistant.closeAssistant();
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

    <!-- Floating Action Button (Tameru AI Assistant) -->
    <button
      type="button"
      id="global-assistant-fab"
      class="group fixed bottom-20 right-4 sm:bottom-8 sm:right-8 z-40 flex h-14 w-14 items-center justify-center rounded-full bg-accent text-accent-contrast shadow-xl shadow-accent/30 hover:shadow-2xl hover:shadow-accent/50 hover:-translate-y-0.5 active:translate-y-0 active:scale-95 transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2"
      :title="$t('assistant.title') + ' (Ctrl+J)'"
      :aria-label="$t('assistant.title')"
      @click="assistant.toggleAssistant()"
    >
      <TameruAssistantIcon :size="24" class="transition-transform duration-200 group-hover:scale-110" />
      <!-- Status glow ring / dot -->
      <span
        class="absolute -top-0.5 -right-0.5 h-3.5 w-3.5 rounded-full border-2 border-bg bg-emerald-400"
        :class="{ 'animate-pulse': assistant.isLoading }"
      />
    </button>

    <MobileNav />
    <ToastHost />
    <ConfirmDialog />
    <TransactionModal />
    <CommandPalette />
    <PurchaseSimulatorModal v-if="ui.simulatorModalOpen" @close="ui.closeSimulator()" />
    <AssistantChat />
  </div>
</template>
