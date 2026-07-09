import { Component, inject, OnInit } from '@angular/core';

import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { MatTableModule } from '@angular/material/table';

import { MatIconModule } from '@angular/material/icon';

import { MatButtonModule } from '@angular/material/button';

import { MatFormFieldModule } from '@angular/material/form-field';

import { MatInputModule } from '@angular/material/input';

import { MatDialog } from '@angular/material/dialog';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';

import { MasterDataService } from '../../../core/services/master-data.service';

import { ToastService } from '../../../core/services/toast.service';

import { SiteContextService, Site } from '../../../core/services/site-context.service';

import { SiteEditDialogComponent } from '../../../shared/components/master-edit/site-edit-dialog.component';



@Component({

  selector: 'app-site-list',

  standalone: true,

  imports: [ReactiveFormsModule, MatTableModule, MatIconModule, MatButtonModule, MatFormFieldModule, MatInputModule, PageHeaderComponent],

  template: `

    <app-page-header title="Sites" subtitle="Manage project sites"></app-page-header>



    <section class="abr-panel">

      <h2 class="abr-panel__title"><mat-icon>add_circle</mat-icon>Add Site</h2>

      <form [formGroup]="form" class="abr-form-grid" (ngSubmit)="save()">

        <mat-form-field appearance="outline"><mat-label>Site Name</mat-label><input matInput formControlName="siteName" /></mat-form-field>

        <mat-form-field appearance="outline"><mat-label>Address</mat-label><input matInput formControlName="address" /></mat-form-field>

        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid"><mat-icon>add</mat-icon> Add Site</button>

      </form>

    </section>



    <div class="abr-table-card">



    <table mat-table [dataSource]="sites" class="abr-table sticky-header">

      <ng-container matColumnDef="siteName"><th mat-header-cell *matHeaderCellDef>Name</th><td mat-cell *matCellDef="let row">{{ row.siteName }}</td></ng-container>

      <ng-container matColumnDef="address"><th mat-header-cell *matHeaderCellDef>Address</th><td mat-cell *matCellDef="let row">{{ row.address }}</td></ng-container>

      <ng-container matColumnDef="isActive"><th mat-header-cell *matHeaderCellDef>Active</th><td mat-cell *matCellDef="let row"><span class="abr-chip" [class.abr-chip--success]="row.isActive" [class.abr-chip--danger]="!row.isActive">{{ row.isActive ? 'Active' : 'Inactive' }}</span></td></ng-container>

      <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef>Actions</th>

        <td mat-cell *matCellDef="let row">

          <button mat-button (click)="edit(row)">Edit</button>

          @if (row.isActive) {

            <button mat-button color="warn" (click)="toggleActive(row)">Disable</button>

          } @else {

            <button mat-button color="primary" (click)="toggleActive(row)">Enable</button>

          }

        </td>

      </ng-container>

      <tr mat-header-row *matHeaderRowDef="cols"></tr>

      <tr mat-row *matRowDef="let row; columns: cols"></tr>

      <tr class="empty-row" *matNoDataRow><td [attr.colspan]="cols.length"><mat-icon>location_city</mat-icon>No sites yet.</td></tr>

    </table>

    </div>

  `,

  styles: [`.abr-form-grid button { min-width: 140px; }`]

})

export class SiteListComponent implements OnInit {

  private readonly masterData = inject(MasterDataService);

  private readonly siteContext = inject(SiteContextService);

  private readonly toast = inject(ToastService);

  private readonly fb = inject(FormBuilder);

  private readonly dialog = inject(MatDialog);



  sites: Site[] = [];

  cols = ['siteName', 'address', 'isActive', 'actions'];

  form = this.fb.nonNullable.group({ siteName: ['', Validators.required], address: [''] });



  ngOnInit(): void { this.load(); }



  load(): void {

    this.siteContext.loadSites().subscribe({

      next: (r) => { if (r.success) this.sites = r.data; }

    });

  }



  save(): void {

    this.masterData.createSite(this.form.getRawValue()).subscribe({

      next: (r) => { if (r.success) { this.toast.success('Site created'); this.form.reset(); this.load(); } }

    });

  }



  edit(site: Site): void {

    const ref = this.dialog.open(SiteEditDialogComponent, {

      data: { siteName: site.siteName, address: site.address, startDate: site.startDate }

    });

    ref.afterClosed().subscribe((result) => {

      if (!result) return;

      this.masterData.updateSite(site.id, { ...result, isActive: site.isActive }).subscribe({

        next: (r) => { if (r.success) { this.toast.success('Site updated'); this.load(); } }

      });

    });

  }



  toggleActive(site: Site): void {

    const isActive = !site.isActive;

    this.masterData.updateSite(site.id, {

      siteName: site.siteName,

      address: site.address,

      startDate: site.startDate,

      isActive

    }).subscribe({

      next: (r) => {

        if (r.success) {

          this.toast.success(isActive ? 'Site enabled' : 'Site disabled');

          this.load();

        }

      }

    });

  }

}

