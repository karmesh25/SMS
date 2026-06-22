import { Component, effect, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ReportService } from '../../../core/services/report.service';
import { MasterDataService } from '../../../core/services/master-data.service';
import { SiteContextService } from '../../../core/services/site-context.service';
import { IndianCurrencyPipe } from '../../../shared/pipes/indian-currency.pipe';
import { ReportExportButtonsComponent } from '../../../shared/components/report-export-buttons/report-export-buttons.component';
import { ModuleSubnavComponent } from '../../../shared/components/module-subnav/module-subnav.component';
import { REPORT_NAV_ITEMS } from '../../../shared/nav/module-nav-items';

interface MainLedger { id: string; ledgerName: string; }
interface LedgerItem { ledgerName: string; totalAmount: number; }
interface BalanceSheetData {
  siteName: string;
  dateFrom?: string;
  dateTo?: string;
  aavakItems: LedgerItem[];
  totalAavak: number;
  javakItems: LedgerItem[];
  totalJavak: number;
  profit: number;
}

@Component({
  selector: 'app-balance-sheet',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, PageHeaderComponent, IndianCurrencyPipe, ReportExportButtonsComponent, ModuleSubnavComponent],
  template: `
    <div class="report-container">
      <div class="no-print">
        <app-page-header title="Balance Sheet" subtitle="Ledger-wise Aavak and Javak totals" />
        <app-module-subnav [items]="reportNav" />
        <form [formGroup]="form" class="filters">
          <mat-form-field appearance="outline"><mat-label>From</mat-label><input matInput type="date" formControlName="dateFrom" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>To</mat-label><input matInput type="date" formControlName="dateTo" /></mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Main Ledger</mat-label>
            <mat-select formControlName="mainLedgerId">
              <mat-option value="">All</mat-option>
              @for (m of mainLedgers; track m.id) { <mat-option [value]="m.id">{{ m.ledgerName }}</mat-option> }
            </mat-select>
          </mat-form-field>
          <button mat-flat-button color="primary" type="button" (click)="search()">Search</button>
          <app-report-export-buttons reportType="balance-sheet" [filters]="exportFilters()" [disabled]="!siteId" />
        </form>
      </div>
      @if (data) {
        <div class="print-only"><h2>{{ data.siteName }} — Balance Sheet</h2></div>
        <div class="split">
          <section>
            <h3 class="aavak-header">Aavak</h3>
            <table class="mat-elevation-z1">
              <tr><th>Ledger</th><th>Total</th></tr>
              @for (item of data.aavakItems; track item.ledgerName) {
                <tr><td>{{ item.ledgerName }}</td><td>{{ item.totalAmount | indianCurrency }}</td></tr>
              }
              <tr><th>Total</th><th>{{ data.totalAavak | indianCurrency }}</th></tr>
            </table>
          </section>
          <section>
            <h3 class="javak-header">Javak</h3>
            <table class="mat-elevation-z1">
              <tr><th>Ledger</th><th>Total</th></tr>
              @for (item of data.javakItems; track item.ledgerName) {
                <tr><td>{{ item.ledgerName }}</td><td>{{ item.totalAmount | indianCurrency }}</td></tr>
              }
              <tr><th>Total</th><th>{{ data.totalJavak | indianCurrency }}</th></tr>
            </table>
          </section>
        </div>
        <p [class]="data.profit >= 0 ? 'profit-positive' : 'profit-negative'">
          {{ data.profit >= 0 ? 'Net Profit' : 'Net Loss' }}: Rs. {{ data.profit | indianCurrency }}
        </p>
      }
    </div>
  `,
  styleUrls: ['../report-shared.scss'],
  styles: [`
    .split { display: grid; grid-template-columns: 1fr 1fr; gap: 2rem; margin-top: 1rem; }
    @media (max-width: 800px) { .split { grid-template-columns: 1fr; } }
    h3 { padding: 0.5rem; margin: 0 0 0.5rem; }
  `]
})
export class BalanceSheetComponent implements OnInit {
  private readonly reports = inject(ReportService);
  private readonly masterData = inject(MasterDataService);
  private readonly siteContext = inject(SiteContextService);
  private readonly fb = inject(FormBuilder);

  siteId: string | null = null;
  readonly reportNav = REPORT_NAV_ITEMS;
  mainLedgers: MainLedger[] = [];
  data: BalanceSheetData | null = null;

  form = this.fb.nonNullable.group({
    dateFrom: [''],
    dateTo: [''],
    mainLedgerId: ['']
  });

  constructor() {
    effect(() => {
      this.siteId = this.siteContext.activeSiteId();
      if (this.siteId) { this.loadMain(); this.search(); }
    });
  }

  ngOnInit(): void {
    this.siteContext.loadSites().subscribe();
    this.siteId = this.siteContext.activeSiteId();
    if (this.siteId) { this.loadMain(); this.search(); }
  }

  loadMain(): void {
    if (!this.siteId) return;
    this.masterData.getMainLedgers(this.siteId).subscribe({
      next: (r) => { if (r.success) this.mainLedgers = r.data as MainLedger[]; }
    });
  }

  search(): void {
    if (!this.siteId) return;
    const raw = this.form.getRawValue();
    const params: Record<string, string | number | boolean> = { siteId: this.siteId };
    if (raw.dateFrom) params['dateFrom'] = raw.dateFrom;
    if (raw.dateTo) params['dateTo'] = raw.dateTo;
    if (raw.mainLedgerId) params['mainLedgerId'] = raw.mainLedgerId;
    this.reports.getBalanceSheet(params).subscribe({
      next: (r) => { if (r.success) this.data = r.data as BalanceSheetData; }
    });
  }

  exportFilters(): Record<string, string | number | boolean> {
    const raw = this.form.getRawValue();
    const params: Record<string, string | number | boolean> = { siteId: this.siteId! };
    if (raw.dateFrom) params['dateFrom'] = raw.dateFrom;
    if (raw.dateTo) params['dateTo'] = raw.dateTo;
    if (raw.mainLedgerId) params['mainLedgerId'] = raw.mainLedgerId;
    return params;
  }
}
