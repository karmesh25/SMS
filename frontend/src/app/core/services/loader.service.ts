import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoaderService {
  private readonly loadingSignal = signal(false);

  readonly loading = this.loadingSignal.asReadonly();

  show(): void {
    this.loadingSignal.set(true);
  }

  hide(): void {
    this.loadingSignal.set(false);
  }
}
