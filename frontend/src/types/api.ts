// Mirrors the backend response envelope (docs/API_SPEC.md) and the MVP DTOs the client consumes.

export interface ApiError {
  code: string;
  details?: { field: string; message: string }[] | null;
  traceId?: string | null;
}

export interface ApiSuccess<T> {
  success: true;
  data: T;
  message: null;
  error: null;
}

export interface ApiFailure {
  success: false;
  data: null;
  message: string;
  error: ApiError;
}

export type ApiResponse<T> = ApiSuccess<T> | ApiFailure;

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

// --- Accounts ---------------------------------------------------------------

export type AccountType = 'Cash' | 'Bank' | 'EWallet' | 'Investment' | 'Blocked';

export interface Account {
  id: string;
  name: string;
  groupId: string | null;
  groupName: string | null;
  type: string;
  openingBalance: number;
  balance: number;
  currencyCode: string;
  isActive: boolean;
  sortOrder: number;
}

export interface AccountGroup {
  id: string;
  name: string;
  sortOrder: number;
  accountCount: number;
  totalBalance: number;
}

// --- Ledger -----------------------------------------------------------------

export type TransactionType = 'Income' | 'Expense' | 'Transfer';
export type TransactionStatus = 'Cleared' | 'Uncleared';

export interface Transaction {
  id: string;
  type: string;
  date: string;
  title: string;
  amount: number;
  currencyCode: string;
  accountId: string;
  toAccountId: string | null;
  budgetCategoryId: string | null;
  categoryId: string | null;
  subCategoryId: string | null;
  status: string;
  description: string | null;
}

// --- Categories -------------------------------------------------------------

export type CategoryLevel = 'Budget' | 'Category' | 'Sub';
export type CategoryFlow = 'Income' | 'Expense' | 'Transfer' | 'Any';

export interface Category {
  id: string;
  name: string;
  level: string;
  parentId: string | null;
  flow: string;
  isSystem: boolean;
  isActive: boolean;
  sortOrder: number;
}

// --- Identity ---------------------------------------------------------------

export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
  locale: string;
}

export interface AuthTokens {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  user: AuthUser;
}

// --- Reporting (M5) ---------------------------------------------------------

export interface AccountBalance {
  accountId: string;
  name: string;
  groupName: string | null;
  type: string;
  balance: number;
  currencyCode: string;
}

export interface NetWorthReport {
  total: number;
  currencyCode: string;
  accounts: AccountBalance[];
}

export interface MonthlyCashflow {
  month: number;
  income: number;
  expense: number;
  net: number;
}

export interface CashflowReport {
  year: number;
  month: number;
  income: number;
  expense: number;
  net: number;
  trend: MonthlyCashflow[];
}
