import { Component, effect, inject, OnInit } from '@angular/core';

import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { MatTableModule } from '@angular/material/table';

import { MatIconModule } from '@angular/material/icon';

import { MatButtonModule } from '@angular/material/button';

import { MatFormFieldModule } from '@angular/material/form-field';

import { MatInputModule } from '@angular/material/input';

import { MatDialog } from '@angular/material/dialog';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';

import { MasterDataService } from '../../../core/services/master-data.service';

import { SiteContextService } from '../../../core/services/site-context.service';

import { ToastService } from '../../../core/services/toast.service';

import { BrokerEditDialogComponent } from '../../../shared/components/master-edit/broker-edit-dialog.component';

import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';



interface BrokerRow {

  id: string;

  name: string;

  contactNo?: string;

  contactNo2?: string;

  address?: string;

}



@Component({

  selector: 'app-broker-list',

  standalone: true,

  imports: [ReactiveFormsModule, MatTableModule, MatIconModule, MatButtonModule, MatFormFieldModule, MatInputModule, PageHeaderComponent],

  template: `

    <app-page-header title="Brokers" subtitle="Broker master list"></app-page-header>



    <section class="abr-panel">

      <h2 class="abr-panel__title"><mat-icon>person_add</mat-icon>Add Broker</h2>

      <form [formGroup]="form" class="abr-form-grid" (ngSubmit)="save()">

        <mat-form-field appearance="outline"><mat-label>Name</mat-label><input matInput formControlName="name" /></mat-form-field>

        <mat-form-field appearance="outline"><mat-label>Contact</mat-label><input matInput formControlName="contactNo" /></mat-form-field>

        <mat-form-field appearance="outline"><mat-label>Contact 2</mat-label><input matInput formControlName="contactNo2" /></mat-form-field>

        <mat-form-field appearance="outline"><mat-label>Address</mat-label><input matInput formControlName="address" /></mat-form-field>

        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid"><mat-icon>add</mat-icon> Add Broker</button>

      </form>

    </section>



    <div class="abr-table-card">



    <table mat-table [dataSource]="brokers" class="abr-table sticky-header">

      <ng-container matColumnDef="name"><th mat-header-cell *matHeaderCellDef>Name</th><td mat-cell *matCellDef="let row">{{ row.name }}</td></ng-container>

      <ng-container matColumnDef="contactNo"><th mat-header-cell *matHeaderCellDef>Contact</th><td mat-cell *matCellDef="let row">{{ row.contactNo }}</td></ng-container>

      <ng-container matColumnDef="address"><th mat-header-cell *matHeaderCellDef>Address</th><td mat-cell *matCellDef="let row">{{ row.address }}</td></ng-container>

      <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef>Actions</th>

        <td mat-cell *matCellDef="let row">

          <button mat-button (click)="edit(row)">Edit</button>

          <button mat-button color="warn" (click)="remove(row)">Delete</button>

        </td>

      </ng-container>

      <tr mat-header-row *matHeaderRowDef="cols"></tr>

      <tr mat-row *matRowDef="let row; columns: cols"></tr>

      <tr class="empty-row" *matNoDataRow><td [attr.colspan]="cols.length"><mat-icon>badge</mat-icon>No brokers yet.</td></tr>

    </table>

    </div>

  `,

  styles: [`.abr-form-grid button { min-width: 140px; }`]

})

export class BrokerListComponent implements OnInit {

  private readonly masterData = inject(MasterDataService);

  private readonly siteContext = inject(SiteContextService);

  private readonly toast = inject(ToastService);

  private readonly fb = inject(FormBuilder);

  private readonly dialog = inject(MatDialog);



  brokers: BrokerRow[] = [];

  cols = ['name', 'contactNo', 'address', 'actions'];

  siteId: string | null = null;



  form = this.fb.nonNullable.group({

    name: ['', Validators.required],

    contactNo: [''],

    contactNo2: [''],

    address: ['']

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

    this.masterData.getBrokers(this.siteId).subscribe({

      next: (r) => { if (r.success) this.brokers = r.data as BrokerRow[]; }

    });

  }



  save(): void {

    if (!this.siteId) return;

    this.masterData.createBroker({ ...this.form.getRawValue(), siteId: this.siteId }).subscribe({

      next: (r) => { if (r.success) { this.toast.success('Broker added'); this.form.reset(); this.load(); } }

    });

  }



  edit(row: BrokerRow): void {

    const ref = this.dialog.open(BrokerEditDialogComponent, {

      data: { name: row.name, contactNo: row.contactNo, contactNo2: row.contactNo2, address: row.address }

    });

    ref.afterClosed().subscribe((result) => {

      if (!result || !this.siteId) return;

      this.masterData.updateBroker(row.id, { ...result, siteId: this.siteId }).subscribe({

        next: (r) => { if (r.success) { this.toast.success('Broker updated'); this.load(); } }

      });

    });

  }



  remove(row: BrokerRow): void {

    const ref = this.dialog.open(ConfirmDialogComponent, {

      data: { title: 'Delete Broker', message: `Delete broker "${row.name}"?` }

    });

    ref.afterClosed().subscribe((confirmed) => {

      if (!confirmed) return;

      this.masterData.deleteBroker(row.id).subscribe({

        next: (r) => { if (r.success) { this.toast.success('Broker deleted'); this.load(); } }

      });

    });

  }

}

