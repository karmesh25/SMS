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
      padding: 0.55rem 1.75rem;
      background: var(--abr-surface);
      border-bottom: 1px solid var(--abr-border);
      font-size: 0.85rem;
    }

    a {
      color: var(--abr-primary);
      text-decoration: none;
      font-weight: 600;
    }

    a:hover {
      text-decoration: underline;
    }

    .current {
      color: var(--abr-text-secondary);
      font-weight: 500;
    }

    .separator {
      color: var(--abr-text-muted);
    }

    @media (max-width: 959px) {
      .breadcrumb-nav {
        padding: 0.4rem 1rem;
        font-size: 0.8125rem;
      }
    }

    @media (max-width: 599px) {
      .breadcrumb-nav {
        padding: 0.35rem 0.75rem;
      }
    }
  `]
})
export class BreadcrumbComponent {
  readonly breadcrumb = inject(BreadcrumbService);
}
