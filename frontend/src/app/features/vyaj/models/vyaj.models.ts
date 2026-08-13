import { RateBasis, PaymentType } from '../../../shared/utils/vyaj-calc';

export type { RateBasis, PaymentType };

export interface VyajPartySummary {
  id: string;
  siteId: string;
  name: string;
  notes?: string;
  mainLedgerId?: string;
  subLedgerId?: string;
  mainLedgerName?: string;
  subLedgerName?: string;
  vyajDue: number;
  principalDue: number;
  openEntryCount: number;
}

export interface VyajPayment {
  id: string;
  entryId: string;
  paymentDate: string;
  amount: number;
  paymentType: PaymentType;
}

export interface VyajEntry {
  id: string;
  partyId: string;
  principal: number;
  ratePercent: number;
  rateBasis: RateBasis;
  ratePeriodMonths?: number | null;
  emiAmount?: number | null;
  startDate: string;
  isClosed: boolean;
  grossVyaj: number;
  interestPaid: number;
  principalPaid: number;
  vyajDue: number;
  principalDue: number;
  payments: VyajPayment[];
}

export interface VyajPartyDetail {
  id: string;
  siteId: string;
  name: string;
  notes?: string;
  mainLedgerId?: string;
  subLedgerId?: string;
  mainLedgerName?: string;
  subLedgerName?: string;
  totalVyajDue: number;
  totalGrossVyaj: number;
  totalVyajPaid: number;
  totalPrincipalDue: number;
  entries: VyajEntry[];
}

export interface CreateVyajPartyRequest {
  siteId: string;
  name: string;
  notes?: string;
  mainLedgerId?: string | null;
  subLedgerId?: string | null;
}

export interface UpdateVyajPartyRequest {
  name: string;
  notes?: string;
  mainLedgerId?: string | null;
  subLedgerId?: string | null;
}

export interface CreateVyajEntryRequest {
  partyId: string;
  principal: number;
  ratePercent: number;
  rateBasis: RateBasis;
  ratePeriodMonths?: number | null;
  emiAmount?: number | null;
  startDate: string;
}

export interface CreateVyajPaymentRequest {
  entryId: string;
  paymentDate: string;
  amount: number;
  paymentType: PaymentType;
}

export interface RateBasisUiOption {
  value: string;
  label: string;
  rateBasis: RateBasis;
  ratePeriodMonths?: number;
}

export const RATE_BASIS_OPTIONS: RateBasisUiOption[] = [
  { value: 'month-3', label: 'Per month — 3 months', rateBasis: 'month', ratePeriodMonths: 3 },
  { value: 'month-6', label: 'Per month — 6 months', rateBasis: 'month', ratePeriodMonths: 6 },
  { value: 'month-9', label: 'Per month — 9 months', rateBasis: 'month', ratePeriodMonths: 9 },
  { value: 'day', label: 'Date-wise (per day)', rateBasis: 'day' },
  { value: 'year', label: 'Per year', rateBasis: 'year' },
  { value: 'flat', label: 'Flat (one-time)', rateBasis: 'flat' },
  { value: 'month', label: 'Per month (open-ended)', rateBasis: 'month' }
];

export const PAYMENT_TYPE_OPTIONS: { value: PaymentType; label: string }[] = [
  { value: 'interest', label: 'Vyaj paid' },
  { value: 'principal', label: 'Principal paid' }
];

export function rateBasisUiValue(rateBasis: RateBasis, ratePeriodMonths?: number | null): string {
  if (rateBasis === 'month' && (ratePeriodMonths === 3 || ratePeriodMonths === 6 || ratePeriodMonths === 9)) {
    return `month-${ratePeriodMonths}`;
  }
  return rateBasis;
}

export function parseRateBasisUi(value: string): { rateBasis: RateBasis; ratePeriodMonths?: number | null } {
  const opt = RATE_BASIS_OPTIONS.find((o) => o.value === value);
  if (!opt) return { rateBasis: 'month', ratePeriodMonths: null };
  return { rateBasis: opt.rateBasis, ratePeriodMonths: opt.ratePeriodMonths ?? null };
}
