import { ApiClientError } from '@/lib/api';

type TFn = (key: string, named?: Record<string, unknown>) => string;
type TeFn = (key: string) => boolean;

/** Map an unknown error to a localized message via its stable backend code, falling back to generic. */
export function errorMessage(t: TFn, te: TeFn, error: unknown): string {
  const code = error instanceof ApiClientError ? error.code : 'generic';
  return te(`errors.${code}`) ? t(`errors.${code}`) : t('errors.generic');
}
