import { Routes } from '@angular/router';

export const BOOKING_ROUTES: Routes = [
  {
    path: '',
    data: { breadcrumb: 'Booking Grid' },
    loadComponent: () => import('./flat-grid/flat-grid.component').then(m => m.FlatGridComponent)
  },
  {
    path: 'new',
    data: { breadcrumb: 'New Booking' },
    loadComponent: () => import('./booking-form/booking-form.component').then(m => m.BookingFormComponent)
  },
  {
    path: 'edit/:id',
    data: { breadcrumb: 'Edit Booking' },
    loadComponent: () => import('./booking-form/booking-form.component').then(m => m.BookingFormComponent)
  }
];
