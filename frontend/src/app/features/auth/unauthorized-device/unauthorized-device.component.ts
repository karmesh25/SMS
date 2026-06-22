import { Component, inject, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { DeviceService } from '../../../core/services/device.service';

@Component({
  selector: 'app-unauthorized-device',
  standalone: true,
  imports: [MatCardModule, MatIconModule],
  template: `
    <div class="locked-page">
      <mat-card class="locked-card">
        <mat-icon class="lock-icon">lock</mat-icon>
        <h1>Unauthorized Device</h1>
        <p>This application is licensed for a specific device. Contact your administrator.</p>
        <div class="fingerprint">
          <strong>Device Fingerprint</strong>
          <code>{{ deviceService.fingerprintHash() || 'Unavailable' }}</code>
        </div>
        <p class="hint">Provide this fingerprint to your Super Admin for device authorization.</p>
      </mat-card>
    </div>
  `,
  styles: [`
    .locked-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: #1f2937;
      color: #fff;
      padding: 1rem;
    }

    .locked-card {
      max-width: 640px;
      text-align: center;
      padding: 2rem;
    }

    .lock-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      color: #c0392b;
      margin-bottom: 1rem;
    }

    .fingerprint {
      margin: 1.5rem 0;
      text-align: left;
    }

    code {
      display: block;
      margin-top: 0.5rem;
      padding: 0.75rem;
      background: #111827;
      color: #93c5fd;
      word-break: break-all;
      border-radius: 4px;
      font-size: 0.85rem;
    }

    .hint {
      color: #666;
      font-size: 0.875rem;
    }
  `]
})
export class UnauthorizedDeviceComponent implements OnInit {
  readonly deviceService = inject(DeviceService);

  ngOnInit(): void {
    void this.deviceService.verifyDevice();
  }
}
