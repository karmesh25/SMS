import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MasterDataService } from '../../../core/services/master-data.service';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmDialogComponent } from '../confirm-dialog/confirm-dialog.component';
import { MatDialog } from '@angular/material/dialog';
import { IndianAmountDirective } from '../../directives/indian-amount.directive';
import { NameEditDialogComponent } from './name-edit-dialog.component';

export interface ConditionItemsDialogData {
  conditionId: string;
  conditionName: string;
}

interface ConditionItemRow {
  id: string;
  milestoneName: string;
  percentage?: number;
  amount?: number;
  dueAfterDays: number;
  sortOrder: number;
}

@Component({
  selector: 'app-condition-items-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatTableModule, IndianAmountDirective],
  template: `
    <h2 mat-dialog-title>Items — {{ data.conditionName }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="row" (ngSubmit)="addItem()">
        <mat-form-field appearance="outline"><mat-label>Milestone</mat-label><input matInput formControlName="milestoneName" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>%</mat-label><input matInput type="number" formControlName="percentage" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Amount</mat-label><input matInput formControlName="amount" appIndianAmount /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Due Days</mat-label><input matInput type="number" formControlName="dueAfterDays" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Order</mat-label><input matInput type="number" formControlName="sortOrder" /></mat-form-field>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">Add</button>
      </form>

      <table mat-table [dataSource]="items" class="mat-elevation-z1">
        <ng-container matColumnDef="milestoneName"><th mat-header-cell *matHeaderCellDef>Milestone</th><td mat-cell *matCellDef="let row">{{ row.milestoneName }}</td></ng-container>
        <ng-container matColumnDef="percentage"><th mat-header-cell *matHeaderCellDef>%</th><td mat-cell *matCellDef="let row">{{ row.percentage ?? '-' }}</td></ng-container>
        <ng-container matColumnDef="amount"><th mat-header-cell *matHeaderCellDef>Amount</th><td mat-cell *matCellDef="let row">{{ row.amount ?? '-' }}</td></ng-container>
        <ng-container matColumnDef="dueAfterDays"><th mat-header-cell *matHeaderCellDef>Due Days</th><td mat-cell *matCellDef="let row">{{ row.dueAfterDays }}</td></ng-container>
        <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef></th>
          <td mat-cell *matCellDef="let row">
            <button mat-button (click)="editItem(row)">Edit</button>
            <button mat-button color="warn" (click)="deleteItem(row)">Delete</button>
          </td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="cols"></tr>
        <tr mat-row *matRowDef="let row; columns: cols"></tr>
      </table>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="close()">Close</button>
    </mat-dialog-actions>
  `,
  styles: [`.row { display:flex; gap:0.5rem; flex-wrap:wrap; margin-bottom:1rem; align-items:center; } table { width:100%; }`]
})
export class ConditionItemsDialogComponent implements OnInit {
  readonly data = inject<ConditionItemsDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ConditionItemsDialogComponent>);
  private readonly masterData = inject(MasterDataService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(MatDialog);
  private readonly fb = inject(FormBuilder);

  items: ConditionItemRow[] = [];
  cols = ['milestoneName', 'percentage', 'amount', 'dueAfterDays', 'actions'];

  form = this.fb.nonNullable.group({
    milestoneName: ['', Validators.required],
    percentage: [null as number | null],
    amount: [null as number | null],
    dueAfterDays: [30, Validators.min(0)],
    sortOrder: [1, Validators.min(0)]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.masterData.getConditionItems(this.data.conditionId).subscribe({
      next: (r) => { if (r.success) this.items = r.data as ConditionItemRow[]; }
    });
  }

  addItem(): void {
    if (this.form.invalid) return;
    const raw = this.form.getRawValue();
    this.masterData.addConditionItem(this.data.conditionId, {
      milestoneName: raw.milestoneName,
      percentage: raw.percentage ?? undefined,
      amount: raw.amount ?? undefined,
      dueAfterDays: raw.dueAfterDays,
      sortOrder: raw.sortOrder
    }).subscribe({
      next: (r) => {
        if (r.success) {
          this.toast.success('Item added');
          this.form.reset({ dueAfterDays: 30, sortOrder: this.items.length + 1 });
          this.load();
        }
      }
    });
  }

  editItem(row: ConditionItemRow): void {
    const ref = this.dialog.open(NameEditDialogComponent, {
      data: { title: 'Edit Milestone', label: 'Milestone Name', value: row.milestoneName }
    });
    ref.afterClosed().subscribe((result) => {
      if (!result) return;
      this.masterData.updateConditionItem(row.id, {
        milestoneName: result.value,
        percentage: row.percentage,
        amount: row.amount,
        dueAfterDays: row.dueAfterDays,
        sortOrder: row.sortOrder
      }).subscribe({
        next: (r) => { if (r.success) { this.toast.success('Item updated'); this.load(); } }
      });
    });
  }

  deleteItem(row: ConditionItemRow): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Item', message: `Delete milestone "${row.milestoneName}"?` }
    });
    ref.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;
      this.masterData.deleteConditionItem(row.id).subscribe({
        next: (r) => { if (r.success) { this.toast.success('Item deleted'); this.load(); } }
      });
    });
  }

  close(): void {
    this.dialogRef.close(true);
  }
}
