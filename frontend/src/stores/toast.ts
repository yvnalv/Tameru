import { defineStore } from 'pinia';
import { ref } from 'vue';

export type ToastKind = 'success' | 'error' | 'info';
export interface Toast {
  id: number;
  kind: ToastKind;
  message: string;
}

let seq = 0;

export const useToastStore = defineStore('toast', () => {
  const items = ref<Toast[]>([]);

  function push(kind: ToastKind, message: string): void {
    const id = ++seq;
    items.value.push({ id, kind, message });
    window.setTimeout(() => remove(id), 4000);
  }

  function remove(id: number): void {
    items.value = items.value.filter((t) => t.id !== id);
  }

  return {
    items,
    remove,
    success: (m: string) => push('success', m),
    error: (m: string) => push('error', m),
    info: (m: string) => push('info', m),
  };
});
