import { CdkFixedSizeVirtualScroll, CdkVirtualForOf } from '@angular/cdk/scrolling';
import {
  Component,
  EventEmitter,
  forwardRef,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  ViewChild
} from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelect, MatSelectModule } from '@angular/material/select';

export interface SelectOption<T = string> {
  value: T;
  label: string;
}

@Component({
  selector: 'app-searchable-select',
  standalone: true,
  imports: [FormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, CdkFixedSizeVirtualScroll, CdkVirtualForOf],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SearchableSelectComponent),
      multi: true
    }
  ],
  template: `
    <mat-form-field appearance="outline" class="searchable-select">
      <mat-label>{{ label }}</mat-label>
      <mat-select
        #selectRef
        [value]="value"
        [disabled]="disabled"
        (openedChange)="onOpenedChange($event)"
        (selectionChange)="onSelectionChange($event.value)">
        <div class="filter-box" (click)="$event.stopPropagation()" (keydown)="$event.stopPropagation()">
          <input
            matInput
            placeholder="Search..."
            [(ngModel)]="filterText"
            (ngModelChange)="applyFilter()"
            (keydown)="$event.stopPropagation()" />
        </div>
        @if (useVirtualScroll) {
          <cdk-virtual-scroll-viewport itemSize="48" class="virtual-viewport">
            <mat-option
              *cdkVirtualFor="let opt of filteredOptions"
              [value]="opt.value">
              {{ opt.label }}
            </mat-option>
          </cdk-virtual-scroll-viewport>
        } @else {
          @for (opt of filteredOptions; track opt.value) {
            <mat-option [value]="opt.value">{{ opt.label }}</mat-option>
          }
        }
        @if (filteredOptions.length === 0) {
          <mat-option disabled>No matches</mat-option>
        }
      </mat-select>
    </mat-form-field>
  `,
  styles: [`
    .searchable-select { width: 100%; }
    .filter-box {
      padding: 0.5rem 1rem 0.25rem;
      position: sticky;
      top: 0;
      z-index: 1;
      background: var(--abr-surface);
    }
    .virtual-viewport {
      height: 240px;
      width: 100%;
    }
  `]
})
export class SearchableSelectComponent<T = string> implements ControlValueAccessor, OnChanges {
  @Input() label = 'Select';
  @Input() options: SelectOption<T>[] = [];
  @Input() virtualScrollThreshold = 100;
  @Output() readonly selectionChange = new EventEmitter<T>();

  @ViewChild('selectRef') selectRef?: MatSelect;

  value: T | null = null;
  disabled = false;
  filterText = '';
  filteredOptions: SelectOption<T>[] = [];

  private onChange: (value: T | null) => void = () => {};
  private onTouched: () => void = () => {};

  get useVirtualScroll(): boolean {
    return this.options.length > this.virtualScrollThreshold;
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['options']) {
      this.applyFilter();
    }
  }

  writeValue(value: T | null): void {
    this.value = value;
  }

  registerOnChange(fn: (value: T | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  onSelectionChange(value: T): void {
    this.value = value;
    this.onChange(value);
    this.onTouched();
    this.selectionChange.emit(value);
  }

  onOpenedChange(opened: boolean): void {
    if (!opened) {
      this.filterText = '';
      this.applyFilter();
      this.onTouched();
    }
  }

  applyFilter(): void {
    const q = this.filterText.trim().toLowerCase();
    this.filteredOptions = q
      ? this.options.filter((o) => o.label.toLowerCase().includes(q))
      : [...this.options];
  }
}
