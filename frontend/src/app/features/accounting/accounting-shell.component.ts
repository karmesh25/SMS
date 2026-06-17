import { Component } from '@angular/core';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-accounting-shell',
  standalone: true,
  imports: [PageHeaderComponent],
  template: `<app-page-header title="Accounting" subtitle="Daily entry and dastavej"></app-page-header>`
})
export class AccountingShellComponent {}
