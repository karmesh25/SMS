import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface WingEditDialogData {
  wingName: string;
  floors: number;
  flatsPerFloor: number;
  shops: number;
  isBungalow: boolean;
}

export type WingEditDialogResult = WingEditDialogData;

@Component({
  selector: 'app-wing-edit-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatCheckboxModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>Edit Wing</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form">
        <mat-form-field appearance="outline"><mat-label>Wing Name</mat-label><input matInput formControlName="wingName" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Floors</mat-label><input matInput type="number" formControlName="floors" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Flats Per Floor</mat-label><input matInput type="number" formControlName="flatsPerFloor" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Shops</mat-label><input matInput type="number" formControlName="shops" /></mat-form-field>
        <mat-checkbox formControlName="isBungalow">Is Bungalow</mat-checkbox>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="close()">Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="save()">Save</button>
    </mat-dialog-actions>
  `,
  styles: [`.form { display:grid; grid-template-columns:1fr 1fr; gap:0.75rem; min-width:400px; }`]
})
export class WingEditDialogComponent {
  readonly data = inject<WingEditDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<WingEditDialogComponent, WingEditDialogResult | undefined>);
  private readonly fb = inject(FormBuilder);

  form = this.fb.nonNullable.group({
    wingName: [this.data.wingName, Validators.required],
    floors: [this.data.floors, [Validators.required, Validators.min(1)]],
    flatsPerFloor: [this.data.flatsPerFloor, [Validators.required, Validators.min(1)]],
    shops: [this.data.shops, Validators.min(0)],
    isBungalow: [this.data.isBungalow]
  });

  close(): void {
    this.dialogRef.close();
  }

  save(): void {
    if (this.form.invalid) return;
    this.dialogRef.close(this.form.getRawValue());
  }
}
