import { Pipe, PipeTransform } from '@angular/core';

/** Formats stored rupee amounts for display as amount÷1000 with no grouping. */
export function formatDisplayAmount(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(Number(value))) {
    return '0';
  }

  const scaled = Number(value) / 1000;
  // Money has 2 decimal places → up to 5 after ÷1000
  let text = scaled.toFixed(5);
  if (text.includes('.')) {
    text = text.replace(/\.?0+$/, '');
  }
  return text === '-0' ? '0' : text;
}

/** Indian grouping for form inputs (real rupees, not obfuscated). */
export function formatIndianGroupedAmount(value: number | null | undefined, decimals = 2): string {
  if (value === null || value === undefined || Number.isNaN(Number(value))) {
    return '0.00';
  }
  const fixed = Number(value).toFixed(decimals);
  const [intPart, decPart] = fixed.split('.');
  const lastThree = intPart.slice(-3);
  const other = intPart.slice(0, -3);
  const formatted = other.replace(/\B(?=(\d{2})+(?!\d))/g, ',');
  const prefix = other.length > 0 ? `${formatted},` : '';
  return `${prefix}${lastThree}.${decPart}`;
}

@Pipe({ name: 'indianCurrency', standalone: true })
export class IndianCurrencyPipe implements PipeTransform {
  /**
   * Displays obfuscated amounts (÷1000, no commas).
   * Second arg kept for call-site compatibility; ignored.
   */
  transform(value: number | null | undefined, _decimals = 2): string {
    return formatDisplayAmount(value);
  }
}
