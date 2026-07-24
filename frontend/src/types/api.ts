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
