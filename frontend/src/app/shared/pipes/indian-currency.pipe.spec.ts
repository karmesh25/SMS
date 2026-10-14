import { formatDisplayAmount, formatIndianGroupedAmount, IndianCurrencyPipe } from './indian-currency.pipe';

describe('formatDisplayAmount (÷1000 obfuscation)', () => {
  it('scales example profit amount', () => {
    expect(formatDisplayAmount(363861552)).toBe('363861.552');
  });

  it('trims trailing zeros for whole thousands', () => {
    expect(formatDisplayAmount(1500000)).toBe('1500');
    expect(formatDisplayAmount(67000)).toBe('67');
  });

  it('handles zero and nullish', () => {
    expect(formatDisplayAmount(0)).toBe('0');
    expect(formatDisplayAmount(null)).toBe('0');
    expect(formatDisplayAmount(undefined)).toBe('0');
  });

  it('keeps precision for paisa after scale', () => {
    expect(formatDisplayAmount(0.01)).toBe('0.00001');
  });
});

describe('formatIndianGroupedAmount (form inputs)', () => {
  it('keeps Indian grouping for real rupees', () => {
    expect(formatIndianGroupedAmount(1500000)).toBe('15,00,000.00');
  });
});

describe('IndianCurrencyPipe', () => {
  it('uses obfuscated display format', () => {
    const pipe = new IndianCurrencyPipe();
    expect(pipe.transform(363861552)).toBe('363861.552');
  });
});
