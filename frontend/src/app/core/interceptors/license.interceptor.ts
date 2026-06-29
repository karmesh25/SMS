import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { LICENSE_EXPIRED_MESSAGE } from '../services/license.service';

let redirectingToLicenseExpired = false;

function redirectToLicenseExpired(router: Router): void {
  if (redirectingToLicenseExpired) {
    return;
  }

  redirectingToLicenseExpired = true;
  void router.navigate(['/license-expired']).finally(() => {
    redirectingToLicenseExpired = false;
  });
}

function isLicenseExpiredError(error: HttpErrorResponse): boolean {
  if (error.status !== 403) {
    return false;
  }

  const message = error.error?.message;
  return typeof message === 'string' && message === LICENSE_EXPIRED_MESSAGE;
}

export const licenseInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (isLicenseExpiredError(error) && !req.url.includes('/license/status')) {
        redirectToLicenseExpired(router);
      }

      return throwError(() => error);
    })
  );
};
