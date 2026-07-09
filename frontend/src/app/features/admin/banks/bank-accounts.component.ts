import { Component, effect, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDialog } from '@angular/material/dialog';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { MasterDataService } from '../../../core/services/master-data.service';
import { SiteContextService } from '../../../core/services/site-context.service';
import { ToastService } from '../../../core/services/toast.service';
import { BankEditDialogComponent } from '../../../shared/components/master-edit/bank-edit-dialog.component';

interface BankRow {
  id: string;
  bankName: string;
  accountNo: string;
  ifscCode?: string;
  branch?: string;
  openingBalance: number;
  isActive: boolean;
}

@Component({
  selector: 'app-bank-accounts',
  standalone: true,
  imports: [ReactiveFormsModule, MatTableModule, MatIconModule, MatButtonModule, MatFormFieldModule, MatInputModule, PageHeaderComponent],
  template: `
    <app-page-header title="Bank Accounts" subtitle="Site bank accounts"></app-page-header>

    <section class="abr-panel">
      <h2 class="abr-panel__title"><mat-icon>add_circle</mat-icon>Add Bank Account</h2>
      <form [formGroup]="form" class="abr-form-grid" (ngSubmit)="save()">
        <mat-form-field appearance="outline"><mat-label>Bank Name</mat-label><input matInput formControlName="bankName" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Account No</mat-label><input matInput formControlName="accountNo" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>IFSC</mat-label><input matInput formControlName="ifscCode" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Branch</mat-label><input matInput formControlName="branch" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Opening Balance</mat-label><input matInput type="number" formControlName="openingBalance" /></mat-form-field>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">
          <mat-icon>add</mat-icon>
          Add Account
        </button>
      </form>
    </section>

    <div class="abr-table-card">
      <table mat-table [dataSource]="banks" class="abr-table sticky-header">
        <ng-container matColumnDef="bankName"><th mat-header-cell *matHeaderCellDef>Bank</th><td mat-cell *matCellDef="let row">{{ row.bankName }}</td></ng-container>
        <ng-container matColumnDef="accountNo"><th mat-header-cell *matHeaderCellDef>Account</th><td mat-cell *matCellDef="let row">{{ row.accountNo }}</td></ng-container>
        <ng-container matColumnDef="ifscCode"><th mat-header-cell *matHeaderCellDef>IFSC</th><td mat-cell *matCellDef="let row">{{ row.ifscCode || '—' }}</td></ng-container>
        <ng-container matColumnDef="isActive"><th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let row">
            <span class="abr-chip" [class.abr-chip--success]="row.isActive" [class.abr-chip--danger]="!row.isActive">
              {{ row.isActive ? 'Active' : 'Inactive' }}
            </span>
          </td>
        </ng-container>
        <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef>Actions</th>
          <td mat-cell *matCellDef="let row">
            <button mat-button (click)="edit(row)">Edit</button>
            @if (row.isActive) {
              <button mat-button color="warn" (click)="toggle(row.id)">Disable</button>
            } @else {
              <button mat-button color="primary" (click)="toggle(row.id)">Enable</button>
            }
          </td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="cols"></tr>
        <tr mat-row *matRowDef="let row; columns: cols"></tr>
        <tr class="empty-row" *matNoDataRow><td [attr.colspan]="cols.length"><mat-icon>account_balance</mat-icon>No bank accounts yet.</td></tr>
      </table>
    </div>
  `,
  styles: [`
    .abr-form-grid button { min-width: 140px; }
  `]
})
export class BankAccountsComponent implements OnInit {
  private readonly masterData = inject(MasterDataService);
  private readonly siteContext = inject(SiteContextService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);

  banks: BankRow[] = [];
  cols = ['bankName', 'accountNo', 'ifscCode', 'isActive', 'actions'];
  siteId: string | null = null;

  form = this.fb.nonNullable.group({
    bankName: ['', Validators.required],
    accountNo: ['', Validators.required],
    ifscCode: [''],
    branch: [''],
    openingBalance: [0]
  });

  constructor() {
    effect(() => {
      this.siteId = this.siteContext.activeSiteId();
      if (this.siteId) this.load();
    });
  }

  ngOnInit(): void {
    this.siteContext.loadSites().subscribe();
    this.siteId = this.siteContext.activeSiteId();
    if (this.siteId) this.load();
  }

  load(): void {
    if (!this.siteId) return;
    this.masterData.getBanks(this.siteId).subscribe({
      next: (r) => { if (r.success) this.banks = r.data as BankRow[]; }
    });
  }

  save(): void {
    if (!this.siteId) return;
    this.masterData.createBank({ ...this.form.getRawValue(), siteId: this.siteId }).subscribe({
      next: (r) => { if (r.success) { this.toast.success('Bank account added'); this.form.reset({ openingBalance: 0 }); this.load(); } }
    });
  }

  edit(row: BankRow): void {
    const ref = this.dialog.open(BankEditDialogComponent, {
      data: {
        bankName: row.bankName,
        accountNo: row.accountNo,
        ifscCode: row.ifscCode,
        branch: row.branch,
        openingBalance: row.openingBalance,
        isActive: row.isActive
      }
    });
    ref.afterClosed().subscribe((result) => {
      if (!result) return;
      this.masterData.updateBank(row.id, result).subscribe({
        next: (r) => { if (r.success) { this.toast.success('Bank account updated'); this.load(); } }
      });
    });
  }

  toggle(id: string): void {
    this.masterData.toggleBank(id).subscribe({
      next: (r) => { if (r.success) { this.toast.success('Status updated'); this.load(); } }
    });
  }
}
