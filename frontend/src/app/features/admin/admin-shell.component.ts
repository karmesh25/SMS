import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { HasRoleDirective } from '../../shared/directives/has-role.directive';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [MatCardModule, PageHeaderComponent, RouterLink, RouterLinkActive, HasRoleDirective],
  template: `
    <app-page-header title="Administration" subtitle="Site administration modules"></app-page-header>
    <mat-card>
      <mat-card-content class="links">
        <a routerLink="/admin/users" routerLinkActive="active" *appHasRole="'SuperAdmin'">User Management</a>
        <a routerLink="/admin/devices" routerLinkActive="active" *appHasRole="'SuperAdmin'">Device Licenses</a>
        <a routerLink="/admin/sites" routerLinkActive="active" *appHasRole="['SuperAdmin', 'Admin']">Sites</a>
        <a routerLink="/admin/wings" routerLinkActive="active" *appHasRole="['SuperAdmin', 'Admin']">Wings</a>
        <a routerLink="/admin/ledgers" routerLinkActive="active" *appHasRole="['SuperAdmin', 'Admin']">Ledgers</a>
        <a routerLink="/admin/conditions" routerLinkActive="active" *appHasRole="['SuperAdmin', 'Admin']">Conditions</a>
        <a routerLink="/admin/banks" routerLinkActive="active" *appHasRole="['SuperAdmin', 'Admin']">Bank Accounts</a>
        <a routerLink="/admin/brokers" routerLinkActive="active" *appHasRole="['SuperAdmin', 'Admin']">Brokers</a>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .links {
      display: flex;
      gap: 1rem;
      flex-wrap: wrap;
    }

    a {
      color: #1f4e79;
      text-decoration: none;
      font-weight: 500;
    }

    a.active {
      text-decoration: underline;
    }
  `]
})
export class AdminShellComponent {}
