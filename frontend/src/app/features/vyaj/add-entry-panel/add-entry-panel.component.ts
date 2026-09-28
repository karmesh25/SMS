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
import {
  parseRateBasisUi,
  RATE_BASIS_OPTIONS,
  RateBasisUiOption,
  VyajRateBasisOption
} from '../models/vyaj.models';

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
  readonly partyHasOpenEntries = input(false);
  readonly suggestedPrincipal = input<number | null>(null);
  readonly managedRateOptions = input<VyajRateBasisOption[]>([]);

  readonly saved = output<{
    principal?: number | null;
    ratePercent: number;
    rateBasis: string;
    ratePeriodMonths?: number | null;
    customDays?: number | null;
    emiAmount?: number | null;
    startDate: string;
  }>();
  readonly closed = output<void>();

  readonly form = this.fb.nonNullable.group({
    principal: ['', [Validators.required, Validators.min(0.01)]],
    ratePercent: [2, [Validators.required, Validators.min(0.01)]],
    rateBasisUi: ['month-3', Validators.required],
    customDays: [30 as number | null],
    emiAmount: [''],
    startDate: [new Date().toISOString().slice(0, 10), Validators.required]
  });

  readonly formValues = signal(this.form.getRawValue());

  readonly rateBasisOptions = computed<RateBasisUiOption[]>(() => {
    const managed = this.managedRateOptions()
      .filter((o) => o.isActive)
      .map((o) => ({
        value: `custom-${o.dayCount}`,
        label: o.label,
        rateBasis: 'custom' as const,
        customDays: o.dayCount
      }));
    return [...RATE_BASIS_OPTIONS.slice(0, -1), ...managed, RATE_BASIS_OPTIONS[RATE_BASIS_OPTIONS.length - 1]];
  });

  readonly showCustomDays = computed(() => this.formValues().rateBasisUi === 'custom-input');

  readonly principalRequired = computed(() => !this.partyHasOpenEntries());

  readonly showPrincipalField = computed(() => true);

  readonly remainingAfterFirstEmi = computed(() => {
    const v = this.formValues();
    const principal = this.resolvePreviewPrincipal(v.principal);
    const emi = parseFloat(String(v.emiAmount ?? '').replace(/,/g, '')) || 0;
    return Math.max(0, principal - emi);
  });

  readonly previewGrossVyaj = computed(() => {
    const v = this.formValues();
    const principal = this.resolvePreviewPrincipal(v.principal);
    const emi = parseFloat(String(v.emiAmount ?? '').replace(/,/g, '')) || 0;
    const ratePercent = Number(v.ratePercent) || 0;
    const parsed = parseRateBasisUi(v.rateBasisUi, v.customDays);
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
      const required = this.principalRequired();
      const ctrl = this.form.controls.principal;
      if (required) {
        ctrl.setValidators([Validators.required, Validators.min(0.01)]);
      } else {
        ctrl.clearValidators();
      }
      ctrl.updateValueAndValidity({ emitEvent: false });
    });

    effect(() => {
      if (!this.open()) {
        this.emiTouchedByUser = false;
        this.form.reset({
          principal: '',
          ratePercent: 2,
          rateBasisUi: 'month-3',
          customDays: 30,
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
    const principalRaw = String(v.principal ?? '').replace(/,/g, '');
    const principal = principalRaw ? parseFloat(principalRaw) : null;
    const emiRaw = String(v.emiAmount ?? '').replace(/,/g, '');
    const emiAmount = emiRaw ? parseFloat(emiRaw) : null;
    const parsed = parseRateBasisUi(v.rateBasisUi, Number(v.customDays) || null);
    if (parsed.rateBasis === 'custom' && !(parsed.customDays && parsed.customDays > 0)) return;

    this.saved.emit({
      principal: principal && principal > 0 ? principal : null,
      ratePercent: Number(v.ratePercent),
      rateBasis: parsed.rateBasis,
      ratePeriodMonths: parsed.ratePeriodMonths ?? null,
      customDays: parsed.customDays ?? null,
      emiAmount: emiAmount && emiAmount > 0 ? emiAmount : null,
      startDate: v.startDate
    });
  }

  private resolvePreviewPrincipal(raw: string): number {
    const parsed = parseFloat(String(raw).replace(/,/g, '')) || 0;
    if (parsed > 0) return parsed;
    return this.suggestedPrincipal() ?? 0;
  }

  private maybeSuggestEmi(): void {
    if (this.emiTouchedByUser) return;
    const v = this.form.getRawValue();
    const parsed = parseRateBasisUi(v.rateBasisUi, Number(v.customDays) || null);
    const principal = this.resolvePreviewPrincipal(v.principal);
    if (!parsed.ratePeriodMonths || principal <= 0) return;
    const suggested = Math.round((principal / parsed.ratePeriodMonths) * 100) / 100;
    if (suggested > 0 && suggested < principal) {
      this.form.controls.emiAmount.setValue(String(suggested), { emitEvent: false });
      this.formValues.set(this.form.getRawValue());
    }
  }
}
