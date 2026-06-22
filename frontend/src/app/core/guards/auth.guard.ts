import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { DeviceService } from '../services/device.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const deviceService = inject(DeviceService);
  const router = inject(Router);

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
  const router = inject(Router);

  if (!deviceService.isVerified()) {
    return router.createUrlTree(['/unauthorized-device']);
  }

  if (authService.isLoggedIn()) {
    return router.createUrlTree(['/dashboard']);
  }

  return true;
};
