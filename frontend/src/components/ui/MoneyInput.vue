<script setup lang="ts">
import { ref, watch, computed, nextTick, onMounted } from 'vue';
import { parseMoneyInput, formatThousands } from '@/lib/moneyParser';

const props = withDefaults(
  defineProps<{
    modelValue: number;
    currency?: string;
    id?: string;
    placeholder?: string;
    autofocus?: boolean;
    showChips?: boolean;
    invalid?: boolean;
  }>(),
  {
    currency: 'IDR',
    id: 'money-input',
    placeholder: '0',
    autofocus: false,
    showChips: true,
    invalid: false,
  },
);

const emit = defineEmits<{
  'update:modelValue': [value: number];
  submit: [];
}>();

const inputRef = ref<HTMLInputElement | null>(null);
const isFocused = ref(false);
const rawText = ref(props.modelValue ? formatThousands(props.modelValue) : '');

// When external modelValue changes and input is not focused, sync rawText
watch(
  () => props.modelValue,
  (newVal) => {
    if (!isFocused.value) {
      rawText.value = newVal ? formatThousands(newVal) : '';
    }
  },
);

// Live preview calculation if rawText contains expressions or suffixes (e.g. 50k, 25k+15k)
const liveParsed = computed(() => parseMoneyInput(rawText.value));
const showExpressionHint = computed(() => {
  const t = rawText.value.trim().toLowerCase();
  return (
    t.length > 0 &&
    (t.includes('k') ||
      t.includes('rb') ||
      t.includes('jt') ||
      t.includes('m') ||
      t.includes('+') ||
      t.includes('-')) &&
    liveParsed.value > 0
  );
});

function onInput(e: Event): void {
  const val = (e.target as HTMLInputElement).value;
  rawText.value = val;
  const parsed = parseMoneyInput(val);
  emit('update:modelValue', parsed);
}

function onFocus(): void {
  isFocused.value = true;
  // If value is 0, start with empty text
  if (props.modelValue === 0) {
    rawText.value = '';
  } else {
    rawText.value = formatThousands(props.modelValue);
  }
}

function onBlur(): void {
  isFocused.value = false;
  const parsed = parseMoneyInput(rawText.value);
  emit('update:modelValue', parsed);
  rawText.value = parsed > 0 ? formatThousands(parsed) : '';
}

function onKeyDown(e: KeyboardEvent): void {
  if (e.key === 'Enter') {
    e.preventDefault();
    onBlur();
    emit('submit');
  }
}

function addIncrement(amount: number): void {
  const next = Math.max(0, (props.modelValue || 0) + amount);
  emit('update:modelValue', next);
  rawText.value = formatThousands(next);
}

function clearAmount(): void {
  emit('update:modelValue', 0);
  rawText.value = '';
  inputRef.value?.focus();
}

onMounted(() => {
  if (props.autofocus) {
    nextTick(() => inputRef.value?.focus());
  }
});

defineExpose({
  focus: () => inputRef.value?.focus(),
});
</script>

<template>
  <div class="space-y-1.5">
    <div
      class="relative flex items-center rounded-control border bg-surface transition-colors focus-within:border-accent"
      :class="invalid ? 'border-negative' : 'border-border-strong'"
    >
      <!-- Currency prefix badge -->
      <span
        class="pointer-events-none select-none pl-3 text-sm font-semibold tracking-wide text-text-muted"
        aria-hidden="true"
      >
        {{ currency }}
      </span>

      <!-- Input box -->
      <input
        :id="id"
        ref="inputRef"
        type="text"
        inputmode="text"
        autocomplete="off"
        :placeholder="placeholder"
        :value="rawText"
        :aria-invalid="invalid || undefined"
        class="h-11 w-full min-w-0 bg-transparent px-3 text-base font-semibold tnum text-text placeholder:font-normal placeholder:text-text-muted focus:outline-none sm:text-lg"
        @input="onInput"
        @focus="onFocus"
        @blur="onBlur"
        @keydown="onKeyDown"
      />

      <!-- Clear action button if amount exists -->
      <button
        v-if="modelValue > 0 || rawText"
        type="button"
        class="mr-2.5 rounded px-1.5 py-0.5 text-xs font-medium text-text-muted hover:bg-surface-2 hover:text-text"
        aria-label="Clear amount"
        @click="clearAmount"
      >
        ✕
      </button>
    </div>

    <!-- Live calculation hint if user typed e.g. '50k' or '25k + 15k' -->
    <div v-if="showExpressionHint" class="flex items-center gap-1.5 px-1 text-xs text-accent">
      <span class="opacity-75">=</span>
      <span class="font-semibold">{{ currency }} {{ formatThousands(liveParsed) }}</span>
    </div>

    <!-- Quick denomination chips for IDR -->
    <div v-if="showChips && currency === 'IDR'" class="flex flex-wrap items-center gap-1.5 pt-0.5">
      <button
        type="button"
        class="rounded-full border border-border bg-surface-2 px-2.5 py-1 text-xs font-medium text-text-muted hover:border-accent hover:text-accent focus:outline-none"
        @click="addIncrement(10000)"
      >
        +10k
      </button>
      <button
        type="button"
        class="rounded-full border border-border bg-surface-2 px-2.5 py-1 text-xs font-medium text-text-muted hover:border-accent hover:text-accent focus:outline-none"
        @click="addIncrement(20000)"
      >
        +20k
      </button>
      <button
        type="button"
        class="rounded-full border border-border bg-surface-2 px-2.5 py-1 text-xs font-medium text-text-muted hover:border-accent hover:text-accent focus:outline-none"
        @click="addIncrement(50000)"
      >
        +50k
      </button>
      <button
        type="button"
        class="rounded-full border border-border bg-surface-2 px-2.5 py-1 text-xs font-medium text-text-muted hover:border-accent hover:text-accent focus:outline-none"
        @click="addIncrement(100000)"
      >
        +100k
      </button>
    </div>
  </div>
</template>
