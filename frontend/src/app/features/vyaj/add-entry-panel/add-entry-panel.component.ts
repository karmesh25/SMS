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
import { parseRateBasisUi, RATE_BASIS_OPTIONS } from '../models/vyaj.models';

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
    ratePeriodMonths?: number | null;
    emiAmount?: number | null;
    startDate: string;
  }>();
  readonly closed = output<void>();

  readonly rateBasisOptions = RATE_BASIS_OPTIONS;

  readonly form = this.fb.nonNullable.group({
    principal: ['', [Validators.required, Validators.min(0.01)]],
    ratePercent: [2, [Validators.required, Validators.min(0.01)]],
    rateBasisUi: ['month-3', Validators.required],
    emiAmount: [''],
    startDate: [new Date().toISOString().slice(0, 10), Validators.required]
  });

  readonly formValues = signal(this.form.getRawValue());

  readonly previewGrossVyaj = computed(() => {
    const v = this.formValues();
    const principal = parseFloat(String(v.principal).replace(/,/g, '')) || 0;
    const ratePercent = Number(v.ratePercent) || 0;
    const parsed = parseRateBasisUi(v.rateBasisUi);
    return calculateGrossVyaj(principal, ratePercent, parsed.rateBasis, v.startDate, new Date(), parsed.ratePeriodMonths);
  });

  constructor() {
    effect((onCleanup) => {
      const sub = this.form.valueChanges.subscribe(() => {
        this.formValues.set(this.form.getRawValue());
        this.maybeSuggestEmi();
      });
      onCleanup(() => sub.unsubscribe());
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const principal = parseFloat(String(v.principal).replace(/,/g, ''));
    const emiRaw = String(v.emiAmount ?? '').replace(/,/g, '');
    const emiAmount = emiRaw ? parseFloat(emiRaw) : null;
    const parsed = parseRateBasisUi(v.rateBasisUi);
    this.saved.emit({
      principal,
      ratePercent: Number(v.ratePercent),
      rateBasis: parsed.rateBasis,
      ratePeriodMonths: parsed.ratePeriodMonths ?? null,
      emiAmount: emiAmount && emiAmount > 0 ? emiAmount : null,
      startDate: v.startDate
    });
  }

  private maybeSuggestEmi(): void {
    const v = this.form.getRawValue();
    const parsed = parseRateBasisUi(v.rateBasisUi);
    const principal = parseFloat(String(v.principal).replace(/,/g, '')) || 0;
    const currentEmi = String(v.emiAmount ?? '').trim();
    if (!parsed.ratePeriodMonths || principal <= 0) return;
    if (currentEmi) return;
    const suggested = Math.round((principal / parsed.ratePeriodMonths) * 100) / 100;
    this.form.controls.emiAmount.setValue(String(suggested), { emitEvent: false });
  }
}
