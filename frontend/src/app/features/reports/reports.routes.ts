import { Routes } from '@angular/router';

export const REPORTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./reports-shell.component').then(m => m.ReportsShellComponent)
  },
  {
    path: 'all-entry',
    data: { breadcrumb: 'All Daily Entry' },
    loadComponent: () => import('./all-entry/report-all-entry.component').then(m => m.ReportAllEntryComponent)
  },
  {
    path: 'balance-sheet',
    data: { breadcrumb: 'Balance Sheet' },
    loadComponent: () => import('./balance-sheet/balance-sheet.component').then(m => m.BalanceSheetComponent)
  },
  {
    path: 'till-date',
    data: { breadcrumb: 'Till Date' },
    loadComponent: () => import('./till-date/till-date-report.component').then(m => m.TillDateReportComponent)
  },
  {
    path: 'monthwise',
    data: { breadcrumb: 'Monthwise Totals' },
    loadComponent: () => import('./monthwise/monthwise-report.component').then(m => m.MonthwiseReportComponent)
  },
  {
    path: 'bank-statement',
    data: { breadcrumb: 'Bank Statement' },
    loadComponent: () => import('./bank-statement/bank-statement.component').then(m => m.BankStatementComponent)
  },
  {
    path: 'sell-details',
    data: { breadcrumb: 'Sell Details' },
    loadComponent: () => import('./sell-details/sell-details.component').then(m => m.SellDetailsComponent)
  },
  {
    path: 'installment',
    data: { breadcrumb: 'Installment Report' },
    loadComponent: () => import('./installment/installment-report.component').then(m => m.InstallmentReportComponent)
  }
];
