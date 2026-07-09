import { Component, effect, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ReportService } from '../../../core/services/report.service';
import { SiteContextService } from '../../../core/services/site-context.service';
import { IndianCurrencyPipe } from '../../../shared/pipes/indian-currency.pipe';
import { ReportExportButtonsComponent } from '../../../shared/components/report-export-buttons/report-export-buttons.component';
import { ModuleSubnavComponent } from '../../../shared/components/module-subnav/module-subnav.component';
import { REPORT_NAV_ITEMS } from '../../../shared/nav/module-nav-items';

interface MonthwiseRow {
  monthLabel: string;
  aavakTotal: number;
  javakTotal: number;
  net: number;
}

@Component({
  selector: 'app-monthwise-report',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatInputModule, MatTableModule, MatIconModule, PageHeaderComponent, IndianCurrencyPipe, ReportExportButtonsComponent, ModuleSubnavComponent],
  template: `
    <div class="report-container">
      <div class="no-print">
        <app-page-header title="Monthwise Totals" subtitle="Aavak and Javak by month" />
        <app-module-subnav [items]="reportNav" />
        <section class="abr-panel">
          <h2 class="abr-panel__title"><mat-icon>filter_alt</mat-icon>Filters</h2>
          <form [formGroup]="form" class="filters">
            <mat-form-field appearance="outline"><mat-label>From</mat-label><input matInput type="date" formControlName="dateFrom" /></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>To</mat-label><input matInput type="date" formControlName="dateTo" /></mat-form-field>
            <button mat-flat-button color="primary" type="button" (click)="search()">Search</button>
            <app-report-export-buttons reportType="monthwise" [filters]="exportFilters()" [disabled]="!siteId" />
          </form>
        </section>
      </div>
      <div class="abr-table-card">
      <table mat-table [dataSource]="rows" class="abr-table sticky-header">
        <ng-container matColumnDef="monthLabel"><th mat-header-cell *matHeaderCellDef>Month</th><td mat-cell *matCellDef="let r">{{ r.monthLabel }}</td></ng-container>
        <ng-container matColumnDef="aavakTotal"><th mat-header-cell *matHeaderCellDef>Aavak</th><td mat-cell *matCellDef="let r">{{ r.aavakTotal | indianCurrency }}</td></ng-container>
        <ng-container matColumnDef="javakTotal"><th mat-header-cell *matHeaderCellDef>Javak</th><td mat-cell *matCellDef="let r">{{ r.javakTotal | indianCurrency }}</td></ng-container>
        <ng-container matColumnDef="net"><th mat-header-cell *matHeaderCellDef>Net</th><td mat-cell *matCellDef="let r" [class]="r.net >= 0 ? 'profit-positive' : 'profit-negative'">{{ r.net | indianCurrency }}</td></ng-container>
        <tr mat-header-row *matHeaderRowDef="cols"></tr>
        <tr mat-row *matRowDef="let row; columns: cols"></tr>
        <tr class="empty-row" *matNoDataRow><td [attr.colspan]="cols.length"><mat-icon>info_outline</mat-icon>No records found.</td></tr>
      </table>
      </div>
    </div>
  `,
  styleUrls: ['../report-shared.scss']
})
export class MonthwiseReportComponent implements OnInit {
  private readonly reports = inject(ReportService);
  private readonly siteContext = inject(SiteContextService);
  private readonly fb = inject(FormBuilder);

  siteId: string | null = null;
  readonly reportNav = REPORT_NAV_ITEMS;
  rows: MonthwiseRow[] = [];
  cols = ['monthLabel', 'aavakTotal', 'javakTotal', 'net'];

  form = this.fb.nonNullable.group({ dateFrom: [''], dateTo: [''] });

  constructor() {
    effect(() => {
      this.siteId = this.siteContext.activeSiteId();
      if (this.siteId) this.search();
    });
  }

  ngOnInit(): void {
    this.siteContext.loadSites().subscribe();
    this.siteId = this.siteContext.activeSiteId();
    if (this.siteId) this.search();
  }

  search(): void {
    if (!this.siteId) return;
    const raw = this.form.getRawValue();
    const params: Record<string, string | number | boolean> = { siteId: this.siteId };
    if (raw.dateFrom) params['dateFrom'] = raw.dateFrom;
    if (raw.dateTo) params['dateTo'] = raw.dateTo;
    this.reports.getMonthwise(params).subscribe({
      next: (r) => { if (r.success) this.rows = r.data as MonthwiseRow[]; }
    });
  }

  exportFilters(): Record<string, string | number | boolean> {
    const raw = this.form.getRawValue();
    const params: Record<string, string | number | boolean> = { siteId: this.siteId! };
    if (raw.dateFrom) params['dateFrom'] = raw.dateFrom;
    if (raw.dateTo) params['dateTo'] = raw.dateTo;
    return params;
  }
}
