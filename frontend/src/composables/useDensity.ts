import { computed } from 'vue';
import { useUiStore } from '@/stores/ui';

/** Shared density state: `comfortable` (default) or `compact` list rows, persisted via the ui store. */
export function useDensity() {
  const ui = useUiStore();
  const compact = computed(() => ui.density === 'compact');
  const rowPad = computed(() => (compact.value ? 'py-1.5' : 'py-3'));

  function toggle(): void {
    ui.setDensity(compact.value ? 'comfortable' : 'compact');
  }

  return { density: computed(() => ui.density), compact, rowPad, toggle };
}
