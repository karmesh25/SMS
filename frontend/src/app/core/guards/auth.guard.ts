import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { DeviceService } from '../services/device.service';
import { LicenseService } from '../services/license.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const deviceService = inject(DeviceService);
  const licenseService = inject(LicenseService);
  const router = inject(Router);

  if (!licenseService.isValid()) {
    return router.createUrlTree(['/license-expired']);
  }

  if (!deviceService.isVerified()) {
    return router.createUrlTree(['/unauthorized-device']);
  }

  if (!authService.isLoggedIn()) {
    return router.createUrlTree(['/login']);
  }

  return true;
};

export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const deviceService = inject(DeviceService);
  const licenseService = inject(LicenseService);
  const router = inject(Router);

  if (!licenseService.isValid()) {
    return router.createUrlTree(['/license-expired']);
  }

  if (!deviceService.isVerified()) {
    return router.createUrlTree(['/unauthorized-device']);
  }

  if (authService.isLoggedIn()) {
    return router.createUrlTree(['/dashboard']);
  }

  return true;
};
