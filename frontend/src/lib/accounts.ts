// Typed Accounts API module (docs/API_SPEC.md → Accounts).
import { api } from '@/lib/api';
import type { Account, AccountGroup } from '@/types/api';

export interface AccountInput {
  name: string;
  type: string;
  openingBalance: number;
  groupId: string | null;
  currencyCode: string | null;
  sortOrder: number;
}

export function listAccounts(includeInactive = false): Promise<Account[]> {
  return api.get<Account[]>('/accounts', { params: { includeInactive } });
}

export function getAccount(id: string): Promise<Account> {
  return api.get<Account>(`/accounts/${id}`);
}

export function createAccount(input: AccountInput): Promise<Account> {
  return api.post<Account>('/accounts', input);
}

export function updateAccount(id: string, input: AccountInput): Promise<Account> {
  return api.put<Account>(`/accounts/${id}`, input);
}

export function deactivateAccount(id: string): Promise<void> {
  return api.post<void>(`/accounts/${id}/deactivate`);
}

export function listAccountGroups(): Promise<AccountGroup[]> {
  return api.get<AccountGroup[]>('/account-groups');
}
