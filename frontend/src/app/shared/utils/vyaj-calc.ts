export type RateBasis = 'flat' | 'month' | 'year' | 'day';
export type PaymentType = 'interest' | 'principal';

export interface VyajPaymentInput {
  amount: number;
  paymentType: PaymentType;
}

export interface VyajEntryTotals {
  grossVyaj: number;
  interestPaid: number;
  principalPaid: number;
  vyajDue: number;
  principalDue: number;
}

function parseDate(value: string | Date): Date {
  if (value instanceof Date) return value;
  const [y, m, d] = value.split('T')[0].split('-').map(Number);
  return new Date(y, m - 1, d);
}

export function daysBetween(start: string | Date, end: string | Date): number {
  const s = parseDate(start);
  const e = parseDate(end);
  const ms = e.getTime() - s.getTime();
  return Math.max(0, Math.floor(ms / (1000 * 60 * 60 * 24)));
}

export function monthsBetween(start: string | Date, end: string | Date): number {
  const s = parseDate(start);
  const e = parseDate(end);
  if (e < s) return 0;

  let months = (e.getFullYear() - s.getFullYear()) * 12 + (e.getMonth() - s.getMonth());
  if (e.getDate() < s.getDate()) months--;

  const anchor = new Date(s.getFullYear(), s.getMonth() + months, s.getDate());
  const remDays = Math.max(0, Math.round((e.getTime() - anchor.getTime()) / 86400000));
  return Math.max(0, months) + remDays / 30;
}

export function roundMoney(value: number): number {
  return Math.round(value * 100) / 100;
}

export function calculateGrossVyaj(
  principal: number,
  ratePercent: number,
  rateBasis: RateBasis,
  startDate: string | Date,
  asOfDate: string | Date = new Date()
): number {
  if (principal <= 0 || ratePercent <= 0) return 0;

  const rate = ratePercent / 100;

  switch (rateBasis) {
    case 'flat':
      return roundMoney(principal * rate);
    case 'month':
      return roundMoney(principal * rate * monthsBetween(startDate, asOfDate));
    case 'year':
      return roundMoney(principal * rate * (daysBetween(startDate, asOfDate) / 365));
    case 'day':
      return roundMoney(principal * rate * daysBetween(startDate, asOfDate));
    default:
      return roundMoney(principal * rate * monthsBetween(startDate, asOfDate));
  }
}

export function calculateEntryTotals(
  principal: number,
  ratePercent: number,
  rateBasis: RateBasis,
  startDate: string | Date,
  payments: VyajPaymentInput[] = [],
  asOfDate: string | Date = new Date()
): VyajEntryTotals {
  const grossVyaj = calculateGrossVyaj(principal, ratePercent, rateBasis, startDate, asOfDate);

  let interestPaid = 0;
  let principalPaid = 0;

  for (const p of payments) {
    if (p.paymentType === 'principal') principalPaid += p.amount;
    else interestPaid += p.amount;
  }

  return {
    grossVyaj: roundMoney(grossVyaj),
    interestPaid: roundMoney(interestPaid),
    principalPaid: roundMoney(principalPaid),
    vyajDue: roundMoney(Math.max(0, grossVyaj - interestPaid)),
    principalDue: roundMoney(Math.max(0, principal - principalPaid))
  };
}
