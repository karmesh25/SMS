import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { IndianAmountDirective } from '../../directives/indian-amount.directive';

export interface BankEditDialogData {
  bankName: string;
  accountNo: string;
  ifscCode?: string;
  branch?: string;
  openingBalance: number;
  isActive: boolean;
}

export type BankEditDialogResult = BankEditDialogData;

@Component({
  selector: 'app-bank-edit-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, IndianAmountDirective],
  template: `
    <h2 mat-dialog-title>Edit Bank Account</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form">
        <mat-form-field appearance="outline"><mat-label>Bank Name</mat-label><input matInput formControlName="bankName" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Account No</mat-label><input matInput formControlName="accountNo" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>IFSC</mat-label><input matInput formControlName="ifscCode" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Branch</mat-label><input matInput formControlName="branch" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Opening Balance</mat-label><input matInput type="number" formControlName="openingBalance" appIndianAmount /></mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="close()">Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="save()">Save</button>
    </mat-dialog-actions>
  `,
  styles: [`.form { display:grid; grid-template-columns:1fr 1fr; gap:0.75rem; min-width:400px; }`]
})
export class BankEditDialogComponent {
  readonly data = inject<BankEditDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<BankEditDialogComponent, BankEditDialogResult | undefined>);
  private readonly fb = inject(FormBuilder);

  form = this.fb.nonNullable.group({
    bankName: [this.data.bankName, Validators.required],
    accountNo: [this.data.accountNo, Validators.required],
    ifscCode: [this.data.ifscCode ?? ''],
    branch: [this.data.branch ?? ''],
    openingBalance: [this.data.openingBalance],
    isActive: [this.data.isActive]
  });

  close(): void {
    this.dialogRef.close();
  }

  save(): void {
    if (this.form.invalid) return;
    const raw = this.form.getRawValue();
    this.dialogRef.close({
      ...raw,
      ifscCode: raw.ifscCode || undefined,
      branch: raw.branch || undefined
    });
  }
}
