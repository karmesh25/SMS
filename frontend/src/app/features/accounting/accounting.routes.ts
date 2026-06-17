import { Routes } from '@angular/router';

export const ACCOUNTING_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./accounting-shell.component').then(m => m.AccountingShellComponent) }
];
