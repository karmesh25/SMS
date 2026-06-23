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
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { DailyEntry, DailyEntryService, ProfitSummary } from '../../../core/services/daily-entry.service';
import { MasterDataService } from '../../../core/services/master-data.service';
import { SiteContextService } from '../../../core/services/site-context.service';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { BalanceDialogComponent } from './balance-dialog.component';
import { IndianCurrencyPipe } from '../../../shared/pipes/indian-currency.pipe';
import { AppDatePipe } from '../../../shared/pipes/app-date.pipe';
import { IndianAmountDirective } from '../../../shared/directives/indian-amount.directive';
import { ModuleSubnavComponent } from '../../../shared/components/module-subnav/module-subnav.component';
import { SearchableSelectComponent, SelectOption } from '../../../shared/components/searchable-select/searchable-select.component';
import { ACCOUNTING_NAV_ITEMS } from '../../../shared/nav/module-nav-items';

interface MainLedger { id: string; ledgerName: string; }
interface SubLedger { id: string; ledgerName: string; flatNo?: string; }
interface BankRow { id: string; bankName: string; accountNo: string; }

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
        <button mat-stroked-button (click)="openBalance()">Balance</button>
      </div>
    </div>

    <app-module-subnav [items]="accountingNav" />

    <form [formGroup]="form" class="entry-form abr-form-grid" (ngSubmit)="submit()">
      <mat-button-toggle-group formControlName="entryType" class="type-toggle">
        <mat-button-toggle value="aavak">Aavak</mat-button-toggle>
        <mat-button-toggle value="javak">Javak</mat-button-toggle>
      </mat-button-toggle-group>
      <mat-form-field appearance="outline"><mat-label>Date</mat-label><input matInput type="date" formControlName="entryDate" /></mat-form-field>
      <app-searchable-select
        label="Main Ledger"
        formControlName="mainLedgerId"
        [options]="mainLedgerOptions()"
        (selectionChange)="onMainChange()" />
      <app-searchable-select
        label="Sub Ledger"
        formControlName="subLedgerId"
        [options]="subLedgerOptions()" />
      <mat-form-field appearance="outline"><mat-label>Amount</mat-label><input matInput formControlName="amount" appIndianAmount /></mat-form-field>
      <mat-form-field appearance="outline">
        <mat-label>Cash / Bank</mat-label>
        <mat-select formControlName="cashBank">
          <mat-option value="Cash">Cash</mat-option>
          @for (b of banks; track b.id) {
            <mat-option [value]="b.bankName + ' - ' + b.accountNo">{{ b.bankName }} - {{ b.accountNo }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
      <mat-form-field appearance="outline"><mat-label>Description</mat-label><input matInput formControlName="description" /></mat-form-field>
      <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">Entry</button>
    </form>

    @if (isTabletDown()) {
      <mat-button-toggle-group
        class="entry-table-tabs"
        [value]="activeTableView"
        (change)="activeTableView = $event.value">
        <mat-button-toggle value="aavak">Aavak (Receipts)</mat-button-toggle>
        <mat-button-toggle value="javak">Javak (Payments)</mat-button-toggle>
      </mat-button-toggle-group>
    }

    <div
      class="entries-view"
      [class.entries-view--desktop]="!isTabletDown()"
      [class.entries-view--aavak]="isTabletDown() && activeTableView === 'aavak'"
      [class.entries-view--javak]="isTabletDown() && activeTableView === 'javak'">
      <section class="aavak-section">
        <h3 class="aavak-title">Aavak (Receipts)</h3>
        <div class="abr-scroll-x">
        <table mat-table [dataSource]="aavakEntries" class="mat-elevation-z1 abr-table sticky-header">
          <ng-container matColumnDef="entryDate"><th mat-header-cell *matHeaderCellDef>Date</th><td mat-cell *matCellDef="let row">{{ row.entryDate | appDate }}</td></ng-container>
          <ng-container matColumnDef="mainLedgerName"><th mat-header-cell *matHeaderCellDef>Ledger</th><td mat-cell *matCellDef="let row">{{ row.mainLedgerName }}</td></ng-container>
          <ng-container matColumnDef="subLedgerName"><th mat-header-cell *matHeaderCellDef>Sub</th><td mat-cell *matCellDef="let row">{{ row.subLedgerName }}</td></ng-container>
          <ng-container matColumnDef="amount"><th mat-header-cell *matHeaderCellDef>Amount</th><td mat-cell *matCellDef="let row">{{ row.amount | indianCurrency }}</td></ng-container>
          <ng-container matColumnDef="description"><th mat-header-cell *matHeaderCellDef>Desc</th><td mat-cell *matCellDef="let row">{{ row.description }}</td></ng-container>
          <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef></th><td mat-cell *matCellDef="let row"><button mat-button color="warn" (click)="remove(row.id)">Del</button></td></ng-container>
          <tr mat-header-row *matHeaderRowDef="cols"></tr>
          <tr mat-row *matRowDef="let row; columns: cols"></tr>
          <tr class="empty-row" *matNoDataRow>
            <td [attr.colspan]="cols.length"><mat-icon>receipt_long</mat-icon>No aavak entries yet.</td>
          </tr>
        </table>
        </div>
      </section>
      <section class="javak-section">
        <h3 class="javak-title">Javak (Payments)</h3>
        <div class="abr-scroll-x">
        <table mat-table [dataSource]="javakEntries" class="mat-elevation-z1 abr-table sticky-header">
          <ng-container matColumnDef="entryDate"><th mat-header-cell *matHeaderCellDef>Date</th><td mat-cell *matCellDef="let row">{{ row.entryDate | appDate }}</td></ng-container>
          <ng-container matColumnDef="mainLedgerName"><th mat-header-cell *matHeaderCellDef>Ledger</th><td mat-cell *matCellDef="let row">{{ row.mainLedgerName }}</td></ng-container>
          <ng-container matColumnDef="subLedgerName"><th mat-header-cell *matHeaderCellDef>Sub</th><td mat-cell *matCellDef="let row">{{ row.subLedgerName }}</td></ng-container>
          <ng-container matColumnDef="amount"><th mat-header-cell *matHeaderCellDef>Amount</th><td mat-cell *matCellDef="let row">{{ row.amount | indianCurrency }}</td></ng-container>
          <ng-container matColumnDef="description"><th mat-header-cell *matHeaderCellDef>Desc</th><td mat-cell *matCellDef="let row">{{ row.description }}</td></ng-container>
          <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef></th><td mat-cell *matCellDef="let row"><button mat-button color="warn" (click)="remove(row.id)">Del</button></td></ng-container>
          <tr mat-header-row *matHeaderRowDef="cols"></tr>
          <tr mat-row *matRowDef="let row; columns: cols"></tr>
          <tr class="empty-row" *matNoDataRow>
            <td [attr.colspan]="cols.length"><mat-icon>receipt_long</mat-icon>No javak entries yet.</td>
          </tr>
        </table>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .header-row { display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 1rem; }
    .header-actions { display: flex; align-items: center; gap: 1rem; padding-top: 0.5rem; flex-wrap: wrap; }
    .profit { font-weight: 700; color: #27ae60; font-size: 1.1rem; }
    .profit.negative { color: #e74c3c; }
    .entry-form { margin-bottom: 1.5rem; }
    .type-toggle { grid-column: 1 / -1; width: 100%; }
    .entry-table-tabs {
      display: flex;
      width: 100%;
      margin-bottom: 1rem;
    }
    .entry-table-tabs mat-button-toggle { flex: 1; }
    .entries-view--desktop {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1.5rem;
    }
    .entries-view--aavak .javak-section,
    .entries-view--javak .aavak-section { display: none; }
    .entries-view--aavak .aavak-section h3,
    .entries-view--javak .javak-section h3 { display: none; }
    .aavak-title { color: #27ae60; }
    .javak-title { color: #2980b9; }
    table { width: 100%; min-width: 520px; }
    @media (max-width: 599px) {
      .type-toggle {
        display: flex;
        flex-direction: column;
      }
      .type-toggle mat-button-toggle {
        flex: 1 1 auto;
      }
    }
  `]
})
export class DailyEntryComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dailyEntryService = inject(DailyEntryService);
  private readonly masterData = inject(MasterDataService);
  private readonly siteContext = inject(SiteContextService);
  private readonly toast = inject(ToastService);
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
  aavakEntries: DailyEntry[] = [];
  javakEntries: DailyEntry[] = [];
  profit: ProfitSummary | null = null;
  cols = ['entryDate', 'mainLedgerName', 'subLedgerName', 'amount', 'description', 'actions'];
  isMemberAccount = false;
  readonly accountingNav = ACCOUNTING_NAV_ITEMS;
  activeTableView: 'aavak' | 'javak' = 'aavak';

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
    description: ['']
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
    this.dailyEntryService.getList({ siteId: this.siteId, entryType: 'aavak', pageSize: 50 }).subscribe({
      next: (r) => { if (r.success) this.aavakEntries = r.data.items; }
    });
    this.dailyEntryService.getList({ siteId: this.siteId, entryType: 'javak', pageSize: 50 }).subscribe({
      next: (r) => { if (r.success) this.javakEntries = r.data.items; }
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
}
