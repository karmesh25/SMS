import { Component, effect, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
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

interface MainLedger { id: string; ledgerName: string; }
interface EntryItem {
  entryDate: string;
  amount: number;
  cashBank: string;
  description?: string;
}
interface SubLedgerItem {
  subLedgerId: string;
  ledgerName: string;
  totalAmount: number;
  entries: EntryItem[];
}
interface LedgerItem {
  mainLedgerId: string;
  ledgerName: string;
  totalAmount: number;
  subLedgers: SubLedgerItem[];
}
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
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatIconModule, PageHeaderComponent, IndianCurrencyPipe, AppDatePipe, ReportExportButtonsComponent, ModuleSubnavComponent],
  template: `
    <div class="report-container">
      <div class="no-print">
        <app-page-header title="Balance Sheet" subtitle="Ledger-wise Aavak and Javak totals" />
        <app-module-subnav [items]="reportNav" />
        <section class="abr-panel">
          <h2 class="abr-panel__title"><mat-icon>filter_alt</mat-icon>Filters</h2>
          <form [formGroup]="form">
            <div class="filters">
              <mat-form-field appearance="outline"><mat-label>From</mat-label><input matInput type="date" formControlName="dateFrom" /></mat-form-field>
              <mat-form-field appearance="outline"><mat-label>To</mat-label><input matInput type="date" formControlName="dateTo" /></mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Main Ledger</mat-label>
                <mat-select formControlName="mainLedgerId">
                  <mat-option value="">All</mat-option>
                  @for (m of mainLedgers; track m.id) { <mat-option [value]="m.id">{{ m.ledgerName }}</mat-option> }
                </mat-select>
              </mat-form-field>
            </div>
            <div class="filter-actions">
              <button mat-flat-button color="primary" type="button" (click)="search()">Search</button>
              <app-report-export-buttons reportType="balance-sheet" [filters]="exportFilters()" [disabled]="!siteId" />
            </div>
          </form>
        </section>
      </div>
      @if (data) {
        <div class="print-only"><h2>{{ data.siteName }} — Balance Sheet</h2></div>
        <div class="split">
          <section>
            <h3 class="aavak-header">Aavak</h3>
            <div class="abr-table-card">
              <table class="bs-table">
                <tr><th class="expand-col"></th><th>Ledger</th><th class="amt-col">Total</th></tr>
                @for (item of data.aavakItems; track item.mainLedgerId) {
                  <tr class="main-row">
                    <td class="expand-col">
                      <button mat-icon-button type="button" class="expand-btn" (click)="toggleExpand('aavak', item.mainLedgerId)" [attr.aria-label]="isExpanded('aavak', item.mainLedgerId) ? 'Collapse' : 'Expand'">
                        <mat-icon>{{ isExpanded('aavak', item.mainLedgerId) ? 'remove' : 'add' }}</mat-icon>
                      </button>
                    </td>
                    <td>{{ item.ledgerName }}</td>
                    <td class="amt-col">{{ item.totalAmount | indianCurrency }}</td>
                  </tr>
                  @if (isExpanded('aavak', item.mainLedgerId)) {
                    @for (sub of item.subLedgers; track sub.subLedgerId) {
                      <tr class="sub-row">
                        <td class="expand-col">
                          <button mat-icon-button type="button" class="expand-btn" (click)="toggleExpand('aavak', item.mainLedgerId, sub.subLedgerId)" [attr.aria-label]="isExpanded('aavak', item.mainLedgerId, sub.subLedgerId) ? 'Collapse entries' : 'Expand entries'">
                            <mat-icon>{{ isExpanded('aavak', item.mainLedgerId, sub.subLedgerId) ? 'remove' : 'add' }}</mat-icon>
                          </button>
                        </td>
                        <td class="sub-ledger">{{ sub.ledgerName }}</td>
                        <td class="amt-col">{{ sub.totalAmount | indianCurrency }}</td>
                      </tr>
                      @if (isExpanded('aavak', item.mainLedgerId, sub.subLedgerId)) {
                        <tr class="entry-header-row">
                          <td class="expand-col"></td>
                          <td colspan="2">
                            <table class="entry-table">
                              <tr><th>Date</th><th>Cash/Bank</th><th class="amt-col">Amount</th><th>Description</th></tr>
                              @for (entry of sub.entries; track $index) {
                                <tr>
                                  <td class="nowrap">{{ entry.entryDate | appDate }}</td>
                                  <td class="nowrap">{{ entry.cashBank || '-' }}</td>
                                  <td class="amt-col">{{ entry.amount | indianCurrency }}</td>
                                  <td class="desc-cell">{{ entry.description || '-' }}</td>
                                </tr>
                              } @empty {
                                <tr><td colspan="4">No entries</td></tr>
                              }
                            </table>
                          </td>
                        </tr>
                      }
                    }
                  }
                }
                <tr><th class="expand-col"></th><th>Total</th><th class="amt-col">{{ data.totalAavak | indianCurrency }}</th></tr>
              </table>
            </div>
          </section>
          <section>
            <h3 class="javak-header">Javak</h3>
            <div class="abr-table-card">
              <table class="bs-table">
                <tr><th class="expand-col"></th><th>Ledger</th><th class="amt-col">Total</th></tr>
                @for (item of data.javakItems; track item.mainLedgerId) {
                  <tr class="main-row">
                    <td class="expand-col">
                      <button mat-icon-button type="button" class="expand-btn" (click)="toggleExpand('javak', item.mainLedgerId)" [attr.aria-label]="isExpanded('javak', item.mainLedgerId) ? 'Collapse' : 'Expand'">
                        <mat-icon>{{ isExpanded('javak', item.mainLedgerId) ? 'remove' : 'add' }}</mat-icon>
                      </button>
                    </td>
                    <td>{{ item.ledgerName }}</td>
                    <td class="amt-col">{{ item.totalAmount | indianCurrency }}</td>
                  </tr>
                  @if (isExpanded('javak', item.mainLedgerId)) {
                    @for (sub of item.subLedgers; track sub.subLedgerId) {
                      <tr class="sub-row">
                        <td class="expand-col">
                          <button mat-icon-button type="button" class="expand-btn" (click)="toggleExpand('javak', item.mainLedgerId, sub.subLedgerId)" [attr.aria-label]="isExpanded('javak', item.mainLedgerId, sub.subLedgerId) ? 'Collapse entries' : 'Expand entries'">
                            <mat-icon>{{ isExpanded('javak', item.mainLedgerId, sub.subLedgerId) ? 'remove' : 'add' }}</mat-icon>
                          </button>
                        </td>
                        <td class="sub-ledger">{{ sub.ledgerName }}</td>
                        <td class="amt-col">{{ sub.totalAmount | indianCurrency }}</td>
                      </tr>
                      @if (isExpanded('javak', item.mainLedgerId, sub.subLedgerId)) {
                        <tr class="entry-header-row">
                          <td class="expand-col"></td>
                          <td colspan="2">
                            <table class="entry-table">
                              <tr><th>Date</th><th>Cash/Bank</th><th class="amt-col">Amount</th><th>Description</th></tr>
                              @for (entry of sub.entries; track $index) {
                                <tr>
                                  <td class="nowrap">{{ entry.entryDate | appDate }}</td>
                                  <td class="nowrap">{{ entry.cashBank || '-' }}</td>
                                  <td class="amt-col">{{ entry.amount | indianCurrency }}</td>
                                  <td class="desc-cell">{{ entry.description || '-' }}</td>
                                </tr>
                              } @empty {
                                <tr><td colspan="4">No entries</td></tr>
                              }
                            </table>
                          </td>
                        </tr>
                      }
                    }
                  }
                }
                <tr><th class="expand-col"></th><th>Total</th><th class="amt-col">{{ data.totalJavak | indianCurrency }}</th></tr>
              </table>
            </div>
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
    @media (max-width: 959px) { .split { grid-template-columns: 1fr; } }
    h3 { padding: 0.5rem; margin: 0 0 0.5rem; }
    .bs-table { width: 100%; border-collapse: collapse; }
    .bs-table th, .bs-table td { border: 1px solid var(--abr-border); padding: 0.35rem 0.5rem; vertical-align: top; }
    .expand-col { width: 2.5rem; padding: 0 !important; text-align: center; }
    .expand-btn { width: 2rem; height: 2rem; padding: 0; }
    .expand-btn mat-icon { font-size: 1.125rem; width: 1.125rem; height: 1.125rem; }
    .amt-col { text-align: right; white-space: nowrap; }
    .sub-row td { font-size: 0.9em; color: var(--abr-text-muted, #666); }
    .sub-ledger { padding-left: 0.5rem !important; }
    .entry-table { width: 100%; border-collapse: collapse; margin: 0.15rem 0; }
    .entry-table th, .entry-table td { border: 1px solid var(--abr-border); padding: 0.25rem 0.4rem; font-size: 0.8em; }
    .entry-table th { background: var(--abr-surface-2); font-weight: 600; }
    .nowrap { white-space: nowrap; }
    .desc-cell { word-break: break-word; overflow-wrap: anywhere; }
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
  private expandedKeys = new Set<string>();

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
    this.expandedKeys.clear();
    const raw = this.form.getRawValue();
    const params: Record<string, string | number | boolean> = { siteId: this.siteId };
    if (raw.dateFrom) params['dateFrom'] = raw.dateFrom;
    if (raw.dateTo) params['dateTo'] = raw.dateTo;
    if (raw.mainLedgerId) params['mainLedgerId'] = raw.mainLedgerId;
    this.reports.getBalanceSheet(params).subscribe({
      next: (r) => { if (r.success) this.data = r.data as BalanceSheetData; }
    });
  }

  expandKey(side: 'aavak' | 'javak', mainLedgerId: string, subLedgerId?: string): string {
    return subLedgerId ? `${side}:${mainLedgerId}:${subLedgerId}` : `${side}:${mainLedgerId}`;
  }

  isExpanded(side: 'aavak' | 'javak', mainLedgerId: string, subLedgerId?: string): boolean {
    return this.expandedKeys.has(this.expandKey(side, mainLedgerId, subLedgerId));
  }

  toggleExpand(side: 'aavak' | 'javak', mainLedgerId: string, subLedgerId?: string): void {
    const key = this.expandKey(side, mainLedgerId, subLedgerId);
    const next = new Set(this.expandedKeys);
    if (next.has(key)) next.delete(key);
    else next.add(key);
    this.expandedKeys = next;
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
