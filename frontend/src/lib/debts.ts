import { api } from '@/lib/api';
import type {
  Liability,
  LiabilityPayment,
  DebtsSummary,
  CreateLiabilityInput,
  UpdateLiabilityInput,
  RecordPaymentInput,
} from '@/types/api';

export function listDebts(status?: string, type?: string): Promise<Liability[]> {
  const params: Record<string, string> = {};
  if (status) params.status = status;
  if (type) params.type = type;
  return api.get<Liability[]>('/debts', { params });
}

export function getDebtsSummary(): Promise<DebtsSummary> {
  return api.get<DebtsSummary>('/debts/summary');
}

export function getDebt(id: string): Promise<Liability> {
  return api.get<Liability>(`/debts/${id}`);
}

export function createDebt(input: CreateLiabilityInput): Promise<Liability> {
  return api.post<Liability>('/debts', input);
}

export function updateDebt(id: string, input: UpdateLiabilityInput): Promise<Liability> {
  return api.put<Liability>(`/debts/${id}`, input);
}

export function deleteDebt(id: string): Promise<boolean> {
  return api.delete<boolean>(`/debts/${id}`);
}

export function getDebtPayments(id: string): Promise<LiabilityPayment[]> {
  return api.get<LiabilityPayment[]>(`/debts/${id}/payments`);
}

export function recordDebtPayment(id: string, input: RecordPaymentInput): Promise<LiabilityPayment> {
  return api.post<LiabilityPayment>(`/debts/${id}/payments`, input);
}

export function deleteDebtPayment(id: string, paymentId: string): Promise<boolean> {
  return api.delete<boolean>(`/debts/${id}/payments/${paymentId}`);
}
