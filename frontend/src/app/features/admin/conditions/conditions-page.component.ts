import { Component, effect, inject, OnInit } from '@angular/core';

import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { MatTableModule } from '@angular/material/table';

import { MatIconModule } from '@angular/material/icon';

import { MatButtonModule } from '@angular/material/button';

import { MatFormFieldModule } from '@angular/material/form-field';

import { MatInputModule } from '@angular/material/input';

import { MatSelectModule } from '@angular/material/select';

import { MatDialog } from '@angular/material/dialog';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';

import { MasterDataService } from '../../../core/services/master-data.service';

import { SiteContextService } from '../../../core/services/site-context.service';

import { ToastService } from '../../../core/services/toast.service';

import { ConditionEditDialogComponent } from '../../../shared/components/master-edit/condition-edit-dialog.component';

import { ConditionItemsDialogComponent } from '../../../shared/components/master-edit/condition-items-dialog.component';

import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';



interface Condition { id: string; conditionName: string; conditionType: string; itemCount: number; }



@Component({

  selector: 'app-conditions-page',

  standalone: true,

  imports: [ReactiveFormsModule, MatTableModule, MatIconModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, PageHeaderComponent],

  template: `

    <app-page-header title="Payment Conditions" subtitle="Installment milestones"></app-page-header>



    <form [formGroup]="form" class="row" (ngSubmit)="save()">

      <mat-form-field appearance="outline"><mat-label>Condition Name</mat-label><input matInput formControlName="conditionName" /></mat-form-field>

      <mat-form-field appearance="outline">

        <mat-label>Type</mat-label>

        <mat-select formControlName="conditionType">

          <mat-option value="manual">Manual</mat-option>

          <mat-option value="auto">Auto</mat-option>

        </mat-select>

      </mat-form-field>

      <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">Add Condition</button>

    </form>



    <div class="abr-scroll-x">



    <table mat-table [dataSource]="conditions" class="mat-elevation-z1 abr-table sticky-header">

      <ng-container matColumnDef="conditionName"><th mat-header-cell *matHeaderCellDef>Name</th><td mat-cell *matCellDef="let row">{{ row.conditionName }}</td></ng-container>

      <ng-container matColumnDef="conditionType"><th mat-header-cell *matHeaderCellDef>Type</th><td mat-cell *matCellDef="let row">{{ row.conditionType }}</td></ng-container>

      <ng-container matColumnDef="itemCount"><th mat-header-cell *matHeaderCellDef>Items</th><td mat-cell *matCellDef="let row">{{ row.itemCount }}</td></ng-container>

      <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef>Actions</th>

        <td mat-cell *matCellDef="let row">

          <button mat-button (click)="openItems(row)">Items</button>

          <button mat-button (click)="edit(row)">Edit</button>

          <button mat-button color="warn" (click)="remove(row)">Delete</button>

        </td>

      </ng-container>

      <tr mat-header-row *matHeaderRowDef="cols"></tr>

      <tr mat-row *matRowDef="let row; columns: cols"></tr>

      <tr class="empty-row" *matNoDataRow><td [attr.colspan]="cols.length"><mat-icon>info_outline</mat-icon>No records found.</td></tr>

    </table>

    </div>

  `,

  styles: [`.row { display:flex; gap:1rem; flex-wrap:wrap; margin-bottom:1.5rem; align-items:center; } table { width:100%; }`]

})

export class ConditionsPageComponent implements OnInit {

  private readonly masterData = inject(MasterDataService);

  private readonly siteContext = inject(SiteContextService);

  private readonly toast = inject(ToastService);

  private readonly fb = inject(FormBuilder);

  private readonly dialog = inject(MatDialog);



  conditions: Condition[] = [];

  cols = ['conditionName', 'conditionType', 'itemCount', 'actions'];

  siteId: string | null = null;



  form = this.fb.nonNullable.group({

    conditionName: ['', Validators.required],

    conditionType: ['manual', Validators.required]

  });



  constructor() {

    effect(() => {

      this.siteId = this.siteContext.activeSiteId();

      if (this.siteId) this.load();

    });

  }



  ngOnInit(): void {

    this.siteContext.loadSites().subscribe();

    this.siteId = this.siteContext.activeSiteId();

    if (this.siteId) this.load();

  }



  load(): void {

    if (!this.siteId) return;

    this.masterData.getConditions(this.siteId).subscribe({

      next: (r) => { if (r.success) this.conditions = r.data as Condition[]; }

    });

  }



  save(): void {

    if (!this.siteId) return;

    this.masterData.createCondition({ ...this.form.getRawValue(), siteId: this.siteId }).subscribe({

      next: (r) => { if (r.success) { this.toast.success('Condition created'); this.form.reset({ conditionType: 'manual' }); this.load(); } }

    });

  }



  openItems(condition: Condition): void {

    const ref = this.dialog.open(ConditionItemsDialogComponent, {

      width: '800px',

      data: { conditionId: condition.id, conditionName: condition.conditionName }

    });

    ref.afterClosed().subscribe((changed) => { if (changed) this.load(); });

  }



  edit(condition: Condition): void {

    const ref = this.dialog.open(ConditionEditDialogComponent, {

      data: { conditionName: condition.conditionName, conditionType: condition.conditionType }

    });

    ref.afterClosed().subscribe((result) => {

      if (!result) return;

      this.masterData.updateCondition(condition.id, result).subscribe({

        next: (r) => { if (r.success) { this.toast.success('Condition updated'); this.load(); } }

      });

    });

  }



  remove(condition: Condition): void {

    const ref = this.dialog.open(ConfirmDialogComponent, {

      data: { title: 'Delete Condition', message: `Delete "${condition.conditionName}"?` }

    });

    ref.afterClosed().subscribe((confirmed) => {

      if (!confirmed) return;

      this.masterData.deleteCondition(condition.id).subscribe({

        next: (r) => { if (r.success) { this.toast.success('Condition deleted'); this.load(); } }

      });

    });

  }

}

