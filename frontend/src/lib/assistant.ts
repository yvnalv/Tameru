// AI Assistant API client
import { api } from '@/lib/api';
import type { ChatRequest, ChatResponse, AiProviderConfig, TestConnectionResponse } from '@/types/api';

export const AI_CONFIG_KEY = 'tameru.ai_config';

export function getStoredAiConfig(): AiProviderConfig | null {
  try {
    const raw = localStorage.getItem(AI_CONFIG_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export function setStoredAiConfig(config: AiProviderConfig | null): void {
  try {
    if (config) {
      localStorage.setItem(AI_CONFIG_KEY, JSON.stringify(config));
    } else {
      localStorage.removeItem(AI_CONFIG_KEY);
    }
  } catch {
    // ignore quota/storage issues
  }
}

export function sendAssistantMessage(message: string, conversationId?: string): Promise<ChatResponse> {
  const provider = getStoredAiConfig();
  const payload: ChatRequest = {
    message,
    conversationId,
    provider: provider || undefined,
  };
  return api.post<ChatResponse>('/assistant/chat', payload);
}

export function testAiConnection(provider: AiProviderConfig): Promise<TestConnectionResponse> {
  return api.post<TestConnectionResponse>('/assistant/test-connection', { provider });
}

export function clearAssistantConversation(conversationId: string): Promise<boolean> {
  return api.delete<boolean>(`/assistant/conversation/${encodeURIComponent(conversationId)}`);
}
