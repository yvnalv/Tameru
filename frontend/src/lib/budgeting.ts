// Typed Budget + Master Plan API modules (docs/API_SPEC.md → Budget, Master Plan).
import { api } from '@/lib/api';
import type {
  BudgetPeriod, BudgetPeriodSummary, MasterPlan, MasterPlanItem, MasterPlanSection,
} from '@/types/api';

// --- Budget periods ---------------------------------------------------------

export function listBudgetPeriods(year?: number): Promise<BudgetPeriodSummary[]> {
  return api.get<BudgetPeriodSummary[]>('/budget-periods', { params: year ? { year } : {} });
}

export function getBudgetPeriod(year: number, month: number): Promise<BudgetPeriod> {
  return api.get<BudgetPeriod>(`/budget-periods/${year}/${month}`);
}

export function createBudgetPeriod(year: number, month: number, note?: string): Promise<BudgetPeriod> {
  return api.post<BudgetPeriod>('/budget-periods', { year, month, note: note ?? null });
}

export interface BudgetLineInput {
  categoryId: string;
  planAmount: number;
}

export function upsertBudgetLines(periodId: string, lines: BudgetLineInput[]): Promise<BudgetPeriod> {
  return api.put<BudgetPeriod>(`/budget-periods/${periodId}/lines`, { lines });
}

// --- Master Plan ------------------------------------------------------------

export function getMasterPlan(): Promise<MasterPlan> {
  return api.get<MasterPlan>('/master-plan');
}

export interface MasterPlanItemInput {
  sectionId: string;
  name: string;
  price: number;
  frequency: number;
  sortOrder: number;
}

export function createMasterPlanItem(input: MasterPlanItemInput): Promise<MasterPlanItem> {
  return api.post<MasterPlanItem>('/master-plan/items', input);
}

export function updateMasterPlanItem(
  id: string,
  input: { name: string; price: number; frequency: number; sortOrder: number },
): Promise<MasterPlanItem> {
  return api.put<MasterPlanItem>(`/master-plan/items/${id}`, input);
}

export function deleteMasterPlanItem(id: string): Promise<void> {
  return api.delete<void>(`/master-plan/items/${id}`);
}

export function updateMasterPlanSection(id: string, targetPercent: number): Promise<MasterPlanSection> {
  return api.put<MasterPlanSection>(`/master-plan/sections/${id}`, { targetPercent });
}
