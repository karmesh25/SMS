import { Routes } from '@angular/router';

/** @deprecated Prefer /accounting/vyaj. Kept for reference; app redirects /vyaj. */
export const VYAJ_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./vyaj-khata.component').then(m => m.VyajKhataComponent)
  }
];
