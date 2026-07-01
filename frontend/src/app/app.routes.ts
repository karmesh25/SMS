import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';
import { permissionGuard } from './core/guards/permission.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'login',
    canActivate: [guestGuard],
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
    path: 'license-expired',
    loadComponent: () =>
      import('./features/auth/license-expired/license-expired.component').then(
        m => m.LicenseExpiredComponent
      )
  },
  {
    path: 'forbidden',
    loadComponent: () => import('./features/errors/error-page.component').then(m => m.ErrorPageComponent),
    data: {
      code: '403',
      title: 'Access Denied',
      message: 'You do not have permission to view this page.',
      icon: 'block'
    }
  },
  {
    path: 'not-found',
    loadComponent: () => import('./features/errors/error-page.component').then(m => m.ErrorPageComponent),
    data: {
      code: '404',
      title: 'Page Not Found',
      message: 'The page you are looking for does not exist.',
      icon: 'search_off'
    }
  },
  {
    path: 'server-error',
    loadComponent: () => import('./features/errors/error-page.component').then(m => m.ErrorPageComponent),
    data: {
      code: '500',
      title: 'Server Error',
      message: 'Something went wrong on our end. Please try again later.',
      icon: 'error_outline'
    }
  },
  {
    path: 'dashboard',
    canActivate: [authGuard, permissionGuard('dashboard', 'view')],
    data: { breadcrumb: 'Dashboard' },
    loadChildren: () => import('./features/dashboard/dashboard.routes').then(m => m.DASHBOARD_ROUTES)
  },
  {
    path: 'admin',
    canActivate: [authGuard],
    data: { breadcrumb: 'Admin' },
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES)
  },
  {
    path: 'booking',
    canActivate: [authGuard, permissionGuard('booking', 'view')],
    data: { breadcrumb: 'Booking' },
    loadChildren: () => import('./features/booking/booking.routes').then(m => m.BOOKING_ROUTES)
  },
  {
    path: 'accounting',
    canActivate: [authGuard],
    data: { breadcrumb: 'Accounting' },
    loadChildren: () => import('./features/accounting/accounting.routes').then(m => m.ACCOUNTING_ROUTES)
  },
  {
    path: 'vyaj',
    canActivate: [authGuard, permissionGuard('vyaj', 'view')],
    data: { breadcrumb: 'Vyaj Khata' },
    loadChildren: () => import('./features/vyaj/vyaj.routes').then(m => m.VYAJ_ROUTES)
  },
  {
    path: 'reports',
    canActivate: [authGuard, permissionGuard('reports', 'view')],
    data: { breadcrumb: 'Reports' },
    loadChildren: () => import('./features/reports/reports.routes').then(m => m.REPORTS_ROUTES)
  },
  { path: '**', redirectTo: 'not-found' }
];
