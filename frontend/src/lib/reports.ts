// Typed Reporting API module (docs/API_SPEC.md → Reporting, M5).
import { api } from '@/lib/api';
import type { CashflowReport, NetWorthReport } from '@/types/api';

export function getNetWorth(): Promise<NetWorthReport> {
  return api.get<NetWorthReport>('/reports/net-worth');
}

export function getCashflow(year: number, month: number): Promise<CashflowReport> {
  return api.get<CashflowReport>('/reports/cashflow', { params: { year, month } });
}
