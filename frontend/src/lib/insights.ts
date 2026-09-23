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

export function getDeepAnalysis(year?: number, month?: number): Promise<import('@/types/api').DeepAnalysisDto> {
  const params: Record<string, number> = {};
  if (year) params.year = year;
  if (month) params.month = month;
  return api.get<import('@/types/api').DeepAnalysisDto>('/decision/deep-analysis', { params });
}

export function getRecommendations(year?: number, month?: number): Promise<import('@/types/api').RecommendationDto[]> {
  const params: Record<string, number> = {};
  if (year) params.year = year;
  if (month) params.month = month;
  return api.get<import('@/types/api').RecommendationDto[]>('/decision/recommendations', { params });
}
