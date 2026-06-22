import { Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { TableSkeletonComponent } from '../table-skeleton/table-skeleton.component';

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [MatIconModule, TableSkeletonComponent],
  template: `
    @if (loading) {
      <app-table-skeleton [columnCount]="skeletonColumns" [rowCount]="skeletonRows" />
    } @else {
      <div class="data-table-wrap" [class.scrollable]="maxHeight">
        <ng-content />
        @if (isEmpty) {
          <div class="empty-state">
            <mat-icon>{{ emptyIcon }}</mat-icon>
            <span>{{ emptyMessage }}</span>
          </div>
        }
      </div>
    }
  `,
  styles: [`
    .data-table-wrap { width: 100%; }
    .data-table-wrap.scrollable {
      overflow: auto;
      max-height: var(--data-table-max-height, 480px);
    }
    .empty-state {
      text-align: center;
      padding: 2rem 1rem;
      color: #888;
    }
    .empty-state mat-icon {
      display: block;
      margin: 0 auto 0.5rem;
      color: #b0bec5;
      font-size: 40px;
      width: 40px;
      height: 40px;
    }
  `]
})
export class DataTableComponent {
  @Input() loading = false;
  @Input() isEmpty = false;
  @Input() emptyMessage = 'No records found.';
  @Input() emptyIcon = 'info_outline';
  @Input() skeletonColumns = 4;
  @Input() skeletonRows = 5;
  @Input() maxHeight: string | null = '480px';

  get hostStyle(): Record<string, string> | null {
    return this.maxHeight ? { '--data-table-max-height': this.maxHeight } : null;
  }
}
