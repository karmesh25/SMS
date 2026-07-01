import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';

export const ADMIN_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'sites' },
  {
    path: 'users',
    data: { breadcrumb: 'User Management' },
    canActivate: [permissionGuard('users', 'view')],
    loadComponent: () => import('./users/users-admin-shell.component').then(m => m.UsersAdminShellComponent)
  },
  {
    path: 'devices',
    data: { breadcrumb: 'Device Licenses' },
    canActivate: [permissionGuard('devices', 'view')],
    loadComponent: () => import('./devices/device-admin.component').then(m => m.DeviceAdminComponent)
  },
  {
    path: 'sites',
    data: { breadcrumb: 'Sites' },
    canActivate: [permissionGuard('sites', 'view')],
    loadComponent: () => import('./sites/site-list.component').then(m => m.SiteListComponent)
  },
  {
    path: 'wings',
    data: { breadcrumb: 'Wings' },
    canActivate: [permissionGuard('wings', 'view')],
    loadComponent: () => import('./wings/wings-plots-shell.component').then(m => m.WingsPlotsShellComponent)
  },
  {
    path: 'ledgers',
    data: { breadcrumb: 'Ledgers' },
    canActivate: [permissionGuard('ledgers', 'view')],
    loadComponent: () => import('./ledgers/ledger-page.component').then(m => m.LedgerPageComponent)
  },
  {
    path: 'conditions',
    data: { breadcrumb: 'Conditions' },
    canActivate: [permissionGuard('conditions', 'view')],
    loadComponent: () => import('./conditions/conditions-page.component').then(m => m.ConditionsPageComponent)
  },
  {
    path: 'banks',
    data: { breadcrumb: 'Bank Accounts' },
    canActivate: [permissionGuard('banks', 'view')],
    loadComponent: () => import('./banks/bank-accounts.component').then(m => m.BankAccountsComponent)
  },
  {
    path: 'brokers',
    data: { breadcrumb: 'Brokers' },
    canActivate: [permissionGuard('brokers', 'view')],
    loadComponent: () => import('./brokers/broker-list.component').then(m => m.BrokerListComponent)
  }
];
