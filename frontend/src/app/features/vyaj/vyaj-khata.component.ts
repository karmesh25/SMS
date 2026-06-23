import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { BreakpointObserver } from '@angular/cdk/layout';
import { FormsModule } from '@angular/forms';
import { map } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { catchError, EMPTY, filter, finalize, switchMap, tap } from 'rxjs';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { HasRoleDirective } from '../../shared/directives/has-role.directive';
import { IndianCurrencyPipe } from '../../shared/pipes/indian-currency.pipe';
import { AuthService } from '../../core/services/auth.service';
import { SiteContextService } from '../../core/services/site-context.service';
import { ToastService } from '../../core/services/toast.service';
import { VyajService } from '../../core/services/vyaj.service';
import { VyajAddEntryPanelComponent } from './add-entry-panel/add-entry-panel.component';
import { VyajEntryRowComponent } from './entry-row/entry-row.component';
import { VyajPartySidebarComponent } from './party-sidebar/party-sidebar.component';
import { RateBasis, VyajPartyDetail, VyajPartySummary } from './models/vyaj.models';

@Component({
  selector: 'app-vyaj-khata',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    PageHeaderComponent,
    HasRoleDirective,
    IndianCurrencyPipe,
    VyajPartySidebarComponent,
    VyajAddEntryPanelComponent,
    VyajEntryRowComponent
  ],
  templateUrl: './vyaj-khata.component.html',
  styleUrl: './vyaj-khata.component.scss'
})
export class VyajKhataComponent {
  private readonly vyajService = inject(VyajService);
  private readonly siteContext = inject(SiteContextService);
  private readonly toast = inject(ToastService);
  private readonly authService = inject(AuthService);
  private readonly breakpointObserver = inject(BreakpointObserver);

  readonly isTabletDown = toSignal(
    this.breakpointObserver.observe('(max-width: 959px)').pipe(map((r) => r.matches)),
    { initialValue: false }
  );

  readonly loading = signal(false);
  newPartyName = '';
  readonly saving = signal(false);
  readonly parties = signal<VyajPartySummary[]>([]);
  readonly selectedPartyId = signal<string | null>(null);
  readonly partyDetail = signal<VyajPartyDetail | null>(null);
  readonly addEntryOpen = signal(false);

  readonly readOnly = computed(() => this.authService.hasRole('ViewOnly'));

  constructor() {
    toObservable(this.siteContext.activeSiteId)
      .pipe(
        filter((id): id is string => !!id),
        tap(() => {
          this.selectedPartyId.set(null);
          this.partyDetail.set(null);
        }),
        switchMap((siteId) => this.loadParties$(siteId)),
        takeUntilDestroyed()
      )
      .subscribe();

    toObservable(this.selectedPartyId)
      .pipe(
        filter((id): id is string => !!id),
        switchMap((id) => this.loadPartyDetail$(id)),
        takeUntilDestroyed()
      )
      .subscribe();
  }

  private loadParties$(siteId: string) {
    this.loading.set(true);
    return this.vyajService.getParties(siteId).pipe(
      tap((res) => {
        if (res.success) {
          this.parties.set(res.data);
          const selected = this.selectedPartyId();
          if (selected && !res.data.some((p) => p.id === selected)) {
            this.selectedPartyId.set(null);
            this.partyDetail.set(null);
          }
        }
      }),
      catchError(() => {
        this.toast.error('Failed to load vyaj parties');
        return EMPTY;
      }),
      finalize(() => this.loading.set(false))
    );
  }

  private loadPartyDetail$(partyId: string) {
    this.loading.set(true);
    return this.vyajService.getPartyDetail(partyId).pipe(
      tap((res) => {
        if (res.success) this.partyDetail.set(res.data);
      }),
      catchError(() => {
        this.toast.error('Failed to load party detail');
        return EMPTY;
      }),
      finalize(() => this.loading.set(false))
    );
  }

  refresh(): void {
    const siteId = this.siteContext.activeSiteId();
    if (!siteId) return;
    this.loadParties$(siteId).subscribe();
    const partyId = this.selectedPartyId();
    if (partyId) this.loadPartyDetail$(partyId).subscribe();
  }

  selectParty(partyId: string): void {
    this.selectedPartyId.set(partyId);
    this.addEntryOpen.set(false);
  }

  submitNewParty(): void {
    const name = this.newPartyName.trim();
    if (!name) return;
    this.addParty(name);
    this.newPartyName = '';
  }

  addParty(name: string): void {
    const siteId = this.siteContext.activeSiteId();
    if (!siteId) return;

    this.saving.set(true);
    this.vyajService.createParty({ siteId, name }).pipe(
      finalize(() => this.saving.set(false))
    ).subscribe({
      next: (res) => {
        if (res.success) {
          this.toast.success('Party added');
          this.refresh();
          this.selectedPartyId.set(res.data.id);
        }
      },
      error: () => this.toast.error('Failed to add party')
    });
  }

  saveEntry(payload: { principal: number; ratePercent: number; rateBasis: string; startDate: string }): void {
    const partyId = this.selectedPartyId();
    if (!partyId) return;

    this.saving.set(true);
    this.vyajService.createEntry({
      partyId,
      principal: payload.principal,
      ratePercent: payload.ratePercent,
      rateBasis: payload.rateBasis as RateBasis,
      startDate: payload.startDate
    }).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: (res) => {
        if (res.success) {
          this.toast.success('Entry saved');
          this.addEntryOpen.set(false);
          this.refresh();
        }
      },
      error: () => this.toast.error('Failed to save entry')
    });
  }

  toggleClosed(entryId: string, isClosed: boolean): void {
    this.vyajService.toggleEntryClosed(entryId, isClosed).subscribe({
      next: (res) => {
        if (res.success) {
          this.toast.success(isClosed ? 'Entry closed' : 'Entry reopened');
          this.refresh();
        }
      },
      error: () => this.toast.error('Failed to update entry')
    });
  }

  recordPayment(entryId: string, payload: { amount: number; paymentDate: string; paymentType: string }): void {
    this.saving.set(true);
    this.vyajService.createPayment({
      entryId,
      amount: payload.amount,
      paymentDate: payload.paymentDate,
      paymentType: payload.paymentType as 'interest' | 'principal'
    }).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: (res) => {
        if (res.success) {
          this.toast.success('Payment recorded');
          this.refresh();
        }
      },
      error: () => this.toast.error('Failed to record payment')
    });
  }

  deleteEntry(entryId: string): void {
    this.vyajService.deleteEntry(entryId).subscribe({
      next: (res) => {
        if (res.success) {
          this.toast.success('Entry deleted');
          this.refresh();
        }
      },
      error: () => this.toast.error('Failed to delete entry')
    });
  }

  deletePayment(paymentId: string): void {
    this.vyajService.deletePayment(paymentId).subscribe({
      next: (res) => {
        if (res.success) {
          this.toast.success('Payment removed');
          this.refresh();
        }
      },
      error: () => this.toast.error('Failed to delete payment')
    });
  }
}
