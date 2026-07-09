import { Component, effect, inject, OnInit } from '@angular/core';

import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { ActivatedRoute, Router } from '@angular/router';

import { MatFormFieldModule } from '@angular/material/form-field';

import { MatInputModule } from '@angular/material/input';

import { MatSelectModule } from '@angular/material/select';

import { MatButtonModule } from '@angular/material/button';

import { MatCheckboxModule } from '@angular/material/checkbox';

import { MatDatepickerModule } from '@angular/material/datepicker';

import { MatNativeDateModule } from '@angular/material/core';

import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';

import { BookingService } from '../../../core/services/booking.service';

import { MasterDataService } from '../../../core/services/master-data.service';

import { SiteContextService } from '../../../core/services/site-context.service';

import { ToastService } from '../../../core/services/toast.service';

import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

import { IndianAmountDirective } from '../../../shared/directives/indian-amount.directive';



interface ConditionOption { id: string; conditionName: string; }

interface BrokerOption { id: string; name: string; contactNo?: string; }

interface SubLedgerOption { id: string; ledgerName: string; }



@Component({

  selector: 'app-booking-form',

  standalone: true,

  imports: [

    ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule,

    MatButtonModule, MatCheckboxModule, MatDatepickerModule, MatNativeDateModule,

    MatDialogModule, PageHeaderComponent, IndianAmountDirective

  ],

  template: `

    <app-page-header [title]="isEdit ? 'Edit Booking' : 'New Booking'" [subtitle]="flatNo ? 'Flat ' + flatNo : ''"></app-page-header>



    @if (conditions.length === 0 && masterDataLoaded) {

      <p class="warning">Create a payment condition in Admin → Conditions before booking.</p>

    }



    <form [formGroup]="form" class="form-grid" (ngSubmit)="save()">

      <mat-form-field appearance="outline"><mat-label>Flat No</mat-label><input matInput formControlName="flatNo" readonly /></mat-form-field>

      <mat-form-field appearance="outline"><mat-label>Booking Date</mat-label><input matInput type="date" formControlName="bookingDate" /></mat-form-field>

      <mat-form-field appearance="outline"><mat-label>Member Name</mat-label><input matInput formControlName="memberName" /></mat-form-field>

      <mat-form-field appearance="outline"><mat-label>Contact</mat-label><input matInput formControlName="customerContact" /></mat-form-field>

      <mat-form-field appearance="outline">

        <mat-label>Customer Type</mat-label>

        <mat-select formControlName="customerType">

          <mat-option value="real">Real</mat-option>

          <mat-option value="investor">Investor</mat-option>

        </mat-select>

      </mat-form-field>

      <mat-form-field appearance="outline">

        <mat-label>Condition</mat-label>

        <mat-select formControlName="conditionId">

          @for (c of conditions; track c.id) {

            <mat-option [value]="c.id">{{ c.conditionName }}</mat-option>

          }

        </mat-select>

      </mat-form-field>

      <mat-form-field appearance="outline"><mat-label>SQFT</mat-label><input matInput type="number" formControlName="sqft" (input)="onSqftChange()" /></mat-form-field>

      <mat-form-field appearance="outline"><mat-label>Rate / SQFT</mat-label><input matInput type="number" formControlName="rate" appIndianAmount (input)="onRateChange()" /></mat-form-field>

      <mat-form-field appearance="outline"><mat-label>Total Price</mat-label><input matInput type="number" formControlName="totalPrice" appIndianAmount (input)="onTotalChange()" /></mat-form-field>

      <p class="pricing-hint">Enter Rate or Total — the other is calculated automatically from SQFT.</p>

      <mat-form-field appearance="outline">

        <mat-label>Broker</mat-label>

        <mat-select formControlName="brokerId">

          <mat-option [value]="null">None</mat-option>

          @for (b of brokers; track b.id) {

            <mat-option [value]="b.id">{{ b.name }}</mat-option>

          }

        </mat-select>

      </mat-form-field>

      <mat-form-field appearance="outline"><mat-label>Brokerage %</mat-label><input matInput type="number" formControlName="brokeragePct" (input)="onBrokeragePctChange()" /></mat-form-field>

      <mat-form-field appearance="outline"><mat-label>Brokerage Amount</mat-label><input matInput type="number" formControlName="brokerageAmount" appIndianAmount (input)="onBrokerageAmountChange()" /></mat-form-field>

      <p class="pricing-hint">Enter Brokerage % or Amount — the other is calculated from Total Price.</p>

      <mat-form-field appearance="outline" class="full"><mat-label>Notes</mat-label><textarea matInput formControlName="notes" rows="2"></textarea></mat-form-field>

      <mat-checkbox formControlName="isArjaMarjaSell">Is Arja-Marja Sell</mat-checkbox>



      <div class="actions">

        <button mat-button type="button" (click)="goBack()">Cancel</button>

        @if (isEdit && bookingStatus === 'active') {

          <button mat-stroked-button color="warn" type="button" (click)="cancelBooking()">Cancel Flat</button>

        }

        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || conditions.length === 0">Save</button>

      </div>

    </form>

  `,

  styles: [`

    .form-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 1rem; align-items: start; }

    .full { grid-column: 1 / -1; }

    .actions { grid-column: 1 / -1; display: flex; gap: 1rem; justify-content: flex-end; flex-wrap: wrap; }
    @media (max-width: 599px) {
      .actions { flex-direction: column; align-items: stretch; }
      .actions button { width: 100%; }
    }

    .warning { color: #c0392b; margin-bottom: 1rem; font-size: 0.875rem; }

    .pricing-hint { grid-column: 1 / -1; margin: -0.5rem 0 0; color: #64748b; font-size: 0.8rem; }

  `]

})

