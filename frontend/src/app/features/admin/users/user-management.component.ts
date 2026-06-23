import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { CurrentUser } from '../../../core/models/api-response.model';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatTableModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    PageHeaderComponent
  ],
  template: `
    <app-page-header title="User Management" subtitle="Manage system users (Super Admin)"></app-page-header>

    <form [formGroup]="form" class="form-grid" (ngSubmit)="createUser()">
      <mat-form-field appearance="outline">
        <mat-label>Username</mat-label>
        <input matInput formControlName="username" />
      </mat-form-field>
      <mat-form-field appearance="outline">
        <mat-label>Email</mat-label>
        <input matInput formControlName="email" />
      </mat-form-field>
      <mat-form-field appearance="outline">
        <mat-label>Password</mat-label>
        <input matInput type="password" formControlName="password" />
      </mat-form-field>
      <mat-form-field appearance="outline">
        <mat-label>Role</mat-label>
        <mat-select formControlName="role">
          @for (role of roles; track role) {
            <mat-option [value]="role">{{ role }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
      <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">Add User</button>
    </form>

    <div class="abr-scroll-x">

    <table mat-table [dataSource]="users" class="mat-elevation-z1 abr-table sticky-header">
      <ng-container matColumnDef="username">
        <th mat-header-cell *matHeaderCellDef>Username</th>
        <td mat-cell *matCellDef="let row">{{ row.username }}</td>
      </ng-container>
      <ng-container matColumnDef="email">
        <th mat-header-cell *matHeaderCellDef>Email</th>
        <td mat-cell *matCellDef="let row">{{ row.email }}</td>
      </ng-container>
      <ng-container matColumnDef="role">
        <th mat-header-cell *matHeaderCellDef>Role</th>
        <td mat-cell *matCellDef="let row">{{ row.role }}</td>
      </ng-container>
      <ng-container matColumnDef="isActive">
        <th mat-header-cell *matHeaderCellDef>Active</th>
        <td mat-cell *matCellDef="let row">{{ row.isActive ? 'Yes' : 'No' }}</td>
      </ng-container>
      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef>Actions</th>
        <td mat-cell *matCellDef="let row">
          <button mat-button (click)="toggleActive(row.id)">Toggle</button>
          <button mat-button (click)="forceReset(row.id)">Force Reset</button>
        </td>
      </ng-container>

      <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
      <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
      <tr class="empty-row" *matNoDataRow><td [attr.colspan]="displayedColumns.length"><mat-icon>info_outline</mat-icon>No records found.</td></tr>
    </table>
    </div>
  `,
  styles: [`
    .form-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
      gap: 1rem;
      margin-bottom: 1.5rem;
      align-items: center;
    }

    table {
      width: 100%;
    }
  `]
})
export class UserManagementComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  users: CurrentUser[] = [];
  roles = ['SuperAdmin', 'Admin', 'OfficeStaff', 'ViewOnly'];
  displayedColumns = ['username', 'email', 'role', 'isActive', 'actions'];

  readonly form = this.fb.nonNullable.group({
    username: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    role: ['OfficeStaff', Validators.required]
  });

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.api.get<CurrentUser[]>('/users').subscribe({
      next: (response) => {
        if (response.success) {
          this.users = response.data;
        }
      }
    });
  }

  createUser(): void {
    if (this.form.invalid) {
      return;
    }

    this.api.post<CurrentUser>('/users', { ...this.form.getRawValue(), siteIds: [] }).subscribe({
      next: (response) => {
        if (response.success) {
          this.toast.success('User created');
          this.form.reset({ role: 'OfficeStaff' });
          this.loadUsers();
        }
      },
      error: (error) => this.toast.error(error.error?.message ?? 'Failed to create user')
    });
  }

  toggleActive(id: string): void {
    this.api.put(`/users/${id}/toggle-active`, {}).subscribe({
      next: (response) => {
        if (response.success) {
          this.toast.success('User status updated');
          this.loadUsers();
        }
      }
    });
  }

  forceReset(id: string): void {
    this.api.put(`/users/${id}/force-password-reset`, {}).subscribe({
      next: (response) => {
        if (response.success) {
          this.toast.success('Password reset flagged');
        }
      }
    });
  }
}
