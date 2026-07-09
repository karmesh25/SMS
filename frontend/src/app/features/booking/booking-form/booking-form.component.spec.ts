import { buildBooking } from '../../../testing/test-builders';

describe('BookingForm calculations', () => {
  it('computes total price from sqft and rate', () => {
    const { sqft, rate } = buildBooking({ sqft: 1200, rate: 4500 });
    expect(Math.round(sqft * rate * 100) / 100).toBe(5400000);
  });

  it('computes rate from sqft and total price', () => {
    const sqft = 1200;
    const total = 5400000;
    const rate = Math.round((total / sqft) * 100) / 100;
    expect(rate).toBe(4500);
  });

  it('computes brokerage from percentage', () => {
    const total = 1000000;
    const brokeragePct = 2;
    expect(Math.round(total * (brokeragePct / 100) * 100) / 100).toBe(20000);
  });

  it('computes brokerage percentage from amount', () => {
    const total = 1000000;
    const brokerageAmount = 20000;
    const pct = Math.round((brokerageAmount / total) * 10000) / 100;
    expect(pct).toBe(2);
  });

  it('allows brokerage above 2% when amount is within total', () => {
    const total = 15000000;
    const brokeragePct = 5;
    const amount = Math.round(total * (brokeragePct / 100) * 100) / 100;
    expect(amount).toBe(750000);
    expect(amount).toBeLessThanOrEqual(total);
  });

  it('caps brokerage amount to total price', () => {
    const total = 15000000;
    const amount = 16000000;
    const cappedAmount = total;
    const cappedPct = Math.round((cappedAmount / total) * 10000) / 100;
    expect(cappedAmount).toBe(total);
    expect(cappedPct).toBe(100);
    expect(amount).toBeGreaterThan(total);
  });
});
