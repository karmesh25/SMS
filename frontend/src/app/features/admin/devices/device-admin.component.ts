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

    <section class="abr-panel this-device">
      <h2 class="abr-panel__title"><mat-icon>fingerprint</mat-icon>This Computer's Device Hash</h2>
      @if (currentHash) {
        <div class="hash-row">
          <code class="hash">{{ currentHash }}</code>
          <div class="hash-actions">
            <button mat-stroked-button type="button" (click)="copyHash()"><mat-icon>content_copy</mat-icon> Copy</button>
            <button mat-flat-button color="primary" type="button" (click)="useThisPc()"><mat-icon>add_task</mat-icon> Use to authorize</button>
          </div>
        </div>
      } @else {
        <p class="hash-empty">Reading this device's hardware fingerprint…</p>
      }
    </section>

    <section class="abr-panel">
      <h2 class="abr-panel__title"><mat-icon>add_circle</mat-icon>Authorize Device</h2>
      <form [formGroup]="form" class="abr-form-grid" (ngSubmit)="authorize()">
        <mat-form-field appearance="outline">
          <mat-label>Device Name</mat-label>
          <input matInput formControlName="deviceName" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="fingerprint-field">
          <mat-label>Fingerprint Hash</mat-label>
          <input matInput formControlName="fingerprintHash" />
        </mat-form-field>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid"><mat-icon>add</mat-icon> Authorize</button>
      </form>
    </section>

    <div class="abr-table-card">

    <table mat-table [dataSource]="devices" class="abr-table sticky-header">
      <ng-container matColumnDef="deviceName">
        <th mat-header-cell *matHeaderCellDef>Device</th>
        <td mat-cell *matCellDef="let row">{{ row.deviceName }}</td>
      </ng-container>
      <ng-container matColumnDef="fingerprintHash">
        <th mat-header-cell *matHeaderCellDef>Fingerprint</th>
        <td mat-cell *matCellDef="let row">{{ row.fingerprintHash }}</td>
      </ng-container>
      <ng-container matColumnDef="isActive">
        <th mat-header-cell *matHeaderCellDef>Status</th>
        <td mat-cell *matCellDef="let row">
          <span class="abr-chip" [class.abr-chip--success]="row.isActive" [class.abr-chip--danger]="!row.isActive">
            {{ row.isActive ? 'Authorized' : 'Revoked' }}
          </span>
        </td>
      </ng-container>
      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef>Actions</th>
        <td mat-cell *matCellDef="let row">
          <button mat-button (click)="toggle(row.id)">Toggle</button>
        </td>
      </ng-container>

      <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
      <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
      <tr class="empty-row" *matNoDataRow><td [attr.colspan]="displayedColumns.length"><mat-icon>devices</mat-icon>No devices authorized yet.</td></tr>
    </table>
    </div>
  `,
  styles: [`
    .abr-form-grid button { min-width: 140px; }

    .fingerprint-field {
      min-width: 280px;
    }

    .hash-row {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
    }
    .hash {
      font-family: 'Consolas', 'Courier New', monospace;
      font-size: 0.85rem;
      word-break: break-all;
      color: var(--abr-primary-strong);
      background: var(--abr-surface-2);
      border: 1px solid var(--abr-border);
      border-radius: var(--abr-radius-sm);
      padding: 0.5rem 0.75rem;
      flex: 1;
      min-width: 240px;
    }
    .hash-actions { display: flex; gap: 0.5rem; flex-wrap: wrap; }
    .hash-empty { color: var(--abr-text-muted); margin: 0; }
  `]
})
export class DeviceAdminComponent implements OnInit {
  private readonly deviceService = inject(DeviceService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  devices: DeviceLicenseRow[] = [];
  displayedColumns = ['deviceName', 'fingerprintHash', 'isActive', 'actions'];
  currentHash = '';

  readonly form = this.fb.nonNullable.group({
    deviceName: ['', Validators.required],
    fingerprintHash: ['', [Validators.required, Validators.minLength(64), Validators.maxLength(64)]]
  });

  ngOnInit(): void {
    this.loadDevices();
    void this.deviceService.verifyDevice().then(() => {
      this.currentHash = this.deviceService.fingerprintHash();
    });
  }

  copyHash(): void {
    if (!this.currentHash) {
      return;
    }
    navigator.clipboard?.writeText(this.currentHash).then(
      () => this.toast.success('Device hash copied to clipboard'),
      () => this.toast.error('Could not copy to clipboard')
    );
  }

  useThisPc(): void {
    if (!this.currentHash) {
      return;
    }
    this.form.patchValue({
      fingerprintHash: this.currentHash,
      deviceName: this.form.getRawValue().deviceName || 'This PC'
    });
    this.toast.success("Filled this PC's hash — set a name, then click Authorize");
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
