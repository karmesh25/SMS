import { Component, Input, OnDestroy, forwardRef } from '@angular/core';
import {
  ControlValueAccessor,
  FormControl,
  NG_VALUE_ACCESSOR,
  ReactiveFormsModule
} from '@angular/forms';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Subscription } from 'rxjs';

/** Converts between ISO `yyyy-MM-dd` strings and Material Date values. */
@Component({
  selector: 'app-date-field',
  standalone: true,
  host: {
    '[class.compact]': 'compact'
  },
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatIconModule
  ],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DateFieldComponent),
      multi: true
    }
  ],
  template: `
    <mat-form-field
      [appearance]="appearance"
      [class.compact]="compact"
      [subscriptSizing]="compact ? 'dynamic' : 'fixed'">
      <mat-label>{{ label }}</mat-label>
      <input
        matInput
        [matDatepicker]="picker"
        [formControl]="inner"
        [placeholder]="placeholder"
        autocomplete="off" />
      <mat-datepicker-toggle matIconSuffix [for]="picker" />
      <mat-datepicker #picker />
      @if (hint) {
        <mat-hint>{{ hint }}</mat-hint>
      }
    </mat-form-field>
  `,
  styles: [`
    :host { display: block; }
    mat-form-field { width: 100%; }
    :host.compact mat-form-field,
    mat-form-field.compact { width: 160px; }
    mat-form-field.compact ::ng-deep .mat-mdc-form-field-subscript-wrapper {
      display: none;
    }
  `]
})
export class DateFieldComponent implements ControlValueAccessor, OnDestroy {
  @Input() label = 'Date';
  @Input() placeholder = 'dd/mm/yyyy';
  @Input() hint = '';
  @Input() appearance: 'outline' | 'fill' = 'outline';
  @Input() compact = false;

  readonly inner = new FormControl<Date | null>(null);
  private sub: Subscription;
  private onChange: (value: string) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  constructor() {
    this.sub = this.inner.valueChanges.subscribe((date) => {
      this.onChange(this.toIso(date));
      this.onTouched();
    });
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  writeValue(value: string | Date | null | undefined): void {
    if (value instanceof Date) {
      this.inner.setValue(value, { emitEvent: false });
      return;
    }
    if (!value) {
      this.inner.setValue(null, { emitEvent: false });
      return;
    }
    this.inner.setValue(this.fromIso(String(value)), { emitEvent: false });
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    if (isDisabled) this.inner.disable({ emitEvent: false });
    else this.inner.enable({ emitEvent: false });
  }

  private toIso(date: Date | null): string {
    if (!date || Number.isNaN(date.getTime())) return '';
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  private fromIso(value: string): Date | null {
    const raw = value.includes('T') ? value.slice(0, 10) : value;
    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(raw);
    if (!match) return null;
    const y = Number(match[1]);
    const m = Number(match[2]);
    const d = Number(match[3]);
    const date = new Date(y, m - 1, d);
    return Number.isNaN(date.getTime()) ? null : date;
  }
}
