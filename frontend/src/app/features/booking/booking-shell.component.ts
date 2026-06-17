import { Component } from '@angular/core';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-booking-shell',
  standalone: true,
  imports: [PageHeaderComponent],
  template: `<app-page-header title="Flat Booking" subtitle="Wing selection and flat grid"></app-page-header>`
})
export class BookingShellComponent {}
