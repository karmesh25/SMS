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
      align-items: flex-end;
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .breadcrumb {
      color: var(--abr-text-muted);
      font-size: 0.8rem;
      font-weight: 600;
      letter-spacing: 0.02em;
      text-transform: uppercase;
      margin-bottom: 0.3rem;
    }

    h1 {
      margin: 0;
      font-size: 1.7rem;
      font-weight: 700;
      color: var(--abr-text);
      letter-spacing: -0.01em;
    }

    p {
      margin: 0.3rem 0 0;
      color: var(--abr-text-muted);
      font-size: 0.92rem;
    }

    .actions {
      display: flex;
      align-items: flex-end;
      gap: 0.5rem;
      flex-wrap: wrap;
      padding-top: 0.15rem;
    }

    .actions ::ng-deep button.mat-mdc-outlined-button,
    .actions ::ng-deep button.mat-mdc-unelevated-button,
    .actions ::ng-deep button.mat-mdc-button {
      height: 40px;
      margin-bottom: 4px;
    }

    @media (max-width: 959px) {
      .page-header {
        flex-direction: column;
        align-items: stretch;
        margin-bottom: 1rem;
      }
    }

    @media (max-width: 599px) {
      h1 {
        font-size: 1.35rem;
      }
    }
  `]
})
export class PageHeaderComponent {
  @Input({ required: true }) title!: string;
  @Input() subtitle?: string;
  @Input() breadcrumb?: string;
}
