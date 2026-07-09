import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { catchError, EMPTY, filter, finalize, switchMap, tap } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { CardSkeletonComponent } from '../../shared/components/card-skeleton/card-skeleton.component';
import { TableSkeletonComponent } from '../../shared/components/table-skeleton/table-skeleton.component';
import { DashboardService, DashboardSummary } from '../../core/services/dashboard.service';
import { SiteContextService } from '../../core/services/site-context.service';
import { ToastService } from '../../core/services/toast.service';
import { IndianCurrencyPipe } from '../../shared/pipes/indian-currency.pipe';
import { AppDatePipe } from '../../shared/pipes/app-date.pipe';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatTableModule,
    PageHeaderComponent,
    CardSkeletonComponent,
    TableSkeletonComponent,
    IndianCurrencyPipe,
    AppDatePipe
  ],
  template: `
    <div class="dashboard-header">
      <app-page-header title="Dashboard" subtitle="Overview of site activity"></app-page-header>
      <button mat-stroked-button (click)="refresh()" [disabled]="loading()">
        <mat-icon>refresh</mat-icon>
        Refresh
      </button>
    </div>

    @if (loading()) {
      <div class="kpi-grid">
        @for (i of [1, 2, 3, 4]; track i) {
          <app-card-skeleton [showBar]="i === 1" />
        }
      </div>
      <div class="tables-grid">
        <app-table-skeleton [columnCount]="5" [rowCount]="4" />
        <app-table-skeleton [columnCount]="4" [rowCount]="5" />
      </div>
    } @else {
      @if (summary(); as s) {
      <div class="kpi-grid">
        <mat-card class="kpi-card">
          <mat-card-header>
            <mat-icon mat-card-avatar class="kpi-icon flats">apartment</mat-icon>
            <mat-card-title>Flats</mat-card-title>
            <mat-card-subtitle>Booking occupancy</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <div class="kpi-value">{{ s.bookedFlats }} / {{ s.totalFlats }}</div>
            <div class="kpi-meta">{{ s.availableFlats }} available · {{ s.cancelledFlats }} cancelled</div>
            <mat-progress-bar mode="determinate" [value]="s.bookingPercentage" />
            <div class="kpi-pct">{{ s.bookingPercentage }}% booked</div>
          </mat-card-content>
        </mat-card>

        <mat-card class="kpi-card">
          <mat-card-header>
            <mat-icon mat-card-avatar class="kpi-icon profit" [class.negative]="s.netProfit < 0">trending_up</mat-icon>
            <mat-card-title>Net Profit</mat-card-title>
            <mat-card-subtitle>Aavak minus Javak</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <div class="kpi-value" [class.negative]="s.netProfit < 0">
              Rs. {{ s.netProfit | indianCurrency }}
            </div>
          </mat-card-content>
        </mat-card>

        <mat-card class="kpi-card">
          <mat-card-header>
            <mat-icon mat-card-avatar class="kpi-icon outstanding">payments</mat-icon>
            <mat-card-title>Outstanding</mat-card-title>
            <mat-card-subtitle>Pending member amounts</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <div class="kpi-value">Rs. {{ s.totalOutstanding | indianCurrency }}</div>
          </mat-card-content>
        </mat-card>

        <mat-card class="kpi-card">
          <mat-card-header>
            <mat-icon mat-card-avatar class="kpi-icon cashflow">account_balance</mat-icon>
            <mat-card-title>Aavak vs Javak</mat-card-title>
            <mat-card-subtitle>Total receipts & payments</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <div class="cashflow-row">
              <span class="aavak">Aavak: Rs. {{ s.totalAavak | indianCurrency }}</span>
              <span class="javak">Javak: Rs. {{ s.totalJavak | indianCurrency }}</span>
            </div>
          </mat-card-content>
        </mat-card>
      </div>

      <div class="tables-grid">
        <mat-card>
          <mat-card-header>
            <mat-card-title>Wing Summary</mat-card-title>
          </mat-card-header>
          <mat-card-content class="table-wrap">
            <table mat-table [dataSource]="s.wingSummary" class="abr-table sticky-header">
              <ng-container matColumnDef="wingName">
                <th mat-header-cell *matHeaderCellDef>Wing</th>
                <td mat-cell *matCellDef="let row">{{ row.wingName }}</td>
              </ng-container>
              <ng-container matColumnDef="total">
                <th mat-header-cell *matHeaderCellDef>Total</th>
                <td mat-cell *matCellDef="let row">{{ row.total }}</td>
              </ng-container>
              <ng-container matColumnDef="booked">
                <th mat-header-cell *matHeaderCellDef>Booked</th>
                <td mat-cell *matCellDef="let row">{{ row.booked }}</td>
              </ng-container>
              <ng-container matColumnDef="available">
                <th mat-header-cell *matHeaderCellDef>Available</th>
                <td mat-cell *matCellDef="let row">{{ row.available }}</td>
              </ng-container>
              <ng-container matColumnDef="pct">
                <th mat-header-cell *matHeaderCellDef>%</th>
                <td mat-cell *matCellDef="let row">{{ row.bookingPercentage }}%</td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="wingCols"></tr>
              <tr mat-row *matRowDef="let row; columns: wingCols"></tr>
              <tr class="empty-row" *matNoDataRow>
                <td [attr.colspan]="wingCols.length">
                  <mat-icon>info_outline</mat-icon>
                  No wings configured for this site.
                </td>
              </tr>
            </table>
          </mat-card-content>
        </mat-card>

        <mat-card>
          <mat-card-header>
            <mat-card-title>Recent Entries</mat-card-title>
          </mat-card-header>
          <mat-card-content class="table-wrap">
            <table mat-table [dataSource]="s.recentEntries" class="abr-table sticky-header">
              <ng-container matColumnDef="entryDate">
                <th mat-header-cell *matHeaderCellDef>Date</th>
                <td mat-cell *matCellDef="let row">{{ row.entryDate | appDate }}</td>
              </ng-container>
              <ng-container matColumnDef="entryType">
                <th mat-header-cell *matHeaderCellDef>Type</th>
                <td mat-cell *matCellDef="let row">
                  <span class="type-badge" [class.aavak]="row.entryType === 'aavak'">{{ row.entryType }}</span>
                </td>
              </ng-container>
              <ng-container matColumnDef="ledger">
                <th mat-header-cell *matHeaderCellDef>Ledger</th>
                <td mat-cell *matCellDef="let row">
                  {{ row.mainLedgerName }}
                  @if (row.subLedgerName) {
                    <span class="sub-ledger">/ {{ row.subLedgerName }}</span>
                  }
                </td>
              </ng-container>
              <ng-container matColumnDef="amount">
                <th mat-header-cell *matHeaderCellDef>Amount</th>
                <td mat-cell *matCellDef="let row">Rs. {{ row.amount | indianCurrency }}</td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="entryCols"></tr>
              <tr mat-row *matRowDef="let row; columns: entryCols"></tr>
              <tr class="empty-row" *matNoDataRow>
                <td [attr.colspan]="entryCols.length">
                  <mat-icon>receipt_long</mat-icon>
                  No daily entries recorded yet.
                </td>
              </tr>
            </table>
          </mat-card-content>
        </mat-card>
      </div>
      }
    }
  `,
  styles: [`
    .dashboard-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 1rem;
      margin-bottom: 1.25rem;
    }
    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(230px, 1fr));
      gap: 1.15rem;
      margin-bottom: 1.5rem;
    }
    .kpi-card {
      position: relative;
      overflow: hidden;
      transition: transform var(--abr-dur) var(--abr-ease), box-shadow var(--abr-dur) var(--abr-ease);
    }
    .kpi-card::before {
      content: '';
      position: absolute;
      inset: 0 auto 0 0;
      width: 4px;
      background: var(--abr-primary);
    }
    .kpi-card:hover { transform: translateY(-2px); box-shadow: var(--abr-shadow-md) !important; }
    .kpi-card mat-card-content { padding-top: 0.5rem; }
    .kpi-icon {
      border-radius: var(--abr-radius-md);
      color: #fff;
      padding: 8px;
      box-shadow: var(--abr-shadow-xs);
    }
    .kpi-icon.flats { background: var(--abr-primary); }
    .kpi-icon.profit { background: var(--abr-success); }
    .kpi-icon.profit.negative { background: var(--abr-danger); }
    .kpi-icon.outstanding { background: var(--abr-warning); }
    .kpi-icon.cashflow { background: var(--abr-accent); }
    .kpi-value { font-size: 1.65rem; font-weight: 700; color: var(--abr-text); letter-spacing: -0.01em; }
    .kpi-value.negative { color: var(--abr-danger); }
    .kpi-meta { font-size: 0.85rem; color: var(--abr-text-muted); margin: 0.25rem 0 0.75rem; }
    .kpi-pct { font-size: 0.8rem; color: var(--abr-text-muted); margin-top: 0.35rem; }
    .cashflow-row { display: flex; flex-direction: column; gap: 0.35rem; font-weight: 600; }
    .aavak { color: var(--abr-success); }
    .javak { color: var(--abr-danger); }
    .tables-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
      gap: 1.15rem;
    }
    .table-wrap { overflow: auto; max-height: 360px; -webkit-overflow-scrolling: touch; }
    .table-wrap table { min-width: 480px; }
    @media (max-width: 599px) {
      .dashboard-header { flex-direction: column; align-items: stretch; }
      .kpi-grid { grid-template-columns: 1fr; }
      .tables-grid { grid-template-columns: 1fr; }
    }
    @media (min-width: 600px) and (max-width: 959px) {
      .kpi-grid { grid-template-columns: repeat(2, 1fr); }
    }
    .type-badge {
      display: inline-block;
      text-transform: capitalize;
      font-size: 0.72rem;
      font-weight: 600;
      padding: 2px 10px;
      border-radius: var(--abr-radius-pill);
      background: var(--abr-danger-soft);
      color: var(--abr-danger);
    }
    .type-badge.aavak { background: var(--abr-success-soft); color: var(--abr-success); }
    .sub-ledger { color: var(--abr-text-muted); font-size: 0.85rem; }
  `]
})
export class DashboardComponent {
  private readonly dashboardService = inject(DashboardService);
  private readonly siteContext = inject(SiteContextService);
  private readonly toast = inject(ToastService);

  readonly loading = signal(false);
  readonly summary = signal<DashboardSummary | null>(null);
  readonly wingCols = ['wingName', 'total', 'booked', 'available', 'pct'];
  readonly entryCols = ['entryDate', 'entryType', 'ledger', 'amount'];

  constructor() {
    toObservable(this.siteContext.activeSiteId)
      .pipe(
        filter((siteId): siteId is string => !!siteId),
        switchMap((siteId) => this.fetchSummary(siteId)),
        takeUntilDestroyed()
      )
      .subscribe();
  }

  refresh(): void {
    const siteId = this.siteContext.activeSiteId();
    if (siteId) {
      this.fetchSummary(siteId).subscribe();
    }
  }

  private fetchSummary(siteId: string) {
    this.loading.set(true);
    return this.dashboardService.getSummary(siteId).pipe(
      tap((res) => {
        if (res.success) {
          this.summary.set(res.data);
        } else {
          this.toast.error('Failed to load dashboard summary.');
        }
      }),
      catchError(() => {
        this.toast.error('Failed to load dashboard summary.');
        return EMPTY;
      }),
      finalize(() => this.loading.set(false))
    );
  }
}
