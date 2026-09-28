import { RateBasis, PaymentType } from '../../../shared/utils/vyaj-calc';

export type { RateBasis, PaymentType };

export type VyajPartyKind = 'direct' | 'asset';

export interface VyajPartySummary {
  id: string;
  siteId: string;
  name: string;
  notes?: string;
  partyKind: VyajPartyKind;
  mainLedgerId?: string;
  subLedgerId?: string;
  bookingId?: string;
  mainLedgerName?: string;
  subLedgerName?: string;
  bookingLabel?: string;
  bookingRemaining?: number;
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
  customDays?: number | null;
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
  partyKind: VyajPartyKind;
  mainLedgerId?: string;
  subLedgerId?: string;
  bookingId?: string;
  mainLedgerName?: string;
  subLedgerName?: string;
  bookingLabel?: string;
  bookingRemaining?: number;
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
  partyKind: VyajPartyKind;
  mainLedgerId?: string | null;
  subLedgerId?: string | null;
  bookingId?: string | null;
}

export interface UpdateVyajPartyRequest {
  name: string;
  notes?: string;
  partyKind: VyajPartyKind;
  mainLedgerId?: string | null;
  subLedgerId?: string | null;
  bookingId?: string | null;
}

export interface CreateVyajEntryRequest {
  partyId: string;
  principal?: number | null;
  ratePercent: number;
  rateBasis: RateBasis;
  ratePeriodMonths?: number | null;
  customDays?: number | null;
  emiAmount?: number | null;
  startDate: string;
}

export interface CreateVyajPaymentRequest {
  entryId: string;
  paymentDate: string;
  amount: number;
  paymentType: PaymentType;
}

export interface VyajRateBasisOption {
  id: string;
  siteId: string;
  label: string;
  dayCount: number;
  isActive: boolean;
}

export interface RateBasisUiOption {
  value: string;
  label: string;
  rateBasis: RateBasis;
  ratePeriodMonths?: number;
  customDays?: number;
}

export const RATE_BASIS_OPTIONS: RateBasisUiOption[] = [
  { value: 'month-3', label: 'Per month — compound every 3 months', rateBasis: 'month', ratePeriodMonths: 3 },
  { value: 'month-6', label: 'Per month — compound every 6 months', rateBasis: 'month', ratePeriodMonths: 6 },
  { value: 'month-9', label: 'Per month — compound every 9 months', rateBasis: 'month', ratePeriodMonths: 9 },
  { value: 'day', label: 'Date-wise (per day)', rateBasis: 'day' },
  { value: 'year', label: 'Per year', rateBasis: 'year' },
  { value: 'flat', label: 'Flat (one-time)', rateBasis: 'flat' },
  { value: 'month', label: 'Per month (compound monthly)', rateBasis: 'month' },
  { value: 'custom-input', label: 'Custom days…', rateBasis: 'custom' }
];

export const PAYMENT_TYPE_OPTIONS: { value: PaymentType; label: string }[] = [
  { value: 'interest', label: 'Vyaj (interest)' },
  { value: 'principal', label: 'Principal' }
];

export function paymentTypeLabel(type: PaymentType | string): string {
  if (type === 'principal') return 'Principal';
  if (type === 'capitalize') return 'Added to principal';
  return 'Vyaj';
}

export function rateBasisUiValue(
  rateBasis: RateBasis,
  ratePeriodMonths?: number | null,
  customDays?: number | null
): string {
  if (rateBasis === 'month' && (ratePeriodMonths === 3 || ratePeriodMonths === 6 || ratePeriodMonths === 9)) {
    return `month-${ratePeriodMonths}`;
  }
  if (rateBasis === 'custom' && customDays) {
    return `custom-${customDays}`;
  }
  return rateBasis === 'custom' ? 'custom-input' : rateBasis;
}

export function parseRateBasisUi(
  value: string,
  customDaysInput?: number | null
): { rateBasis: RateBasis; ratePeriodMonths?: number | null; customDays?: number | null } {
  if (value === 'custom-input') {
    return { rateBasis: 'custom', ratePeriodMonths: null, customDays: customDaysInput ?? null };
  }
  if (value.startsWith('custom-')) {
    const days = Number(value.slice('custom-'.length));
    return { rateBasis: 'custom', ratePeriodMonths: null, customDays: Number.isFinite(days) ? days : null };
  }
  const opt = RATE_BASIS_OPTIONS.find((o) => o.value === value);
  if (!opt) return { rateBasis: 'month', ratePeriodMonths: null, customDays: null };
  return {
    rateBasis: opt.rateBasis,
    ratePeriodMonths: opt.ratePeriodMonths ?? null,
    customDays: opt.customDays ?? null
  };
}
