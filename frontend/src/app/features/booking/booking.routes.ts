import { Routes } from '@angular/router';

export const BOOKING_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./booking-shell.component').then(m => m.BookingShellComponent) }
];