export class BookingFormComponent implements OnInit {

  private readonly fb = inject(FormBuilder);

  private readonly route = inject(ActivatedRoute);

  private readonly router = inject(Router);

  private readonly bookingService = inject(BookingService);

  private readonly masterData = inject(MasterDataService);

  private readonly siteContext = inject(SiteContextService);

  private readonly toast = inject(ToastService);

  private readonly dialog = inject(MatDialog);



  isEdit = false;

  bookingId: string | null = null;

  flatId: string | null = null;

  flatNo = '';

  bookingStatus = '';

  conditions: ConditionOption[] = [];

  brokers: BrokerOption[] = [];

  memberSubLedgers: SubLedgerOption[] = [];

  masterDataLoaded = false;

  private lastLoadedSiteId: string | null = null;

  private lastPricingDriver: 'rate' | 'total' = 'rate';

  private lastBrokerageDriver: 'pct' | 'amount' = 'pct';



  form = this.fb.nonNullable.group({

    flatNo: [''],

    bookingDate: [new Date().toISOString().slice(0, 10), Validators.required],

    memberName: ['', Validators.required],

    customerContact: [''],

    customerType: ['real', Validators.required],

    conditionId: ['', Validators.required],

    sqft: [0, [Validators.required, Validators.min(1)]],

    rate: [0, [Validators.required, Validators.min(0)]],

    totalPrice: [0, [Validators.required, Validators.min(0)]],

    brokerId: [null as string | null],

    brokeragePct: [0, [Validators.min(0)]],

    brokerageAmount: [0, [Validators.min(0)]],

    isArjaMarjaSell: [false],

    notes: ['']

  });



  constructor() {

    effect(() => {

      const siteId = this.siteContext.activeSiteId();

      if (siteId && siteId !== this.lastLoadedSiteId) {

        this.loadMasterData(siteId);

      }

    });

  }



