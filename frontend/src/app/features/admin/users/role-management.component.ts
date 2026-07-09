import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { APP_MODULES, AppRole, CreateRoleRequest, ModulePermission, UpdateRoleRequest } from '../../../core/models/permission.model';
import { RoleService } from '../../../core/services/role.service';
import { ToastService } from '../../../core/services/toast.service';

interface RoleDialogData {
  role?: AppRole;
}

@Component({
  selector: 'app-role-edit-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatTableModule
  ],
  template: `
    <h2 mat-dialog-title>{{ data.role ? 'Edit Role' : 'Create Role' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="role-form">
        <mat-form-field appearance="outline">
          <mat-label>Name</mat-label>
          <input matInput formControlName="name" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Description</mat-label>
          <input matInput formControlName="description" />
        </mat-form-field>
      </form>

      <div class="abr-table-card">
      <table class="perm-table">
        <thead>
          <tr>
            <th>Module</th>
            <th>View</th>
            <th>Manage</th>
          </tr>
        </thead>
        <tbody>
          @for (row of permissionRows; track row.moduleKey) {
            <tr>
              <td>{{ row.label }}</td>
              <td>
                <mat-checkbox
                  [checked]="row.canView"
                  (change)="onViewChange(row.moduleKey, $event.checked)" />
              </td>
              <td>
                @if (row.moduleKey !== 'dashboard') {
                  <mat-checkbox
                    [checked]="row.canManage"
                    [disabled]="!row.canView"
                    (change)="onManageChange(row.moduleKey, $event.checked)" />
                } @else {
                  <span class="na">—</span>
                }
              </td>
            </tr>
          }
        </tbody>
      </table>
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid || !hasAnyView" (click)="save()">
        Save
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .role-form {
      display: grid;
      gap: 0.5rem;
      margin-bottom: 1rem;
    }

    .perm-table {
      width: 100%;
      border-collapse: collapse;
    }

    .perm-table th,
    .perm-table td {
      padding: 0.35rem 0.5rem;
      border-bottom: 1px solid var(--abr-border);
      text-align: left;
    }

    .na {
      color: var(--abr-text-muted);
    }
  `]
})
export class RoleEditDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<RoleEditDialogComponent, CreateRoleRequest | UpdateRoleRequest | undefined>);
  readonly data = inject<RoleDialogData>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    name: [this.data.role?.name ?? '', Validators.required],
    description: [this.data.role?.description ?? '']
  });

  permissionRows = APP_MODULES.map((m) => {
    const existing = this.data.role?.permissions.find((p) => p.moduleKey === m.key);
    return {
      moduleKey: m.key,
      label: m.label,
      canView: existing?.canView ?? false,
      canManage: existing?.canManage ?? false
    };
  });

  get hasAnyView(): boolean {
    return this.permissionRows.some((r) => r.canView);
  }

  onViewChange(moduleKey: string, checked: boolean): void {
    const row = this.permissionRows.find((r) => r.moduleKey === moduleKey);
    if (!row) return;
    row.canView = checked;
    if (!checked) {
      row.canManage = false;
    }
  }

  onManageChange(moduleKey: string, checked: boolean): void {
    const row = this.permissionRows.find((r) => r.moduleKey === moduleKey);
    if (!row) return;
    row.canManage = checked;
    if (checked) {
      row.canView = true;
    }
  }

  save(): void {
    if (this.form.invalid || !this.hasAnyView) {
      return;
    }

    const permissions: ModulePermission[] = this.permissionRows.map((r) => ({
      moduleKey: r.moduleKey,
      canView: r.canView,
      canManage: r.canManage
    }));

    this.dialogRef.close({
      name: this.form.controls.name.value.trim(),
      description: this.form.controls.description.value.trim() || undefined,
      permissions
    });
  }
}

