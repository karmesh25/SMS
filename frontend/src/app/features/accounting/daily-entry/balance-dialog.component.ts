import { DecimalPipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { BalanceSummary, DailyEntryService } from '../../../core/services/daily-entry.service';
import { IndianCurrencyPipe } from '../../../shared/pipes/indian-currency.pipe';

export interface BalanceDialogData {
  siteId: string;
}

@Component({
  selector: 'app-balance-dialog',
  standalone: true,
  imports: [DecimalPipe, MatDialogModule, MatButtonModule, MatTableModule, IndianCurrencyPipe],
  template: `
    <h2 mat-dialog-title>Cash & Bank Balances</h2>
    <mat-dialog-content>
      @if (balance) {
        <p class="cash-row"><strong>Cash:</strong> Rs. {{ balance.cashBalance | indianCurrency }}</p>
        <table mat-table [dataSource]="balance.bankBalances" class="mat-elevation-z1">
          <ng-container matColumnDef="bankName"><th mat-header-cell *matHeaderCellDef>Bank</th><td mat-cell *matCellDef="let row">{{ row.bankName }}</td></ng-container>
          <ng-container matColumnDef="accountNo"><th mat-header-cell *matHeaderCellDef>Account</th><td mat-cell *matCellDef="let row">{{ row.accountNo }}</td></ng-container>
          <ng-container matColumnDef="balance"><th mat-header-cell *matHeaderCellDef>Balance</th><td mat-cell *matCellDef="let row">Rs. {{ row.balance | indianCurrency }}</td></ng-container>
          <tr mat-header-row *matHeaderRowDef="cols"></tr>
          <tr mat-row *matRowDef="let row; columns: cols"></tr>
        </table>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Close</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .cash-row { font-size: 1.1rem; margin-bottom: 1rem; color: var(--abr-primary-strong); }
    table { width: 100%; }
  `]
})
export class BalanceDialogComponent implements OnInit {
  readonly data = inject<BalanceDialogData>(MAT_DIALOG_DATA);
  private readonly dailyEntryService = inject(DailyEntryService);

  balance: BalanceSummary | null = null;
  cols = ['bankName', 'accountNo', 'balance'];

  ngOnInit(): void {
    this.dailyEntryService.getBalance(this.data.siteId).subscribe({
      next: (r) => { if (r.success) this.balance = r.data; }
    });
  }
}
