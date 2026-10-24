import { Component, computed, effect, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { BreakpointObserver } from '@angular/cdk/layout';
import { map } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { HttpErrorResponse } from '@angular/common/http';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { DailyEntry, DailyEntryService, ProfitSummary } from '../../../core/services/daily-entry.service';
import { MasterDataService } from '../../../core/services/master-data.service';
import { SiteContextService } from '../../../core/services/site-context.service';
import { ToastService } from '../../../core/services/toast.service';
import { FileDownloadOutcome, FileDownloadService } from '../../../core/services/file-download.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { BalanceDialogComponent } from './balance-dialog.component';
import { ImportResultDialogComponent } from './import-result-dialog.component';
import { IndianCurrencyPipe } from '../../../shared/pipes/indian-currency.pipe';
import { AppDatePipe } from '../../../shared/pipes/app-date.pipe';
import { IndianAmountDirective } from '../../../shared/directives/indian-amount.directive';
import { ModuleSubnavComponent } from '../../../shared/components/module-subnav/module-subnav.component';
import { SearchableSelectComponent, SelectOption } from '../../../shared/components/searchable-select/searchable-select.component';
import { ACCOUNTING_NAV_ITEMS } from '../../../shared/nav/module-nav-items';
import {
  buildDailyEntryTreeRows,
  DailyEntryTreeRow
} from './daily-entry-tree.util';

interface MainLedger { id: string; ledgerName: string; }
interface SubLedger { id: string; ledgerName: string; flatNo?: string; }
interface BankRow { id: string; bankName: string; accountNo: string; }

type DateRangePreset = '7d' | '30d' | '3m' | 'custom';

function toLocalIsoDate(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/** Inclusive calendar ranges ending today (for quick presets). */
export function rangeForPreset(preset: Exclude<DateRangePreset, 'custom'>): { dateFrom: string; dateTo: string } {
  const to = new Date();
  to.setHours(0, 0, 0, 0);
  const from = new Date(to);
  const daysBack = preset === '7d' ? 6 : preset === '30d' ? 29 : 89;
  from.setDate(from.getDate() - daysBack);
  return { dateFrom: toLocalIsoDate(from), dateTo: toLocalIsoDate(to) };
}

const DEFAULT_DATE_RANGE = rangeForPreset('7d');

@Component({
  selector: 'app-daily-entry',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatButtonModule, MatButtonToggleModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatTableModule, MatIconModule,
    PageHeaderComponent, IndianCurrencyPipe, AppDatePipe, IndianAmountDirective,
    ModuleSubnavComponent, SearchableSelectComponent
  ],
  template: `
    <div class="header-row">
      <app-page-header title="Daily Entry" subtitle="Aavak / Javak accounting"></app-page-header>
      <div class="header-actions">
        @if (profit) {
          <span class="profit" [class.negative]="profit.profit < 0">
            Profit: Rs. {{ profit.profit | indianCurrency }}
          </span>
        }
        <button mat-stroked-button (click)="downloadSample()">Sample Excel</button>
        <button mat-stroked-button (click)="fileInput.click()" [disabled]="importing">Import Excel</button>
        <button mat-stroked-button (click)="exportExcel()" [disabled]="!siteId">Excel</button>
        <button mat-stroked-button (click)="exportPdf()" [disabled]="!siteId">PDF</button>
        <button mat-stroked-button (click)="openBalance()">Balance</button>
        <input #fileInput type="file" accept=".xlsx" class="file-input" (change)="onFileSelected($event)" />
      </div>
    </div>

    <app-module-subnav [items]="accountingNav" />

    <section class="abr-panel new-entry-panel">
      <h2 class="abr-panel__title"><mat-icon>receipt_long</mat-icon>New Entry</h2>
      <form [formGroup]="form" class="entry-form abr-form-grid" (ngSubmit)="submit()">
      <mat-form-field appearance="outline" subscriptSizing="dynamic" class="entry-type-field">
        <mat-label>Type</mat-label>
        <mat-select formControlName="entryType">
          <mat-option value="aavak">Aavak</mat-option>
          <mat-option value="javak">Javak</mat-option>
        </mat-select>
      </mat-form-field>
      <mat-form-field appearance="outline" subscriptSizing="dynamic">
        <mat-label>Date</mat-label>
        <input matInput type="date" formControlName="entryDate" />
      </mat-form-field>
      <app-searchable-select
        label="Main Ledger"
        formControlName="mainLedgerId"
        [options]="mainLedgerOptions()"
        subscriptSizing="dynamic"
        (selectionChange)="onMainChange()" />
      <app-searchable-select
        label="Sub Ledger"
        formControlName="subLedgerId"
        [options]="subLedgerOptions()"
        subscriptSizing="dynamic" />
      <mat-form-field appearance="outline" subscriptSizing="dynamic">
        <mat-label>Amount</mat-label>
        <input matInput formControlName="amount" appIndianAmount />
      </mat-form-field>
      <mat-form-field appearance="outline" subscriptSizing="dynamic">
        <mat-label>Cash / Bank</mat-label>
        <mat-select formControlName="cashBank">
          <mat-option value="Cash">Cash</mat-option>
          @for (b of banks; track b.id) {
            <mat-option [value]="b.bankName + ' - ' + b.accountNo">{{ b.bankName }} - {{ b.accountNo }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
      <mat-form-field appearance="outline" subscriptSizing="dynamic" class="entry-desc-field">
        <mat-label>Description</mat-label>
        <input matInput formControlName="description" maxlength="200" />
      </mat-form-field>
      <button mat-flat-button color="primary" type="submit" class="entry-submit" [disabled]="form.invalid">Entry</button>
      </form>
    </section>

    @if (isTabletDown()) {
      <mat-button-toggle-group
        class="entry-table-tabs"
        [value]="activeTableView"
        (change)="activeTableView = $event.value">
        <mat-button-toggle value="aavak">Aavak (Receipts)</mat-button-toggle>
        <mat-button-toggle value="javak">Javak (Payments)</mat-button-toggle>
      </mat-button-toggle-group>
    }

    <div class="entries-toolbar">
      <mat-form-field appearance="outline" class="entries-search" subscriptSizing="dynamic">
        <mat-label>Search ledgers or amount</mat-label>
        <input
          matInput
          autocomplete="off"
          [value]="searchQuery()"
          (input)="onSearchInput($event)"
          placeholder="" />
        <mat-icon matPrefix>search</mat-icon>
      </mat-form-field>
      <mat-form-field appearance="outline" class="entries-date-from" subscriptSizing="dynamic">
        <mat-label>From date</mat-label>
        <input
          matInput
          type="date"
          [value]="dateFrom()"
          (change)="onDateFromChange($event)" />
      </mat-form-field>
      <mat-form-field appearance="outline" class="entries-date-to" subscriptSizing="dynamic">
        <mat-label>To date</mat-label>
        <input
          matInput
          type="date"
          [value]="dateTo()"
          (change)="onDateToChange($event)" />
      </mat-form-field>
      <mat-form-field appearance="outline" class="entries-date-preset" subscriptSizing="dynamic">
        <mat-label>Quick filter</mat-label>
        <mat-select [value]="dateRangePreset()" (selectionChange)="onDatePresetChange($event.value)">
          <mat-option value="7d">Last 7 days</mat-option>
          <mat-option value="30d">Last 30 days</mat-option>
          <mat-option value="3m">Last 3 months</mat-option>
          <mat-option value="custom">Custom</mat-option>
        </mat-select>
      </mat-form-field>
    </div>

    <div
      class="entries-view"
      [class.entries-view--desktop]="!isTabletDown()"
      [class.entries-view--aavak]="isTabletDown() && activeTableView === 'aavak'"
      [class.entries-view--javak]="isTabletDown() && activeTableView === 'javak'">
      <section class="aavak-section">
        <h3 class="aavak-title">Aavak (Receipts)</h3>
        <div class="abr-table-card">
        <table mat-table [dataSource]="aavakTreeRows()" class="abr-table sticky-header" [trackBy]="trackTreeRow">
          <ng-container matColumnDef="entryDate">
            <th mat-header-cell *matHeaderCellDef>Date</th>
            <td mat-cell *matCellDef="let row">
              @if (row.kind === 'entry') {
                {{ row.entry.entryDate | appDate }}
              }
            </td>
          </ng-container>
          <ng-container matColumnDef="mainLedgerName">
            <th mat-header-cell *matHeaderCellDef>Ledger</th>
            <td mat-cell *matCellDef="let row">
              @if (row.kind === 'main') {
                <button type="button" class="tree-toggle" (click)="toggleExpand(row.mainLedgerId); $event.stopPropagation()">
                  <mat-icon>{{ row.expanded ? 'expand_less' : 'expand_more' }}</mat-icon>
                  <span class="main-ledger-name">{{ row.mainLedgerName }}</span>
                </button>
              }
            </td>
          </ng-container>
          <ng-container matColumnDef="subLedgerName">
            <th mat-header-cell *matHeaderCellDef>Sub</th>
            <td mat-cell *matCellDef="let row" [class.tree-child]="row.kind === 'entry'">
              @if (row.kind === 'entry') {
                {{ row.entry.subLedgerName }}
              }
            </td>
          </ng-container>
          <ng-container matColumnDef="amount">
            <th mat-header-cell *matHeaderCellDef>Amount</th>
            <td mat-cell *matCellDef="let row">
              @if (row.kind === 'main') {
                <strong>{{ row.totalAmount | indianCurrency }}</strong>
              } @else {
                {{ row.entry.amount | indianCurrency }}
              }
            </td>
          </ng-container>
          <ng-container matColumnDef="description">
            <th mat-header-cell *matHeaderCellDef>Desc</th>
            <td mat-cell *matCellDef="let row">
              @if (row.kind === 'entry') {
                {{ row.entry.description }}
              }
            </td>
          </ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef></th>
            <td mat-cell *matCellDef="let row">
              @if (row.kind === 'entry') {
                <button mat-button color="warn" (click)="remove(row.entry.id)">Del</button>
              }
            </td>
          </ng-container>
          <tr mat-header-row *matHeaderRowDef="cols"></tr>
          <tr
            mat-row
            *matRowDef="let row; columns: cols"
            [class.tree-main-row]="row.kind === 'main'"
            [class.tree-entry-row]="row.kind === 'entry'"
            (click)="row.kind === 'main' && toggleExpand(row.mainLedgerId)"></tr>
          <tr class="empty-row" *matNoDataRow>
            <td [attr.colspan]="cols.length"><mat-icon>receipt_long</mat-icon>No aavak entries yet.</td>
          </tr>
        </table>
        </div>
      </section>
      <section class="javak-section">
        <h3 class="javak-title">Javak (Payments)</h3>
        <div class="abr-table-card">
        <table mat-table [dataSource]="javakTreeRows()" class="abr-table sticky-header" [trackBy]="trackTreeRow">
          <ng-container matColumnDef="javak_entryDate">
            <th mat-header-cell *matHeaderCellDef>Date</th>
            <td mat-cell *matCellDef="let row">
              @if (row.kind === 'entry') {
                {{ row.entry.entryDate | appDate }}
              }
            </td>
          </ng-container>
          <ng-container matColumnDef="javak_mainLedgerName">
            <th mat-header-cell *matHeaderCellDef>Ledger</th>
            <td mat-cell *matCellDef="let row">
              @if (row.kind === 'main') {
                <button type="button" class="tree-toggle" (click)="toggleExpand(row.mainLedgerId); $event.stopPropagation()">
                  <mat-icon>{{ row.expanded ? 'expand_less' : 'expand_more' }}</mat-icon>
                  <span class="main-ledger-name">{{ row.mainLedgerName }}</span>
                </button>
              }
            </td>
          </ng-container>
          <ng-container matColumnDef="javak_subLedgerName">
            <th mat-header-cell *matHeaderCellDef>Sub</th>
            <td mat-cell *matCellDef="let row" [class.tree-child]="row.kind === 'entry'">
              @if (row.kind === 'entry') {
                {{ row.entry.subLedgerName }}
              }
            </td>
          </ng-container>
          <ng-container matColumnDef="javak_amount">
            <th mat-header-cell *matHeaderCellDef>Amount</th>
            <td mat-cell *matCellDef="let row">
              @if (row.kind === 'main') {
                <strong>{{ row.totalAmount | indianCurrency }}</strong>
              } @else {
                {{ row.entry.amount | indianCurrency }}
              }
            </td>
          </ng-container>
          <ng-container matColumnDef="javak_description">
            <th mat-header-cell *matHeaderCellDef>Desc</th>
            <td mat-cell *matCellDef="let row">
              @if (row.kind === 'entry') {
                {{ row.entry.description }}
              }
            </td>
          </ng-container>
          <ng-container matColumnDef="javak_actions">
            <th mat-header-cell *matHeaderCellDef></th>
            <td mat-cell *matCellDef="let row">
              @if (row.kind === 'entry') {
                <button mat-button color="warn" (click)="remove(row.entry.id)">Del</button>
              }
            </td>
          </ng-container>
          <tr mat-header-row *matHeaderRowDef="javakCols"></tr>
          <tr
            mat-row
            *matRowDef="let row; columns: javakCols"
            [class.tree-main-row]="row.kind === 'main'"
            [class.tree-entry-row]="row.kind === 'entry'"
            (click)="row.kind === 'main' && toggleExpand(row.mainLedgerId)"></tr>
          <tr class="empty-row" *matNoDataRow>
            <td [attr.colspan]="javakCols.length"><mat-icon>receipt_long</mat-icon>No javak entries yet.</td>
          </tr>
        </table>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .header-row { display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 1rem; }
    .header-actions { display: flex; align-items: center; gap: 0.5rem; padding-top: 0.5rem; flex-wrap: wrap; }
    .file-input { display: none; }
    .profit { font-weight: 700; color: var(--abr-success); font-size: 1.1rem; }
    .profit.negative { color: var(--abr-danger); }

    .new-entry-panel {
      --mat-form-field-container-height: 40px;
      --mat-form-field-container-vertical-padding: 8px;
      --mat-form-field-container-text-size: 1rem;
      padding: 0.75rem 1rem;
      margin-bottom: 0.75rem;
      overflow: hidden;
    }
    .new-entry-panel .abr-panel__title {
      margin-bottom: 0.5rem;
      font-size: 0.95rem;
    }
    .new-entry-panel .abr-panel__title mat-icon {
      font-size: 1.1rem;
      width: 1.1rem;
      height: 1.1rem;
    }
    .entry-form.abr-form-grid {
      display: flex;
      flex-wrap: nowrap;
      gap: 0.4rem 0.5rem;
      align-items: center;
      width: 100%;
      min-width: 0;
    }
    ::ng-deep .entry-form .mat-mdc-form-field,
    .entry-form app-searchable-select {
      flex: 1 1 0;
      min-width: 0;
      width: auto;
      max-width: none;
    }
    ::ng-deep .entry-form app-searchable-select .searchable-select,
    ::ng-deep .entry-form .mat-mdc-form-field {
      width: 100%;
    }
    .entry-type-field {
      flex: 0 0 100px !important;
      max-width: 100px;
    }
    .entry-desc-field {
      flex: 1.2 1 0 !important;
      min-width: 0 !important;
    }
    .entry-submit {
      flex: 0 0 auto;
      height: 40px;
      min-height: 40px;
      padding: 0 0.9rem;
      line-height: 40px;
      align-self: center;
      font-size: 1rem;
      white-space: nowrap;
    }
    .entry-table-tabs {
      display: flex;
      width: 100%;
      margin-bottom: 0.75rem;
    }
    .entry-table-tabs mat-button-toggle { flex: 1; }
    .entries-toolbar {
      display: flex;
      flex-wrap: wrap;
      align-items: flex-start;
      gap: 0.65rem 0.75rem;
      margin-bottom: 0.75rem;
    }
    .entries-search,
    .entries-date-from,
    .entries-date-to,
    .entries-date-preset {
      --mat-form-field-container-height: 40px;
      --mat-form-field-container-vertical-padding: 8px;
      --mat-form-field-container-text-size: 0.95rem;
      --mat-form-field-outlined-container-shape: var(--abr-radius-md, 10px);
      margin-bottom: 0;
      display: block;
    }
    .entries-search {
      width: 100%;
      max-width: 260px;
    }
    .entries-date-from,
    .entries-date-to {
      width: 100%;
      max-width: 150px;
    }
    .entries-date-preset {
      width: 100%;
      max-width: 160px;
    }
    ::ng-deep .entries-toolbar .mdc-notched-outline__leading {
      border-top-left-radius: var(--abr-radius-md, 10px) !important;
      border-bottom-left-radius: var(--abr-radius-md, 10px) !important;
    }
    ::ng-deep .entries-toolbar .mdc-notched-outline__trailing {
      border-top-right-radius: var(--abr-radius-md, 10px) !important;
      border-bottom-right-radius: var(--abr-radius-md, 10px) !important;
    }
    ::ng-deep .entries-toolbar .mat-mdc-form-field-infix {
      min-height: 40px;
      padding-top: 8px;
      padding-bottom: 8px;
    }
    ::ng-deep .entries-search .mat-mdc-form-field-icon-prefix {
      padding: 0 0 0 0.15rem;
      color: var(--abr-text-muted, #6b7280);
    }
    .entries-view--desktop {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1.5rem;
    }
    .entries-view--aavak .javak-section,
    .entries-view--javak .aavak-section { display: none; }
    .entries-view--aavak .aavak-section h3,
    .entries-view--javak .javak-section h3 { display: none; }
    .aavak-title { color: var(--abr-success); }
    .javak-title { color: var(--abr-danger); }
    table { width: 100%; min-width: 520px; }
    .abr-table .mat-column-entryDate,
    .abr-table .mat-column-javak_entryDate { white-space: nowrap; }
    .tree-main-row {
      cursor: pointer;
      font-weight: 600;
      background: color-mix(in srgb, var(--abr-surface, #fff) 92%, var(--abr-border, #e0e0e0));
    }
    .tree-toggle {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      border: 0;
      background: transparent;
      padding: 0;
      cursor: pointer;
      font: inherit;
      color: inherit;
      text-align: left;
    }
    .tree-toggle mat-icon {
      font-size: 1.25rem;
      width: 1.25rem;
      height: 1.25rem;
      flex-shrink: 0;
    }
    .tree-child {
      padding-left: 1.5rem !important;
    }
    .tree-entry-row td:first-child {
      padding-left: 1.75rem;
    }
    @media (max-width: 959px) {
      .entry-form.abr-form-grid {
        flex-wrap: wrap;
      }
      .entry-type-field {
        flex: 1 1 120px !important;
        max-width: none;
      }
    }
    @media (max-width: 599px) {
      .header-actions { width: 100%; }
      .header-actions button { flex: 1 1 calc(50% - 0.25rem); min-width: 140px; }
      .entries-search { max-width: min(100%, 280px); }
    }
  `]
})
export class DailyEntryComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dailyEntryService = inject(DailyEntryService);
  private readonly masterData = inject(MasterDataService);
  private readonly siteContext = inject(SiteContextService);
  private readonly toast = inject(ToastService);
  private readonly fileDownloads = inject(FileDownloadService);
  private readonly dialog = inject(MatDialog);
  private readonly breakpointObserver = inject(BreakpointObserver);

  readonly isTabletDown = toSignal(
    this.breakpointObserver.observe('(max-width: 959px)').pipe(map((r) => r.matches)),
    { initialValue: false }
  );

  siteId: string | null = null;
  readonly mainLedgers = signal<MainLedger[]>([]);
  readonly subLedgers = signal<SubLedger[]>([]);
  banks: BankRow[] = [];
  readonly aavakEntries = signal<DailyEntry[]>([]);
  readonly javakEntries = signal<DailyEntry[]>([]);
  readonly searchQuery = signal('');
  readonly dateRangePreset = signal<DateRangePreset>('7d');
  readonly dateFrom = signal(DEFAULT_DATE_RANGE.dateFrom);
  readonly dateTo = signal(DEFAULT_DATE_RANGE.dateTo);
  readonly expandedMainIds = signal<ReadonlySet<string>>(new Set());
  profit: ProfitSummary | null = null;
  cols = ['entryDate', 'mainLedgerName', 'subLedgerName', 'amount', 'description', 'actions'];
  javakCols = [
    'javak_entryDate',
    'javak_mainLedgerName',
    'javak_subLedgerName',
    'javak_amount',
    'javak_description',
    'javak_actions'
  ];
  isMemberAccount = false;
  readonly accountingNav = ACCOUNTING_NAV_ITEMS;
  activeTableView: 'aavak' | 'javak' = 'aavak';
  importing = false;

  readonly aavakTreeRows = computed(() =>
    buildDailyEntryTreeRows(this.aavakEntries(), this.searchQuery(), this.expandedMainIds())
  );

  readonly javakTreeRows = computed(() =>
    buildDailyEntryTreeRows(this.javakEntries(), this.searchQuery(), this.expandedMainIds())
  );

  readonly mainLedgerOptions = computed<SelectOption<string>[]>(() =>
    this.mainLedgers().map((m) => ({ value: m.id, label: m.ledgerName }))
  );

  readonly subLedgerOptions = computed<SelectOption<string>[]>(() =>
    this.subLedgers().map((s) => ({ value: s.id, label: this.subLedgerLabel(s) }))
  );

  form = this.fb.nonNullable.group({
    entryType: ['aavak', Validators.required],
    entryDate: [new Date().toISOString().slice(0, 10), Validators.required],
    mainLedgerId: ['', Validators.required],
    subLedgerId: ['', Validators.required],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    cashBank: ['Cash', Validators.required],
    description: ['', Validators.maxLength(200)]
  });

  constructor() {
    effect(() => {
      this.siteId = this.siteContext.activeSiteId();
      if (this.siteId) this.loadAll();
    });

    this.form.get('entryType')?.valueChanges.pipe(takeUntilDestroyed()).subscribe((type) => {
      if (type === 'aavak' || type === 'javak') {
        this.activeTableView = type;
      }
    });
  }

  ngOnInit(): void {
    this.siteContext.loadSites().subscribe();
    this.siteId = this.siteContext.activeSiteId();
    if (this.siteId) this.loadAll();
  }

  trackTreeRow = (_index: number, row: DailyEntryTreeRow): string =>
    row.kind === 'main' ? `m-${row.mainLedgerId}` : `e-${row.entry.id}`;

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value ?? '';
    this.searchQuery.set(value);
  }

  onDatePresetChange(preset: DateRangePreset): void {
    this.dateRangePreset.set(preset);
    if (preset === 'custom') return;
    const range = rangeForPreset(preset);
    this.dateFrom.set(range.dateFrom);
    this.dateTo.set(range.dateTo);
    this.loadEntries();
  }

  onDateFromChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    if (!value) return;
    this.dateFrom.set(value);
    this.dateRangePreset.set('custom');
    this.loadEntries();
  }

  onDateToChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    if (!value) return;
    this.dateTo.set(value);
    this.dateRangePreset.set('custom');
    this.loadEntries();
  }

  toggleExpand(mainLedgerId: string): void {
    const next = new Set(this.expandedMainIds());
    if (next.has(mainLedgerId)) {
      next.delete(mainLedgerId);
    } else {
      next.add(mainLedgerId);
    }
    this.expandedMainIds.set(next);
  }

  loadAll(): void {
    if (!this.siteId) return;
    this.masterData.getMainLedgers(this.siteId).subscribe({
      next: (r) => {
        if (r.success) {
          this.mainLedgers.set(r.data as MainLedger[]);
          this.subLedgers.set([]);
        }
      }
    });
    this.masterData.getBanks(this.siteId).subscribe({
      next: (r) => { if (r.success) this.banks = (r.data as BankRow[]).filter(b => (b as { isActive?: boolean }).isActive !== false); }
    });
    this.loadEntries();
    this.loadProfit();
  }

  loadEntries(): void {
    if (!this.siteId) return;
    let dateFrom = this.dateFrom();
    let dateTo = this.dateTo();
    if (dateFrom && dateTo && dateFrom > dateTo) {
      [dateFrom, dateTo] = [dateTo, dateFrom];
      this.dateFrom.set(dateFrom);
      this.dateTo.set(dateTo);
    }
    const base = { siteId: this.siteId, dateFrom, dateTo, pageSize: 500 };
    this.dailyEntryService.getList({ ...base, entryType: 'aavak' }).subscribe({
      next: (r) => { if (r.success) this.aavakEntries.set(r.data.items); }
    });
    this.dailyEntryService.getList({ ...base, entryType: 'javak' }).subscribe({
      next: (r) => { if (r.success) this.javakEntries.set(r.data.items); }
    });
  }

  loadProfit(): void {
    if (!this.siteId) return;
    this.dailyEntryService.getProfit(this.siteId).subscribe({
      next: (r) => { if (r.success) this.profit = r.data; }
    });
  }

  onMainChange(): void {
    const mainId = this.form.value.mainLedgerId;
    if (!mainId) return;
    const main = this.mainLedgers().find(m => m.id === mainId);
    this.isMemberAccount = main?.ledgerName === 'Member A/c';
    this.masterData.getSubLedgers(mainId).subscribe({
      next: (r) => { if (r.success) this.subLedgers.set(r.data as SubLedger[]); }
    });
    this.form.patchValue({ subLedgerId: '' });
  }

  subLedgerLabel(s: SubLedger): string {
    if (this.isMemberAccount && s.flatNo) return `${s.flatNo} - ${s.ledgerName}`;
    return s.ledgerName;
  }

  submit(): void {
    if (!this.siteId) return;
    const raw = this.form.getRawValue();
    this.dailyEntryService.create({ ...raw, siteId: this.siteId }).subscribe({
      next: (r) => {
        if (r.success) {
          this.toast.success('Entry posted');
          this.form.patchValue({ amount: 0, description: '' });
          this.loadEntries();
          this.loadProfit();
        }
      }
    });
  }

  remove(id: string): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Entry', message: 'Soft-delete this entry?', confirmText: 'Delete' }
    });
    ref.afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.dailyEntryService.delete(id).subscribe({
        next: (r) => {
          if (r.success) {
            this.toast.success('Entry deleted');
            this.loadEntries();
            this.loadProfit();
          }
        }
      });
    });
  }

  openBalance(): void {
    if (!this.siteId) return;
    this.dialog.open(BalanceDialogComponent, { data: { siteId: this.siteId }, width: '520px' });
  }

  downloadSample(): void {
    this.dailyEntryService.downloadImportSample().subscribe({
      next: (outcome) => this.handleDownloadOutcome(outcome, 'daily-entry-import-sample.xlsx', 'Sample file ready'),
      error: () => this.toast.error('Failed to download sample file')
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file || !this.siteId) return;

    this.importing = true;
    this.dailyEntryService.importExcel(this.siteId, file).subscribe({
      next: (r) => {
        this.importing = false;
        if (!r.success || !r.data) {
          this.toast.error(r.message ?? 'Import failed');
          return;
        }
        this.loadAll();
        this.dialog.open(ImportResultDialogComponent, { data: r.data, width: 'min(480px, calc(100vw - 2rem))' });
        if (r.data.importedCount > 0) {
          this.toast.success(`Imported ${r.data.importedCount} entries`);
        }
      },
      error: (err) => {
        this.importing = false;
        this.toast.error(this.extractImportError(err));
      }
    });
  }

  exportExcel(): void {
    if (!this.siteId) return;
    this.dailyEntryService.exportLedgerExcel(this.siteId).subscribe({
      next: (outcome) => this.handleDownloadOutcome(
        outcome,
        `daily-entry-ledger-${new Date().toISOString().slice(0, 10)}.xlsx`,
        'Export downloaded'
      ),
      error: () => this.toast.error('Export failed')
    });
  }

  exportPdf(): void {
    if (!this.siteId) return;
    this.dailyEntryService.exportLedgerPdf(this.siteId).subscribe({
      next: (outcome) => this.handleDownloadOutcome(
        outcome,
        `daily-entry-ledger-${new Date().toISOString().slice(0, 10)}.pdf`,
        'Export downloaded'
      ),
      error: () => this.toast.error('Export failed')
    });
  }

  private handleDownloadOutcome(
    outcome: FileDownloadOutcome,
    fallbackFilename: string,
    browserSuccessMessage: string
  ): void {
    if (outcome.mode === 'pendrive') {
      this.toast.success(outcome.message ?? `Saved to pendrive: ${outcome.savedPath ?? fallbackFilename}`);
      return;
    }

    if (outcome.blob) {
      this.fileDownloads.saveToBrowser(outcome.blob, outcome.filename ?? fallbackFilename);
      this.toast.success(browserSuccessMessage);
    }
  }

  private extractImportError(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      const body = err.error as { message?: string } | null;
      return body?.message ?? 'Import failed';
    }
    return 'Import failed';
  }
}
