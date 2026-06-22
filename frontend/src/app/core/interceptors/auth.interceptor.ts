import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';

import { inject } from '@angular/core';

import { Router } from '@angular/router';

import { catchError, from, switchMap, throwError } from 'rxjs';

import { AuthService } from '../services/auth.service';



let refreshInFlight: Promise<boolean> | null = null;

let redirectingToLogin = false;



function refreshOnce(authService: AuthService): Promise<boolean> {

  if (!refreshInFlight) {

    refreshInFlight = authService.tryRefreshToken().finally(() => {

      refreshInFlight = null;

    });

  }



  return refreshInFlight;

}



function redirectToLogin(router: Router, authService: AuthService): void {

  if (redirectingToLogin) {

    return;

  }



  redirectingToLogin = true;

  authService.clearSession();

  void router.navigate(['/login']).finally(() => {

    redirectingToLogin = false;

  });

}



export const authInterceptor: HttpInterceptorFn = (req, next) => {

  const authService = inject(AuthService);

  const router = inject(Router);

  const token = authService.getToken();



  const authReq = token

    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })

    : req;



  return next(authReq).pipe(

    catchError((error: HttpErrorResponse) => {

      if (error.status !== 401 || req.url.includes('/auth/login') || req.url.includes('/auth/refresh')) {
        if (error.status === 500 && !req.url.includes('/auth/')) {
          void router.navigate(['/server-error']);
        }
        return throwError(() => error);
      }



      if (!authService.hasRefreshToken()) {

        redirectToLogin(router, authService);

        return throwError(() => error);

      }



      return from(refreshOnce(authService)).pipe(

        switchMap((refreshed) => {

          if (!refreshed) {

            redirectToLogin(router, authService);

            return throwError(() => error);

          }



          const newToken = authService.getToken();

          if (!newToken) {

            redirectToLogin(router, authService);

            return throwError(() => error);

          }



          const retryReq = req.clone({

            setHeaders: { Authorization: `Bearer ${newToken}` }

          });

          return next(retryReq);

        })

      );

    })

  );

};


