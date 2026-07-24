// Typed Categories API module (docs/API_SPEC.md → Categories). The API returns a flat list;
// build the Budget→Category→Sub tree client-side via parentId.
import { api } from '@/lib/api';
import type { Category } from '@/types/api';

export interface CategoryQuery {
  level?: string;
  flow?: string;
  parentId?: string;
  includeInactive?: boolean;
}

export interface CategoryInput {
  name: string;
  level: string;
  parentId: string | null;
  flow: string | null;
  sortOrder: number;
}

export function listCategories(query: CategoryQuery = {}): Promise<Category[]> {
  return api.get<Category[]>('/categories', { params: query });
}

export function createCategory(input: CategoryInput): Promise<Category> {
  return api.post<Category>('/categories', input);
}

export function updateCategory(
  id: string,
  input: { name: string; flow: string | null; sortOrder: number },
): Promise<Category> {
  return api.put<Category>(`/categories/${id}`, input);
}

export function deactivateCategory(id: string): Promise<void> {
  return api.post<void>(`/categories/${id}/deactivate`);
}

/** A category whose flow accepts the given transaction type (Income/Expense). */
export function flowAccepts(flow: string, txnType: string): boolean {
  if (flow === 'Any') return true;
  return flow === txnType;
}
