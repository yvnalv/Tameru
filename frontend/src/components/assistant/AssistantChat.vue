<script setup lang="ts">
import { ref, nextTick, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useAssistantStore } from '@/stores/assistant';
import ChatMessage from './ChatMessage.vue';
import TameruAssistantIcon from '@/components/brand/TameruAssistantIcon.vue';
import {
  X,
  Send,
  Trash2,
  Bot,
  Receipt,
  ShieldCheck,
  TrendingUp,
  PieChart,
} from 'lucide-vue-next';

const { t } = useI18n();
const assistantStore = useAssistantStore();

const inputText = ref('');
const messagesContainer = ref<HTMLElement | null>(null);
const inputRef = ref<HTMLTextAreaElement | null>(null);

function scrollToBottom(): void {
  void nextTick(() => {
    if (messagesContainer.value) {
      messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
    }
  });
}

watch(
  () => assistantStore.messages.length,
  () => {
    scrollToBottom();
  },
);

watch(
  () => assistantStore.isOpen,
  (open) => {
    if (open) {
      scrollToBottom();
      void nextTick(() => {
        inputRef.value?.focus();
      });
    }
  },
);

async function handleSend(): Promise<void> {
  const text = inputText.value.trim();
  if (!text || assistantStore.isLoading) return;
  inputText.value = '';
  await assistantStore.sendMessage(text);
}

function handleKeydown(e: KeyboardEvent): void {
  if (e.key === 'Enter' && !e.shiftKey) {
    e.preventDefault();
    void handleSend();
  }
}

function sendQuickPrompt(promptText: string): void {
  inputText.value = '';
  void assistantStore.sendMessage(promptText);
}

function handleClear(): void {
  void assistantStore.clearHistory();
}
</script>

