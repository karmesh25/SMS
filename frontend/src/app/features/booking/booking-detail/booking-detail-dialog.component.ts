import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Booking, BookingService, InstallmentMilestone, InstallmentSummary } from '../../../core/services/booking.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { IndianCurrencyPipe } from '../../../shared/pipes/indian-currency.pipe';
import { AppDatePipe } from '../../../shared/pipes/app-date.pipe';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { RecordPaymentDialogComponent } from './record-payment-dialog.component';

export interface BookingDetailDialogData {
  flatId: string;
  flatStatus: string;
}

@Component({
  selector: 'app-booking-detail-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatTableModule, MatIconModule, MatProgressBarModule, IndianCurrencyPipe, AppDatePipe, HasPermissionDirective],
  template: `
    <h2 mat-dialog-title>{{ unitLabel }} {{ booking?.flatNo ?? 'Details' }}</h2>
    <mat-dialog-content>
      @if (booking) {
        <div class="summary">
          <p><strong>Member:</strong> {{ booking.memberName }}</p>
          <p><strong>Contact:</strong> {{ booking.customerContact || '—' }}</p>
          <p><strong>Wing:</strong> {{ booking.wingName }} | <strong>Status:</strong> {{ booking.status }}</p>
          <p><strong>Total SQFT:</strong> {{ booking.sqft }} | <strong>Rate/SQFT:</strong> {{ booking.rate | indianCurrency }}</p>
          <p><strong>Total Price:</strong> {{ booking.totalPrice | indianCurrency }} | <strong>Broker:</strong> {{ booking.brokerName ?? 'None' }}</p>
          <p><strong>Condition:</strong> {{ booking.conditionName }} | <strong>Date:</strong> {{ booking.bookingDate | appDate }}</p>
        </div>

        @if (installments) {
          <div class="inst-summary">
            <p>Paid: {{ installments.totalPaid | indianCurrency }} / {{ installments.totalDue | indianCurrency }} ({{ installments.percentagePaid }}%)</p>
            <mat-progress-bar mode="determinate" [value]="installments.percentagePaid"></mat-progress-bar>
          </div>

          <div class="abr-scroll-x">
          <table mat-table [dataSource]="installments.milestones" class="mat-elevation-z1 abr-table sticky-header">
            <ng-container matColumnDef="milestoneName"><th mat-header-cell *matHeaderCellDef>Milestone</th><td mat-cell *matCellDef="let row">{{ row.milestoneName }}</td></ng-container>
            <ng-container matColumnDef="dueDate"><th mat-header-cell *matHeaderCellDef>Due</th><td mat-cell *matCellDef="let row">{{ row.dueDate | appDate }}</td></ng-container>
            <ng-container matColumnDef="dueAmount"><th mat-header-cell *matHeaderCellDef>Due Amt</th><td mat-cell *matCellDef="let row">{{ row.dueAmount | indianCurrency }}</td></ng-container>
            <ng-container matColumnDef="paidAmount"><th mat-header-cell *matHeaderCellDef>Paid</th><td mat-cell *matCellDef="let row">{{ row.paidAmount | indianCurrency }}</td></ng-container>
            <ng-container matColumnDef="status"><th mat-header-cell *matHeaderCellDef>Status</th><td mat-cell *matCellDef="let row"><span [class]="'status-' + row.status">{{ row.status }}</span></td></ng-container>
            <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef></th><td mat-cell *matCellDef="let row">
              @if (booking.status === 'active' && row.status !== 'paid' && authService.hasPermission('booking', 'manage')) {
                <button mat-button (click)="recordPayment(row)">Pay</button>
              }
            </td></ng-container>
            <tr mat-header-row *matHeaderRowDef="cols"></tr>
            <tr mat-row *matRowDef="let row; columns: cols"></tr>
            <tr class="empty-row" *matNoDataRow><td [attr.colspan]="cols.length"><mat-icon>info_outline</mat-icon>No records found.</td></tr>
          </table>
          </div>
        }
      } @else if (loading) {
        <p>Loading...</p>
      } @else {
        <p>No booking found for this flat.</p>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="close()">Close</button>
      @if (booking) {
        <button mat-button (click)="goDastavej()">Dastavej</button>
      }
      @if (booking && booking.status === 'active' && nextPendingMilestone()) {
        <button
          mat-flat-button
          color="accent"
          *appHasPermission="'booking'; level: 'manage'"
          (click)="addBookingPayment()">
          Add Booking Payment
        </button>
      }
      @if (booking && booking.status === 'active') {
        <button mat-flat-button color="primary" (click)="edit()">Edit Booking</button>
      }
    </mat-dialog-actions>
  `,
  styles: [`
    .summary p { margin: 0.25rem 0; }
    .inst-summary { margin: 1rem 0; }
    table { width: 100%; margin-top: 1rem; min-width: 520px; }
    @media (max-width: 599px) {
      mat-dialog-actions { flex-direction: column; align-items: stretch; gap: 0.5rem; }
      mat-dialog-actions button { width: 100%; margin: 0 !important; }
    }
    .status-paid { color: var(--abr-success); font-weight: 600; }
    .status-partial { color: var(--abr-info); font-weight: 600; }
    .status-pending { color: var(--abr-text-muted); }
    .status-overdue { color: var(--abr-danger); font-weight: 600; }
  `]
})
export class BookingDetailDialogComponent implements OnInit {
  readonly data = inject<BookingDetailDialogData>(MAT_DIALOG_DATA);
  readonly authService = inject(AuthService);
  private readonly dialogRef = inject(MatDialogRef<BookingDetailDialogComponent>);
  private readonly bookingService = inject(BookingService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);

