import { Component, effect, inject, OnInit } from '@angular/core';

import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { MatTableModule } from '@angular/material/table';

import { MatButtonModule } from '@angular/material/button';

import { MatFormFieldModule } from '@angular/material/form-field';

import { MatInputModule } from '@angular/material/input';

import { MatSelectModule } from '@angular/material/select';

import { MatDialog } from '@angular/material/dialog';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';

import { MasterDataService } from '../../../core/services/master-data.service';

import { SiteContextService } from '../../../core/services/site-context.service';

import { ToastService } from '../../../core/services/toast.service';

import { NameEditDialogComponent } from '../../../shared/components/master-edit/name-edit-dialog.component';

import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';



interface MainLedger { id: string; ledgerName: string; description?: string; }

interface SubLedger { id: string; ledgerName: string; flatNo?: string; mainLedgerId: string; }



@Component({

  selector: 'app-ledger-page',

  standalone: true,

  imports: [ReactiveFormsModule, MatTableModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, PageHeaderComponent],

  template: `

    <app-page-header title="Ledger Management" subtitle="Main and sub ledgers"></app-page-header>



    <div class="split">

      <section>

        <h3>Main Ledgers</h3>

        <form [formGroup]="mainForm" class="row" (ngSubmit)="addMain()">

          <mat-form-field appearance="outline"><mat-label>Name</mat-label><input matInput formControlName="ledgerName" /></mat-form-field>

          <button mat-flat-button color="primary" type="submit" [disabled]="mainForm.invalid">Add</button>

        </form>

        <table mat-table [dataSource]="mainLedgers" class="mat-elevation-z1">

          <ng-container matColumnDef="ledgerName"><th mat-header-cell *matHeaderCellDef>Name</th><td mat-cell *matCellDef="let row">{{ row.ledgerName }}</td></ng-container>

          <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef>Actions</th>

            <td mat-cell *matCellDef="let row">

              <button mat-button (click)="selectMain(row)">Sub</button>

              <button mat-button (click)="editMain(row)">Edit</button>

              <button mat-button color="warn" (click)="deleteMain(row)">Delete</button>

            </td>

          </ng-container>

          <tr mat-header-row *matHeaderRowDef="mainCols"></tr>

          <tr mat-row *matRowDef="let row; columns: mainCols" [class.selected]="row.id === selectedMainId" (click)="selectMain(row)"></tr>

        </table>

      </section>



      <section>

        <h3>Sub Ledgers</h3>

        @if (selectedMainId) {

          <form [formGroup]="subForm" class="row" (ngSubmit)="addSub()">

            <mat-form-field appearance="outline"><mat-label>Name</mat-label><input matInput formControlName="ledgerName" /></mat-form-field>

            @if (isMemberAccount) {

              <mat-form-field appearance="outline"><mat-label>Flat No</mat-label><input matInput formControlName="flatNo" placeholder="Search flat" /></mat-form-field>

            }

            <button mat-flat-button color="primary" type="submit" [disabled]="subForm.invalid">Add Sub</button>

          </form>

          <div class="row" [formGroup]="searchForm">

            <mat-form-field appearance="outline"><mat-label>Search by Flat</mat-label><input matInput formControlName="searchFlat" (keyup.enter)="search()" /></mat-form-field>

            <button mat-button (click)="search()">Search</button>

          </div>

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

        } @else {

          <p>Select a main ledger to manage sub ledgers.</p>

        }

      </section>

    </div>

  `,

  styles: [`

    .split { display:grid; grid-template-columns:1fr 1fr; gap:2rem; }

    .row { display:flex; gap:1rem; flex-wrap:wrap; margin-bottom:1rem; align-items:center; }

    table { width:100%; }

    tr.selected { background:rgba(25,118,210,0.08); }

    @media (max-width:900px) { .split { grid-template-columns:1fr; } }

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



  loadMain(): void {

    if (!this.siteId) return;

    this.masterData.getMainLedgers(this.siteId).subscribe({

      next: (r) => { if (r.success) this.mainLedgers = r.data as MainLedger[]; }

    });

  }



  addMain(): void {

    if (!this.siteId) return;

    this.masterData.createMainLedger({ ...this.mainForm.getRawValue(), siteId: this.siteId }).subscribe({

      next: (r) => { if (r.success) { this.toast.success('Main ledger added'); this.mainForm.reset(); this.loadMain(); } }

    });

  }



  selectMain(row: MainLedger): void {

    this.selectedMainId = row.id;

    this.selectedMainName = row.ledgerName;

    this.isMemberAccount = row.ledgerName.toLowerCase().includes('member');

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

      next: (r) => { if (r.success) { this.toast.success('Sub ledger added'); this.subForm.reset(); this.selectMain({ id: this.selectedMainId!, ledgerName: this.selectedMainName }); } }

    });

  }



  editSub(row: SubLedger): void {

    const ref = this.dialog.open(NameEditDialogComponent, {

      data: { title: 'Edit Sub Ledger', label: 'Ledger Name', value: row.ledgerName }

    });

    ref.afterClosed().subscribe((result) => {

      if (!result || !this.selectedMainId) return;

      this.masterData.updateSubLedger(row.id, { ledgerName: result.value, mainLedgerId: this.selectedMainId }).subscribe({

        next: (r) => { if (r.success) { this.toast.success('Sub ledger updated'); this.selectMain({ id: this.selectedMainId!, ledgerName: this.selectedMainName }); } }

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

            this.selectMain({ id: this.selectedMainId!, ledgerName: this.selectedMainName });

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

