import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface NameEditDialogData {
  title: string;
  label?: string;
  value: string;
}

export interface NameEditDialogResult {
  value: string;
}

@Component({
  selector: 'app-name-edit-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>{{ data.label ?? 'Name' }}</mat-label>
          <input matInput formControlName="value" />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="close()">Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="save()">Save</button>
    </mat-dialog-actions>
  `,
  styles: [`.full-width { width: 100%; min-width: 280px; }`]
})
export class NameEditDialogComponent {
  readonly data = inject<NameEditDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<NameEditDialogComponent, NameEditDialogResult | undefined>);
  private readonly fb = inject(FormBuilder);

  form = this.fb.nonNullable.group({
    value: [this.data.value, Validators.required]
  });

  close(): void {
    this.dialogRef.close();
  }

  save(): void {
    if (this.form.invalid) return;
    this.dialogRef.close({ value: this.form.getRawValue().value });
  }
}
