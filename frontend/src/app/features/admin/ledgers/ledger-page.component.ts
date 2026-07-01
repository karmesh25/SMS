import { Component, effect, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { MasterDataService } from '../../../core/services/master-data.service';
import { SiteContextService } from '../../../core/services/site-context.service';
import { ToastService } from '../../../core/services/toast.service';
import { NameEditDialogComponent } from '../../../shared/components/master-edit/name-edit-dialog.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

interface MainLedger { id: string; ledgerName: string; description?: string; }
interface SubLedger { id: string; ledgerName: string; flatNo?: string; mainLedgerId: string; }

type LedgerPanel = 'main' | 'sub';

@Component({
  selector: 'app-ledger-page',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatTableModule, MatButtonModule, MatButtonToggleModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatIconModule, PageHeaderComponent
  ],
  template: `
    <app-page-header title="Ledger Management" subtitle="Main and sub ledgers"></app-page-header>

    <mat-button-toggle-group
      class="ledger-tabs"
      [value]="activePanel"
      (change)="onPanelChange($event.value)">
      <mat-button-toggle value="main">Main Ledger</mat-button-toggle>
      <mat-button-toggle value="sub">Sub Ledger</mat-button-toggle>
    </mat-button-toggle-group>

    @if (activePanel === 'main') {
      <section class="panel">
        <form [formGroup]="mainForm" class="compact-bar" (ngSubmit)="addMain()">
          <mat-form-field appearance="outline" subscriptSizing="dynamic" class="name-field">
            <mat-label>Name</mat-label>
            <input matInput formControlName="ledgerName" />
          </mat-form-field>
          <button mat-flat-button color="primary" type="submit" [disabled]="mainForm.invalid">+ Add</button>
        </form>

        <div class="abr-scroll-x">
          <table mat-table [dataSource]="mainLedgers" class="mat-elevation-z1">
            <ng-container matColumnDef="ledgerName"><th mat-header-cell *matHeaderCellDef>Name</th><td mat-cell *matCellDef="let row">{{ row.ledgerName }}</td></ng-container>
            <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let row">
                <button mat-button (click)="editMain(row); $event.stopPropagation()">Edit</button>
                <button mat-button color="warn" (click)="deleteMain(row); $event.stopPropagation()">Delete</button>
              </td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="mainCols"></tr>
            <tr mat-row *matRowDef="let row; columns: mainCols" [class.selected]="row.id === selectedMainId" (click)="openSubPanel(row)"></tr>
          </table>
        </div>
      </section>
    } @else {
      <section class="panel">
        @if (selectedMainId && mainLedgers.length > 0) {
          <div class="compact-bar">
            <mat-form-field appearance="outline" subscriptSizing="dynamic" class="picker-field">
              <mat-label>Main Ledger</mat-label>
              <mat-select [value]="selectedMainId" (selectionChange)="onMainLedgerPick($event.value)">
                @for (main of mainLedgers; track main.id) {
                  <mat-option [value]="main.id">{{ main.ledgerName }}</mat-option>
                }
              </mat-select>
            </mat-form-field>

            <ng-container [formGroup]="subForm">
              <mat-form-field appearance="outline" subscriptSizing="dynamic" class="name-field">
                <mat-label>Sub Name</mat-label>
                <input matInput formControlName="ledgerName" (keyup.enter)="addSub()" />
              </mat-form-field>
              <button mat-flat-button color="primary" type="button" (click)="addSub()" [disabled]="subForm.invalid">+ Add Sub</button>
            </ng-container>

            <ng-container [formGroup]="searchForm">
              <mat-form-field appearance="outline" subscriptSizing="dynamic" class="search-field">
                <mat-label>Search by Flat</mat-label>
                <input matInput formControlName="searchFlat" (keyup.enter)="search()" />
                <button matSuffix mat-icon-button type="button" (click)="search()" aria-label="Search">
                  <mat-icon>search</mat-icon>
                </button>
              </mat-form-field>
            </ng-container>
          </div>

          <div class="abr-scroll-x">
            <table mat-table [dataSource]="subLedgers" class="mat-elevation-z1">
              <ng-container matColumnDef="ledgerName"><th mat-header-cell *matHeaderCellDef>Name</th><td mat-cell *matCellDef="let row">{{ row.ledgerName }}</td></ng-container>
              <ng-container matColumnDef="flatNo"><th mat-header-cell *matHeaderCellDef>Flat</th><td mat-cell *matCellDef="let row">{{ row.flatNo || '-' }}</td></ng-container>
              <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef>Actions</th>
                <td mat-cell *matCellDef="let row">
                  <button mat-button (click)="editSub(row)">Edit</button>
                  <button mat-button color="warn" (click)="deleteSub(row)">Delete</button>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="subCols"></tr>
              <tr mat-row *matRowDef="let row; columns: subCols"></tr>
            </table>
          </div>
        } @else {
          <p class="empty-hint">Add a main ledger first, then manage sub ledgers here.</p>
        }
      </section>
    }
  `,
  styles: [`
    .ledger-tabs {
      display: flex;
      width: 100%;
      margin-bottom: 0.75rem;
    }
    .ledger-tabs mat-button-toggle {
      flex: 1;
      height: 36px;
      line-height: 36px;
    }
    .panel { margin-top: 0.5rem; }
    .compact-bar {
      display: flex;
      gap: 0.75rem;
      align-items: flex-start;
      flex-wrap: wrap;
      margin-bottom: 0.75rem;
    }
    .picker-field { width: 180px; flex-shrink: 0; }
    .name-field { flex: 1; min-width: 160px; }
    .search-field { width: 200px; flex-shrink: 0; }
    table { width: 100%; min-width: 320px; }
    tr.selected { background: rgba(25, 118, 210, 0.08); }
    .empty-hint { color: #888; padding: 1rem 0; }

    @media (max-width: 599px) {
      .picker-field,
      .search-field,
      .name-field { width: 100%; }
    }
  `]
})
export class LedgerPageComponent implements OnInit {
  private readonly masterData = inject(MasterDataService);
  private readonly siteContext = inject(SiteContextService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);

