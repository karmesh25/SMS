import { Component, effect, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ReportService } from '../../../core/services/report.service';
import { SiteContextService } from '../../../core/services/site-context.service';
import { ToastService } from '../../../core/services/toast.service';
import { IndianCurrencyPipe } from '../../../shared/pipes/indian-currency.pipe';
import { AppDatePipe } from '../../../shared/pipes/app-date.pipe';
import { ReportExportButtonsComponent } from '../../../shared/components/report-export-buttons/report-export-buttons.component';
import { ModuleSubnavComponent } from '../../../shared/components/module-subnav/module-subnav.component';
import { REPORT_NAV_ITEMS } from '../../../shared/nav/module-nav-items';

interface InstallmentRow {
  flatNo: string;
  memberName: string;
  milestoneName: string;
  sortOrder: number;
  dueAmount: number;
  paidAmount: number;
  remaining: number;
  dueDate: string;
  paidDate?: string;
  status: string;
}

@Component({
  selector: 'app-installment-report',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatInputModule, MatTableModule, MatIconModule, PageHeaderComponent, IndianCurrencyPipe, AppDatePipe, ReportExportButtonsComponent, ModuleSubnavComponent],
  template: `
    <div class="report-container">
      <div class="no-print">
        <app-page-header title="Installment Report" subtitle="Payment milestones for a booking" />
        <app-module-subnav [items]="reportNav" />
        <form [formGroup]="form" class="filters">
          <mat-form-field appearance="outline"><mat-label>Flat No</mat-label><input matInput formControlName="flatNo" placeholder="e.g. A101" /></mat-form-field>
          <button mat-flat-button color="primary" type="button" (click)="search()" [disabled]="form.invalid">Search</button>
          <app-report-export-buttons reportType="installment" [filters]="exportFilters()" [disabled]="form.invalid || !siteId" />
        </form>
      </div>
      @if (rows.length) {
        <p>{{ rows[0].flatNo }} — {{ rows[0].memberName }}</p>
      }
      <div class="abr-scroll-x">
      <table mat-table [dataSource]="rows" class="mat-elevation-z1 abr-table sticky-header">
        <ng-container matColumnDef="milestoneName"><th mat-header-cell *matHeaderCellDef>Milestone</th><td mat-cell *matCellDef="let r">{{ r.milestoneName }}</td></ng-container>
        <ng-container matColumnDef="dueDate"><th mat-header-cell *matHeaderCellDef>Due Date</th><td mat-cell *matCellDef="let r">{{ r.dueDate | appDate }}</td></ng-container>
        <ng-container matColumnDef="dueAmount"><th mat-header-cell *matHeaderCellDef>Due</th><td mat-cell *matCellDef="let r">{{ r.dueAmount | indianCurrency }}</td></ng-container>
        <ng-container matColumnDef="paidAmount"><th mat-header-cell *matHeaderCellDef>Paid</th><td mat-cell *matCellDef="let r">{{ r.paidAmount | indianCurrency }}</td></ng-container>
        <ng-container matColumnDef="remaining"><th mat-header-cell *matHeaderCellDef>Remaining</th><td mat-cell *matCellDef="let r">{{ r.remaining | indianCurrency }}</td></ng-container>
        <ng-container matColumnDef="paidDate"><th mat-header-cell *matHeaderCellDef>Paid Date</th><td mat-cell *matCellDef="let r">{{ r.paidDate ? (r.paidDate | appDate) : '-' }}</td></ng-container>
        <ng-container matColumnDef="status"><th mat-header-cell *matHeaderCellDef>Status</th><td mat-cell *matCellDef="let r">{{ r.status }}</td></ng-container>
        <tr mat-header-row *matHeaderRowDef="cols"></tr>
        <tr mat-row *matRowDef="let row; columns: cols"></tr>
        <tr class="empty-row" *matNoDataRow><td [attr.colspan]="cols.length"><mat-icon>info_outline</mat-icon>No records found.</td></tr>
      </table>
      </div>
    </div>
  `,
  styleUrls: ['../report-shared.scss']
})
export class InstallmentReportComponent implements OnInit {
  private readonly reports = inject(ReportService);
  private readonly siteContext = inject(SiteContextService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  siteId: string | null = null;
  readonly reportNav = REPORT_NAV_ITEMS;
  rows: InstallmentRow[] = [];
  cols = ['milestoneName', 'dueDate', 'dueAmount', 'paidAmount', 'remaining', 'paidDate', 'status'];

  form = this.fb.nonNullable.group({ flatNo: ['', Validators.required] });

  constructor() {
    effect(() => { this.siteId = this.siteContext.activeSiteId(); });
  }

  ngOnInit(): void {
    this.siteContext.loadSites().subscribe();
    this.siteId = this.siteContext.activeSiteId();
  }

  search(): void {
    if (!this.siteId || this.form.invalid) return;
    this.reports.getInstallment({ siteId: this.siteId, flatNo: this.form.value.flatNo! }).subscribe({
      next: (r) => {
        if (r.success) this.rows = r.data as InstallmentRow[];
      },
      error: (e) => this.toast.error(e.error?.message ?? 'Booking not found')
    });
  }

  exportFilters(): Record<string, string | number | boolean> {
    return { siteId: this.siteId!, flatNo: this.form.value.flatNo! };
  }
}
