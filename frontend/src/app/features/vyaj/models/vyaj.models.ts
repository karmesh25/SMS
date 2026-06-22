import { RateBasis, PaymentType } from '../../../shared/utils/vyaj-calc';

export type { RateBasis, PaymentType };

export interface VyajPartySummary {
  id: string;
  siteId: string;
  name: string;
  notes?: string;
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
}

export interface UpdateVyajPartyRequest {
  name: string;
  notes?: string;
}

export interface CreateVyajEntryRequest {
  partyId: string;
  principal: number;
  ratePercent: number;
  rateBasis: RateBasis;
  startDate: string;
}

export interface CreateVyajPaymentRequest {
  entryId: string;
  paymentDate: string;
  amount: number;
  paymentType: PaymentType;
}

export const RATE_BASIS_OPTIONS: { value: RateBasis; label: string }[] = [
  { value: 'month', label: 'Per month' },
  { value: 'year', label: 'Per year' },
  { value: 'day', label: 'Per day' },
  { value: 'flat', label: 'Flat (one-time)' }
];

export const PAYMENT_TYPE_OPTIONS: { value: PaymentType; label: string }[] = [
  { value: 'interest', label: 'Vyaj paid' },
  { value: 'principal', label: 'Principal paid' }
];
