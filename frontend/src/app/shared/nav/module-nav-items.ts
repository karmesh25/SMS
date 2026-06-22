import { ModuleNavItem } from '../../shared/components/module-subnav/module-subnav.component';

export const REPORT_NAV_ITEMS: ModuleNavItem[] = [
  { label: 'All Reports', route: '/reports', exact: true },
  { label: 'All Entry', route: '/reports/all-entry' },
  { label: 'Balance Sheet', route: '/reports/balance-sheet' },
  { label: 'Till Date', route: '/reports/till-date' },
  { label: 'Monthwise', route: '/reports/monthwise' },
  { label: 'Bank Statement', route: '/reports/bank-statement' },
  { label: 'Sell Details', route: '/reports/sell-details' },
  { label: 'Installment', route: '/reports/installment' }
];

export const ACCOUNTING_NAV_ITEMS: ModuleNavItem[] = [
  { label: 'Daily Entry', route: '/accounting', exact: true },
  { label: 'Dastavej', route: '/accounting/dastavej' }
];
