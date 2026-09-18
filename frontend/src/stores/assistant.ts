import { defineStore } from 'pinia';
import { ref } from 'vue';
import type { ChatMessageItem } from '@/types/api';
import { sendAssistantMessage, clearAssistantConversation } from '@/lib/assistant';

const CID_KEY = 'tameru.assistant.cid';

export const useAssistantStore = defineStore('assistant', () => {
  const isOpen = ref(false);
  const isLoading = ref(false);
  const conversationId = ref(localStorage.getItem(CID_KEY) || '');
  const messages = ref<ChatMessageItem[]>([]);

  function openAssistant(): void {
    isOpen.value = true;
  }

  function closeAssistant(): void {
    isOpen.value = false;
  }

  function toggleAssistant(): void {
    isOpen.value = !isOpen.value;
  }

  async function sendMessage(text: string): Promise<void> {
    const trimmed = text.trim();
    if (!trimmed || isLoading.value) return;

    if (!conversationId.value) {
      conversationId.value = typeof crypto !== 'undefined' && crypto.randomUUID
        ? crypto.randomUUID()
        : 'cid_' + Date.now();
      localStorage.setItem(CID_KEY, conversationId.value);
    }

    const userMsg: ChatMessageItem = {
      id: typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : 'msg_' + Date.now(),
      role: 'user',
      content: trimmed,
      timestamp: new Date().toISOString(),
    };
    messages.value.push(userMsg);

    isLoading.value = true;
    try {
      const response = await sendAssistantMessage(trimmed, conversationId.value);
      if (response.conversationId) {
        conversationId.value = response.conversationId;
        localStorage.setItem(CID_KEY, response.conversationId);
      }

      const assistantMsg: ChatMessageItem = {
        id: typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : 'msg_' + (Date.now() + 1),
        role: 'assistant',
        content: response.message,
        timestamp: new Date().toISOString(),
        action: response.action,
        insights: response.insights,
      };
      messages.value.push(assistantMsg);

      if (response.action?.type === 'transaction_created') {
        window.dispatchEvent(new CustomEvent('tameru:transaction-created', { detail: response.action.data }));
      }
    } catch (err: any) {
      const errorMsg: ChatMessageItem = {
        id: typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : 'msg_' + (Date.now() + 1),
        role: 'assistant',
        content: err?.message || 'Sorry, I encountered an error communicating with the server. Please try again.',
        timestamp: new Date().toISOString(),
      };
      messages.value.push(errorMsg);
    } finally {
      isLoading.value = false;
    }
  }

  async function clearHistory(): Promise<void> {
    if (conversationId.value) {
      try {
        await clearAssistantConversation(conversationId.value);
      } catch {
        // ignore network error on clear
      }
    }
    messages.value = [];
    conversationId.value = '';
    localStorage.removeItem(CID_KEY);
  }

  return {
    isOpen,
    isLoading,
    conversationId,
    messages,
    openAssistant,
    closeAssistant,
    toggleAssistant,
    sendMessage,
    clearHistory,
  };
});
