<script setup lang="ts">
export interface SelectOption {
  value: string;
  label: string;
}

defineProps<{
  id?: string;
  modelValue: string;
  options: SelectOption[];
  placeholder?: string;
  disabled?: boolean;
}>();

defineEmits<{ 'update:modelValue': [value: string] }>();
</script>

<template>
  <select
    :id="id"
    :value="modelValue"
    :disabled="disabled"
    class="h-10 w-full min-w-0 rounded-control border border-border bg-surface px-3 text-sm text-text focus:border-accent disabled:opacity-50"
    @change="$emit('update:modelValue', ($event.target as HTMLSelectElement).value)"
  >
    <option v-if="placeholder" value="" disabled>{{ placeholder }}</option>
    <option v-for="opt in options" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
  </select>
</template>
