import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [MatCardModule, PageHeaderComponent],
  template: `
    <app-page-header title="Dashboard" subtitle="Overview of site activity"></app-page-header>
    <mat-card>
      <mat-card-content>Dashboard summary cards will be implemented in Phase 7.</mat-card-content>
    </mat-card>
  `
})
export class DashboardComponent {}
