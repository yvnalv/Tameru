// AI Assistant API client
import { api } from '@/lib/api';
import type { ChatRequest, ChatResponse } from '@/types/api';

export function sendAssistantMessage(message: string, conversationId?: string): Promise<ChatResponse> {
  const payload: ChatRequest = {
    message,
    conversationId,
  };
  return api.post<ChatResponse>('/assistant/chat', payload);
}

export function clearAssistantConversation(conversationId: string): Promise<boolean> {
  return api.delete<boolean>(`/assistant/conversation/${encodeURIComponent(conversationId)}`);
}
