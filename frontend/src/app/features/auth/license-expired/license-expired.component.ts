import { Component, inject, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { LICENSE_EXPIRED_MESSAGE, LicenseService } from '../../../core/services/license.service';

@Component({
  selector: 'app-license-expired',
  standalone: true,
  imports: [MatCardModule, MatIconModule],
  template: `
    <div class="locked-page">
      <mat-card class="locked-card">
        <mat-icon class="lock-icon">event_busy</mat-icon>
        <h1>License Expired</h1>
        <p class="message">{{ licenseService.message() || expiredMessage }}</p>
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
      color: #1f2937;
    }

    .locked-card h1 {
      color: #111827;
      margin: 0 0 0.75rem;
      font-size: 1.75rem;
    }

    .locked-card p {
      color: #374151;
      margin: 0.5rem 0;
      font-size: 1rem;
      line-height: 1.5;
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

    .expiry strong {
      color: #111827;
    }

    .expiry span {
      display: block;
      margin-top: 0.5rem;
      font-size: 1.1rem;
      font-weight: 600;
      color: #1d4ed8;
    }

    .locked-card p.message {
      color: #1e40af;
      font-weight: 500;
    }

    .hint {
      color: #4b5563;
      font-size: 0.875rem;
    }
  `]
})
export class LicenseExpiredComponent implements OnInit {
  readonly licenseService = inject(LicenseService);
  readonly expiredMessage = LICENSE_EXPIRED_MESSAGE;

  ngOnInit(): void {
    void this.licenseService.checkStatus();
  }
}