  booking: Booking | null = null;
  installments: InstallmentSummary | null = null;
  loading = true;
  cols = ['milestoneName', 'dueDate', 'dueAmount', 'paidAmount', 'status', 'actions'];
  unitLabel = 'Flat';

  ngOnInit(): void {
    if (this.data.flatStatus === 'available') {
      this.loading = false;
      return;
    }

    this.bookingService.getByFlat(this.data.flatId).subscribe({
      next: (r) => {
        this.loading = false;
        if (r.success) {
          this.booking = r.data;
          this.loadInstallments(r.data.id);
        }
      },
      error: () => { this.loading = false; }
    });
  }

  loadInstallments(bookingId: string): void {
    this.bookingService.getInstallments(bookingId).subscribe({
      next: (r) => { if (r.success) this.installments = r.data; },
      error: (err) => {
        const message = err.error?.message ?? 'Failed to load installments.';
        const details = err.error?.errors?.filter(Boolean).join(', ');
        this.toast.error(details ? `${message}: ${details}` : message);
      }
    });
  }

  nextPendingMilestone(): InstallmentMilestone | null {
    if (!this.installments?.milestones?.length) return null;
    const pending = this.installments.milestones
      .filter((m) => m.status !== 'paid' && m.remainingAmount > 0)
      .sort((a, b) => a.dueDate.localeCompare(b.dueDate));
    return pending[0] ?? null;
  }

  recordPayment(milestone: InstallmentMilestone): void {
    this.openPaymentDialog(milestone);
  }

  addBookingPayment(): void {
    const milestone = this.nextPendingMilestone();
    if (!milestone) {
      this.toast.error('No pending payment for this booking.');
      return;
    }
    this.openPaymentDialog(milestone);
  }

  private openPaymentDialog(milestone: InstallmentMilestone): void {
    const ref = this.dialog.open(RecordPaymentDialogComponent, {
      data: {
        milestone,
        memberName: this.booking?.memberName,
        flatNo: this.booking?.flatNo
      },
      width: 'min(420px, calc(100vw - 2rem))'
    });
    ref.afterClosed().subscribe((saved) => {
      if (saved && this.booking) this.loadInstallments(this.booking.id);
    });
  }

  edit(): void {
    if (!this.booking) return;
    this.dialogRef.close();
    void this.router.navigate(['/booking/edit', this.booking.id]);
  }

  goDastavej(): void {
    this.dialogRef.close();
    void this.router.navigate(['/accounting/dastavej']);
  }

  close(): void { this.dialogRef.close(); }
}