@Component({
  selector: 'app-role-management',
  standalone: true,
  imports: [MatTableModule, MatButtonModule, MatIconModule, PageHeaderComponent],
  template: `
    <app-page-header title="Roles" subtitle="Define custom roles with per-module permissions"></app-page-header>

    <div class="toolbar">
      <button mat-flat-button color="primary" (click)="openCreate()">
        <mat-icon>add</mat-icon>
        New Role
      </button>
    </div>

    <div class="abr-table-card">
      <table mat-table [dataSource]="roles" class="abr-table sticky-header">
        <ng-container matColumnDef="name">
          <th mat-header-cell *matHeaderCellDef>Name</th>
          <td mat-cell *matCellDef="let row">
            {{ row.name }}
            @if (row.isSystem) {
              <span class="abr-chip abr-chip--info">System</span>
            }
          </td>
        </ng-container>
        <ng-container matColumnDef="description">
          <th mat-header-cell *matHeaderCellDef>Description</th>
          <td mat-cell *matCellDef="let row">{{ row.description || '—' }}</td>
        </ng-container>
        <ng-container matColumnDef="userCount">
          <th mat-header-cell *matHeaderCellDef>Users</th>
          <td mat-cell *matCellDef="let row">{{ row.userCount }}</td>
        </ng-container>
        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef>Actions</th>
          <td mat-cell *matCellDef="let row">
            <button mat-button (click)="openEdit(row)">Edit</button>
            @if (!row.isSystem && row.userCount === 0) {
              <button mat-button color="warn" (click)="confirmDelete(row)">Delete</button>
            }
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
        <tr class="empty-row" *matNoDataRow>
          <td [attr.colspan]="displayedColumns.length">
            <mat-icon>admin_panel_settings</mat-icon>No roles yet.
          </td>
        </tr>
      </table>
    </div>
  `,
  styles: [`
    .toolbar {
      margin-bottom: 1rem;
    }
  `]
})
export class RoleManagementComponent implements OnInit {
  private readonly roleService = inject(RoleService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(MatDialog);

  roles: AppRole[] = [];
  displayedColumns = ['name', 'description', 'userCount', 'actions'];

  ngOnInit(): void {
    this.loadRoles();
  }

  loadRoles(): void {
    this.roleService.getAll().subscribe({
      next: (response) => {
        if (response.success) {
          this.roles = response.data;
        }
      },
      error: () => this.toast.error('Failed to load roles')
    });
  }

  openCreate(): void {
    const ref = this.dialog.open(RoleEditDialogComponent, {
      width: '520px',
      data: {} satisfies RoleDialogData
    });

    ref.afterClosed().subscribe((result) => {
      if (!result) return;
      this.roleService.create(result as CreateRoleRequest).subscribe({
        next: (response) => {
          if (response.success) {
            this.toast.success('Role created');
            this.loadRoles();
          }
        },
        error: (error) => this.toast.error(error.error?.message ?? 'Failed to create role')
      });
    });
  }

  openEdit(role: AppRole): void {
    const ref = this.dialog.open(RoleEditDialogComponent, {
      width: '520px',
      data: { role } satisfies RoleDialogData
    });

    ref.afterClosed().subscribe((result) => {
      if (!result) return;
      this.roleService.update(role.id, result as UpdateRoleRequest).subscribe({
        next: (response) => {
          if (response.success) {
            this.toast.success('Role updated');
            this.loadRoles();
          }
        },
        error: (error) => this.toast.error(error.error?.message ?? 'Failed to update role')
      });
    });
  }

  confirmDelete(role: AppRole): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Role',
        message: `Delete role "${role.name}"? This cannot be undone.`,
        confirmText: 'Delete'
      }
    });

    ref.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;
      this.roleService.delete(role.id).subscribe({
        next: (response) => {
          if (response.success) {
            this.toast.success('Role deleted');
            this.loadRoles();
          }
        },
        error: (error) => this.toast.error(error.error?.message ?? 'Failed to delete role')
      });
    });
  }
}
