import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { DeviceService } from '../../../core/services/device.service';
import { ToastService } from '../../../core/services/toast.service';

interface DeviceLicenseRow {
  id: string;
  deviceName: string;
  fingerprintHash: string;
  isActive: boolean;
  lastVerifiedAt?: string;
}

@Component({
  selector: 'app-device-admin',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatTableModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSlideToggleModule,
    PageHeaderComponent
  ],
  template: `
    <app-page-header title="Device Licenses" subtitle="Authorize hardware devices"></app-page-header>

    <form [formGroup]="form" class="form-row" (ngSubmit)="authorize()">
      <mat-form-field appearance="outline">
        <mat-label>Device Name</mat-label>
        <input matInput formControlName="deviceName" />
      </mat-form-field>
      <mat-form-field appearance="outline" class="fingerprint-field">
        <mat-label>Fingerprint Hash</mat-label>
        <input matInput formControlName="fingerprintHash" />
      </mat-form-field>
      <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">Authorize</button>
    </form>

    <div class="abr-scroll-x">

    <table mat-table [dataSource]="devices" class="mat-elevation-z1 abr-table sticky-header">
      <ng-container matColumnDef="deviceName">
        <th mat-header-cell *matHeaderCellDef>Device</th>
        <td mat-cell *matCellDef="let row">{{ row.deviceName }}</td>
      </ng-container>
      <ng-container matColumnDef="fingerprintHash">
        <th mat-header-cell *matHeaderCellDef>Fingerprint</th>
        <td mat-cell *matCellDef="let row">{{ row.fingerprintHash }}</td>
      </ng-container>
      <ng-container matColumnDef="isActive">
        <th mat-header-cell *matHeaderCellDef>Active</th>
        <td mat-cell *matCellDef="let row">{{ row.isActive ? 'Yes' : 'No' }}</td>
      </ng-container>
      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef>Actions</th>
        <td mat-cell *matCellDef="let row">
          <button mat-button (click)="toggle(row.id)">Toggle</button>
        </td>
      </ng-container>

      <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
      <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
      <tr class="empty-row" *matNoDataRow><td [attr.colspan]="displayedColumns.length"><mat-icon>info_outline</mat-icon>No records found.</td></tr>
    </table>
    </div>
  `,
  styles: [`
    .form-row {
      display: flex;
      gap: 1rem;
      align-items: center;
      margin-bottom: 1.5rem;
      flex-wrap: wrap;
    }

    .fingerprint-field {
      flex: 1;
      min-width: 280px;
    }

    table {
      width: 100%;
    }
  `]
})
export class DeviceAdminComponent implements OnInit {
  private readonly deviceService = inject(DeviceService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  devices: DeviceLicenseRow[] = [];
  displayedColumns = ['deviceName', 'fingerprintHash', 'isActive', 'actions'];

  readonly form = this.fb.nonNullable.group({
    deviceName: ['', Validators.required],
    fingerprintHash: ['', [Validators.required, Validators.minLength(64), Validators.maxLength(64)]]
  });

  ngOnInit(): void {
    this.loadDevices();
  }

  loadDevices(): void {
    this.deviceService.getLicenses().subscribe({
      next: (response) => {
        if (response.success) {
          this.devices = response.data as DeviceLicenseRow[];
        }
      }
    });
  }

  authorize(): void {
    if (this.form.invalid) {
      return;
    }

    this.deviceService.authorizeDevice(this.form.getRawValue()).subscribe({
      next: (response) => {
        if (response.success) {
          this.toast.success('Device authorized');
          this.form.reset();
          this.loadDevices();
        }
      },
      error: () => this.toast.error('Failed to authorize device')
    });
  }

  toggle(id: string): void {
    this.deviceService.toggleLicense(id).subscribe({
      next: (response) => {
        if (response.success) {
          this.toast.success('Device status updated');
          this.loadDevices();
        }
      }
    });
  }
}