  ngOnInit(): void {

    this.bookingId = this.route.snapshot.paramMap.get('id');

    this.isEdit = !!this.bookingId;

    this.flatId = this.route.snapshot.queryParamMap.get('flatId');



    this.form.get('sqft')!.valueChanges.subscribe(() => this.recalcFromPricing());

    if (this.isEdit && this.bookingId) {

      this.loadBooking(this.bookingId);

    } else if (this.flatId) {

      this.loadFlat(this.flatId);

    }

  }



  loadMasterData(siteId: string): void {

    this.lastLoadedSiteId = siteId;

    this.masterData.getConditions(siteId).subscribe({

      next: (r) => {

        this.masterDataLoaded = true;

        if (r.success) this.conditions = r.data as ConditionOption[];

      },

      error: () => { this.masterDataLoaded = true; }

    });

    this.masterData.getBrokers(siteId).subscribe({

      next: (r) => { if (r.success) this.brokers = r.data as BrokerOption[]; }

    });

    this.masterData.getMainLedgers(siteId).subscribe({

      next: (r) => {

        if (!r.success) return;

        const member = (r.data as { id: string; ledgerName: string }[]).find(l => l.ledgerName === 'Member A/c');

        if (member) {

          this.masterData.getSubLedgers(member.id).subscribe({

            next: (sr) => { if (sr.success) this.memberSubLedgers = sr.data as SubLedgerOption[]; }

          });

        }

      }

    });

  }



  loadFlat(flatId: string): void {

    this.bookingService.getFlatDetail(flatId).subscribe({

      next: (r) => {

        if (r.success) {

          this.flatNo = r.data.flatNo;

          this.form.patchValue({ flatNo: r.data.flatNo, sqft: r.data.sqft });

          this.recalc();

        }

      },

      error: (err) => this.showApiError(err, 'Failed to load flat details.')

    });

  }



  loadBooking(id: string): void {

    this.bookingService.getById(id).subscribe({

      next: (r) => {

        if (!r.success) return;

        const b = r.data;

        this.flatId = b.flatId;

        this.flatNo = b.flatNo;

        this.bookingStatus = b.status;

        this.form.patchValue({

          flatNo: b.flatNo,

          bookingDate: b.bookingDate,

          memberName: b.memberName,

          customerContact: b.customerContact ?? '',

          customerType: b.customerType,

          conditionId: b.conditionId,

          sqft: b.sqft,

          rate: b.rate,

          brokerId: b.brokerId ?? null,

          brokeragePct: b.brokeragePct,

          isArjaMarjaSell: b.isArjaMarjaSell,

          notes: b.notes ?? ''

        });

        this.recalc();

        this.loadMasterData(b.siteId);

      },

      error: (err) => this.showApiError(err, 'Failed to load booking.')

    });

  }



  onSqftChange(): void {
    this.recalcFromPricing();
  }

  onRateChange(): void {
    this.lastPricingDriver = 'rate';
    this.recalcFromPricing();
  }

  onTotalChange(): void {
    this.lastPricingDriver = 'total';
    this.recalcFromPricing();
  }

  onBrokeragePctChange(): void {
    this.lastBrokerageDriver = 'pct';
    this.recalcBrokerage();
  }

  onBrokerageAmountChange(): void {
    this.lastBrokerageDriver = 'amount';
    this.recalcBrokerage();
  }

  /** Kept for callers; prefers last pricing/brokerage drivers. */
  recalc(): void {
    this.recalcFromPricing();
  }

  private recalcFromPricing(): void {
    const sqft = this.form.getRawValue().sqft ?? 0;
    let rate = this.form.getRawValue().rate ?? 0;
    let total = this.form.getRawValue().totalPrice ?? 0;

    if (this.lastPricingDriver === 'total' && sqft > 0) {
      rate = Math.round((total / sqft) * 100) / 100;
      this.form.patchValue({ rate }, { emitEvent: true });
    } else {
      total = Math.round(sqft * rate * 100) / 100;
      this.form.patchValue({ totalPrice: total }, { emitEvent: true });
    }

    this.recalcBrokerage();
  }

