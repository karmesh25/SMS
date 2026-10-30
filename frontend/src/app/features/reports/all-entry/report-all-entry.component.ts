import { Component, effect, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
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
interface SubLedger { id: string; ledgerName: string; }
interface AllEntryRow {
  date: string;
  aavakLedger?: string;
  aavakSubLedger?: string;
  aavakFlatNo?: string;
  aavakCashBank?: string;
  aavakAmount?: number;
  aavakDescription?: string;
  javakLedger?: string;
  javakSubLedger?: string;
  javakCashBank?: string;
  javakAmount?: number;
  javakDescription?: string;
}

@Component({
  selector: 'app-report-all-entry',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatPaginatorModule, MatIconModule, PageHeaderComponent, IndianCurrencyPipe, AppDatePipe, ReportExportButtonsComponent, ModuleSubnavComponent],
  template: `
    <div class="report-container">
      <div class="no-print">
        <app-page-header title="All Daily Entry" subtitle="Aavak and Javak entries paired by date" />
        <app-module-subnav [items]="reportNav" />
        <section class="abr-panel">
          <h2 class="abr-panel__title"><mat-icon>filter_alt</mat-icon>Filters</h2>
          <form [formGroup]="form">
            <div class="filters">
              <mat-form-field appearance="outline"><mat-label>From</mat-label><input matInput type="date" formControlName="dateFrom" /></mat-form-field>
              <mat-form-field appearance="outline"><mat-label>To</mat-label><input matInput type="date" formControlName="dateTo" /></mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Main Ledger</mat-label>
                <mat-select formControlName="mainLedgerId" (selectionChange)="loadSub()">
                  <mat-option value="">All</mat-option>
                  @for (m of mainLedgers; track m.id) { <mat-option [value]="m.id">{{ m.ledgerName }}</mat-option> }
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Sub Ledger</mat-label>
                <mat-select formControlName="subLedgerId">
                  <mat-option value="">All</mat-option>
                  @for (s of subLedgers; track s.id) { <mat-option [value]="s.id">{{ s.ledgerName }}</mat-option> }
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline"><mat-label>Flat No</mat-label><input matInput formControlName="flatNo" /></mat-form-field>
            </div>
            <div class="filter-actions">
              <button mat-flat-button color="primary" type="button" (click)="search()">Search</button>
              <app-report-export-buttons reportType="all-entry" [filters]="exportFilters()" [disabled]="!canExport" />
            </div>
          </form>
        </section>
      </div>
      <div class="print-only"><h2>All Daily Entry Report</h2><p>{{ form.value.dateFrom }} — {{ form.value.dateTo }}</p></div>
      <div class="abr-table-card">
      <table class="abr-table sticky-header all-entry-table">
        <thead>
          <tr>
            <th colspan="7" class="aavak-header">Aavak</th>
            <th colspan="5" class="javak-header javak-start">Javak</th>
          </tr>
          <tr>
            <th class="col-date">Date</th><th class="col-ledger">Ledger</th><th class="col-sub">Sub</th><th class="col-flat">Flat</th><th class="col-cash">Cash/Bank</th><th class="col-amt">Amt</th><th class="col-desc">Desc</th>
            <th class="col-ledger javak-start">Ledger</th><th class="col-sub">Sub</th><th class="col-cash">Cash/Bank</th><th class="col-amt">Amt</th><th class="col-desc">Desc</th>
          </tr>
        </thead>
        <tbody>
          @for (row of rows; track $index) {
            <tr>
              <td class="col-date">{{ row.date | appDate }}</td>
              <td class="col-ledger">{{ row.aavakLedger ?? '-' }}</td>
              <td class="col-sub">{{ row.aavakSubLedger ?? '-' }}</td>
              <td class="col-flat">{{ row.aavakFlatNo ?? '-' }}</td>
              <td class="col-cash">{{ row.aavakCashBank ?? '-' }}</td>
              <td class="col-amt">{{ row.aavakAmount != null ? (row.aavakAmount | indianCurrency) : '-' }}</td>
              <td class="col-desc">{{ row.aavakDescription ?? '-' }}</td>
              <td class="col-ledger javak-start">{{ row.javakLedger ?? '-' }}</td>
              <td class="col-sub">{{ row.javakSubLedger ?? '-' }}</td>
              <td class="col-cash">{{ row.javakCashBank ?? '-' }}</td>
              <td class="col-amt">{{ row.javakAmount != null ? (row.javakAmount | indianCurrency) : '-' }}</td>
              <td class="col-desc">{{ row.javakDescription ?? '-' }}</td>
            </tr>
          } @empty {
            <tr class="empty-row"><td colspan="12"><mat-icon>info_outline</mat-icon>No records found.</td></tr>
          }
        </tbody>
      </table>
      </div>
      <mat-paginator class="no-print" [length]="total" [pageSize]="pageSize" [pageIndex]="page - 1" (page)="onPage($event)" />
    </div>
  `,
  styleUrls: ['../report-shared.scss']
})
export class ReportAllEntryComponent implements OnInit {
  private readonly reports = inject(ReportService);
  private readonly masterData = inject(MasterDataService);
  private readonly siteContext = inject(SiteContextService);
  private readonly fb = inject(FormBuilder);

