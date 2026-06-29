import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { LicenseService } from '../../../core/services/license.service';

@Component({
  selector: 'app-license-expired',
  standalone: true,
  imports: [MatCardModule, MatIconModule],
  template: `
    <div class="locked-page">
      <mat-card class="locked-card">
        <mat-icon class="lock-icon">event_busy</mat-icon>
        <h1>License Expired</h1>
        <p>{{ licenseService.message() }}</p>
        @if (licenseService.expiryDate()) {
          <div class="expiry">
            <strong>Expiry Date</strong>
            <span>{{ licenseService.expiryDate() }}</span>
          </div>
        }
        <p class="hint">Contact your administrator to renew the subscription license.</p>
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

    .expiry {
      margin: 1.5rem 0;
      text-align: center;
    }

    .expiry span {
      display: block;
      margin-top: 0.5rem;
      font-size: 1.1rem;
      color: #93c5fd;
    }

    .hint {
      color: #666;
      font-size: 0.875rem;
    }
  `]
})
export class LicenseExpiredComponent {
  readonly licenseService = inject(LicenseService);
}
