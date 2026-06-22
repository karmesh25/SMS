import { Component, computed, effect, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { IndianAmountDirective } from '../../../shared/directives/indian-amount.directive';
import { IndianCurrencyPipe } from '../../../shared/pipes/indian-currency.pipe';
import { calculateGrossVyaj } from '../../../shared/utils/vyaj-calc';
import { RATE_BASIS_OPTIONS } from '../models/vyaj.models';

@Component({
  selector: 'app-vyaj-add-entry-panel',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    IndianAmountDirective,
    IndianCurrencyPipe
  ],
  templateUrl: './add-entry-panel.component.html',
  styleUrl: './add-entry-panel.component.scss'
})
export class VyajAddEntryPanelComponent {
  private readonly fb = new FormBuilder();

  readonly open = input(false);
  readonly saving = input(false);

  readonly saved = output<{
    principal: number;
    ratePercent: number;
    rateBasis: string;
    startDate: string;
  }>();
  readonly closed = output<void>();

  readonly rateBasisOptions = RATE_BASIS_OPTIONS;

  readonly form = this.fb.nonNullable.group({
    principal: ['', [Validators.required, Validators.min(0.01)]],
    ratePercent: [2, [Validators.required, Validators.min(0.01)]],
    rateBasis: ['month' as const, Validators.required],
    startDate: [new Date().toISOString().slice(0, 10), Validators.required]
  });

  readonly formValues = signal(this.form.getRawValue());

  readonly previewGrossVyaj = computed(() => {
    const v = this.formValues();
    const principal = parseFloat(String(v.principal).replace(/,/g, '')) || 0;
    const ratePercent = Number(v.ratePercent) || 0;
    return calculateGrossVyaj(principal, ratePercent, v.rateBasis, v.startDate);
  });

  constructor() {
    effect((onCleanup) => {
      const sub = this.form.valueChanges.subscribe(() => {
        this.formValues.set(this.form.getRawValue());
      });
      onCleanup(() => sub.unsubscribe());
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const principal = parseFloat(String(v.principal).replace(/,/g, ''));
    this.saved.emit({
      principal,
      ratePercent: Number(v.ratePercent),
      rateBasis: v.rateBasis,
      startDate: v.startDate
    });
  }
}
