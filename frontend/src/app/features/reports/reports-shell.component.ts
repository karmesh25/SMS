import { Component } from '@angular/core';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-reports-shell',
  standalone: true,
  imports: [PageHeaderComponent],
  template: `<app-page-header title="Reports" subtitle="Financial and operational reports"></app-page-header>`
})
export class ReportsShellComponent {}
