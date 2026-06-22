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
      background: #fff;
      border-radius: 8px;
      overflow: hidden;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
    }
    .skeleton-header, .skeleton-row {
      display: grid;
      grid-template-columns: repeat(var(--cols, 4), 1fr);
      gap: 1rem;
      padding: 0.75rem 1rem;
    }
    .skeleton-header { background: #f0f4f8; }
    .skeleton-row:not(:last-child) { border-bottom: 1px solid #eef2f6; }
    .skeleton-cell {
      height: 14px;
      background: linear-gradient(90deg, #eceff1 25%, #f5f7fa 50%, #eceff1 75%);
      background-size: 200% 100%;
      animation: shimmer 1.2s infinite;
      border-radius: 4px;
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
