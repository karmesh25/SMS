import { Component, inject } from '@angular/core';
import { LoaderService } from '../../../core/services/loader.service';

@Component({
  selector: 'app-loader',
  standalone: true,
  template: `
    @if (loader.loading()) {
      <div class="loader-overlay">
        <div class="loader">
          <div class="loader-ring">
            <span class="loader-mark">ABR</span>
          </div>
          <div class="loader-dots" aria-label="Loading">
            <span></span><span></span><span></span>
          </div>
        </div>
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
      background: color-mix(in srgb, var(--abr-bg) 68%, transparent);
      backdrop-filter: blur(3px);
      z-index: 2000;
      animation: fade-in var(--abr-dur) var(--abr-ease);
    }

    .loader {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
    }

    .loader-ring {
      position: relative;
      width: 64px;
      height: 64px;
      display: grid;
      place-items: center;
      border-radius: 50%;
      background:
        conic-gradient(from 0deg, transparent 0%, var(--abr-primary) 85%, transparent 100%);
      -webkit-mask: radial-gradient(farthest-side, transparent calc(100% - 5px), #000 calc(100% - 4px));
      mask: radial-gradient(farthest-side, transparent calc(100% - 5px), #000 calc(100% - 4px));
      animation: spin 0.9s linear infinite;
    }

    .loader-mark {
      position: absolute;
      font-size: 0.82rem;
      font-weight: 800;
      letter-spacing: 0.04em;
      color: var(--abr-primary-strong);
      /* counter-rotate so the label stays upright inside the spinning ring */
      animation: spin-reverse 0.9s linear infinite, pulse 1.4s ease-in-out infinite;
    }

    .loader-dots {
      display: flex;
      gap: 0.4rem;
    }

    .loader-dots span {
      width: 7px;
      height: 7px;
      border-radius: 50%;
      background: var(--abr-primary);
      opacity: 0.4;
      animation: bounce 1.2s ease-in-out infinite;
    }
    .loader-dots span:nth-child(2) { animation-delay: 0.15s; }
    .loader-dots span:nth-child(3) { animation-delay: 0.3s; }

    @keyframes spin { to { transform: rotate(360deg); } }
    @keyframes spin-reverse { to { transform: rotate(-360deg); } }
    @keyframes pulse {
      0%, 100% { opacity: 0.75; }
      50% { opacity: 1; }
    }
    @keyframes bounce {
      0%, 100% { opacity: 0.35; transform: translateY(0); }
      40% { opacity: 1; transform: translateY(-4px); }
    }
    @keyframes fade-in {
      from { opacity: 0; }
      to { opacity: 1; }
    }
  `]
})
export class LoaderComponent {
  readonly loader = inject(LoaderService);
}
