import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-error-page',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatCardModule, MatIconModule],
  template: `
    <div class="error-page">
      <mat-card>
        <mat-card-content>
          <mat-icon class="error-icon">{{ icon }}</mat-icon>
          <h1>{{ code }}</h1>
          <p>{{ title }}</p>
          <p class="message">{{ message }}</p>
          <a mat-flat-button color="primary" routerLink="/dashboard">Back to Dashboard</a>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .error-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem;
      background: var(--abr-surface-2);
    }
    mat-card-content {
      text-align: center;
      padding: 2rem 3rem;
    }
    .error-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: var(--abr-primary-strong);
      margin-bottom: 1rem;
    }
    h1 {
      font-size: 4rem;
      margin: 0;
      color: var(--abr-primary-strong);
    }
    p { margin: 0.5rem 0; color: var(--abr-text-secondary); }
    .message { margin-bottom: 1.5rem; }
  `]
})
export class ErrorPageComponent {
  private readonly route = inject(ActivatedRoute);

  readonly code = this.route.snapshot.data['code'] as string;
  readonly title = this.route.snapshot.data['title'] as string;
  readonly message = this.route.snapshot.data['message'] as string;
  readonly icon = this.route.snapshot.data['icon'] as string;
}
