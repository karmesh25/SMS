import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  { path: 'login', loadComponent: () => import('./login/login.component').then(m => m.LoginComponent) },
  {
    path: 'unauthorized-device',
    loadComponent: () =>
      import('./unauthorized-device/unauthorized-device.component').then(m => m.UnauthorizedDeviceComponent)
  },
  { path: '', pathMatch: 'full', redirectTo: 'login' }
];
