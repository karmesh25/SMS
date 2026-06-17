import { Component } from '@angular/core';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [PageHeaderComponent],
  template: `<app-page-header title="Administration" subtitle="Site administration modules"></app-page-header>`
})
export class AdminShellComponent {}
