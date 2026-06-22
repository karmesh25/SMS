import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, retry, throwError, timer } from 'rxjs';

const RETRY_COUNT = 2;
const RETRY_DELAY_MS = 1000;
const NO_RETRY_STATUSES = new Set([401, 403, 422]);

export const retryInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    retry({
      count: RETRY_COUNT,
      delay: (error, retryIndex) => {
        if (error instanceof HttpErrorResponse && NO_RETRY_STATUSES.has(error.status)) {
          throw error;
        }
        return timer(RETRY_DELAY_MS * retryIndex);
      }
    })
  );

export const navigationCancelInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (router.getCurrentNavigation()?.extras?.replaceUrl) {
        return throwError(() => error);
      }
      return throwError(() => error);
    })
  );
};
