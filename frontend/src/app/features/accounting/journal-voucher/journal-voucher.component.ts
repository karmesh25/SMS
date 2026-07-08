import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatDialog } from '@angular/material/dialog';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ModuleSubnavComponent } from '../../../shared/components/module-subnav/module-subnav.component';
import { SearchableSelectComponent, SelectOption } from '../../../shared/components/searchable-select/searchable-select.component';
import { ACCOUNTING_NAV_ITEMS } from '../../../shared/nav/module-nav-items';
import { MasterDataService } from '../../../core/services/master-data.service';
import { SiteContextService } from '../../../core/services/site-context.service';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { AppDatePipe } from '../../../shared/pipes/app-date.pipe';
import { IndianCurrencyPipe } from '../../../shared/pipes/indian-currency.pipe';
import { JournalVoucher, JournalVoucherService } from '../../../core/services/journal-voucher.service';
import { FileDownloadOutcome, FileDownloadService } from '../../../core/services/file-download.service';

interface MainLedger { id: string; ledgerName: string; }
interface SubLedger { id: string; ledgerName: string; mainLedgerId: string; mainLedgerName?: string; flatNo?: string; }

@Component({
  selector: 'app-journal-voucher',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatTableModule,
    PageHeaderComponent,
    ModuleSubnavComponent,
    SearchableSelectComponent,
    AppDatePipe,
    IndianCurrencyPipe
  ],
  template: `
    <div class="header-row">
      <app-page-header title="Journal Voucher" subtitle="Balanced debit/credit voucher entries"></app-page-header>
      <div class="header-actions">
        <button mat-stroked-button (click)="exportExcel()" [disabled]="!siteId">Excel</button>
        <button mat-stroked-button (click)="exportPdf()" [disabled]="!siteId">PDF</button>
      </div>
    </div>

    <app-module-subnav [items]="accountingNav" />

    <form [formGroup]="form" class="voucher-form" (ngSubmit)="save()">
      <div class="form-head abr-form-grid">
        <mat-form-field appearance="outline">
          <mat-label>Voucher Date</mat-label>
          <input matInput type="date" formControlName="voucherDate" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Voucher No</mat-label>
          <input matInput [value]="selectedVoucherNo()" readonly />
        </mat-form-field>
        <mat-form-field appearance="outline" class="narration">
          <mat-label>Narration</mat-label>
          <input matInput formControlName="narration" maxlength="500" />
        </mat-form-field>
      </div>

      <div class="line-table abr-scroll-x">
        <table mat-table [dataSource]="lineControls()">
          <ng-container matColumnDef="entryType">
            <th mat-header-cell *matHeaderCellDef>Type</th>
            <td mat-cell *matCellDef="let group; let i = index">
              <mat-form-field appearance="outline">
                <mat-select [formControl]="group.controls.entryType">
                  <mat-option value="dr">DR</mat-option>
                  <mat-option value="cr">CR</mat-option>
                </mat-select>
              </mat-form-field>
            </td>
          </ng-container>
          <ng-container matColumnDef="subLedgerId">
            <th mat-header-cell *matHeaderCellDef>Account</th>
            <td mat-cell *matCellDef="let group">
              <app-searchable-select [options]="subLedgerOptions()" [formControl]="group.controls.subLedgerId" label="Sub Ledger" />
            </td>
          </ng-container>
          <ng-container matColumnDef="amount">
            <th mat-header-cell *matHeaderCellDef>Amount</th>
            <td mat-cell *matCellDef="let group">
              <mat-form-field appearance="outline">
                <input matInput type="number" min="0.01" step="0.01" [formControl]="group.controls.amount" />
              </mat-form-field>
            </td>
          </ng-container>
          <ng-container matColumnDef="remove">
            <th mat-header-cell *matHeaderCellDef></th>
            <td mat-cell *matCellDef="let _; let i = index">
              <button mat-icon-button color="warn" type="button" (click)="removeLine(i)" [disabled]="lines.length <= 2">
                <mat-icon>delete</mat-icon>
              </button>
            </td>
          </ng-container>
          <tr mat-header-row *matHeaderRowDef="lineColumns"></tr>
          <tr mat-row *matRowDef="let row; columns: lineColumns"></tr>
        </table>
      </div>

      <div class="line-actions">
        <button mat-stroked-button type="button" (click)="addLine()">Add Row</button>
      </div>

      <div class="totals">
        <span>Total Debit: {{ totalDebit() | indianCurrency }}</span>
        <span>Total Credit: {{ totalCredit() | indianCurrency }}</span>
      </div>
      @if (!totalsMatch()) {
        <p class="error">Debit and Credit totals must match before saving.</p>
      }

      <div class="form-actions">
        <button mat-flat-button color="primary" type="submit" [disabled]="!canSave()">Save</button>
        <button mat-button type="button" (click)="resetForm()">Clear</button>
      </div>
    </form>

    <section class="recent-list">
      <h3>Recent Vouchers</h3>
      <div class="abr-scroll-x">
        <table mat-table [dataSource]="vouchers()" class="mat-elevation-z1">
          <ng-container matColumnDef="voucherNo"><th mat-header-cell *matHeaderCellDef>Voucher No</th><td mat-cell *matCellDef="let row">{{ row.voucherNo }}</td></ng-container>
          <ng-container matColumnDef="voucherDate"><th mat-header-cell *matHeaderCellDef>Date</th><td mat-cell *matCellDef="let row">{{ row.voucherDate | appDate }}</td></ng-container>
          <ng-container matColumnDef="narration"><th mat-header-cell *matHeaderCellDef>Narration</th><td mat-cell *matCellDef="let row">{{ row.narration }}</td></ng-container>
          <ng-container matColumnDef="totalDebit"><th mat-header-cell *matHeaderCellDef>Debit</th><td mat-cell *matCellDef="let row">{{ row.totalDebit | indianCurrency }}</td></ng-container>
          <ng-container matColumnDef="totalCredit"><th mat-header-cell *matHeaderCellDef>Credit</th><td mat-cell *matCellDef="let row">{{ row.totalCredit | indianCurrency }}</td></ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef></th>
            <td mat-cell *matCellDef="let row">
              <button mat-button type="button" (click)="edit(row)">Edit</button>
              <button mat-button color="warn" type="button" (click)="removeVoucher(row)">Delete</button>
            </td>
          </ng-container>
          <tr mat-header-row *matHeaderRowDef="listColumns"></tr>
          <tr mat-row *matRowDef="let row; columns: listColumns"></tr>
        </table>
      </div>
    </section>
  `,
  styles: [`
    .header-row { display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; flex-wrap: wrap; }
    .header-actions { display: flex; gap: 0.5rem; }
    .voucher-form { margin-top: 1rem; }
    .form-head { margin-bottom: 1rem; }
    .narration { grid-column: span 2; }
    .line-table table { width: 100%; min-width: 700px; }
    .line-actions { margin-top: 0.5rem; }
    .totals { display: flex; gap: 1.5rem; font-weight: 600; margin-top: 1rem; }
    .error { color: #d32f2f; margin: 0.5rem 0 0; }
    .form-actions { margin-top: 1rem; display: flex; gap: 0.5rem; }
    .recent-list { margin-top: 1.5rem; }
    .recent-list table { width: 100%; min-width: 760px; }
  `]
})
export class JournalVoucherComponent {
  private readonly fb = inject(FormBuilder);
  private readonly jvService = inject(JournalVoucherService);
  private readonly masterData = inject(MasterDataService);
  private readonly siteContext = inject(SiteContextService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(MatDialog);
  private readonly fileDownloads = inject(FileDownloadService);

  readonly accountingNav = ACCOUNTING_NAV_ITEMS;
  readonly lineColumns = ['entryType', 'subLedgerId', 'amount', 'remove'];
  readonly listColumns = ['voucherNo', 'voucherDate', 'narration', 'totalDebit', 'totalCredit', 'actions'];

  readonly vouchers = signal<JournalVoucher[]>([]);
  readonly mainLedgers = signal<MainLedger[]>([]);
  readonly subLedgers = signal<SubLedger[]>([]);
  readonly selectedId = signal<string | null>(null);
  readonly selectedVoucherNo = signal<string>('Auto');

  siteId: string | null = null;

  readonly form = this.fb.nonNullable.group({
    voucherDate: [new Date().toISOString().slice(0, 10), Validators.required],
    narration: ['', Validators.maxLength(500)],
    lines: this.fb.array([this.createLineGroup(1), this.createLineGroup(2)])
  });

  readonly subLedgerOptions = computed<SelectOption<string>[]>(() =>
    this.subLedgers().map((s) => ({
      value: s.id,
      label: `${s.mainLedgerName ?? ''} / ${s.ledgerName}${s.flatNo ? ` (${s.flatNo})` : ''}`
    }))
  );

  readonly totalDebit = computed(() =>
    this.lineControls()
      .filter((g) => g.controls.entryType.value === 'dr')
      .reduce((sum, g) => sum + Number(g.controls.amount.value ?? 0), 0)
  );

  readonly totalCredit = computed(() =>
    this.lineControls()
      .filter((g) => g.controls.entryType.value === 'cr')
      .reduce((sum, g) => sum + Number(g.controls.amount.value ?? 0), 0)
  );

  readonly totalsMatch = computed(() => this.totalDebit() === this.totalCredit());

  constructor() {
    effect(() => {
      this.siteId = this.siteContext.activeSiteId();
      if (this.siteId) {
        this.loadMasterData();
        this.loadVouchers();
      }
    });
  }

  get lines(): FormArray {
    return this.form.controls.lines as FormArray;
  }

  lineControls() {
    return this.lines.controls as ReturnType<typeof this.createLineGroup>[];
  }

  addLine(): void {
    this.lines.push(this.createLineGroup(this.lines.length + 1));
  }

  removeLine(index: number): void {
    if (this.lines.length <= 2) return;
    this.lines.removeAt(index);
    this.reindexLines();
  }

  canSave(): boolean {
    return this.form.valid && this.lines.length >= 2 && this.totalsMatch();
  }

  save(): void {
    if (!this.siteId || !this.canSave()) return;

    const payload = {
      siteId: this.siteId,
      voucherDate: this.form.controls.voucherDate.value,
      narration: this.form.controls.narration.value || null,
      lines: this.lineControls().map((g, idx) => ({
        subLedgerId: g.controls.subLedgerId.value,
        entryType: g.controls.entryType.value,
        amount: Number(g.controls.amount.value),
        lineNo: idx + 1
      }))
    };

    const currentId = this.selectedId();
    const request$ = currentId ? this.jvService.update(currentId, payload) : this.jvService.create(payload);
    request$.subscribe({
      next: (r) => {
        if (!r.success) return;
        this.toast.success(currentId ? 'Journal voucher updated' : 'Journal voucher created');
        this.resetForm();
        this.loadVouchers();
      }
    });
  }

  edit(voucher: JournalVoucher): void {
    this.selectedId.set(voucher.id);
    this.selectedVoucherNo.set(voucher.voucherNo);
    this.form.patchValue({
      voucherDate: voucher.voucherDate,
      narration: voucher.narration ?? ''
    });
    this.lines.clear();
    voucher.lines
      .sort((a, b) => a.lineNo - b.lineNo)
      .forEach((line, idx) => this.lines.push(this.createLineGroup(idx + 1, line.entryType, line.subLedgerId, line.amount)));
    this.reindexLines();
  }

  removeVoucher(voucher: JournalVoucher): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Voucher', message: `Delete ${voucher.voucherNo}?`, confirmText: 'Delete' }
    });
    ref.afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.jvService.delete(voucher.id).subscribe({
        next: (r) => {
          if (r.success) {
            this.toast.success('Journal voucher deleted');
            this.loadVouchers();
            if (this.selectedId() === voucher.id) {
              this.resetForm();
            }
          }
        }
      });
    });
  }

  resetForm(): void {
    this.selectedId.set(null);
    this.selectedVoucherNo.set('Auto');
    this.form.reset({
      voucherDate: new Date().toISOString().slice(0, 10),
      narration: ''
    });
    this.lines.clear();
    this.lines.push(this.createLineGroup(1));
    this.lines.push(this.createLineGroup(2));
  }

  exportExcel(): void {
    if (!this.siteId) return;
    this.jvService.exportLedgerExcel(this.siteId).subscribe({
      next: (outcome) => this.handleDownload(outcome, `journal-voucher-ledger-${new Date().toISOString().slice(0, 10)}.xlsx`)
    });
  }

  exportPdf(): void {
    if (!this.siteId) return;
    this.jvService.exportLedgerPdf(this.siteId).subscribe({
      next: (outcome) => this.handleDownload(outcome, `journal-voucher-ledger-${new Date().toISOString().slice(0, 10)}.pdf`)
    });
  }

  private handleDownload(outcome: FileDownloadOutcome, fallbackFilename: string): void {
    if (outcome.mode === 'pendrive') {
      this.toast.success(outcome.message ?? `Saved to pendrive: ${outcome.savedPath ?? fallbackFilename}`);
      return;
    }
    if (outcome.blob) {
      this.fileDownloads.saveToBrowser(outcome.blob, outcome.filename ?? fallbackFilename);
      this.toast.success('Export downloaded');
    }
  }

  private loadMasterData(): void {
    if (!this.siteId) return;
    this.masterData.getMainLedgers(this.siteId).subscribe({
      next: (r) => {
        if (r.success) this.mainLedgers.set(r.data as MainLedger[]);
      }
    });
    this.masterData.getMainLedgers(this.siteId).subscribe({
      next: (r) => {
        if (!r.success) return;
        const mains = r.data as MainLedger[];
        const allSubs: SubLedger[] = [];
        let pending = mains.length;
        if (pending === 0) {
          this.subLedgers.set([]);
          return;
        }
        mains.forEach((main) => {
          this.masterData.getSubLedgers(main.id).subscribe({
            next: (subRes) => {
              pending--;
              if (subRes.success) {
                allSubs.push(...(subRes.data as SubLedger[]).map((s) => ({ ...s, mainLedgerName: main.ledgerName, mainLedgerId: main.id })));
              }
              if (pending === 0) this.subLedgers.set(allSubs);
            },
            error: () => {
              pending--;
              if (pending === 0) this.subLedgers.set(allSubs);
            }
          });
        });
      }
    });
  }

  private loadVouchers(): void {
    if (!this.siteId) return;
    this.jvService.getList({ siteId: this.siteId, page: 1, pageSize: 25 }).subscribe({
      next: (r) => {
        if (r.success) this.vouchers.set(r.data.items);
      }
    });
  }

  private createLineGroup(lineNo: number, entryType: 'dr' | 'cr' = 'dr', subLedgerId = '', amount = 0) {
    return this.fb.nonNullable.group({
      lineNo: [lineNo],
      entryType: [entryType, Validators.required],
      subLedgerId: [subLedgerId, Validators.required],
      amount: [amount, [Validators.required, Validators.min(0.01)]]
    });
  }

  private reindexLines(): void {
    this.lineControls().forEach((group, index) => group.controls.lineNo.setValue(index + 1));
  }
}
