import { Component } from '@angular/core';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ModuleSubnavComponent } from '../../shared/components/module-subnav/module-subnav.component';
import { REPORT_NAV_ITEMS } from '../../shared/nav/module-nav-items';

@Component({
  selector: 'app-reports-shell',
  standalone: true,
  imports: [PageHeaderComponent, ModuleSubnavComponent],
  template: `
    <app-page-header title="Reports" subtitle="Financial and operational reports"></app-page-header>
    <app-module-subnav [items]="navItems" />
    <p class="hint">Select a report from the navigation above.</p>
  `,
  styles: [`
    .hint { color: var(--abr-text-muted); margin-top: 0.5rem; }
  `]
})
export class ReportsShellComponent {
  readonly navItems = REPORT_NAV_ITEMS;
}
