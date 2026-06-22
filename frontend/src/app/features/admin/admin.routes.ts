import { Routes } from '@angular/router';

import { roleGuard } from '../../core/guards/role.guard';

export const ADMIN_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'sites' },
  {
    path: 'users',
    data: { breadcrumb: 'User Management' },
    canActivate: [roleGuard('SuperAdmin')],
    loadComponent: () => import('./users/user-management.component').then(m => m.UserManagementComponent)
  },
  {
    path: 'devices',
    data: { breadcrumb: 'Device Licenses' },
    canActivate: [roleGuard('SuperAdmin')],
    loadComponent: () => import('./devices/device-admin.component').then(m => m.DeviceAdminComponent)
  },
  {
    path: 'sites',
    data: { breadcrumb: 'Sites' },
    canActivate: [roleGuard('SuperAdmin', 'Admin')],
    loadComponent: () => import('./sites/site-list.component').then(m => m.SiteListComponent)
  },
  {
    path: 'wings',
    data: { breadcrumb: 'Wings' },
    canActivate: [roleGuard('SuperAdmin', 'Admin')],
    loadComponent: () => import('./wings/wing-management.component').then(m => m.WingManagementComponent)
  },
  {
    path: 'ledgers',
    data: { breadcrumb: 'Ledgers' },
    canActivate: [roleGuard('SuperAdmin', 'Admin')],
    loadComponent: () => import('./ledgers/ledger-page.component').then(m => m.LedgerPageComponent)
  },
  {
    path: 'conditions',
    data: { breadcrumb: 'Conditions' },
    canActivate: [roleGuard('SuperAdmin', 'Admin')],
    loadComponent: () => import('./conditions/conditions-page.component').then(m => m.ConditionsPageComponent)
  },
  {
    path: 'banks',
    data: { breadcrumb: 'Bank Accounts' },
    canActivate: [roleGuard('SuperAdmin', 'Admin')],
    loadComponent: () => import('./banks/bank-accounts.component').then(m => m.BankAccountsComponent)
  },
  {
    path: 'brokers',
    data: { breadcrumb: 'Brokers' },
    canActivate: [roleGuard('SuperAdmin', 'Admin')],
    loadComponent: () => import('./brokers/broker-list.component').then(m => m.BrokerListComponent)
  }
];
