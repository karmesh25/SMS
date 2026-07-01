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
import { PlotEditDialogComponent } from '../../../shared/components/master-edit/plot-edit-dialog.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

interface PlotRow {
  id: string;
  wingName: string;
  flatsPerFloor: number;
  flatCount: number;
}

@Component({
  selector: 'app-plot-management',
  standalone: true,
  imports: [ReactiveFormsModule, MatTableModule, MatIconModule, MatButtonModule, MatFormFieldModule, MatInputModule, PageHeaderComponent],
  template: `
    <app-page-header title="Plots" subtitle="Create plot schemes and auto-generate plot units"></app-page-header>

    <form [formGroup]="form" class="form-grid" (ngSubmit)="save()">
      <mat-form-field appearance="outline"><mat-label>Plot Scheme Name</mat-label><input matInput formControlName="plotName" /></mat-form-field>
      <mat-form-field appearance="outline"><mat-label>Number of Plots</mat-label><input matInput type="number" formControlName="plotCount" /></mat-form-field>
      <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || !siteId">Add Plot Scheme</button>
    </form>

    <div class="abr-scroll-x">
      <table mat-table [dataSource]="plots" class="mat-elevation-z1 abr-table sticky-header">
        <ng-container matColumnDef="plotName"><th mat-header-cell *matHeaderCellDef>Scheme</th><td mat-cell *matCellDef="let row">{{ row.wingName }}</td></ng-container>
        <ng-container matColumnDef="plotCount"><th mat-header-cell *matHeaderCellDef>Plots</th><td mat-cell *matCellDef="let row">{{ row.flatsPerFloor }}</td></ng-container>
        <ng-container matColumnDef="flatCount"><th mat-header-cell *matHeaderCellDef>Total Units</th><td mat-cell *matCellDef="let row">{{ row.flatCount }}</td></ng-container>
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
    </div>
  `,
  styles: [`.form-grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(160px,1fr)); gap:1rem; margin-bottom:1.5rem; align-items:center; } table { width:100%; }`]
})
export class PlotManagementComponent implements OnInit {
  private readonly masterData = inject(MasterDataService);
  private readonly siteContext = inject(SiteContextService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);

  plots: PlotRow[] = [];
  cols = ['plotName', 'plotCount', 'flatCount', 'actions'];
  siteId: string | null = null;

  form = this.fb.nonNullable.group({
    plotName: ['', Validators.required],
    plotCount: [10, [Validators.required, Validators.min(1)]]
  });

  constructor() {
    effect(() => {
      this.siteId = this.siteContext.activeSiteId();
      if (this.siteId) this.loadPlots();
    });
  }

  ngOnInit(): void {
    this.siteContext.loadSites().subscribe();
    this.siteId = this.siteContext.activeSiteId();
    if (this.siteId) this.loadPlots();
  }

  loadPlots(): void {
    if (!this.siteId) return;
    this.masterData.getPlots(this.siteId).subscribe({
      next: (r) => { if (r.success) this.plots = r.data as PlotRow[]; }
    });
  }

  save(): void {
    if (!this.siteId) return;
    const raw = this.form.getRawValue();
    this.masterData.createPlot({
      siteId: this.siteId,
      plotName: raw.plotName,
      plotCount: raw.plotCount
    }).subscribe({
      next: (r) => {
        if (r.success) {
          this.toast.success('Plot scheme created');
          this.form.patchValue({ plotName: '', plotCount: 10 });
          this.loadPlots();
        }
      },
      error: (err) => this.toast.error(err.error?.message ?? 'Failed to create plot scheme')
    });
  }

  edit(row: PlotRow): void {
    const ref = this.dialog.open(PlotEditDialogComponent, {
      data: {
        plotName: row.wingName,
        plotCount: row.flatsPerFloor
      }
    });
    ref.afterClosed().subscribe((result) => {
      if (!result) return;
      this.masterData.updatePlot(row.id, {
        plotName: result.plotName,
        plotCount: result.plotCount
      }).subscribe({
        next: (r) => { if (r.success) { this.toast.success('Plot updated'); this.loadPlots(); } },
        error: (err) => this.toast.error(err.error?.message ?? 'Failed to update plot')
      });
    });
  }

  remove(row: PlotRow): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Plot Scheme', message: `Delete "${row.wingName}" and all its plot units?` }
    });
    ref.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;
      this.masterData.deletePlot(row.id).subscribe({
        next: (r) => { if (r.success) { this.toast.success('Plot deleted'); this.loadPlots(); } },
        error: (err) => this.toast.error(err.error?.message ?? 'Failed to delete plot')
      });
    });
  }
}