  mainLedgers: MainLedger[] = [];
  subLedgers: SubLedger[] = [];
  mainCols = ['ledgerName', 'actions'];
  subCols = ['ledgerName', 'flatNo', 'actions'];
  selectedMainId: string | null = null;
  selectedMainName = '';
  isMemberAccount = false;
  siteId: string | null = null;
  activePanel: LedgerPanel = 'main';

  mainForm = this.fb.nonNullable.group({ ledgerName: ['', Validators.required] });
  subForm = this.fb.nonNullable.group({ ledgerName: ['', Validators.required], flatNo: [''] });
  searchForm = this.fb.nonNullable.group({ searchFlat: [''] });

  constructor() {
    effect(() => {
      this.siteId = this.siteContext.activeSiteId();
      if (this.siteId) this.loadMain();
    });
  }

  ngOnInit(): void {
    this.siteContext.loadSites().subscribe();
    this.siteId = this.siteContext.activeSiteId();
    if (this.siteId) this.loadMain();
  }

  onPanelChange(panel: LedgerPanel): void {
    if (panel === 'sub' && !this.selectedMainId && this.mainLedgers.length > 0) {
      this.selectMain(this.mainLedgers[0], false);
    }
    this.activePanel = panel;
  }

  onMainLedgerPick(mainId: string): void {
    const main = this.mainLedgers.find((m) => m.id === mainId);
    if (main) this.selectMain(main, false);
  }

  loadMain(): void {
    if (!this.siteId) return;
    this.masterData.getMainLedgers(this.siteId).subscribe({
      next: (r) => {
        if (!r.success) return;
        this.mainLedgers = r.data as MainLedger[];
        if (this.mainLedgers.length === 0) {
          this.selectedMainId = null;
          this.selectedMainName = '';
          this.subLedgers = [];
          this.activePanel = 'main';
          return;
        }
        const current = this.selectedMainId
          ? this.mainLedgers.find((m) => m.id === this.selectedMainId)
          : null;
        this.selectMain(current ?? this.mainLedgers[0], false);
      }
    });
  }

