import { Component, effect, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { BookingService } from '../../../core/services/booking.service';
import { SiteContextService } from '../../../core/services/site-context.service';
import { ToastService } from '../../../core/services/toast.service';
import { IndianCurrencyPipe } from '../../../shared/pipes/indian-currency.pipe';
import { AppDatePipe } from '../../../shared/pipes/app-date.pipe';
import { IndianAmountDirective } from '../../../shared/directives/indian-amount.directive';
import { ModuleSubnavComponent } from '../../../shared/components/module-subnav/module-subnav.component';
import { SearchableSelectComponent, SelectOption } from '../../../shared/components/searchable-select/searchable-select.component';
import { ACCOUNTING_NAV_ITEMS } from '../../../shared/nav/module-nav-items';

interface BookingOption { id: string; flatNo: string; memberName: string; }
interface DastavejRow {
  id: string;
  flatNo: string;
  memberName: string;
  bookingDate: string;
  dastavejDate?: string;
  satakhatDate?: string;
  documentNumber?: string;
  serviceTax?: number;
  status: string;
}

@Component({
  selector: 'app-dastavej-entry',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink, RouterLinkActive, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatTableModule, MatIconModule,
    PageHeaderComponent, IndianCurrencyPipe, AppDatePipe, IndianAmountDirective,
    ModuleSubnavComponent, SearchableSelectComponent
  ],
  template: `
    <app-page-header title="Dastavej / Satakhat" subtitle="Sale deed and agreement dates"></app-page-header>

    <app-module-subnav [items]="accountingNav" />

    <form [formGroup]="form" class="form-grid" (ngSubmit)="save()">
      <app-searchable-select
        label="Booking"
        formControlName="bookingId"
        [options]="bookingOptions"
        (selectionChange)="onBookingSelect()" />
      <mat-form-field appearance="outline"><mat-label>Dastavej Date</mat-label><input matInput type="date" formControlName="dastavejDate" /></mat-form-field>
      <mat-form-field appearance="outline"><mat-label>Satakhat Date</mat-label><input matInput type="date" formControlName="satakhatDate" /></mat-form-field>
      <mat-form-field appearance="outline"><mat-label>Document Number</mat-label><input matInput formControlName="documentNumber" /></mat-form-field>
      <mat-form-field appearance="outline"><mat-label>Service Tax</mat-label><input matInput type="number" formControlName="serviceTax" appIndianAmount /></mat-form-field>
      <mat-form-field appearance="outline"><mat-label>Notes</mat-label><input matInput formControlName="notes" /></mat-form-field>
      <button mat-flat-button color="primary" type="submit" [disabled]="!form.value.bookingId">Save</button>
    </form>

    <table mat-table [dataSource]="rows" class="mat-elevation-z1 abr-table sticky-header">
      <ng-container matColumnDef="flatNo"><th mat-header-cell *matHeaderCellDef>Flat</th><td mat-cell *matCellDef="let row">{{ row.flatNo }}</td></ng-container>
      <ng-container matColumnDef="memberName"><th mat-header-cell *matHeaderCellDef>Member</th><td mat-cell *matCellDef="let row">{{ row.memberName }}</td></ng-container>
      <ng-container matColumnDef="dastavejDate"><th mat-header-cell *matHeaderCellDef>Dastavej</th><td mat-cell *matCellDef="let row">{{ row.dastavejDate ? (row.dastavejDate | appDate) : '-' }}</td></ng-container>
      <ng-container matColumnDef="satakhatDate"><th mat-header-cell *matHeaderCellDef>Satakhat</th><td mat-cell *matCellDef="let row">{{ row.satakhatDate ? (row.satakhatDate | appDate) : '-' }}</td></ng-container>
      <ng-container matColumnDef="documentNumber"><th mat-header-cell *matHeaderCellDef>Doc No</th><td mat-cell *matCellDef="let row">{{ row.documentNumber ?? '-' }}</td></ng-container>
      <ng-container matColumnDef="serviceTax"><th mat-header-cell *matHeaderCellDef>Service Tax</th><td mat-cell *matCellDef="let row">{{ row.serviceTax != null ? (row.serviceTax | indianCurrency) : '-' }}</td></ng-container>
      <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef></th><td mat-cell *matCellDef="let row"><button mat-button (click)="editRow(row)">Edit</button></td></ng-container>
      <tr mat-header-row *matHeaderRowDef="cols"></tr>
      <tr mat-row *matRowDef="let row; columns: cols"></tr>
      <tr class="empty-row" *matNoDataRow><td [attr.colspan]="cols.length"><mat-icon>info_outline</mat-icon>No records found.</td></tr>
    </table>
  `,
  styles: [`
    .tabs { display: flex; gap: 1rem; margin-bottom: 1rem; }
    .tabs a { color: #1f4e79; text-decoration: none; font-weight: 500; }
    .tabs a.active { text-decoration: underline; }
    .form-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem; margin-bottom: 1.5rem; align-items: center; }
    table { width: 100%; }
  `]
})
export class DastavejEntryComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly bookingService = inject(BookingService);
  private readonly siteContext = inject(SiteContextService);
  private readonly toast = inject(ToastService);

  bookings: BookingOption[] = [];
  readonly accountingNav = ACCOUNTING_NAV_ITEMS;
  get bookingOptions(): SelectOption<string>[] {
    return this.bookings.map((b) => ({ value: b.id, label: `${b.flatNo} - ${b.memberName}` }));
  }
  rows: DastavejRow[] = [];
  cols = ['flatNo', 'memberName', 'dastavejDate', 'satakhatDate', 'documentNumber', 'serviceTax', 'actions'];
  siteId: string | null = null;

  form = this.fb.group({
    bookingId: [''],
    dastavejDate: [''],
    satakhatDate: [''],
    documentNumber: [''],
    serviceTax: [null as number | null],
    notes: ['']
  });

  constructor() {
    effect(() => {
      this.siteId = this.siteContext.activeSiteId();
      if (this.siteId) this.load();
    });
  }

  ngOnInit(): void {
    this.siteContext.loadSites().subscribe();
    this.siteId = this.siteContext.activeSiteId();
    if (this.siteId) this.load();
  }

  load(): void {
    if (!this.siteId) return;
    this.bookingService.getDastavejList(this.siteId).subscribe({
      next: (r) => {
        if (r.success) {
          this.rows = r.data;
          this.bookings = r.data.map(row => ({ id: row.id, flatNo: row.flatNo, memberName: row.memberName }));
        }
      }
    });
  }

  onBookingSelect(): void {
    const id = this.form.value.bookingId;
    const row = this.rows.find(r => r.id === id);
    if (!row) return;
    this.form.patchValue({
      dastavejDate: row.dastavejDate ?? '',
      satakhatDate: row.satakhatDate ?? '',
      documentNumber: row.documentNumber ?? '',
      serviceTax: row.serviceTax ?? null
    });
  }

  editRow(row: DastavejRow): void {
    this.form.patchValue({
      bookingId: row.id,
      dastavejDate: row.dastavejDate ?? '',
      satakhatDate: row.satakhatDate ?? '',
      documentNumber: row.documentNumber ?? '',
      serviceTax: row.serviceTax ?? null
    });
  }

  save(): void {
    const id = this.form.value.bookingId;
    if (!id) return;
    const raw = this.form.getRawValue();
    this.bookingService.updateDastavej(id, {
      dastavejDate: raw.dastavejDate || null,
      satakhatDate: raw.satakhatDate || null,
      documentNumber: raw.documentNumber || null,
      serviceTax: raw.serviceTax,
      notes: raw.notes || null
    }).subscribe({
      next: (r) => {
        if (r.success) {
          this.toast.success('Dastavej details saved');
          this.load();
        }
      }
    });
  }
}
