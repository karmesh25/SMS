import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

export interface DeviceVerifyResult {
  result: string;
  fingerprintHash: string;
  isValid: boolean;
}

@Injectable({ providedIn: 'root' })
export class DeviceService {
  private readonly verifiedSignal = signal(false);
  private readonly fingerprintSignal = signal('');

  readonly isVerified = this.verifiedSignal.asReadonly();
  readonly fingerprintHash = this.fingerprintSignal.asReadonly();

  constructor(private readonly http: HttpClient) {}

  async verifyDevice(): Promise<boolean> {
    try {
      const response = await firstValueFrom(
        this.http.get<ApiResponse<DeviceVerifyResult>>(`${environment.apiUrl}/device/verify`)
      );

      if (response.success && response.data) {
        this.verifiedSignal.set(response.data.isValid);
        this.fingerprintSignal.set(response.data.fingerprintHash);
        return response.data.isValid;
      }
    } catch {
      this.verifiedSignal.set(false);
    }

    return false;
  }

  getLicenses() {
    return this.http.get<ApiResponse<unknown[]>>(`${environment.apiUrl}/device/licenses`);
  }

  authorizeDevice(payload: { deviceName: string; fingerprintHash: string }) {
    return this.http.post<ApiResponse<unknown>>(`${environment.apiUrl}/device/authorize`, payload);
  }

  toggleLicense(id: string) {
    return this.http.put<ApiResponse<unknown>>(`${environment.apiUrl}/device/licenses/${id}/toggle`, {});
  }
}

export function deviceInitializer(deviceService: DeviceService): () => Promise<void> {
  return () =>
    deviceService.verifyDevice().then((valid) => {
      if (!valid) {
        window.location.href = '/unauthorized-device';
      }
    });
}