  addMain(): void {
    if (!this.siteId) return;
    this.masterData.createMainLedger({ ...this.mainForm.getRawValue(), siteId: this.siteId }).subscribe({
      next: (r) => { if (r.success) { this.toast.success('Main ledger added'); this.mainForm.reset(); this.loadMain(); } }
    });
  }

  openSubPanel(row: MainLedger): void {
    this.selectMain(row, true);
  }

  selectMain(row: MainLedger, openSub = false): void {
    this.selectedMainId = row.id;
    this.selectedMainName = row.ledgerName;
    this.isMemberAccount = row.ledgerName.toLowerCase().includes('member');
    if (openSub) this.activePanel = 'sub';
    this.masterData.getSubLedgers(row.id).subscribe({
      next: (r) => { if (r.success) this.subLedgers = r.data as SubLedger[]; }
    });
  }

  editMain(row: MainLedger): void {
    const ref = this.dialog.open(NameEditDialogComponent, {
      data: { title: 'Edit Main Ledger', label: 'Ledger Name', value: row.ledgerName }
    });
    ref.afterClosed().subscribe((result) => {
      if (!result || !this.siteId) return;
      this.masterData.updateMainLedger(row.id, { ledgerName: result.value, siteId: this.siteId }).subscribe({
        next: (r) => { if (r.success) { this.toast.success('Main ledger updated'); this.loadMain(); } }
      });
    });
  }

  deleteMain(row: MainLedger): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Main Ledger', message: `Delete "${row.ledgerName}"?` }
    });
    ref.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;
      this.masterData.deleteMainLedger(row.id).subscribe({
        next: (r) => {
          if (r.success) {
            this.toast.success('Main ledger deleted');
            if (this.selectedMainId === row.id) {
              this.selectedMainId = null;
              this.subLedgers = [];
              this.activePanel = 'main';
            }
            this.loadMain();
          }
        }
      });
    });
  }

  addSub(): void {
    if (!this.selectedMainId) return;
    this.masterData.createSubLedger({ mainLedgerId: this.selectedMainId, ledgerName: this.subForm.value.ledgerName! }).subscribe({
      next: (r) => { if (r.success) { this.toast.success('Sub ledger added'); this.subForm.reset(); this.selectMain({ id: this.selectedMainId!, ledgerName: this.selectedMainName }, true); } }
    });
  }

  editSub(row: SubLedger): void {
    const ref = this.dialog.open(NameEditDialogComponent, {
      data: { title: 'Edit Sub Ledger', label: 'Ledger Name', value: row.ledgerName }
    });
    ref.afterClosed().subscribe((result) => {
      if (!result || !this.selectedMainId) return;
      this.masterData.updateSubLedger(row.id, { ledgerName: result.value, mainLedgerId: this.selectedMainId }).subscribe({
        next: (r) => { if (r.success) { this.toast.success('Sub ledger updated'); this.selectMain({ id: this.selectedMainId!, ledgerName: this.selectedMainName }, true); } }
      });
    });
  }

  deleteSub(row: SubLedger): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Sub Ledger', message: `Delete "${row.ledgerName}"?` }
    });
    ref.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;
      this.masterData.deleteSubLedger(row.id).subscribe({
        next: (r) => {
          if (r.success) {
            this.toast.success('Sub ledger deleted');
            this.selectMain({ id: this.selectedMainId!, ledgerName: this.selectedMainName }, true);
          }
        }
      });
    });
  }

  search(): void {
    const flatNo = this.searchForm.value.searchFlat;
    if (!this.siteId || !flatNo) return;
    this.masterData.searchSubLedgers(this.siteId, flatNo).subscribe({
      next: (r) => { if (r.success) this.subLedgers = r.data as SubLedger[]; }
    });
  }
}
