import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-table-skeleton',
  standalone: true,
  template: `
    <div class="skeleton-table" [style.--cols]="columnCount">
      <div class="skeleton-header">
        @for (c of columns; track $index) {
          <div class="skeleton-cell header"></div>
        }
      </div>
      @for (r of rows; track $index) {
        <div class="skeleton-row">
          @for (c of columns; track $index) {
            <div class="skeleton-cell"></div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .skeleton-table {
      background: var(--abr-surface);
      border: 1px solid var(--abr-border);
      border-radius: var(--abr-radius-lg);
      overflow: hidden;
      box-shadow: var(--abr-shadow-sm);
    }
    .skeleton-header, .skeleton-row {
      display: grid;
      grid-template-columns: repeat(var(--cols, 4), 1fr);
      gap: 1rem;
      padding: 0.75rem 1rem;
    }
    .skeleton-header { background: var(--abr-surface-2); }
    .skeleton-row:not(:last-child) { border-bottom: 1px solid var(--abr-border); }
    .skeleton-cell {
      height: 14px;
      background: linear-gradient(90deg, var(--abr-surface-3) 25%, var(--abr-surface-2) 50%, var(--abr-surface-3) 75%);
      background-size: 200% 100%;
      animation: shimmer 1.3s infinite;
      border-radius: var(--abr-radius-sm);
    }
    .skeleton-cell.header { height: 16px; opacity: 0.7; }
    @keyframes shimmer {
      0% { background-position: 200% 0; }
      100% { background-position: -200% 0; }
    }
  `]
})
export class TableSkeletonComponent {
  @Input() columnCount = 4;
  @Input() rowCount = 5;

  get columns(): number[] {
    return Array.from({ length: this.columnCount }, (_, i) => i);
  }

  get rows(): number[] {
    return Array.from({ length: this.rowCount }, (_, i) => i);
  }
}
