<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';

export interface HistoricalPayee {
  title: string;
  type?: string;
  categoryId?: string | null;
  budgetCategoryId?: string | null;
  accountId?: string | null;
}

const props = withDefaults(
  defineProps<{
    modelValue: string;
    id?: string;
    placeholder?: string;
    required?: boolean;
    invalid?: boolean;
    history?: HistoricalPayee[];
  }>(),
  {
    id: 'payee-input',
    placeholder: '',
    required: false,
    invalid: false,
    history: () => [],
  },
);

const emit = defineEmits<{
  'update:modelValue': [value: string];
  selectPayee: [payee: HistoricalPayee];
}>();

const inputRef = ref<HTMLInputElement | null>(null);
const isOpen = ref(false);
const highlightedIndex = ref(-1);

// Build deduplicated payee map from history (most recent first)
const uniquePayees = computed<HistoricalPayee[]>(() => {
  const seen = new Set<string>();
  const list: HistoricalPayee[] = [];
  for (const item of props.history) {
    const norm = item.title.trim().toLowerCase();
    if (norm && !seen.has(norm)) {
      seen.add(norm);
      list.push(item);
    }
  }
  return list;
});

// Suggestions based on query
const suggestions = computed(() => {
  const q = props.modelValue.trim().toLowerCase();
  if (!q) return uniquePayees.value.slice(0, 6);
  return uniquePayees.value.filter((p) => p.title.toLowerCase().includes(q)).slice(0, 6);
});

function onInput(e: Event): void {
  const val = (e.target as HTMLInputElement).value;
  emit('update:modelValue', val);
  isOpen.value = true;
  highlightedIndex.value = -1;
}

function onFocus(): void {
  if (suggestions.value.length > 0) {
    isOpen.value = true;
  }
}

function selectSuggestion(payee: HistoricalPayee): void {
  emit('update:modelValue', payee.title);
  emit('selectPayee', payee);
  isOpen.value = false;
  highlightedIndex.value = -1;
}

function onKeyDown(e: KeyboardEvent): void {
  if (!isOpen.value || suggestions.value.length === 0) return;

  if (e.key === 'ArrowDown') {
    e.preventDefault();
    highlightedIndex.value = (highlightedIndex.value + 1) % suggestions.value.length;
  } else if (e.key === 'ArrowUp') {
    e.preventDefault();
    highlightedIndex.value =
      highlightedIndex.value <= 0 ? suggestions.value.length - 1 : highlightedIndex.value - 1;
  } else if (e.key === 'Enter') {
    if (highlightedIndex.value >= 0 && highlightedIndex.value < suggestions.value.length) {
      e.preventDefault();
      selectSuggestion(suggestions.value[highlightedIndex.value]);
    }
  } else if (e.key === 'Escape') {
    isOpen.value = false;
  }
}

function onDocClick(e: MouseEvent): void {
  if (inputRef.value && !inputRef.value.contains(e.target as Node)) {
    isOpen.value = false;
  }
}

onMounted(() => document.addEventListener('click', onDocClick));
onUnmounted(() => document.removeEventListener('click', onDocClick));

defineExpose({
  focus: () => inputRef.value?.focus(),
});
</script>

<template>
  <div class="relative">
    <input
      :id="id"
      ref="inputRef"
      type="text"
      autocomplete="off"
      :required="required"
      :placeholder="placeholder"
      :value="modelValue"
      :aria-invalid="invalid || undefined"
      :aria-expanded="isOpen"
      class="h-10 w-full min-w-0 rounded-control border bg-surface px-3 text-sm text-text placeholder:text-text-muted focus:border-accent focus:outline-none"
      :class="invalid ? 'border-negative' : 'border-border-strong'"
      @input="onInput"
      @focus="onFocus"
      @keydown="onKeyDown"
    />

    <!-- Autocomplete Dropdown -->
    <div
      v-if="isOpen && suggestions.length > 0"
      class="absolute left-0 right-0 top-full z-50 mt-1 max-h-48 overflow-y-auto rounded-control border border-border bg-surface shadow-lift"
    >
      <ul class="py-1">
        <li
          v-for="(s, idx) in suggestions"
          :key="s.title"
          class="flex cursor-pointer items-center justify-between px-3 py-2 text-sm transition-colors hover:bg-surface-2"
          :class="highlightedIndex === idx ? 'bg-surface-2 text-accent' : 'text-text'"
          @mousedown.prevent="selectSuggestion(s)"
        >
          <span class="font-medium truncate">{{ s.title }}</span>
          <span v-if="s.categoryId" class="text-xs text-text-muted shrink-0 pl-2">
            Auto-fill
          </span>
        </li>
      </ul>
    </div>
  </div>
</template>
