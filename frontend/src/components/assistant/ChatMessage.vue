<script setup lang="ts">
import { computed, ref } from 'vue';
import type { ChatMessageItem } from '@/types/api';
import ActionCard from './ActionCard.vue';
import { Bot, User, Copy, Check } from 'lucide-vue-next';

const props = defineProps<{
  message: ChatMessageItem;
}>();

const copied = ref(false);

const isUser = computed(() => props.message.role === 'user');

const formattedTime = computed(() => {
  const d = new Date(props.message.timestamp);
  return isNaN(d.getTime()) ? '' : d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
});

/**
 * Basic markdown parser for bold, inline code, and line breaks.
 */
const formattedText = computed(() => {
  let text = props.message.content || '';
  // HTML escape
  text = text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');

  // Bold **text**
  text = text.replace(/\*\*(.*?)\*\*/g, '<strong class="font-semibold text-slate-100">$1</strong>');
  // Italic *text*
  text = text.replace(/\*(.*?)\*/g, '<em class="italic text-slate-300">$1</em>');
  // Inline code `code`
  text = text.replace(/`([^`]+)`/g, '<code class="px-1.5 py-0.5 rounded bg-slate-900/60 font-mono text-[11px] text-indigo-300 border border-slate-700/40">$1</code>');
  // List items starting with "- " or "* "
  text = text.replace(/(?:^|\n)[-*]\s+(.+)/g, '<div class="flex items-start gap-1.5 my-1"><span class="text-indigo-400 mt-1">•</span><span>$1</span></div>');
  // Newlines to <br> if not already inside div
  text = text.replace(/\n(?!\s*<div)/g, '<br />');

  return text;
});

async function copyText(): Promise<void> {
  if (!props.message.content) return;
  try {
    await navigator.clipboard.writeText(props.message.content);
    copied.value = true;
    setTimeout(() => {
      copied.value = false;
    }, 2000);
  } catch {
    // ignore
  }
}
</script>

<template>
  <div
    class="group flex gap-2.5 text-sm"
    :class="isUser ? 'flex-row-reverse items-end' : 'flex-row items-start'"
  >
    <!-- Avatar -->
    <div
      class="flex h-7 w-7 shrink-0 items-center justify-center rounded-lg text-xs"
      :class="isUser ? 'bg-indigo-600 text-white' : 'bg-slate-800 border border-slate-700 text-indigo-400'"
    >
      <User v-if="isUser" class="h-4 w-4" />
      <Bot v-else class="h-4 w-4" />
    </div>

    <!-- Bubble Container -->
    <div
      class="relative max-w-[85%] rounded-2xl px-3.5 py-2.5 transition-all shadow-sm"
      :class="
        isUser
          ? 'bg-indigo-600 text-white rounded-tr-none'
          : 'bg-slate-800/90 border border-slate-700/60 text-slate-200 rounded-tl-none'
      "
    >
      <!-- Message Body -->
      <div
        class="text-xs leading-relaxed break-words"
        v-html="formattedText"
      />

      <!-- Action / Insights Card -->
      <ActionCard
        v-if="!isUser && (message.action || (message.insights && message.insights.length > 0))"
        :action="message.action"
        :insights="message.insights"
      />

      <!-- Footer: Timestamp & Copy -->
      <div
        class="mt-1.5 flex items-center gap-2 text-[10px]"
        :class="isUser ? 'justify-end text-indigo-200/80' : 'justify-between text-slate-400'"
      >
        <button
          v-if="!isUser"
          type="button"
          class="opacity-0 group-hover:opacity-100 transition-opacity hover:text-slate-200 flex items-center gap-1"
          title="Copy message"
          @click="copyText"
        >
          <Check v-if="copied" class="h-3 w-3 text-emerald-400" />
          <Copy v-else class="h-3 w-3" />
          <span>{{ copied ? 'Copied' : 'Copy' }}</span>
        </button>
        <span class="tabular-nums font-mono">{{ formattedTime }}</span>
      </div>
    </div>
  </div>
</template>
