import { Component, Input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

export interface ModuleNavItem {
  label: string;
  route: string;
  exact?: boolean;
}

@Component({
  selector: 'app-module-subnav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="module-subnav">
      @for (item of items; track item.route) {
        <a
          [routerLink]="item.route"
          routerLinkActive="active"
          [routerLinkActiveOptions]="{ exact: item.exact ?? false }">
          {{ item.label }}
        </a>
      }
    </nav>
  `,
  styles: [`
    .module-subnav {
      display: flex;
      gap: 1rem;
      margin-bottom: 1rem;
      flex-wrap: wrap;
      border-bottom: 1px solid #eef2f6;
      padding-bottom: 0.5rem;
    }
    a {
      color: #1f4e79;
      text-decoration: none;
      font-weight: 500;
      padding: 0.25rem 0;
      border-bottom: 2px solid transparent;
    }
    a.active {
      border-bottom-color: #2e75b6;
      color: #2e75b6;
    }
  `]
})
export class ModuleSubnavComponent {
  @Input({ required: true }) items: ModuleNavItem[] = [];
}
