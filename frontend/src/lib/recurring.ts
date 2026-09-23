import { api } from '@/lib/api';
import type {
  RecurringBill,
  RecurringBillsSummary,
  CreateRecurringBillInput,
  UpdateRecurringBillInput,
  PayRecurringBillInput,
  PayRecurringBillResult,
} from '@/types/api';

export function listRecurringBills(filter?: string): Promise<RecurringBill[]> {
  const params: Record<string, string> = {};
  if (filter) params.filter = filter;
  return api.get<RecurringBill[]>('/ledger/recurring-bills', { params });
}

export function getRecurringSummary(): Promise<RecurringBillsSummary> {
  return api.get<RecurringBillsSummary>('/ledger/recurring-bills/summary');
}

export function getRecurringBill(id: string): Promise<RecurringBill> {
  return api.get<RecurringBill>(`/ledger/recurring-bills/${id}`);
}

export function createRecurringBill(input: CreateRecurringBillInput): Promise<RecurringBill> {
  return api.post<RecurringBill>('/ledger/recurring-bills', input);
}

export function updateRecurringBill(id: string, input: UpdateRecurringBillInput): Promise<RecurringBill> {
  return api.put<RecurringBill>(`/ledger/recurring-bills/${id}`, input);
}

export function deleteRecurringBill(id: string): Promise<boolean> {
  return api.delete<boolean>(`/ledger/recurring-bills/${id}`);
}

export function payRecurringBill(id: string, input: PayRecurringBillInput): Promise<PayRecurringBillResult> {
  return api.post<PayRecurringBillResult>(`/ledger/recurring-bills/${id}/pay`, input);
}