<template>
  <div>
    <!-- Backdrop (Mobile/Tablet) -->
    <Transition
      enter-active-class="transition-opacity duration-200"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition-opacity duration-150"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="assistantStore.isOpen"
        class="fixed inset-0 z-40 bg-slate-950/60 backdrop-blur-sm md:hidden"
        @click="assistantStore.closeAssistant"
      />
    </Transition>

    <!-- Slide-out Drawer Panel -->
    <aside
      class="fixed inset-y-0 right-0 z-50 flex w-full flex-col border-l border-slate-800 bg-slate-900 shadow-2xl transition-transform duration-300 sm:w-[420px]"
      :class="assistantStore.isOpen ? 'translate-x-0' : 'translate-x-full'"
    >
      <!-- Header -->
      <div class="flex items-center justify-between border-b border-slate-800/80 px-4 py-3.5 bg-slate-900/90 backdrop-blur">
        <div class="flex items-center gap-2.5">
          <div class="flex h-8 w-8 items-center justify-center rounded-xl bg-gradient-to-br from-accent to-blue-600 text-white shadow-md shadow-accent/20">
            <TameruAssistantIcon :size="18" />
          </div>
          <div>
            <div class="flex items-center gap-1.5">
              <h2 class="text-sm font-semibold text-slate-100">{{ t('assistant.title') }}</h2>
              <span class="rounded-full bg-indigo-500/10 px-2 py-0.5 text-[10px] font-medium text-indigo-400 border border-indigo-500/20">
                Copilot
              </span>
            </div>
            <p class="text-[11px] text-slate-400 truncate max-w-[220px]">
              {{ t('assistant.subtitle') }}
            </p>
          </div>
        </div>

        <div class="flex items-center gap-1">
          <button
            type="button"
            class="flex h-8 w-8 items-center justify-center rounded-lg text-slate-400 hover:bg-slate-800 hover:text-slate-200 transition-colors"
            :title="t('assistant.clearChat')"
            @click="handleClear"
          >
            <Trash2 class="h-4 w-4" />
          </button>
          <button
            type="button"
            class="flex h-8 w-8 items-center justify-center rounded-lg text-slate-400 hover:bg-slate-800 hover:text-slate-200 transition-colors"
            title="Close drawer"
            @click="assistantStore.closeAssistant"
          >
            <X class="h-4 w-4" />
          </button>
        </div>
      </div>

      <!-- Messages Area -->
      <div
        ref="messagesContainer"
        class="flex-1 overflow-y-auto p-4 space-y-4 scroll-smooth"
      >
        <!-- Welcome / Empty State -->
        <div
          v-if="assistantStore.messages.length === 0"
          class="flex flex-col items-center justify-center h-full py-8 text-center"
        >
          <div class="relative flex h-14 w-14 items-center justify-center rounded-2xl bg-indigo-500/10 border border-indigo-500/20 text-indigo-400 shadow-inner">
            <Bot class="h-7 w-7" />
            <span class="absolute -top-1 -right-1 flex h-3 w-3">
              <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-indigo-400 opacity-75"></span>
              <span class="relative inline-flex rounded-full h-3 w-3 bg-indigo-500"></span>
            </span>
          </div>

          <h3 class="mt-4 text-base font-semibold text-slate-100">
            {{ t('assistant.welcomeTitle') }}
          </h3>
          <p class="mt-2 text-xs text-slate-400 max-w-[300px] leading-relaxed">
            {{ t('assistant.welcomeMessage') }}
          </p>

          <!-- Quick Suggestion Chips -->
          <div class="mt-6 w-full space-y-2">
            <div class="text-[11px] font-medium text-slate-400 uppercase tracking-wider">
              Quick Prompts
            </div>
            <div class="grid grid-cols-2 gap-2 text-left">
              <button
                type="button"
                class="group flex items-center gap-2 rounded-xl border border-slate-800 bg-slate-800/40 p-2.5 text-xs text-slate-300 hover:border-indigo-500/30 hover:bg-slate-800/80 transition-all"
                @click="sendQuickPrompt('Catat kopi 35k gopay')"
              >
                <div class="flex h-6 w-6 shrink-0 items-center justify-center rounded-lg bg-indigo-500/10 text-indigo-400 group-hover:bg-indigo-500/20">
                  <Receipt class="h-3.5 w-3.5" />
                </div>
                <div class="truncate">
                  <div class="font-medium text-slate-200">Log Expense</div>
                  <div class="text-[10px] text-slate-400 truncate">Kopi 35k gopay</div>
                </div>
              </button>

              <button
                type="button"
                class="group flex items-center gap-2 rounded-xl border border-slate-800 bg-slate-800/40 p-2.5 text-xs text-slate-300 hover:border-indigo-500/30 hover:bg-slate-800/80 transition-all"
                @click="sendQuickPrompt('Safe to spend?')"
              >
                <div class="flex h-6 w-6 shrink-0 items-center justify-center rounded-lg bg-emerald-500/10 text-emerald-400 group-hover:bg-emerald-500/20">
                  <ShieldCheck class="h-3.5 w-3.5" />
                </div>
                <div class="truncate">
                  <div class="font-medium text-slate-200">Safe-to-Spend</div>
                  <div class="text-[10px] text-slate-400 truncate">Check liquidity</div>
                </div>
              </button>

              <button
                type="button"
                class="group flex items-center gap-2 rounded-xl border border-slate-800 bg-slate-800/40 p-2.5 text-xs text-slate-300 hover:border-indigo-500/30 hover:bg-slate-800/80 transition-all"
                @click="sendQuickPrompt('Can I afford 750k?')"
              >
                <div class="flex h-6 w-6 shrink-0 items-center justify-center rounded-lg bg-amber-500/10 text-amber-400 group-hover:bg-amber-500/20">
                  <PieChart class="h-3.5 w-3.5" />
                </div>
                <div class="truncate">
                  <div class="font-medium text-slate-200">Simulate</div>
                  <div class="text-[10px] text-slate-400 truncate">Can I afford 750k?</div>
                </div>
              </button>

              <button
                type="button"
                class="group flex items-center gap-2 rounded-xl border border-slate-800 bg-slate-800/40 p-2.5 text-xs text-slate-300 hover:border-indigo-500/30 hover:bg-slate-800/80 transition-all"
                @click="sendQuickPrompt('Give me financial insights')"
              >
                <div class="flex h-6 w-6 shrink-0 items-center justify-center rounded-lg bg-purple-500/10 text-purple-400 group-hover:bg-purple-500/20">
                  <TrendingUp class="h-3.5 w-3.5" />
                </div>
                <div class="truncate">
                  <div class="font-medium text-slate-200">Get Insights</div>
                  <div class="text-[10px] text-slate-400 truncate">Velocity & anomalies</div>
                </div>
              </button>
            </div>
          </div>
        </div>

        <!-- Chat Message History -->
        <ChatMessage
          v-for="msg in assistantStore.messages"
          :key="msg.id"
          :message="msg"
        />

        <!-- Loading Indicator -->
        <div
          v-if="assistantStore.isLoading"
          class="flex items-center gap-2.5 text-slate-400 text-xs py-2"
        >
          <div class="flex h-7 w-7 items-center justify-center rounded-lg bg-slate-800 border border-slate-700 text-indigo-400">
            <Bot class="h-4 w-4" />
          </div>
          <div class="flex items-center gap-1.5 rounded-2xl rounded-tl-none bg-slate-800/80 border border-slate-700/60 px-4 py-2.5">
            <span class="h-1.5 w-1.5 rounded-full bg-indigo-400 animate-bounce"></span>
            <span class="h-1.5 w-1.5 rounded-full bg-indigo-400 animate-bounce [animation-delay:0.2s]"></span>
            <span class="h-1.5 w-1.5 rounded-full bg-indigo-400 animate-bounce [animation-delay:0.4s]"></span>
          </div>
        </div>
      </div>

      <!-- Input Area -->
      <div class="border-t border-slate-800 bg-slate-900/90 p-3">
        <form @submit.prevent="handleSend" class="relative">
          <textarea
            ref="inputRef"
            v-model="inputText"
            rows="2"
            :placeholder="t('assistant.placeholder')"
            class="w-full resize-none rounded-xl border border-slate-700/80 bg-slate-800/90 py-2 pl-3.5 pr-12 text-xs text-slate-100 placeholder-slate-400 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 transition-all disabled:opacity-50"
            :disabled="assistantStore.isLoading"
            @keydown="handleKeydown"
          />
          <button
            type="submit"
            :disabled="!inputText.trim() || assistantStore.isLoading"
            class="absolute bottom-3 right-2 flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-600 text-white transition-all hover:bg-indigo-500 disabled:opacity-30 disabled:pointer-events-none"
            :title="t('assistant.send')"
          >
            <Send class="h-3.5 w-3.5" />
          </button>
        </form>

        <div class="mt-2 flex items-center justify-between text-[10px] text-slate-400 px-1">
          <span>{{ t('assistant.shortcutHint') }}</span>
          <span class="font-mono">Enter ↵ to send</span>
        </div>
      </div>
    </aside>
  </div>
</template>
