import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { BookingService, InstallmentMilestone } from '../../../core/services/booking.service';
import { ToastService } from '../../../core/services/toast.service';
import { IndianAmountDirective } from '../../../shared/directives/indian-amount.directive';
import { IndianCurrencyPipe } from '../../../shared/pipes/indian-currency.pipe';

export interface RecordPaymentDialogData {
  milestone: InstallmentMilestone;
  memberName?: string;
  flatNo?: string;
}

@Component({
  selector: 'app-record-payment-dialog',
  standalone: true,
  imports: [IndianCurrencyPipe, ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, IndianAmountDirective],
  template: `
    <h2 mat-dialog-title>
      @if (data.memberName && data.flatNo) {
        Payment — {{ data.memberName }} ({{ unitLabel }} {{ data.flatNo }})
      } @else {
        Record Payment
      }
    </h2>
    <mat-dialog-content>
      <p><strong>{{ data.milestone.milestoneName }}</strong> — Due: {{ data.milestone.dueAmount | indianCurrency }} | Remaining: {{ data.milestone.remainingAmount | indianCurrency }}</p>
      <form [formGroup]="form" class="form">
        <mat-form-field appearance="outline"><mat-label>Amount</mat-label><input matInput formControlName="amount" appIndianAmount /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Payment Date</mat-label><input matInput type="date" formControlName="paidDate" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Notes</mat-label><input matInput formControlName="notes" /></mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="close()">Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="save()">Record</button>
    </mat-dialog-actions>
  `,
  styles: [`.form { display: flex; flex-direction: column; gap: 0.5rem; min-width: 320px; }`]
})
export class RecordPaymentDialogComponent {
  readonly data = inject<RecordPaymentDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<RecordPaymentDialogComponent, boolean>);
  private readonly bookingService = inject(BookingService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly unitLabel = 'Flat';

  form = this.fb.nonNullable.group({
    amount: [this.data.milestone.remainingAmount || 0, [Validators.required, Validators.min(0.01)]],
    paidDate: [new Date().toISOString().slice(0, 10), Validators.required],
    notes: ['']
  });

  save(): void {
    const raw = this.form.getRawValue();
    this.bookingService.recordPayment({
      bookingInstallmentId: this.data.milestone.id,
      amount: raw.amount,
      paidDate: raw.paidDate,
      notes: raw.notes || null
    }).subscribe({
      next: (r) => {
        if (r.success) {
          this.toast.success('Payment recorded — accounting entry created');
          this.dialogRef.close(true);
        }
      },
      error: (err) => this.toast.error(err.error?.message ?? 'Failed to record payment')
    });
  }

  close(): void { this.dialogRef.close(false); }
}
