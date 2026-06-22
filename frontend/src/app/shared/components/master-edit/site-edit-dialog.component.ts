import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface SiteEditDialogData {
  siteName: string;
  address?: string;
  startDate?: string;
}

export interface SiteEditDialogResult {
  siteName: string;
  address?: string;
  startDate?: string;
}

@Component({
  selector: 'app-site-edit-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>Edit Site</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form">
        <mat-form-field appearance="outline">
          <mat-label>Site Name</mat-label>
          <input matInput formControlName="siteName" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Address</mat-label>
          <input matInput formControlName="address" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Start Date</mat-label>
          <input matInput type="date" formControlName="startDate" />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="close()">Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="save()">Save</button>
    </mat-dialog-actions>
  `,
  styles: [`.form { display:flex; flex-direction:column; gap:0.5rem; min-width:320px; }`]
})
export class SiteEditDialogComponent {
  readonly data = inject<SiteEditDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<SiteEditDialogComponent, SiteEditDialogResult | undefined>);
  private readonly fb = inject(FormBuilder);

  form = this.fb.nonNullable.group({
    siteName: [this.data.siteName, Validators.required],
    address: [this.data.address ?? ''],
    startDate: [this.data.startDate ?? '']
  });

  close(): void {
    this.dialogRef.close();
  }

  save(): void {
    if (this.form.invalid) return;
    const raw = this.form.getRawValue();
    this.dialogRef.close({
      siteName: raw.siteName,
      address: raw.address || undefined,
      startDate: raw.startDate || undefined
    });
  }
}
