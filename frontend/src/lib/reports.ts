// Typed Reporting API module (docs/API_SPEC.md → Reporting, M5).
import { api } from '@/lib/api';
import type {
  CashflowReport, CategoryTrackerReport, NetWorthReport, OverviewReport,
} from '@/types/api';

export function getNetWorth(): Promise<NetWorthReport> {
  return api.get<NetWorthReport>('/reports/net-worth');
}

export function getCashflow(year: number, month: number): Promise<CashflowReport> {
  return api.get<CashflowReport>('/reports/cashflow', { params: { year, month } });
}

export function getOverview(year: number): Promise<OverviewReport> {
  return api.get<OverviewReport>('/reports/overview', { params: { year } });
}

export function getCategoryTracker(
  granularity: 'monthly' | 'daily',
  from: string,
  to: string,
): Promise<CategoryTrackerReport> {
  return api.get<CategoryTrackerReport>('/reports/category-tracker', {
    params: { granularity, from, to },
  });
}
