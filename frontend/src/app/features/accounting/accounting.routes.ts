import { Routes } from '@angular/router';

export const ACCOUNTING_ROUTES: Routes = [
  {
    path: '',
    data: { breadcrumb: 'Daily Entry' },
    loadComponent: () => import('./daily-entry/daily-entry.component').then(m => m.DailyEntryComponent)
  },
  {
    path: 'dastavej',
    data: { breadcrumb: 'Dastavej Entry' },
    loadComponent: () => import('./dastavej/dastavej-entry.component').then(m => m.DastavejEntryComponent)
  }
];
