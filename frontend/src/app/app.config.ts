import { ApplicationConfig, APP_INITIALIZER, ErrorHandler, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { licenseInterceptor } from './core/interceptors/license.interceptor';
import { retryInterceptor } from './core/interceptors/retry.interceptor';
import { GlobalErrorHandlerService } from './core/handlers/global-error-handler.service';
import { DeviceService, deviceInitializer } from './core/services/device.service';
import { LicenseService, licenseInitializer } from './core/services/license.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimationsAsync(),
    provideHttpClient(withInterceptors([retryInterceptor, licenseInterceptor, authInterceptor])),
    { provide: ErrorHandler, useClass: GlobalErrorHandlerService },
    {
      provide: APP_INITIALIZER,
      useFactory: licenseInitializer,
      deps: [LicenseService],
      multi: true
    },
    {
      provide: APP_INITIALIZER,
      useFactory: deviceInitializer,
      deps: [DeviceService, LicenseService],
      multi: true
    }
  ]
};
