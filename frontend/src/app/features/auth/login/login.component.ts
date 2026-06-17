import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [MatCardModule, PageHeaderComponent],
  template: `
    <app-page-header title="Login" subtitle="ABR Society & Real Estate Management System"></app-page-header>
    <mat-card>
      <mat-card-content>
        <p>Login form will be implemented in Phase 1.</p>
      </mat-card-content>
    </mat-card>
  `
})
export class LoginComponent {}
