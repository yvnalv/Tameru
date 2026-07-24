// Typed Ledger/Transactions API module (docs/API_SPEC.md → Transactions).
import { api } from '@/lib/api';
import type { Paged, Transaction } from '@/types/api';

export interface TransactionFilter {
  type?: string;
  accountId?: string;
  categoryId?: string;
  status?: string;
  from?: string;
  to?: string;
  q?: string;
  page?: number;
  pageSize?: number;
}

export interface TransactionInput {
  type: string;
  date: string;
  title: string;
  amount: number;
  accountId: string;
  toAccountId?: string | null;
  budgetCategoryId?: string | null;
  categoryId?: string | null;
  subCategoryId?: string | null;
  status?: string | null;
  currencyCode?: string | null;
  description?: string | null;
}

export function listTransactions(filter: TransactionFilter = {}): Promise<Paged<Transaction>> {
  return api.get<Paged<Transaction>>('/transactions', { params: filter });
}

export function createTransaction(input: TransactionInput): Promise<Transaction> {
  return api.post<Transaction>('/transactions', input);
}

export function updateTransaction(
  id: string,
  input: Omit<TransactionInput, 'type' | 'currencyCode'>,
): Promise<Transaction> {
  return api.put<Transaction>(`/transactions/${id}`, input);
}

export function clearTransaction(id: string): Promise<Transaction> {
  return api.post<Transaction>(`/transactions/${id}/clear`);
}

export function unclearTransaction(id: string): Promise<Transaction> {
  return api.post<Transaction>(`/transactions/${id}/unclear`);
}

export function voidTransaction(id: string): Promise<void> {
  return api.post<void>(`/transactions/${id}/void`);
}
