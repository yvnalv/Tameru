<script setup lang="ts">
defineProps<{
  id?: string;
  type?: string;
  modelValue: string;
  placeholder?: string;
  autocomplete?: string;
  required?: boolean;
  invalid?: boolean;
}>();

defineEmits<{ 'update:modelValue': [value: string] }>();
</script>

<template>
  <input
    :id="id"
    :type="type ?? 'text'"
    :value="modelValue"
    :placeholder="placeholder"
    :autocomplete="autocomplete"
    :required="required"
    :aria-invalid="invalid || undefined"
    class="h-10 w-full min-w-0 rounded-control border bg-surface px-3 text-sm text-text placeholder:text-text-muted focus:border-accent"
    :class="invalid ? 'border-negative' : 'border-border'"
    @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
  />
</template>
