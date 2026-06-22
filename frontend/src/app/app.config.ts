import { ApplicationConfig, APP_INITIALIZER, ErrorHandler, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { retryInterceptor } from './core/interceptors/retry.interceptor';
import { GlobalErrorHandlerService } from './core/handlers/global-error-handler.service';
import { DeviceService, deviceInitializer } from './core/services/device.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimationsAsync(),
    provideHttpClient(withInterceptors([retryInterceptor, authInterceptor])),
    { provide: ErrorHandler, useClass: GlobalErrorHandlerService },
    {
      provide: APP_INITIALIZER,
      useFactory: deviceInitializer,
      deps: [DeviceService],
      multi: true
    }
  ]
};
