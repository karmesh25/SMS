import { buildBooking } from '../../../testing/test-builders';

describe('BookingForm calculations', () => {
  it('computes total price from sqft and rate', () => {
    const { sqft, rate } = buildBooking({ sqft: 1200, rate: 4500 });
    expect(sqft * rate).toBe(5400000);
  });

  it('computes brokerage from percentage', () => {
    const total = 1000000;
    const brokeragePct = 2;
    expect(total * brokeragePct / 100).toBe(20000);
  });
});
