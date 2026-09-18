import { api } from '@/lib/api';
import type {
  SafeToSpendDto,
  SimulatePurchaseRequest,
  SimulatePurchaseResultDto,
} from '@/types/api';

export function getSafeToSpend(): Promise<SafeToSpendDto> {
  return api.get<SafeToSpendDto>('/decision/safe-to-spend');
}

export function simulatePurchase(data: SimulatePurchaseRequest): Promise<SimulatePurchaseResultDto> {
  return api.post<SimulatePurchaseResultDto>('/decision/simulate-purchase', data);
}
