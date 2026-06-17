import { Component, inject } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LoaderService } from '../../../core/services/loader.service';

@Component({
  selector: 'app-loader',
  standalone: true,
  imports: [MatProgressSpinnerModule],
  template: `
    @if (loader.loading()) {
      <div class="loader-overlay">
        <mat-spinner diameter="48"></mat-spinner>
      </div>
    }
  `,
  styles: [`
    .loader-overlay {
      position: fixed;
      inset: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      background: rgba(255, 255, 255, 0.6);
      z-index: 2000;
    }
  `]
})
export class LoaderComponent {
  readonly loader = inject(LoaderService);
}