  siteId: string | null = null;
  readonly reportNav = REPORT_NAV_ITEMS;
  mainLedgers: MainLedger[] = [];
  subLedgers: SubLedger[] = [];
  rows: AllEntryRow[] = [];
  total = 0;
  page = 1;
  pageSize = 50;

  form = this.fb.nonNullable.group({
    dateFrom: [this.monthStart()],
    dateTo: [this.today()],
    mainLedgerId: [''],
    subLedgerId: [''],
    flatNo: ['']
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

  loadSub(): void {
    const mainId = this.form.value.mainLedgerId;
    if (!mainId) { this.subLedgers = []; return; }
    this.masterData.getSubLedgers(mainId).subscribe({
      next: (r) => { if (r.success) this.subLedgers = r.data as SubLedger[]; }
    });
  }

  search(): void {
    if (!this.siteId) return;
    const raw = this.form.getRawValue();
    const params: Record<string, string | number | boolean> = {
      siteId: this.siteId,
      dateFrom: raw.dateFrom,
      dateTo: raw.dateTo,
      page: this.page,
      pageSize: this.pageSize
    };
    if (raw.mainLedgerId) params['mainLedgerId'] = raw.mainLedgerId;
    if (raw.subLedgerId) params['subLedgerId'] = raw.subLedgerId;
    if (raw.flatNo) params['flatNo'] = raw.flatNo;
    this.reports.getAllEntry(params).subscribe({
      next: (r) => {
        if (r.success) {
          this.rows = r.data.items as AllEntryRow[];
          this.total = r.data.totalCount;
        }
      }
    });
  }

  onPage(e: PageEvent): void {
    this.page = e.pageIndex + 1;
    this.pageSize = e.pageSize;
    this.search();
  }

  get canExport(): boolean {
    const raw = this.form.getRawValue();
    return !!this.siteId && !!raw.dateFrom && !!raw.dateTo;
  }

  exportFilters(): Record<string, string | number | boolean> {
    const raw = this.form.getRawValue();
    const params: Record<string, string | number | boolean> = {
      siteId: this.siteId!,
      dateFrom: raw.dateFrom,
      dateTo: raw.dateTo
    };
    if (raw.mainLedgerId) params['mainLedgerId'] = raw.mainLedgerId;
    if (raw.subLedgerId) params['subLedgerId'] = raw.subLedgerId;
    if (raw.flatNo) params['flatNo'] = raw.flatNo;
    return params;
  }

  private today(): string { return new Date().toISOString().slice(0, 10); }
  private monthStart(): string {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-01`;
  }
}
