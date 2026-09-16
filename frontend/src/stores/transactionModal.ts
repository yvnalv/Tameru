import { defineStore } from 'pinia';
import { ref } from 'vue';
import type { Transaction } from '@/types/api';
import type { TransactionInput } from '@/lib/transactions';

export const useTransactionModalStore = defineStore('transactionModal', () => {
  const isOpen = ref(false);
  const editingTx = ref<Transaction | null>(null);
  const prefill = ref<Partial<TransactionInput> | null>(null);
  const lastCreatedTx = ref<Transaction | null>(null);
  const lastEventTimestamp = ref(0);

  function openCreate(initial?: Partial<TransactionInput>): void {
    editingTx.value = null;
    prefill.value = initial ?? null;
    isOpen.value = true;
  }

  function openEdit(tx: Transaction): void {
    editingTx.value = tx;
    prefill.value = null;
    isOpen.value = true;
  }

  function close(): void {
    isOpen.value = false;
    editingTx.value = null;
    prefill.value = null;
  }

  function notifyCreated(tx: Transaction): void {
    lastCreatedTx.value = tx;
    lastEventTimestamp.value = Date.now();
  }

  return {
    isOpen,
    editingTx,
    prefill,
    lastCreatedTx,
    lastEventTimestamp,
    openCreate,
    openEdit,
    close,
    notifyCreated,
  };
});
