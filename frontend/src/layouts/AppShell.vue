<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue';
import { RouterView } from 'vue-router';
import AppSidebar from '@/components/layout/AppSidebar.vue';
import AppTopbar from '@/components/layout/AppTopbar.vue';
import MobileNav from '@/components/layout/MobileNav.vue';
import ToastHost from '@/components/ui/ToastHost.vue';
import ConfirmDialog from '@/components/ui/ConfirmDialog.vue';
import TransactionModal from '@/components/transactions/TransactionModal.vue';
import { useTransactionModalStore } from '@/stores/transactionModal';

const transactionModal = useTransactionModalStore();

function onGlobalKeyDown(e: KeyboardEvent): void {
  if (e.ctrlKey || e.metaKey || e.altKey) return;
  const target = e.target as HTMLElement | null;
  const tag = target?.tagName?.toLowerCase();
  const isEditable = target?.isContentEditable;
  if (tag === 'input' || tag === 'textarea' || tag === 'select' || isEditable) return;

  if (e.key.toLowerCase() === 'n' || e.key.toLowerCase() === 't') {
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
  <div class="flex min-h-screen bg-bg text-text">
    <AppSidebar />

    <div class="flex min-w-0 flex-1 flex-col">
      <AppTopbar />
      <main class="scroll-slim w-full flex-1 px-4 py-6 pb-28 md:px-8 md:pb-8">
        <RouterView />
      </main>
    </div>

    <MobileNav />
    <ToastHost />
    <ConfirmDialog />
    <TransactionModal />
  </div>
</template>
