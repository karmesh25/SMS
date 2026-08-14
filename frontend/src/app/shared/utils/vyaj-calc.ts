export type RateBasis = 'flat' | 'month' | 'year' | 'day';
export type PaymentType = 'interest' | 'principal';

export interface VyajPaymentInput {
  amount: number;
  paymentType: PaymentType;
  paymentDate?: string | Date;
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

function toDateKey(value: string | Date): string {
  const d = parseDate(value);
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
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

export function resolveMonthFactor(
  startDate: string | Date,
  asOfDate: string | Date,
  ratePeriodMonths?: number | null
): number {
  const elapsed = monthsBetween(startDate, asOfDate);
  if (ratePeriodMonths === 3 || ratePeriodMonths === 6 || ratePeriodMonths === 9) {
    return Math.min(elapsed, ratePeriodMonths);
  }
  return elapsed;
}

function accrueSegment(
  outstandingPrincipal: number,
  rate: number,
  rateBasis: RateBasis,
  entryStart: string | Date,
  segmentStart: string | Date,
  segmentEnd: string | Date,
  ratePeriodMonths?: number | null
): number {
  if (outstandingPrincipal <= 0 || parseDate(segmentEnd) <= parseDate(segmentStart)) return 0;

  switch (rateBasis) {
    case 'month':
      return (
        outstandingPrincipal *
        rate *
        Math.max(
          0,
          resolveMonthFactor(entryStart, segmentEnd, ratePeriodMonths) -
            resolveMonthFactor(entryStart, segmentStart, ratePeriodMonths)
        )
      );
    case 'year':
      return outstandingPrincipal * rate * (daysBetween(segmentStart, segmentEnd) / 365);
    case 'day':
      return outstandingPrincipal * rate * daysBetween(segmentStart, segmentEnd);
    default:
      return (
        outstandingPrincipal *
        rate *
        Math.max(
          0,
          resolveMonthFactor(entryStart, segmentEnd, ratePeriodMonths) -
            resolveMonthFactor(entryStart, segmentStart, ratePeriodMonths)
        )
      );
  }
}

/** Accrues vyaj on reducing principal after dated principal payments. */
export function calculateReducingGrossVyaj(
  principal: number,
  ratePercent: number,
  rateBasis: RateBasis,
  startDate: string | Date,
  asOfDate: string | Date = new Date(),
  principalPayments: { amount: number; paymentDate: string | Date }[] = [],
  ratePeriodMonths?: number | null
): number {
  if (principal <= 0 || ratePercent <= 0) return 0;

  const rate = ratePercent / 100;
  const asOf = parseDate(asOfDate);

  const paymentsByDate = new Map<string, number>();
  for (const p of principalPayments) {
    if (p.amount <= 0) continue;
    if (parseDate(p.paymentDate) > asOf) continue;
    const key = toDateKey(p.paymentDate);
    paymentsByDate.set(key, (paymentsByDate.get(key) ?? 0) + p.amount);
  }

  const ordered = [...paymentsByDate.entries()]
    .map(([key, amount]) => ({ date: parseDate(key), amount }))
    .sort((a, b) => a.date.getTime() - b.date.getTime());

  if (rateBasis === 'flat') {
    let outstanding = principal;
    for (const p of ordered) outstanding = Math.max(0, outstanding - p.amount);
    return roundMoney(outstanding * rate);
  }

  let gross = 0;
  let outstandingPrincipal = principal;
  let cursor = parseDate(startDate);

  for (const p of ordered) {
    if (p.date > cursor) {
      gross += accrueSegment(
        outstandingPrincipal,
        rate,
        rateBasis,
        startDate,
        cursor,
        p.date,
        ratePeriodMonths
      );
    }
    outstandingPrincipal = Math.max(0, outstandingPrincipal - p.amount);
    if (p.date > cursor) cursor = p.date;
  }

  if (asOf > cursor) {
    gross += accrueSegment(
      outstandingPrincipal,
      rate,
      rateBasis,
      startDate,
      cursor,
      asOf,
      ratePeriodMonths
    );
  }

  return roundMoney(gross);
}

export function calculateGrossVyaj(
  principal: number,
  ratePercent: number,
  rateBasis: RateBasis,
  startDate: string | Date,
  asOfDate: string | Date = new Date(),
  ratePeriodMonths?: number | null
): number {
  return calculateReducingGrossVyaj(
    principal,
    ratePercent,
    rateBasis,
    startDate,
    asOfDate,
    [],
    ratePeriodMonths
  );
}

export function calculateEntryTotals(
  principal: number,
  ratePercent: number,
  rateBasis: RateBasis,
  startDate: string | Date,
  payments: VyajPaymentInput[] = [],
  asOfDate: string | Date = new Date(),
  ratePeriodMonths?: number | null
): VyajEntryTotals {
  const principalPays = payments
    .filter((p) => p.paymentType === 'principal')
    .map((p) => ({
      amount: p.amount,
      paymentDate: p.paymentDate ?? startDate
    }));

  const grossVyaj = calculateReducingGrossVyaj(
    principal,
    ratePercent,
    rateBasis,
    startDate,
    asOfDate,
    principalPays,
    ratePeriodMonths
  );

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
