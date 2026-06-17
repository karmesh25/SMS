import { Component, Input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [MatButtonModule],
  template: `
    <div class="page-header">
      <div>
        @if (breadcrumb) {
          <div class="breadcrumb">{{ breadcrumb }}</div>
        }
        <h1>{{ title }}</h1>
        @if (subtitle) {
          <p>{{ subtitle }}</p>
        }
      </div>
      <div class="actions">
        <ng-content></ng-content>
      </div>
    </div>
  `,
  styles: [`
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 1rem;
      margin-bottom: 1.5rem;
    }

    .breadcrumb {
      color: #666;
      font-size: 0.875rem;
      margin-bottom: 0.25rem;
    }

    h1 {
      margin: 0;
      font-size: 1.75rem;
      color: #1f4e79;
    }

    p {
      margin: 0.25rem 0 0;
      color: #666;
    }
  `]
})
export class PageHeaderComponent {
  @Input({ required: true }) title!: string;
  @Input() subtitle?: string;
  @Input() breadcrumb?: string;
}
