<script setup lang="ts">
interface Props {
  modelValue: boolean;
  disabled?: boolean;
  label?: string;
  size?: 'sm' | 'md';
}

const props = withDefaults(defineProps<Props>(), {
  disabled: false,
  label: '',
  size: 'md',
});

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
}>();

function toggle() {
  if (!props.disabled) {
    emit('update:modelValue', !props.modelValue);
  }
}
</script>

<template>
  <button
    type="button"
    role="switch"
    :aria-checked="modelValue"
    :aria-label="label || undefined"
    :disabled="disabled"
    class="relative inline-flex shrink-0 cursor-pointer items-center rounded-full p-1 transition-colors duration-200 ease-in-out focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent disabled:cursor-not-allowed disabled:opacity-50"
    :class="[
      size === 'sm' ? 'h-5 w-9 p-0.5' : 'h-6 w-11 p-1',
      modelValue ? 'bg-accent' : 'bg-surface-3 hover:bg-border-strong/40',
    ]"
    @click="toggle"
    @keydown.space.prevent="toggle"
    @keydown.enter.prevent="toggle"
  >
    <span
      class="pointer-events-none inline-block rounded-full bg-white shadow-sm transition-transform duration-200 ease-in-out"
      :class="[
        size === 'sm' ? 'h-4 w-4' : 'h-4 w-4',
        modelValue
          ? size === 'sm'
            ? 'translate-x-4'
            : 'translate-x-5'
          : 'translate-x-0',
      ]"
    />
  </button>
</template>
