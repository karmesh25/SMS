import { DestroyRef, Directive, ElementRef, HostListener, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NgControl } from '@angular/forms';
import { IndianCurrencyPipe } from '../pipes/indian-currency.pipe';

@Directive({
  selector: 'input[appIndianAmount]',
  standalone: true,
  providers: [IndianCurrencyPipe]
})
export class IndianAmountDirective implements OnInit {
  private readonly el = inject(ElementRef<HTMLInputElement>);
  private readonly control = inject(NgControl, { optional: true });
  private readonly currencyPipe = inject(IndianCurrencyPipe);
  private readonly destroyRef = inject(DestroyRef);
  private editing = false;

  ngOnInit(): void {
    this.formatDisplay();
    const ctrl = this.control?.control;
    if (ctrl) {
      ctrl.valueChanges
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((value) => {
          if (!this.editing) {
            this.formatDisplay(value as number);
          }
        });
    }
  }

  @HostListener('focus')
  onFocus(): void {
    this.editing = true;
    const raw = this.control?.value ?? this.el.nativeElement.value;
    if (raw !== null && raw !== undefined && raw !== '') {
      this.el.nativeElement.value = String(Number(raw));
    }
  }

  @HostListener('blur')
  onBlur(): void {
    this.editing = false;
    const parsed = this.parseAmount(this.el.nativeElement.value);
    if (this.control?.control) {
      this.control.control.setValue(parsed, { emitEvent: true });
    }
    this.formatDisplay(parsed);
  }

  @HostListener('input')
  onInput(): void {
    if (!this.editing) return;
    const parsed = this.parseAmount(this.el.nativeElement.value);
    this.control?.control?.setValue(parsed, { emitEvent: true });
  }

  private formatDisplay(value?: number | null): void {
    const num = value ?? this.control?.value;
    if (num === null || num === undefined || num === '' || isNaN(Number(num))) {
      return;
    }
    this.el.nativeElement.value = this.currencyPipe.transform(Number(num));
  }

  private parseAmount(raw: string): number {
    const cleaned = raw.replace(/,/g, '').trim();
    const num = Number(cleaned);
    return isNaN(num) ? 0 : num;
  }
}
