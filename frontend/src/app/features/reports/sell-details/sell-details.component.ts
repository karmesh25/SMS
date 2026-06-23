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

interface WingRow { id: string; wingName: string; }
interface SellRow {
  flatNo: string;
  wingName: string;
  memberName: string;
  bookingDate: string;
  totalPrice: number;
  paid: number;
  remaining: number;
  status: string;
}

@Component({
  selector: 'app-sell-details',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatTableModule, MatIconModule, PageHeaderComponent, IndianCurrencyPipe, AppDatePipe, ReportExportButtonsComponent, ModuleSubnavComponent],
  template: `
    <div class="report-container">
      <div class="no-print">
        <app-page-header title="Sell Details" subtitle="Booked flats with payment summary" />
        <app-module-subnav [items]="reportNav" />
        <form [formGroup]="form" class="filters">
          <mat-form-field appearance="outline">
            <mat-label>Wing</mat-label>
            <mat-select formControlName="wingId">
              <mat-option value="">All</mat-option>
              @for (w of wings; track w.id) { <mat-option [value]="w.id">{{ w.wingName }}</mat-option> }
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Status</mat-label>
            <mat-select formControlName="status">
              <mat-option value="active">Active</mat-option>
              <mat-option value="cancelled">Cancelled</mat-option>
              <mat-option value="">All</mat-option>
            </mat-select>
          </mat-form-field>
          <button mat-flat-button color="primary" type="button" (click)="search()">Search</button>
          <app-report-export-buttons reportType="sell-details" [filters]="exportFilters()" [disabled]="!siteId" />
        </form>
      </div>
      <div class="abr-scroll-x">
      <table mat-table [dataSource]="rows" class="mat-elevation-z1 abr-table sticky-header">
        <ng-container matColumnDef="flatNo"><th mat-header-cell *matHeaderCellDef>Flat</th><td mat-cell *matCellDef="let r">{{ r.flatNo }}</td></ng-container>
        <ng-container matColumnDef="wingName"><th mat-header-cell *matHeaderCellDef>Wing</th><td mat-cell *matCellDef="let r">{{ r.wingName }}</td></ng-container>
        <ng-container matColumnDef="memberName"><th mat-header-cell *matHeaderCellDef>Customer</th><td mat-cell *matCellDef="let r">{{ r.memberName }}</td></ng-container>
        <ng-container matColumnDef="bookingDate"><th mat-header-cell *matHeaderCellDef>Date</th><td mat-cell *matCellDef="let r">{{ r.bookingDate | appDate }}</td></ng-container>
        <ng-container matColumnDef="totalPrice"><th mat-header-cell *matHeaderCellDef>Total</th><td mat-cell *matCellDef="let r">{{ r.totalPrice | indianCurrency }}</td></ng-container>
        <ng-container matColumnDef="paid"><th mat-header-cell *matHeaderCellDef>Paid</th><td mat-cell *matCellDef="let r">{{ r.paid | indianCurrency }}</td></ng-container>
        <ng-container matColumnDef="remaining"><th mat-header-cell *matHeaderCellDef>Remaining</th><td mat-cell *matCellDef="let r">{{ r.remaining | indianCurrency }}</td></ng-container>
        <ng-container matColumnDef="status"><th mat-header-cell *matHeaderCellDef>Status</th><td mat-cell *matCellDef="let r">{{ r.status }}</td></ng-container>
        <tr mat-header-row *matHeaderRowDef="cols"></tr>
        <tr mat-row *matRowDef="let row; columns: cols"></tr>
        <tr class="empty-row" *matNoDataRow><td [attr.colspan]="cols.length"><mat-icon>info_outline</mat-icon>No records found.</td></tr>
      </table>
      </div>
      @if (totals) {
        <p><strong>Totals:</strong> Price Rs. {{ totals.totalPrice | indianCurrency }} | Paid Rs. {{ totals.totalPaid | indianCurrency }} | Remaining Rs. {{ totals.totalRemaining | indianCurrency }}</p>
      }
    </div>
  `,
  styleUrls: ['../report-shared.scss']
})
export class SellDetailsComponent implements OnInit {
  private readonly reports = inject(ReportService);
  private readonly masterData = inject(MasterDataService);
  private readonly siteContext = inject(SiteContextService);
  private readonly fb = inject(FormBuilder);

  siteId: string | null = null;
  readonly reportNav = REPORT_NAV_ITEMS;
  wings: WingRow[] = [];
  rows: SellRow[] = [];
  cols = ['flatNo', 'wingName', 'memberName', 'bookingDate', 'totalPrice', 'paid', 'remaining', 'status'];
  totals: { totalPrice: number; totalPaid: number; totalRemaining: number } | null = null;

  form = this.fb.nonNullable.group({ wingId: [''], status: ['active'] });

  constructor() {
    effect(() => {
      this.siteId = this.siteContext.activeSiteId();
      if (this.siteId) { this.loadWings(); this.search(); }
    });
  }

  ngOnInit(): void {
    this.siteContext.loadSites().subscribe();
    this.siteId = this.siteContext.activeSiteId();
    if (this.siteId) { this.loadWings(); this.search(); }
  }

  loadWings(): void {
    if (!this.siteId) return;
    this.masterData.getWings(this.siteId).subscribe({
      next: (r) => { if (r.success) this.wings = r.data as WingRow[]; }
    });
  }

  search(): void {
    if (!this.siteId) return;
    const raw = this.form.getRawValue();
    const params: Record<string, string | number | boolean> = { siteId: this.siteId };
    if (raw.wingId) params['wingId'] = raw.wingId;
    if (raw.status) params['status'] = raw.status;
    this.reports.getSellDetails(params).subscribe({
      next: (r) => {
        if (r.success) {
          const d = r.data as { items: SellRow[]; totalPrice: number; totalPaid: number; totalRemaining: number };
          this.rows = d.items;
          this.totals = { totalPrice: d.totalPrice, totalPaid: d.totalPaid, totalRemaining: d.totalRemaining };
        }
      }
    });
  }

  exportFilters(): Record<string, string | number | boolean> {
    const raw = this.form.getRawValue();
    const params: Record<string, string | number | boolean> = { siteId: this.siteId! };
    if (raw.wingId) params['wingId'] = raw.wingId;
    if (raw.status) params['status'] = raw.status;
    return params;
  }
}
