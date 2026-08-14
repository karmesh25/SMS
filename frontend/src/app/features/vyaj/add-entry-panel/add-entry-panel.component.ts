import { Component, computed, effect, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { IndianAmountDirective } from '../../../shared/directives/indian-amount.directive';
import { IndianCurrencyPipe } from '../../../shared/pipes/indian-currency.pipe';
import { DateFieldComponent } from '../../../shared/components/date-field/date-field.component';
import { calculateReducingGrossVyaj } from '../../../shared/utils/vyaj-calc';
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
    IndianCurrencyPipe,
    DateFieldComponent
  ],
  templateUrl: './add-entry-panel.component.html',
  styleUrl: './add-entry-panel.component.scss'
})
export class VyajAddEntryPanelComponent {
  private readonly fb = new FormBuilder();
  private emiTouchedByUser = false;

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

  readonly remainingAfterFirstEmi = computed(() => {
    const v = this.formValues();
    const principal = parseFloat(String(v.principal).replace(/,/g, '')) || 0;
    const emi = parseFloat(String(v.emiAmount ?? '').replace(/,/g, '')) || 0;
    return Math.max(0, principal - emi);
  });

  readonly previewGrossVyaj = computed(() => {
    const v = this.formValues();
    const principal = parseFloat(String(v.principal).replace(/,/g, '')) || 0;
    const emi = parseFloat(String(v.emiAmount ?? '').replace(/,/g, '')) || 0;
    const ratePercent = Number(v.ratePercent) || 0;
    const parsed = parseRateBasisUi(v.rateBasisUi);
    const principalPays =
      emi > 0 && emi < principal
        ? [{ amount: emi, paymentDate: v.startDate }]
        : [];
    return calculateReducingGrossVyaj(
      principal,
      ratePercent,
      parsed.rateBasis,
      v.startDate,
      new Date(),
      principalPays,
      parsed.ratePeriodMonths
    );
  });

  constructor() {
    effect((onCleanup) => {
      const sub = this.form.valueChanges.subscribe(() => {
        this.formValues.set(this.form.getRawValue());
        this.maybeSuggestEmi();
      });
      onCleanup(() => sub.unsubscribe());
    });

    effect(() => {
      if (!this.open()) {
        this.emiTouchedByUser = false;
        this.form.reset({
          principal: '',
          ratePercent: 2,
          rateBasisUi: 'month-3',
          emiAmount: '',
          startDate: new Date().toISOString().slice(0, 10)
        });
        this.formValues.set(this.form.getRawValue());
      }
    });
  }

  onEmiInput(): void {
    this.emiTouchedByUser = true;
  }

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const principal = parseFloat(String(v.principal).replace(/,/g, ''));
    const emiRaw = String(v.emiAmount ?? '').replace(/,/g, '');
    const emiAmount = emiRaw ? parseFloat(emiRaw) : null;
    if (emiAmount != null && emiAmount >= principal) return;
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
    if (this.emiTouchedByUser) return;
    const v = this.form.getRawValue();
    const parsed = parseRateBasisUi(v.rateBasisUi);
    const principal = parseFloat(String(v.principal).replace(/,/g, '')) || 0;
    if (!parsed.ratePeriodMonths || principal <= 0) return;
    const suggested = Math.round((principal / parsed.ratePeriodMonths) * 100) / 100;
    this.form.controls.emiAmount.setValue(String(suggested), { emitEvent: false });
    this.formValues.set(this.form.getRawValue());
  }
}
