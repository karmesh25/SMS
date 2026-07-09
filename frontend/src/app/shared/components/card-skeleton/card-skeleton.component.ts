import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-card-skeleton',
  standalone: true,
  template: `
    <div class="skeleton-card" [style.min-height.px]="height">
      <div class="skeleton-line title"></div>
      <div class="skeleton-line value"></div>
      @if (showBar) {
        <div class="skeleton-bar"></div>
      }
    </div>
  `,
  styles: [`
    .skeleton-card {
      background: var(--abr-surface);
      border: 1px solid var(--abr-border);
      border-radius: var(--abr-radius-lg);
      padding: 1.25rem;
      box-shadow: var(--abr-shadow-sm);
    }
    .skeleton-line, .skeleton-bar {
      background: linear-gradient(90deg, var(--abr-surface-3) 25%, var(--abr-surface-2) 50%, var(--abr-surface-3) 75%);
      background-size: 200% 100%;
      animation: shimmer 1.3s infinite;
      border-radius: var(--abr-radius-sm);
    }
    .title { height: 14px; width: 60%; margin-bottom: 1rem; }
    .value { height: 28px; width: 45%; margin-bottom: 0.75rem; }
    .skeleton-bar { height: 8px; width: 100%; }
    @keyframes shimmer {
      0% { background-position: 200% 0; }
      100% { background-position: -200% 0; }
    }
  `]
})
export class CardSkeletonComponent {
  @Input() height = 120;
  @Input() showBar = false;
}
