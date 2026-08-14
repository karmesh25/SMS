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
import { ModuleSubnavComponent } from '../../shared/components/module-subnav/module-subnav.component';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';
import { IndianCurrencyPipe } from '../../shared/pipes/indian-currency.pipe';
import { SelectOption } from '../../shared/components/searchable-select/searchable-select.component';
import { ACCOUNTING_NAV_ITEMS } from '../../shared/nav/module-nav-items';
import { AuthService } from '../../core/services/auth.service';
import { DailyEntryService, ProfitSummary } from '../../core/services/daily-entry.service';
import { FileDownloadOutcome, FileDownloadService } from '../../core/services/file-download.service';
import { MasterDataService } from '../../core/services/master-data.service';
import { SiteContextService } from '../../core/services/site-context.service';
import { ToastService } from '../../core/services/toast.service';
import { VyajService } from '../../core/services/vyaj.service';
import { VyajAddEntryPanelComponent } from './add-entry-panel/add-entry-panel.component';
import { VyajEntryRowComponent } from './entry-row/entry-row.component';
import { NewVyajPartyPayload, VyajPartySidebarComponent } from './party-sidebar/party-sidebar.component';
import { RateBasis, VyajPartyDetail, VyajPartySummary } from './models/vyaj.models';

interface MainLedger { id: string; ledgerName: string; }
interface SubLedger { id: string; ledgerName: string; flatNo?: string; }

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
    ModuleSubnavComponent,
    HasPermissionDirective,
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
  private readonly dailyEntryService = inject(DailyEntryService);
  private readonly masterData = inject(MasterDataService);
  private readonly siteContext = inject(SiteContextService);
  private readonly toast = inject(ToastService);
  private readonly authService = inject(AuthService);
  private readonly fileDownloads = inject(FileDownloadService);
  private readonly breakpointObserver = inject(BreakpointObserver);

  readonly accountingNav = ACCOUNTING_NAV_ITEMS;

  readonly isTabletDown = toSignal(
    this.breakpointObserver.observe('(max-width: 959px)').pipe(map((r) => r.matches)),
    { initialValue: false }
  );

  readonly loading = signal(false);
  newPartyName = '';
  newMainLedgerId = '';
  newSubLedgerId = '';
  readonly saving = signal(false);
  readonly parties = signal<VyajPartySummary[]>([]);
  readonly selectedPartyId = signal<string | null>(null);
  readonly partyDetail = signal<VyajPartyDetail | null>(null);
  readonly addEntryOpen = signal(false);
  readonly profit = signal<ProfitSummary | null>(null);
  readonly mainLedgers = signal<MainLedger[]>([]);
  readonly subLedgers = signal<SubLedger[]>([]);

  readonly readOnly = computed(() => !this.authService.hasPermission('vyaj', 'manage'));
  readonly hasActiveSite = computed(() => !!this.siteContext.activeSiteId());

  readonly siteVyajDue = computed(() => this.parties().reduce((sum, p) => sum + (p.vyajDue || 0), 0));
  readonly sitePrincipalDue = computed(() => this.parties().reduce((sum, p) => sum + (p.principalDue || 0), 0));

  readonly mainLedgerOptions = computed<SelectOption<string>[]>(() =>
    this.mainLedgers().map((m) => ({ value: m.id, label: m.ledgerName }))
  );

  readonly subLedgerOptions = computed<SelectOption<string>[]>(() =>
    this.subLedgers().map((s) => ({
      value: s.id,
      label: s.flatNo ? `${s.flatNo} - ${s.ledgerName}` : s.ledgerName
    }))
  );

  constructor() {
    toObservable(this.siteContext.activeSiteId)
      .pipe(
        filter((id): id is string => !!id),
        tap((siteId) => {
          this.selectedPartyId.set(null);
          this.partyDetail.set(null);
          this.loadMasterData(siteId);
          this.loadProfit(siteId);
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

  private loadProfit(siteId: string): void {
    this.dailyEntryService.getProfit(siteId).subscribe({
      next: (res) => {
        if (res.success) this.profit.set(res.data);
      }
    });
  }

  private loadMasterData(siteId: string): void {
    this.masterData.getMainLedgers(siteId).subscribe({
      next: (r) => {
        if (r.success) this.mainLedgers.set(r.data as MainLedger[]);
      }
    });
    this.subLedgers.set([]);
  }

  onMainLedgerChanged(mainLedgerId: string): void {
    this.newMainLedgerId = mainLedgerId;
    this.newSubLedgerId = '';
    if (!mainLedgerId) {
      this.subLedgers.set([]);
      return;
    }
    this.masterData.getSubLedgers(mainLedgerId).subscribe({
      next: (r) => {
        if (r.success) this.subLedgers.set(r.data as SubLedger[]);
      }
    });
  }

  refresh(): void {
    const siteId = this.siteContext.activeSiteId();
    if (!siteId) return;
    this.loadProfit(siteId);
    this.loadParties$(siteId).subscribe();
    const partyId = this.selectedPartyId();
    if (partyId) this.loadPartyDetail$(partyId).subscribe();
  }

  exportExcel(): void {
    const siteId = this.siteContext.activeSiteId();
    if (!siteId) return;
    this.vyajService.exportExcel(siteId).subscribe({
      next: (outcome) => this.handleDownload(outcome, `vyaj-khata-${new Date().toISOString().slice(0, 10)}.xlsx`),
      error: (err) => void this.fileDownloads.resolveErrorMessage(err).then((msg) => this.toast.error(msg))
    });
  }

  exportPdf(): void {
    const siteId = this.siteContext.activeSiteId();
    if (!siteId) return;
    this.vyajService.exportPdf(siteId).subscribe({
      next: (outcome) => this.handleDownload(outcome, `vyaj-khata-${new Date().toISOString().slice(0, 10)}.pdf`),
      error: (err) => void this.fileDownloads.resolveErrorMessage(err).then((msg) => this.toast.error(msg))
    });
  }

  private handleDownload(outcome: FileDownloadOutcome, fallbackFilename: string): void {
    if (outcome.mode === 'pendrive') {
      this.toast.success(outcome.message ?? `Saved to pendrive: ${outcome.savedPath ?? fallbackFilename}`);
      return;
    }
    if (outcome.blob) {
      this.fileDownloads.saveToBrowser(outcome.blob, outcome.filename ?? fallbackFilename);
      this.toast.success('Vyaj export downloaded');
    }
  }

  selectParty(partyId: string): void {
    this.selectedPartyId.set(partyId);
    this.addEntryOpen.set(false);
  }

  submitNewParty(): void {
    const name = this.newPartyName.trim();
    if (!name) return;
    this.addParty({
      name,
      mainLedgerId: this.newMainLedgerId || null,
      subLedgerId: this.newSubLedgerId || null
    });
    this.newPartyName = '';
    this.newMainLedgerId = '';
    this.newSubLedgerId = '';
  }

  addParty(payload: string | NewVyajPartyPayload): void {
    const siteId = this.siteContext.activeSiteId();
    if (!siteId) return;

    const body = typeof payload === 'string'
      ? { siteId, name: payload }
      : {
          siteId,
          name: payload.name,
          mainLedgerId: payload.mainLedgerId,
          subLedgerId: payload.subLedgerId
        };

    this.saving.set(true);
    this.vyajService.createParty(body).pipe(
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

  saveEntry(payload: {
    principal: number;
    ratePercent: number;
    rateBasis: string;
    ratePeriodMonths?: number | null;
    emiAmount?: number | null;
    startDate: string;
  }): void {
    const partyId = this.selectedPartyId();
    if (!partyId) return;

    this.saving.set(true);
    this.vyajService.createEntry({
      partyId,
      principal: payload.principal,
      ratePercent: payload.ratePercent,
      rateBasis: payload.rateBasis as RateBasis,
      ratePeriodMonths: payload.ratePeriodMonths ?? null,
      emiAmount: payload.emiAmount ?? null,
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