  private recalcBrokerage(): void {
    const total = this.form.getRawValue().totalPrice ?? 0;
    let pct = this.form.getRawValue().brokeragePct ?? 0;
    let amount = this.form.getRawValue().brokerageAmount ?? 0;

    if (this.lastBrokerageDriver === 'amount' && total > 0) {
      pct = Math.round((amount / total) * 10000) / 100;
    } else {
      amount = Math.round(total * (pct / 100) * 100) / 100;
    }

    const capped = this.capBrokerageToTotal(total, pct, amount);
    if (capped.wasCapped) {
      this.toast.error('Brokerage cannot exceed total price.');
    }

    this.form.patchValue(
      { brokeragePct: capped.pct, brokerageAmount: capped.amount },
      { emitEvent: true }
    );
  }

  private capBrokerageToTotal(
    total: number,
    pct: number,
    amount: number
  ): { pct: number; amount: number; wasCapped: boolean } {
    if (total > 0 && amount > total) {
      const cappedAmount = total;
      const cappedPct = Math.round((cappedAmount / total) * 10000) / 100;
      return { pct: cappedPct, amount: cappedAmount, wasCapped: true };
    }
    return { pct, amount, wasCapped: false };
  }



  save(): void {

    this.recalcFromPricing();
    const priced = this.form.getRawValue();
    if ((priced.rate ?? 0) <= 0 || (priced.totalPrice ?? 0) <= 0) {
      this.toast.error('Enter SQFT with Rate or Total Price.');
      return;
    }
    if ((priced.brokerageAmount ?? 0) > (priced.totalPrice ?? 0)) {
      this.toast.error('Brokerage cannot exceed total price.');
      return;
    }

    const payload = {

      memberName: priced.memberName,

      conditionId: priced.conditionId,

      brokerId: priced.brokerId || null,

      bookingDate: priced.bookingDate,

      customerContact: priced.customerContact || null,

      sqft: priced.sqft,

      rate: priced.rate,

      brokeragePct: priced.brokeragePct,

      customerType: priced.customerType,

      isArjaMarjaSell: priced.isArjaMarjaSell,

      notes: priced.notes || null

    };



    if (this.isEdit && this.bookingId) {

      this.bookingService.update(this.bookingId, payload).subscribe({

        next: (r) => {

          if (r.success) { this.toast.success('Booking updated'); void this.router.navigate(['/booking']); }

        },

        error: (err) => this.showApiError(err, 'Failed to update booking.')

      });

    } else if (this.flatId) {

      this.bookingService.create({ ...payload, flatId: this.flatId }).subscribe({

        next: (r) => {

          if (r.success) { this.toast.success('Booking created'); void this.router.navigate(['/booking']); }

        },

        error: (err) => this.showApiError(err, 'Failed to create booking.')

      });

    }

  }



  cancelBooking(): void {

    const ref = this.dialog.open(ConfirmDialogComponent, {

      data: { title: 'Cancel Flat', message: 'Are you sure you want to cancel this booking?', confirmText: 'Cancel Flat' }

    });

    ref.afterClosed().subscribe((confirmed) => {

      if (!confirmed || !this.bookingId) return;

      const cancelDate = new Date().toISOString().slice(0, 10);

      this.bookingService.cancel(this.bookingId, { cancelDate }).subscribe({

        next: (r) => {

          if (r.success) { this.toast.success('Booking cancelled'); void this.router.navigate(['/booking']); }

        },

        error: (err) => this.showApiError(err, 'Failed to cancel booking.')

      });

    });

  }



  goBack(): void { void this.router.navigate(['/booking']); }



  private showApiError(err: { error?: { message?: string; errors?: string[] } }, fallback: string): void {

    const apiMessage = err.error?.message;

    const details = err.error?.errors?.filter(Boolean).join(', ');

    this.toast.error(details ? `${apiMessage ?? fallback}: ${details}` : (apiMessage ?? fallback));

  }

}


