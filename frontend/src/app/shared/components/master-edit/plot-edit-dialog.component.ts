import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface PlotEditDialogData {
  plotName: string;
  plotCount: number;
}

export type PlotEditDialogResult = PlotEditDialogData;

@Component({
  selector: 'app-plot-edit-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>Edit Plot Scheme</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form">
        <mat-form-field appearance="outline"><mat-label>Plot Name</mat-label><input matInput formControlName="plotName" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Number of Plots</mat-label><input matInput type="number" formControlName="plotCount" /></mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="close()">Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="save()">Save</button>
    </mat-dialog-actions>
  `,
  styles: [`.form { display:grid; gap:0.75rem; min-width:min(320px, calc(100vw - 2rem)); }`]
})
export class PlotEditDialogComponent {
  readonly data = inject<PlotEditDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<PlotEditDialogComponent, PlotEditDialogResult | undefined>);
  private readonly fb = inject(FormBuilder);

  form = this.fb.nonNullable.group({
    plotName: [this.data.plotName, Validators.required],
    plotCount: [this.data.plotCount, [Validators.required, Validators.min(1)]]
  });

  close(): void {
    this.dialogRef.close();
  }

  save(): void {
    if (this.form.invalid) return;
    this.dialogRef.close(this.form.getRawValue());
  }
}
