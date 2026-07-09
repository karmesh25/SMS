import { Component, effect, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ReportService } from '../../../core/services/report.service';
import { SiteContextService } from '../../../core/services/site-context.service';
import { IndianCurrencyPipe } from '../../../shared/pipes/indian-currency.pipe';
import { AppDatePipe } from '../../../shared/pipes/app-date.pipe';
import { ReportExportButtonsComponent } from '../../../shared/components/report-export-buttons/report-export-buttons.component';
import { ModuleSubnavComponent } from '../../../shared/components/module-subnav/module-subnav.component';
import { REPORT_NAV_ITEMS } from '../../../shared/nav/module-nav-items';

interface TillDateRow {
  wingName: string;
  flatNo: string;
  memberName: string;
  customerContact?: string;
  brokerName?: string;
  brokerContact?: string;
  bookingDate: string;
  sqft: number;
  rate: number;
  totalPrice: number;
  totalPaid: number;
  remainingAsPerCondition: number;
  totalRemaining: number;
  lastPaymentDate?: string;
  daysFromLastPayment?: number;
  daysFromBooking: number;
  percentagePaid: number;
  dastavejDate?: string;
  serviceTax?: number;
}

@Component({
  selector: 'app-till-date-report',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatCheckboxModule, MatFormFieldModule, MatInputModule, MatRadioModule, MatTableModule, MatIconModule, MatPaginatorModule, PageHeaderComponent, IndianCurrencyPipe, AppDatePipe, ReportExportButtonsComponent, ModuleSubnavComponent],
  template: `
    <div class="report-container">
      <div class="no-print">
        <app-page-header title="Till Date Report" subtitle="Member payment status as of date" />
        <app-module-subnav [items]="reportNav" />
        <section class="abr-panel">
          <h2 class="abr-panel__title"><mat-icon>filter_alt</mat-icon>Filters</h2>
          <form [formGroup]="form" class="filters">
            <mat-form-field appearance="outline"><mat-label>As of Date</mat-label><input matInput type="date" formControlName="asOfDate" /></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Days From Last Payment</mat-label><input matInput type="number" formControlName="daysFromLastPayment" /></mat-form-field>
            <mat-radio-group formControlName="movementType">
              <mat-radio-button value="all">All</mat-radio-button>
              <mat-radio-button value="no-movement">No Movement</mat-radio-button>
            </mat-radio-group>
            <mat-checkbox formControlName="extraReturnOnly">Extra Return Only</mat-checkbox>
            <button mat-flat-button color="primary" type="button" (click)="search()">Search</button>
            <app-report-export-buttons reportType="till-date" [filters]="exportFilters()" [disabled]="!siteId" />
          </form>
        </section>
      </div>
      <div class="abr-table-card">
        <table mat-table [dataSource]="rows" class="abr-table sticky-header">
          <ng-container matColumnDef="flatNo"><th mat-header-cell *matHeaderCellDef>Flat</th><td mat-cell *matCellDef="let r">{{ r.flatNo }}</td></ng-container>
          <ng-container matColumnDef="memberName"><th mat-header-cell *matHeaderCellDef>Member</th><td mat-cell *matCellDef="let r">{{ r.memberName }}</td></ng-container>
          <ng-container matColumnDef="brokerName"><th mat-header-cell *matHeaderCellDef>Broker</th><td mat-cell *matCellDef="let r">{{ r.brokerName ?? '-' }}</td></ng-container>
          <ng-container matColumnDef="bookingDate"><th mat-header-cell *matHeaderCellDef>Booking</th><td mat-cell *matCellDef="let r">{{ r.bookingDate | appDate }}</td></ng-container>
          <ng-container matColumnDef="totalPrice"><th mat-header-cell *matHeaderCellDef>Total</th><td mat-cell *matCellDef="let r">{{ r.totalPrice | indianCurrency }}</td></ng-container>
          <ng-container matColumnDef="totalPaid"><th mat-header-cell *matHeaderCellDef>Paid</th><td mat-cell *matCellDef="let r">{{ r.totalPaid | indianCurrency }}</td></ng-container>
          <ng-container matColumnDef="totalRemaining"><th mat-header-cell *matHeaderCellDef>Remain</th><td mat-cell *matCellDef="let r">{{ r.totalRemaining | indianCurrency }}</td></ng-container>
          <ng-container matColumnDef="lastPaymentDate"><th mat-header-cell *matHeaderCellDef>Last Pmt</th><td mat-cell *matCellDef="let r">{{ r.lastPaymentDate ? (r.lastPaymentDate | appDate) : '-' }}</td></ng-container>
          <ng-container matColumnDef="percentagePaid"><th mat-header-cell *matHeaderCellDef>%</th>
            <td mat-cell *matCellDef="let r" [class]="pctClass(r.percentagePaid)">{{ r.percentagePaid }}%</td>
          </ng-container>
          <ng-container matColumnDef="dastavejDate"><th mat-header-cell *matHeaderCellDef>Dastavej</th><td mat-cell *matCellDef="let r">{{ r.dastavejDate ? (r.dastavejDate | appDate) : '-' }}</td></ng-container>
          <tr mat-header-row *matHeaderRowDef="cols"></tr>
          <tr mat-row *matRowDef="let row; columns: cols"></tr>
          <tr class="empty-row" *matNoDataRow><td [attr.colspan]="cols.length"><mat-icon>info_outline</mat-icon>No records found.</td></tr>
        </table>
      </div>
      <mat-paginator class="no-print" [length]="total" [pageSize]="pageSize" [pageIndex]="page - 1" (page)="onPage($event)" />
    </div>
  `,
  styleUrls: ['../report-shared.scss']
})
export class TillDateReportComponent implements OnInit {
  private readonly reports = inject(ReportService);
  private readonly siteContext = inject(SiteContextService);
  private readonly fb = inject(FormBuilder);

  siteId: string | null = null;
  readonly reportNav = REPORT_NAV_ITEMS;
  rows: TillDateRow[] = [];
  cols = ['flatNo', 'memberName', 'brokerName', 'bookingDate', 'totalPrice', 'totalPaid', 'totalRemaining', 'lastPaymentDate', 'percentagePaid', 'dastavejDate'];
  total = 0;
  page = 1;
  pageSize = 50;

  form = this.fb.nonNullable.group({
    asOfDate: [new Date().toISOString().slice(0, 10)],
    daysFromLastPayment: [null as number | null],
    movementType: ['all'],
    extraReturnOnly: [false]
  });

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
    const params: Record<string, string | number | boolean> = {
      siteId: this.siteId,
      asOfDate: raw.asOfDate,
      movementType: raw.movementType,
      extraReturnOnly: raw.extraReturnOnly,
      page: this.page,
      pageSize: this.pageSize
    };
    if (raw.daysFromLastPayment != null) params['daysFromLastPayment'] = raw.daysFromLastPayment;
    this.reports.getTillDate(params).subscribe({
      next: (r) => {
        if (r.success) {
          this.rows = r.data.items as TillDateRow[];
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

  pctClass(pct: number): string {
    if (pct > 90) return 'pct-high';
    if (pct >= 50) return 'pct-mid';
    return 'pct-low';
  }

  exportFilters(): Record<string, string | number | boolean> {
    const raw = this.form.getRawValue();
    const params: Record<string, string | number | boolean> = {
      siteId: this.siteId!,
      asOfDate: raw.asOfDate,
      movementType: raw.movementType,
      extraReturnOnly: raw.extraReturnOnly
    };
    if (raw.daysFromLastPayment != null) params['daysFromLastPayment'] = raw.daysFromLastPayment;
    return params;
  }
}
