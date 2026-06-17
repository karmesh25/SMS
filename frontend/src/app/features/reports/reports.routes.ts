import { Routes } from '@angular/router';

export const REPORTS_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./reports-shell.component').then(m => m.ReportsShellComponent) }
];
