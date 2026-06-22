import { Routes } from '@angular/router';

export const VYAJ_ROUTES: Routes = [
  {
    path: '',
    data: { breadcrumb: 'Vyaj Khata' },
    loadComponent: () => import('./vyaj-khata.component').then(m => m.VyajKhataComponent)
  }
];
