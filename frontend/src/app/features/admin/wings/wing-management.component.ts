import { Component, effect, inject, OnInit } from '@angular/core';

import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { MatTableModule } from '@angular/material/table';

import { MatIconModule } from '@angular/material/icon';

import { MatButtonModule } from '@angular/material/button';

import { MatFormFieldModule } from '@angular/material/form-field';

import { MatInputModule } from '@angular/material/input';

import { MatCheckboxModule } from '@angular/material/checkbox';

import { MatDialog } from '@angular/material/dialog';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';

import { MasterDataService } from '../../../core/services/master-data.service';

import { SiteContextService } from '../../../core/services/site-context.service';

import { ToastService } from '../../../core/services/toast.service';

import { WingEditDialogComponent } from '../../../shared/components/master-edit/wing-edit-dialog.component';

import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';



interface WingRow {

  id: string;

  wingName: string;

  floors: number;

  flatsPerFloor: number;

  shops: number;

  isBungalow: boolean;

  flatCount: number;

}



@Component({

  selector: 'app-wing-management',

  standalone: true,

  imports: [ReactiveFormsModule, MatTableModule, MatIconModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatCheckboxModule, PageHeaderComponent],

  template: `

    <app-page-header title="Wing Management" subtitle="Create wings and auto-generate flats"></app-page-header>



    <form [formGroup]="form" class="form-grid" (ngSubmit)="save()">

      <mat-form-field appearance="outline"><mat-label>Wing Name</mat-label><input matInput formControlName="wingName" /></mat-form-field>

      <mat-form-field appearance="outline"><mat-label>Floors</mat-label><input matInput type="number" formControlName="floors" /></mat-form-field>

      <mat-form-field appearance="outline"><mat-label>Flats Per Floor</mat-label><input matInput type="number" formControlName="flatsPerFloor" /></mat-form-field>

      <mat-form-field appearance="outline"><mat-label>Shops</mat-label><input matInput type="number" formControlName="shops" /></mat-form-field>

      <mat-form-field appearance="outline"><mat-label>Default SQFT</mat-label><input matInput type="number" formControlName="defaultSqft" /></mat-form-field>

      <mat-checkbox formControlName="isBungalow">Is Bungalow</mat-checkbox>

      <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || !siteId">Add Wing</button>

    </form>



    <table mat-table [dataSource]="wings" class="mat-elevation-z1 abr-table sticky-header">

      <ng-container matColumnDef="wingName"><th mat-header-cell *matHeaderCellDef>Wing</th><td mat-cell *matCellDef="let row">{{ row.wingName }}</td></ng-container>

      <ng-container matColumnDef="floors"><th mat-header-cell *matHeaderCellDef>Floors</th><td mat-cell *matCellDef="let row">{{ row.floors }}</td></ng-container>

      <ng-container matColumnDef="flatsPerFloor"><th mat-header-cell *matHeaderCellDef>Flats/Floor</th><td mat-cell *matCellDef="let row">{{ row.flatsPerFloor }}</td></ng-container>

      <ng-container matColumnDef="flatCount"><th mat-header-cell *matHeaderCellDef>Total Flats</th><td mat-cell *matCellDef="let row">{{ row.flatCount }}</td></ng-container>

      <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef>Actions</th>

        <td mat-cell *matCellDef="let row">

          <button mat-button (click)="edit(row)">Edit</button>

          <button mat-button color="warn" (click)="remove(row)">Delete</button>

        </td>

      </ng-container>

      <tr mat-header-row *matHeaderRowDef="cols"></tr>

      <tr mat-row *matRowDef="let row; columns: cols"></tr>

      <tr class="empty-row" *matNoDataRow><td [attr.colspan]="cols.length"><mat-icon>info_outline</mat-icon>No records found.</td></tr>

    </table>

  `,

  styles: [`.form-grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(160px,1fr)); gap:1rem; margin-bottom:1.5rem; align-items:center; } table { width:100%; }`]

})

export class WingManagementComponent implements OnInit {

  private readonly masterData = inject(MasterDataService);

  private readonly siteContext = inject(SiteContextService);

  private readonly toast = inject(ToastService);

  private readonly fb = inject(FormBuilder);

  private readonly dialog = inject(MatDialog);



  wings: WingRow[] = [];

  cols = ['wingName', 'floors', 'flatsPerFloor', 'flatCount', 'actions'];

  siteId: string | null = null;



  form = this.fb.nonNullable.group({

    wingName: ['', Validators.required],

    floors: [1, [Validators.required, Validators.min(1)]],

    flatsPerFloor: [4, [Validators.required, Validators.min(1)]],

    shops: [0, Validators.min(0)],

    defaultSqft: [1000, Validators.min(1)],

    isBungalow: [false]

  });



  constructor() {

    effect(() => {

      this.siteId = this.siteContext.activeSiteId();

      if (this.siteId) this.loadWings();

    });

  }



  ngOnInit(): void {

    this.siteContext.loadSites().subscribe();

    this.siteId = this.siteContext.activeSiteId();

    if (this.siteId) this.loadWings();

  }



  loadWings(): void {

    if (!this.siteId) return;

    this.masterData.getWings(this.siteId).subscribe({

      next: (r) => { if (r.success) this.wings = r.data as WingRow[]; }

    });

  }



  save(): void {

    if (!this.siteId) return;

    this.masterData.createWing({ ...this.form.getRawValue(), siteId: this.siteId }).subscribe({

      next: (r) => {

        if (r.success) {

          this.toast.success('Wing created with flats');

          this.form.patchValue({ wingName: '', floors: 1, flatsPerFloor: 4, shops: 0 });

          this.loadWings();

        }

      }

    });

  }



  edit(row: WingRow): void {

    const ref = this.dialog.open(WingEditDialogComponent, {

      data: {

        wingName: row.wingName,

        floors: row.floors,

        flatsPerFloor: row.flatsPerFloor,

        shops: row.shops,

        isBungalow: row.isBungalow

      }

    });

    ref.afterClosed().subscribe((result) => {

      if (!result) return;

      this.masterData.updateWing(row.id, result).subscribe({

        next: (r) => { if (r.success) { this.toast.success('Wing updated'); this.loadWings(); } }

      });

    });

  }



  remove(row: WingRow): void {

    const ref = this.dialog.open(ConfirmDialogComponent, {

      data: { title: 'Delete Wing', message: `Delete wing "${row.wingName}" and all its flats?` }

    });

    ref.afterClosed().subscribe((confirmed) => {

      if (!confirmed) return;

      this.masterData.deleteWing(row.id).subscribe({

        next: (r) => { if (r.success) { this.toast.success('Wing deleted'); this.loadWings(); } }

      });

    });

  }

}

