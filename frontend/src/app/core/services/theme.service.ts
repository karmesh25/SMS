import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';

/**
 * In-memory theme controller (no localStorage, per project rules).
 * Applies the active theme by stamping `data-theme` on the <html> element,
 * which drives both our CSS custom properties and the Angular Material
 * dark color theme defined in styles.scss.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly modeSignal = signal<ThemeMode>('light');

  readonly mode = this.modeSignal.asReadonly();
  readonly isDark = computed(() => this.modeSignal() === 'dark');

  constructor() {
    this.apply(this.modeSignal());
  }

  toggle(): void {
    this.setMode(this.isDark() ? 'light' : 'dark');
  }

  setMode(mode: ThemeMode): void {
    this.modeSignal.set(mode);
    this.apply(mode);
  }

  private apply(mode: ThemeMode): void {
    const root = this.document.documentElement;
    if (mode === 'dark') {
      root.setAttribute('data-theme', 'dark');
    } else {
      root.removeAttribute('data-theme');
    }
    root.style.colorScheme = mode;
  }
}
