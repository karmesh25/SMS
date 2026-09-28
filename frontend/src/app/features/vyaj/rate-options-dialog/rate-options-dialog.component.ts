import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { VyajService } from '../../../core/services/vyaj.service';
import { ToastService } from '../../../core/services/toast.service';
import { VyajRateBasisOption } from '../models/vyaj.models';

export interface RateOptionsDialogData {
  siteId: string;
  options: VyajRateBasisOption[];
}

@Component({
  selector: 'app-vyaj-rate-options-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule
  ],
  template: `
    <h2 mat-dialog-title>Manage rate options</h2>
    <mat-dialog-content>
      <p class="hint">Site-scoped day options shown in the entry rate basis dropdown.</p>

      <ul class="options">
        @for (opt of options(); track opt.id) {
          <li>
            <div class="opt-main">
              <strong>{{ opt.label }}</strong>
              <span>{{ opt.dayCount }} days</span>
              @if (!opt.isActive) {
                <span class="inactive">Inactive</span>
              }
            </div>
            <button mat-icon-button type="button" (click)="remove(opt)" aria-label="Delete option">
              <mat-icon>delete</mat-icon>
            </button>
          </li>
        } @empty {
          <li class="empty">No custom options yet.</li>
        }
      </ul>

      <form class="add-form" [formGroup]="form" (ngSubmit)="add()">
        <mat-form-field appearance="outline">
          <mat-label>Label</mat-label>
          <input matInput formControlName="label" placeholder="e.g. 45 days" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Days</mat-label>
          <input matInput type="number" min="1" formControlName="dayCount" />
        </mat-form-field>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || saving()">
          Add
        </button>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" [mat-dialog-close]="changed">Close</button>
    </mat-dialog-actions>
  `,
  styles: `
    .hint { margin: 0 0 0.75rem; color: var(--abr-text-muted, #666); font-size: 0.875rem; }
    .options { list-style: none; margin: 0 0 1rem; padding: 0; display: flex; flex-direction: column; gap: 0.5rem; }
    .options li { display: flex; align-items: center; justify-content: space-between; gap: 0.5rem; padding: 0.4rem 0.5rem; border: 1px solid rgba(0,0,0,0.08); border-radius: 6px; }
    .opt-main { display: flex; flex-wrap: wrap; gap: 0.5rem; align-items: baseline; }
    .inactive { color: #b45309; font-size: 0.75rem; }
    .empty { border: none !important; color: #888; }
    .add-form { display: grid; grid-template-columns: 1fr 100px auto; gap: 0.5rem; align-items: start; }
  `
})
export class VyajRateOptionsDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly vyaj = inject(VyajService);
  private readonly toast = inject(ToastService);
  private readonly data = inject<RateOptionsDialogData>(MAT_DIALOG_DATA);

  readonly options = signal<VyajRateBasisOption[]>([...this.data.options]);
  readonly saving = signal(false);
  changed = false;

  readonly form = this.fb.nonNullable.group({
    label: ['', [Validators.required, Validators.maxLength(100)]],
    dayCount: [45, [Validators.required, Validators.min(1)]]
  });

  add(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.saving.set(true);
    this.vyaj.createRateOption({
      siteId: this.data.siteId,
      label: v.label.trim(),
      dayCount: Number(v.dayCount)
    }).subscribe({
      next: (res) => {
        this.saving.set(false);
        if (res.success && res.data) {
          this.options.update((list) => [...list, res.data]);
          this.form.reset({ label: '', dayCount: 45 });
          this.changed = true;
          this.toast.success('Rate option added');
        }
      },
      error: () => {
        this.saving.set(false);
        this.toast.error('Failed to add rate option');
      }
    });
  }

  remove(opt: VyajRateBasisOption): void {
    this.vyaj.deleteRateOption(opt.id).subscribe({
      next: (res) => {
        if (res.success) {
          this.options.update((list) => list.filter((o) => o.id !== opt.id));
          this.changed = true;
          this.toast.success('Rate option removed');
        }
      },
      error: () => this.toast.error('Failed to remove rate option')
    });
  }
}
