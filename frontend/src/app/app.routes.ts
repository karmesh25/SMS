import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'unauthorized-device',
    loadComponent: () =>
      import('./features/auth/unauthorized-device/unauthorized-device.component').then(
        m => m.UnauthorizedDeviceComponent
      )
  },
  {
    path: 'dashboard',
    loadChildren: () => import('./features/dashboard/dashboard.routes').then(m => m.DASHBOARD_ROUTES)
  },
  {
    path: 'admin',
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES)
  },
  {
    path: 'booking',
    loadChildren: () => import('./features/booking/booking.routes').then(m => m.BOOKING_ROUTES)
  },
  {
    path: 'accounting',
    loadChildren: () => import('./features/accounting/accounting.routes').then(m => m.ACCOUNTING_ROUTES)
  },
  {
    path: 'reports',
    loadChildren: () => import('./features/reports/reports.routes').then(m => m.REPORTS_ROUTES)
  },
  { path: '**', redirectTo: 'dashboard' }
];
