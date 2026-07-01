import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';

export const ACCOUNTING_ROUTES: Routes = [
  {
    path: '',
    data: { breadcrumb: 'Daily Entry' },
    canActivate: [permissionGuard('daily_entry', 'view')],
    loadComponent: () => import('./daily-entry/daily-entry.component').then(m => m.DailyEntryComponent)
  },
  {
    path: 'dastavej',
    data: { breadcrumb: 'Dastavej Entry' },
    canActivate: [permissionGuard('dastavej', 'view')],
    loadComponent: () => import('./dastavej/dastavej-entry.component').then(m => m.DastavejEntryComponent)
  }
];
