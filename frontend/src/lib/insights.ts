// Proactive Insights API client
import { api } from '@/lib/api';
import type { InsightDto } from '@/types/api';

export interface InsightsParams {
  maxResults?: number;
  minSeverity?: string;
}

export function getInsights(params?: InsightsParams): Promise<InsightDto[]> {
  return api.get<InsightDto[]>('/decision/insights', {
    params: params ? {
      maxResults: params.maxResults?.toString(),
      minSeverity: params.minSeverity,
    } : undefined,
  });
}
