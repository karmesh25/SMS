import { Component, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { IndianAmountDirective } from '../../../shared/directives/indian-amount.directive';
import { AppDatePipe } from '../../../shared/pipes/app-date.pipe';
import { IndianCurrencyPipe } from '../../../shared/pipes/indian-currency.pipe';
import { PAYMENT_TYPE_OPTIONS, VyajEntry } from '../models/vyaj.models';

@Component({
  selector: 'app-vyaj-entry-row',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatTooltipModule,
    HasPermissionDirective,
    IndianAmountDirective,
    AppDatePipe,
    IndianCurrencyPipe
  ],
  templateUrl: './entry-row.component.html',
  styleUrl: './entry-row.component.scss'
})
export class VyajEntryRowComponent {
  private readonly fb = new FormBuilder();

  readonly entry = input.required<VyajEntry>();
  readonly readOnly = input(false);
  readonly savingPayment = input(false);

  readonly closedToggled = output<boolean>();
  readonly paymentRecorded = output<{ amount: number; paymentDate: string; paymentType: string }>();
  readonly deleteEntry = output<void>();
  readonly deletePayment = output<string>();

  readonly paymentTypeOptions = PAYMENT_TYPE_OPTIONS;
  readonly showPayment = signal(false);

  readonly paymentForm = this.fb.nonNullable.group({
    paymentType: ['interest' as const, Validators.required],
    amount: ['', [Validators.required, Validators.min(0.01)]],
    paymentDate: [new Date().toISOString().slice(0, 10), Validators.required]
  });

  basisLabel(basis: string): string {
    const map: Record<string, string> = {
      flat: 'flat',
      month: 'month',
      year: 'year',
      day: 'day'
    };
    return map[basis] ?? basis;
  }

  submitPayment(): void {
    if (this.paymentForm.invalid) return;
    const v = this.paymentForm.getRawValue();
    const amount = parseFloat(String(v.amount).replace(/,/g, ''));
    this.paymentRecorded.emit({
      amount,
      paymentDate: v.paymentDate,
      paymentType: v.paymentType
    });
    this.paymentForm.patchValue({ amount: '' });
    this.showPayment.set(false);
  }
}
