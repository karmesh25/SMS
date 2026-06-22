import {
  calculateEntryTotals,
  calculateGrossVyaj,
  daysBetween,
  monthsBetween
} from './vyaj-calc';

describe('vyaj-calc', () => {
  it('flat rate returns principal times rate', () => {
    expect(calculateGrossVyaj(100_000, 5, 'flat', '2024-01-01', '2024-06-01')).toBe(5_000);
  });

  it('month rate uses fractional months', () => {
    expect(calculateGrossVyaj(100_000, 2, 'month', '2024-01-15', '2024-04-14')).toBe(6_000);
  });

  it('month rate accrues partial month by days', () => {
    expect(calculateGrossVyaj(100_000, 2, 'month', '2024-01-15', '2024-01-20')).toBe(333.33);
  });

  it('month rate on forty lakh for five days', () => {
    expect(calculateGrossVyaj(4_000_000, 2, 'month', '2024-06-17', '2024-06-22')).toBe(13_333.33);
  });

  it('year rate uses days over 365', () => {
    expect(calculateGrossVyaj(365_000, 10, 'year', '2024-01-01', '2025-01-01')).toBe(36_600);
  });

  it('day rate uses days elapsed', () => {
    expect(calculateGrossVyaj(10_000, 1, 'day', '2024-01-01', '2024-01-31')).toBe(3_000);
  });

  it('entry totals subtract payments', () => {
    const totals = calculateEntryTotals(
      100_000,
      2,
      'month',
      '2024-01-01',
      [
        { amount: 500, paymentType: 'interest' },
        { amount: 200, paymentType: 'principal' }
      ],
      '2024-04-01'
    );

    expect(totals.grossVyaj).toBe(6_000);
    expect(totals.vyajDue).toBe(5_500);
    expect(totals.principalDue).toBe(99_800);
  });

  it('monthsBetween handles partial month', () => {
    expect(monthsBetween('2024-03-15', '2024-04-10')).toBeCloseTo(26 / 30, 5);
    expect(monthsBetween('2024-03-15', '2024-04-15')).toBe(1);
  });

  it('daysBetween never negative', () => {
    expect(daysBetween('2024-05-01', '2024-04-01')).toBe(0);
    expect(daysBetween('2024-01-01', '2024-01-31')).toBe(30);
  });
});
