import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { PermissionLevel } from '../models/permission.model';

export const permissionGuard = (moduleKey: string, level: PermissionLevel = 'view'): CanActivateFn => () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isLoggedIn()) {
    return router.createUrlTree(['/login']);
  }

  if (!authService.hasPermission(moduleKey, level)) {
    return router.createUrlTree(['/forbidden']);
  }

  return true;
};
