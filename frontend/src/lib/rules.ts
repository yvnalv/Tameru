// Typed Categorization Rules & Ingestion API client
import { api } from '@/lib/api';
import type {
  RuleDto,
  CreateRuleRequest,
  UpdateRuleRequest,
  IngestTransactionRequest,
  IngestResultDto,
  DryRunRuleRequest,
  DryRunResultDto,
  RuleAuditLogDto,
  CreateFromTemplateRequest,
} from '@/types/api';

export function listRules(activeOnly = false): Promise<RuleDto[]> {
  return api.get<RuleDto[]>('/rules', {
    params: activeOnly ? { activeOnly: 'true' } : undefined,
  });
}

export function getRule(id: string): Promise<RuleDto> {
  return api.get<RuleDto>(`/rules/${id}`);
}

export function createRule(data: CreateRuleRequest): Promise<RuleDto> {
  return api.post<RuleDto>('/rules', data);
}

export function updateRule(id: string, data: UpdateRuleRequest): Promise<RuleDto> {
  return api.put<RuleDto>(`/rules/${id}`, data);
}

export function deleteRule(id: string): Promise<void> {
  return api.delete<void>(`/rules/${id}`);
}

export function ingestTransaction(data: IngestTransactionRequest): Promise<IngestResultDto> {
  return api.post<IngestResultDto>('/ingest/transaction', data);
}

export function dryRunRules(data: DryRunRuleRequest): Promise<DryRunResultDto> {
  return api.post<DryRunResultDto>('/rules/dry-run', data);
}

export function listAuditLog(ruleId?: string, limit = 50): Promise<RuleAuditLogDto[]> {
  return api.get<RuleAuditLogDto[]>('/rules/audit-log', {
    params: {
      ...(ruleId ? { ruleId } : {}),
      limit: String(limit),
    },
  });
}

export function listTemplates(): Promise<RuleDto[]> {
  return api.get<RuleDto[]>('/rules/templates');
}

export function createFromTemplate(id: string, data: CreateFromTemplateRequest): Promise<RuleDto> {
  return api.post<RuleDto>(`/rules/from-template/${id}`, data);
}
