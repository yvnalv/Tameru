import { onMounted, onBeforeUnmount, type Ref } from 'vue';

const FOCUSABLE = [
  'a[href]',
  'button:not([disabled])',
  'input:not([disabled])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  '[tabindex]:not([tabindex="-1"])',
].join(',');

function focusable(root: HTMLElement): HTMLElement[] {
  return Array.from(root.querySelectorAll<HTMLElement>(FOCUSABLE)).filter((el) => {
    const rect = el.getBoundingClientRect();
    return rect.width > 0 && rect.height > 0;
  });
}

/**
 * Keeps keyboard focus inside a dialog for as long as it is mounted (WCAG 2.4.3):
 * moves focus in on open, cycles Tab/Shift+Tab within it, and restores focus to whatever
 * was focused before it opened. Used by every modal so the behaviour cannot drift.
 */
export function useFocusTrap(container: Ref<HTMLElement | null>): void {
  let previouslyFocused: HTMLElement | null = null;

  function onKeydown(e: KeyboardEvent): void {
    if (e.key !== 'Tab' || !container.value) return;

    const items = focusable(container.value);
    if (items.length === 0) {
      // Nothing focusable inside: keep focus on the dialog itself rather than letting it escape.
      e.preventDefault();
      container.value.focus();
      return;
    }

    const first = items[0];
    const last = items[items.length - 1];
    const active = document.activeElement as HTMLElement | null;

    // Focus that has drifted outside (or onto the container) re-enters at the correct end.
    if (!active || !container.value.contains(active) || active === container.value) {
      e.preventDefault();
      (e.shiftKey ? last : first).focus();
      return;
    }

    if (e.shiftKey && active === first) {
      e.preventDefault();
      last.focus();
    } else if (!e.shiftKey && active === last) {
      e.preventDefault();
      first.focus();
    }
  }

  onMounted(() => {
    previouslyFocused = document.activeElement as HTMLElement | null;
    document.addEventListener('keydown', onKeydown, true);

    // Let the dialog's content render before choosing a target. Prefer the dialog itself when it is
    // programmatically focusable: that announces its name and content, and avoids landing on the
    // close button, where a stray Enter would dismiss the dialog.
    requestAnimationFrame(() => {
      if (!container.value) return;
      if (container.value.hasAttribute('tabindex')) {
        container.value.focus();
        return;
      }
      const items = focusable(container.value);
      items[0]?.focus();
    });
  });

  onBeforeUnmount(() => {
    document.removeEventListener('keydown', onKeydown, true);
    previouslyFocused?.focus?.();
  });
}
