import { defineStore } from 'pinia';
import { ref } from 'vue';

export interface ConfirmOptions {
  message: string;
  title?: string;
  confirmLabel?: string;
  danger?: boolean;
}

interface PendingConfirm extends ConfirmOptions {
  resolve: (ok: boolean) => void;
}

export const useConfirmStore = defineStore('confirm', () => {
  const pending = ref<PendingConfirm | null>(null);

  /** Show a confirm dialog; resolves true if confirmed, false if cancelled/dismissed. */
  function ask(options: ConfirmOptions): Promise<boolean> {
    return new Promise((resolve) => {
      pending.value = { ...options, resolve };
    });
  }

  function respond(ok: boolean): void {
    pending.value?.resolve(ok);
    pending.value = null;
  }

  return { pending, ask, respond };
});
