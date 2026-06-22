import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'indianCurrency', standalone: true })
export class IndianCurrencyPipe implements PipeTransform {
  transform(value: number | null | undefined, decimals = 2): string {
    if (value === null || value === undefined || isNaN(value)) return '0.00';
    const fixed = value.toFixed(decimals);
    const [intPart, decPart] = fixed.split('.');
    const lastThree = intPart.slice(-3);
    const other = intPart.slice(0, -3);
    const formatted = other.replace(/\B(?=(\d{2})+(?!\d))/g, ',');
    const prefix = other.length > 0 ? `${formatted},` : '';
    return `${prefix}${lastThree}.${decPart}`;
  }
}
