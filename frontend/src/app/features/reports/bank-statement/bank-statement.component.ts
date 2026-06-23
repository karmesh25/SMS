import { Component, effect, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ReportService } from '../../../core/services/report.service';
import { MasterDataService } from '../../../core/services/master-data.service';
import { SiteContextService } from '../../../core/services/site-context.service';
import { IndianCurrencyPipe } from '../../../shared/pipes/indian-currency.pipe';
import { AppDatePipe } from '../../../shared/pipes/app-date.pipe';
import { ReportExportButtonsComponent } from '../../../shared/components/report-export-buttons/report-export-buttons.component';
import { ModuleSubnavComponent } from '../../../shared/components/module-subnav/module-subnav.component';
import { REPORT_NAV_ITEMS } from '../../../shared/nav/module-nav-items';

interface BankRow { id: string; bankName: string; accountNo: string; }
interface StmtRow { entryDate: string; description?: string; debit: number; credit: number; balance: number; }

@Component({
  selector: 'app-bank-statement',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatTableModule, MatIconModule, PageHeaderComponent, IndianCurrencyPipe, AppDatePipe, ReportExportButtonsComponent, ModuleSubnavComponent],
  template: `
    <div class="report-container">
      <div class="no-print">
        <app-page-header title="Bank Statement" subtitle="Running balance per bank account" />
        <app-module-subnav [items]="reportNav" />
        <form [formGroup]="form" class="filters">
          <mat-form-field appearance="outline">
            <mat-label>Bank Account</mat-label>
            <mat-select formControlName="bankAccountId">
              @for (b of banks; track b.id) {
                <mat-option [value]="b.id">{{ b.bankName }} - {{ b.accountNo }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline"><mat-label>From</mat-label><input matInput type="date" formControlName="dateFrom" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>To</mat-label><input matInput type="date" formControlName="dateTo" /></mat-form-field>
          <button mat-flat-button color="primary" type="button" (click)="search()" [disabled]="!form.value.bankAccountId">Search</button>
          <app-report-export-buttons reportType="bank-statement" [filters]="exportFilters()" [disabled]="!canExport" />
        </form>
      </div>
      @if (openingBalance != null) {
        <p>Opening: Rs. {{ openingBalance | indianCurrency }} | Closing: Rs. {{ closingBalance | indianCurrency }}</p>
      }
      <div class="abr-scroll-x">
      <table mat-table [dataSource]="rows" class="mat-elevation-z1 abr-table sticky-header">
        <ng-container matColumnDef="entryDate"><th mat-header-cell *matHeaderCellDef>Date</th><td mat-cell *matCellDef="let r">{{ r.entryDate | appDate }}</td></ng-container>
        <ng-container matColumnDef="description"><th mat-header-cell *matHeaderCellDef>Description</th><td mat-cell *matCellDef="let r">{{ r.description ?? '-' }}</td></ng-container>
        <ng-container matColumnDef="debit"><th mat-header-cell *matHeaderCellDef>Debit</th><td mat-cell *matCellDef="let r">{{ r.debit ? (r.debit | indianCurrency) : '-' }}</td></ng-container>
        <ng-container matColumnDef="credit"><th mat-header-cell *matHeaderCellDef>Credit</th><td mat-cell *matCellDef="let r">{{ r.credit ? (r.credit | indianCurrency) : '-' }}</td></ng-container>
        <ng-container matColumnDef="balance"><th mat-header-cell *matHeaderCellDef>Balance</th><td mat-cell *matCellDef="let r">{{ r.balance | indianCurrency }}</td></ng-container>
        <tr mat-header-row *matHeaderRowDef="cols"></tr>
        <tr mat-row *matRowDef="let row; columns: cols"></tr>
        <tr class="empty-row" *matNoDataRow><td [attr.colspan]="cols.length"><mat-icon>info_outline</mat-icon>No records found.</td></tr>
      </table>
      </div>
    </div>
  `,
  styleUrls: ['../report-shared.scss']
})
export class BankStatementComponent implements OnInit {
  private readonly reports = inject(ReportService);
  private readonly masterData = inject(MasterDataService);
  private readonly siteContext = inject(SiteContextService);
  private readonly fb = inject(FormBuilder);

  siteId: string | null = null;
  readonly reportNav = REPORT_NAV_ITEMS;
  banks: BankRow[] = [];
  rows: StmtRow[] = [];
  cols = ['entryDate', 'description', 'debit', 'credit', 'balance'];
  openingBalance: number | null = null;
  closingBalance = 0;

  form = this.fb.nonNullable.group({
    bankAccountId: [''],
    dateFrom: [''],
    dateTo: ['']
  });

  constructor() {
    effect(() => {
      this.siteId = this.siteContext.activeSiteId();
      if (this.siteId) this.loadBanks();
    });
  }

  ngOnInit(): void {
    this.siteContext.loadSites().subscribe();
    this.siteId = this.siteContext.activeSiteId();
    if (this.siteId) this.loadBanks();
  }

  loadBanks(): void {
    if (!this.siteId) return;
    this.masterData.getBanks(this.siteId).subscribe({
      next: (r) => { if (r.success) this.banks = r.data as BankRow[]; }
    });
  }

  search(): void {
    if (!this.siteId || !this.form.value.bankAccountId) return;
    const raw = this.form.getRawValue();
    const params: Record<string, string | number | boolean> = {
      siteId: this.siteId,
      bankAccountId: raw.bankAccountId
    };
    if (raw.dateFrom) params['dateFrom'] = raw.dateFrom;
    if (raw.dateTo) params['dateTo'] = raw.dateTo;
    this.reports.getBankStatement(params).subscribe({
      next: (r) => {
        if (r.success) {
          const d = r.data as { rows: StmtRow[]; openingBalance: number; closingBalance: number };
          this.rows = d.rows;
          this.openingBalance = d.openingBalance;
          this.closingBalance = d.closingBalance;
        }
      }
    });
  }

  get canExport(): boolean {
    return !!this.siteId && !!this.form.value.bankAccountId;
  }

  exportFilters(): Record<string, string | number | boolean> {
    const raw = this.form.getRawValue();
    const params: Record<string, string | number | boolean> = {
      siteId: this.siteId!,
      bankAccountId: raw.bankAccountId
    };
    if (raw.dateFrom) params['dateFrom'] = raw.dateFrom;
    if (raw.dateTo) params['dateTo'] = raw.dateTo;
    return params;
  }
}
