import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

export interface ConditionEditDialogData {
  conditionName: string;
  conditionType: string;
}

export type ConditionEditDialogResult = ConditionEditDialogData;

@Component({
  selector: 'app-condition-edit-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  template: `
    <h2 mat-dialog-title>Edit Condition</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form">
        <mat-form-field appearance="outline"><mat-label>Condition Name</mat-label><input matInput formControlName="conditionName" /></mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Type</mat-label>
          <mat-select formControlName="conditionType">
            <mat-option value="manual">Manual</mat-option>
            <mat-option value="auto">Auto</mat-option>
          </mat-select>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="close()">Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="save()">Save</button>
    </mat-dialog-actions>
  `,
  styles: [`.form { display:flex; flex-direction:column; gap:0.5rem; min-width:min(320px, calc(100vw - 2rem)); width:100%; }`]
})
export class ConditionEditDialogComponent {
  readonly data = inject<ConditionEditDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ConditionEditDialogComponent, ConditionEditDialogResult | undefined>);
  private readonly fb = inject(FormBuilder);

  form = this.fb.nonNullable.group({
    conditionName: [this.data.conditionName, Validators.required],
    conditionType: [this.data.conditionType, Validators.required]
  });

  close(): void {
    this.dialogRef.close();
  }

  save(): void {
    if (this.form.invalid) return;
    this.dialogRef.close(this.form.getRawValue());
  }
}
