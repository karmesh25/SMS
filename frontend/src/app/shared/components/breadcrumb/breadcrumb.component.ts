import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BreadcrumbService } from '../../../core/services/breadcrumb.service';

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [RouterLink],
  template: `
    @if (breadcrumb.items().length > 0) {
      <nav class="breadcrumb-nav" aria-label="Breadcrumb">
        @for (item of breadcrumb.items(); track item.label; let last = $last) {
          @if (!last && item.url) {
            <a [routerLink]="item.url">{{ item.label }}</a>
          } @else {
            <span class="current">{{ item.label }}</span>
          }
          @if (!last) {
            <span class="separator">&gt;</span>
          }
        }
      </nav>
    }
  `,
  styles: [`
    .breadcrumb-nav {
      display: flex;
      align-items: center;
      flex-wrap: wrap;
      gap: 0.35rem;
      padding: 0.5rem 1.5rem;
      background: #fff;
      border-bottom: 1px solid #e0e6ed;
      font-size: 0.875rem;
    }

    a {
      color: #1f4e79;
      text-decoration: none;
      font-weight: 500;
    }

    a:hover {
      text-decoration: underline;
    }

    .current {
      color: #666;
    }

    .separator {
      color: #999;
    }
  `]
})
export class BreadcrumbComponent {
  readonly breadcrumb = inject(BreadcrumbService);
}
