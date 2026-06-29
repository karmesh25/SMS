import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

export interface LicenseStatus {
  isValid: boolean;
  expiryDate: string;
  message: string;
}

export const LICENSE_EXPIRED_MESSAGE = 'License expired. Please contact your administrator.';

@Injectable({ providedIn: 'root' })
export class LicenseService {
  private readonly validSignal = signal(true);
  private readonly expiryDateSignal = signal('');
  private readonly messageSignal = signal('');

  readonly isValid = this.validSignal.asReadonly();
  readonly expiryDate = this.expiryDateSignal.asReadonly();
  readonly message = this.messageSignal.asReadonly();

  constructor(private readonly http: HttpClient) {}

  async checkStatus(): Promise<boolean> {
    try {
      const response = await firstValueFrom(
        this.http.get<ApiResponse<LicenseStatus>>(`${environment.apiUrl}/license/status`)
      );

      if (response.success && response.data) {
        this.validSignal.set(response.data.isValid);
        this.expiryDateSignal.set(response.data.expiryDate);
        this.messageSignal.set(response.data.message);
        return response.data.isValid;
      }
    } catch {
      this.validSignal.set(false);
      this.messageSignal.set(LICENSE_EXPIRED_MESSAGE);
    }

    return false;
  }
}

export function licenseInitializer(licenseService: LicenseService): () => Promise<void> {
  return async () => {
    const valid = await licenseService.checkStatus();
    if (!valid && !window.location.pathname.startsWith('/license-expired')) {
      window.location.href = '/license-expired';
    }
  };
}
