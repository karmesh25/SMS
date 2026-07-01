import { Component } from '@angular/core';
import { MatTabsModule } from '@angular/material/tabs';
import { UserManagementComponent } from './user-management.component';
import { RoleManagementComponent } from './role-management.component';

@Component({
  selector: 'app-users-admin-shell',
  standalone: true,
  imports: [MatTabsModule, UserManagementComponent, RoleManagementComponent],
  template: `
    <mat-tab-group animationDuration="0ms">
      <mat-tab label="Users">
        <div class="tab-body">
          <app-user-management />
        </div>
      </mat-tab>
      <mat-tab label="Roles">
        <div class="tab-body">
          <app-role-management />
        </div>
      </mat-tab>
    </mat-tab-group>
  `,
  styles: [`
    .tab-body {
      padding-top: 1rem;
    }
  `]
})
export class UsersAdminShellComponent {}
