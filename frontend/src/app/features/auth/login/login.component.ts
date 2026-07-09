import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../../core/services/auth.service';
import { SiteContextService } from '../../../core/services/site-context.service';
import { ToastService } from '../../../core/services/toast.service';
import { LoaderService } from '../../../core/services/loader.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule
  ],
  template: `
    <div class="login-page">
      <div class="login-shell">
        <!-- Brand panel -->
        <aside class="login-brand">
          <div class="brand-top">
            <div class="brand-mark">ABR</div>
            <div class="brand-titles">
              <span class="brand-name">ABR SMS</span>
              <span class="brand-tag">Society &amp; Real Estate</span>
            </div>
          </div>

          <div class="brand-body">
            <h2>Manage your sites with confidence.</h2>
            <p>Bookings, accounting, journal vouchers and reports — one secure, offline-first workspace.</p>
            <ul class="brand-points">
              <li><mat-icon>verified_user</mat-icon><span>Secure, device-locked access</span></li>
              <li><mat-icon>account_balance_wallet</mat-icon><span>Real-time books &amp; ledgers</span></li>
              <li><mat-icon>insights</mat-icon><span>Instant reports &amp; exports</span></li>
            </ul>
          </div>

          <div class="brand-foot">Confidential · ABR Management System</div>
          <span class="brand-orb brand-orb--1"></span>
          <span class="brand-orb brand-orb--2"></span>
        </aside>

        <!-- Form panel -->
        <section class="login-form-panel">
          <div class="form-inner">
            <div class="brand-mark brand-mark--mobile">ABR</div>
            <h1>Welcome back</h1>
            <p class="form-sub">Sign in to continue to your workspace.</p>

            <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Username</mat-label>
                <mat-icon matPrefix>person</mat-icon>
                <input matInput formControlName="username" autocomplete="username" />
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Password</mat-label>
                <mat-icon matPrefix>lock</mat-icon>
                <input
                  matInput
                  [type]="hidePassword ? 'password' : 'text'"
                  formControlName="password"
                  autocomplete="current-password" />
                <button
                  mat-icon-button
                  matSuffix
                  type="button"
                  [attr.aria-label]="hidePassword ? 'Show password' : 'Hide password'"
                  (click)="hidePassword = !hidePassword">
                  <mat-icon>{{ hidePassword ? 'visibility_off' : 'visibility' }}</mat-icon>
                </button>
              </mat-form-field>

              @if (errorMessage) {
                <div class="error" role="alert">
                  <mat-icon>error_outline</mat-icon>
                  <span>{{ errorMessage }}</span>
                </div>
              }

              <button
                mat-flat-button
                color="primary"
                class="full-width submit-btn"
                type="submit"
                [disabled]="form.invalid">
                <mat-icon>login</mat-icon>
                Sign in
              </button>
            </form>
          </div>
        </section>
      </div>
    </div>
  `,
  styles: [`
    :host { display: block; }

    .login-page {
      min-height: 100vh;
      min-height: 100dvh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 1.5rem;
      background:
        radial-gradient(1100px 600px at 15% -10%, rgba(13, 148, 136, 0.18), transparent 60%),
        radial-gradient(900px 500px at 110% 110%, rgba(5, 150, 105, 0.16), transparent 55%),
        var(--abr-bg);
    }

    .login-shell {
      display: grid;
      grid-template-columns: 1.05fr 1fr;
      width: 100%;
      max-width: 960px;
      background: var(--abr-surface);
      border: 1px solid var(--abr-border);
      border-radius: var(--abr-radius-xl);
      box-shadow: var(--abr-shadow-lg);
      overflow: hidden;
    }

    /* Brand panel */
    .login-brand {
      position: relative;
      overflow: hidden;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
      gap: 2rem;
      padding: 2.5rem;
      color: #fff;
      background: var(--abr-brand-gradient-vivid);
    }

    .brand-top { display: flex; align-items: center; gap: 0.75rem; z-index: 1; }

    .brand-mark {
      display: grid;
      place-items: center;
      width: 48px;
      height: 48px;
      border-radius: var(--abr-radius-md);
      background: rgba(255, 255, 255, 0.16);
      border: 1px solid rgba(255, 255, 255, 0.25);
      font-weight: 800;
      font-size: 1rem;
      letter-spacing: 0.03em;
    }

    .brand-titles { display: flex; flex-direction: column; line-height: 1.15; }
    .brand-name { font-weight: 700; font-size: 1.15rem; }
    .brand-tag { font-size: 0.78rem; opacity: 0.85; }

    .brand-body { z-index: 1; }
    .brand-body h2 {
      margin: 0 0 0.6rem;
      font-size: 1.7rem;
      line-height: 1.2;
      font-weight: 700;
      color: #fff;
      letter-spacing: -0.01em;
    }
    .brand-body p { margin: 0 0 1.5rem; opacity: 0.9; font-size: 0.95rem; line-height: 1.5; }

    .brand-points { list-style: none; margin: 0; padding: 0; display: grid; gap: 0.85rem; }
    .brand-points li { display: flex; align-items: center; gap: 0.7rem; font-size: 0.9rem; }
    .brand-points mat-icon {
      flex: 0 0 auto;
      display: grid;
      place-items: center;
      width: 34px;
      height: 34px;
      border-radius: 50%;
      background: rgba(255, 255, 255, 0.16);
      font-size: 1.1rem;
    }

    .brand-foot { z-index: 1; font-size: 0.72rem; opacity: 0.7; }

    .brand-orb {
      position: absolute;
      border-radius: 50%;
      background: rgba(255, 255, 255, 0.1);
      filter: blur(2px);
    }
    .brand-orb--1 { width: 200px; height: 200px; top: -60px; right: -50px; }
    .brand-orb--2 { width: 130px; height: 130px; bottom: -30px; left: -30px; background: rgba(255, 255, 255, 0.08); }

    /* Form panel */
    .login-form-panel {
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2.75rem 2.5rem;
      background: var(--abr-surface);
    }

    .form-inner { width: 100%; max-width: 360px; }

    .brand-mark--mobile {
      display: none;
      background: var(--abr-brand-gradient-vivid);
      color: #fff;
      border: none;
      margin-bottom: 1.25rem;
    }

    .form-inner h1 {
      margin: 0 0 0.35rem;
      font-size: 1.6rem;
      font-weight: 700;
      color: var(--abr-text);
    }
    .form-sub { margin: 0 0 1.75rem; color: var(--abr-text-muted); font-size: 0.9rem; }

    .full-width { width: 100%; }
    mat-form-field.full-width { margin-bottom: 0.35rem; }

    mat-form-field .mat-icon[matPrefix] {
      margin: 0 0.5rem 0 0.15rem;
      color: var(--abr-text-muted);
    }

    .error {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin: 0.25rem 0 1rem;
      padding: 0.6rem 0.75rem;
      font-size: 0.85rem;
      color: var(--abr-danger);
      background: var(--abr-danger-soft);
      border-radius: var(--abr-radius-md);
    }
    .error mat-icon { font-size: 1.15rem; width: 1.15rem; height: 1.15rem; }

    .submit-btn {
      width: 100%;
      height: 48px;
      margin-top: 0.75rem;
      font-size: 0.95rem;
      font-weight: 600;
    }
    .submit-btn mat-icon { margin-right: 0.4rem; }

    /* Responsive */
    @media (max-width: 880px) {
      .login-shell { grid-template-columns: 1fr; max-width: 460px; }
      .login-brand { display: none; }
      .login-form-panel { padding: 2.25rem 1.75rem; }
      .brand-mark--mobile { display: grid; }
    }

    @media (max-width: 480px) {
      .login-page { padding: 0; align-items: stretch; }
      .login-shell {
        border: none;
        border-radius: 0;
        box-shadow: none;
        min-height: 100vh;
        min-height: 100dvh;
        align-content: center;
      }
    }
  `]
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly siteContext = inject(SiteContextService);
  private readonly toast = inject(ToastService);
  private readonly loader = inject(LoaderService);
  private readonly router = inject(Router);

  hidePassword = true;
  errorMessage = '';

  readonly form = this.fb.nonNullable.group({
    username: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.errorMessage = '';
    this.loader.show();

    this.authService.login(this.form.getRawValue()).subscribe({
      next: (response) => {
        this.loader.hide();
        if (response.success) {
          this.toast.success('Login successful');
          this.siteContext.loadSites().subscribe();
          void this.router.navigate(['/dashboard']);
        } else {
          this.errorMessage = response.message;
        }
      },
      error: (error) => {
        this.loader.hide();
        this.errorMessage = error.error?.message ?? 'Login failed. Please try again.';
      }
    });
  }
}
