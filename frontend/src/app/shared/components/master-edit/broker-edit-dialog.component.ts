import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface BrokerEditDialogData {
  name: string;
  contactNo?: string;
  contactNo2?: string;
  address?: string;
}

export type BrokerEditDialogResult = BrokerEditDialogData;

@Component({
  selector: 'app-broker-edit-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>Edit Broker</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form">
        <mat-form-field appearance="outline"><mat-label>Name</mat-label><input matInput formControlName="name" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Contact</mat-label><input matInput formControlName="contactNo" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Contact 2</mat-label><input matInput formControlName="contactNo2" /></mat-form-field>
        <mat-form-field appearance="outline" class="full"><mat-label>Address</mat-label><input matInput formControlName="address" /></mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="close()">Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="save()">Save</button>
    </mat-dialog-actions>
  `,
  styles: [`.form { display:grid; grid-template-columns:1fr 1fr; gap:0.75rem; min-width:min(400px, calc(100vw - 2rem)); width:100%; } .full { grid-column:1 / -1; } @media (max-width: 599px) { .form { grid-template-columns: 1fr; } }`]
})
export class BrokerEditDialogComponent {
  readonly data = inject<BrokerEditDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<BrokerEditDialogComponent, BrokerEditDialogResult | undefined>);
  private readonly fb = inject(FormBuilder);

  form = this.fb.nonNullable.group({
    name: [this.data.name, Validators.required],
    contactNo: [this.data.contactNo ?? ''],
    contactNo2: [this.data.contactNo2 ?? ''],
    address: [this.data.address ?? '']
  });

  close(): void {
    this.dialogRef.close();
  }

  save(): void {
    if (this.form.invalid) return;
    const raw = this.form.getRawValue();
    this.dialogRef.close({
      name: raw.name,
      contactNo: raw.contactNo || undefined,
      contactNo2: raw.contactNo2 || undefined,
      address: raw.address || undefined
    });
  }
}
