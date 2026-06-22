import { TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { DailyEntryComponent } from './daily-entry.component';
import { DailyEntryService } from '../../../core/services/daily-entry.service';
import { MasterDataService } from '../../../core/services/master-data.service';
import { SiteContextService } from '../../../core/services/site-context.service';
import { ToastService } from '../../../core/services/toast.service';
import { MatDialog } from '@angular/material/dialog';

describe('DailyEntryComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DailyEntryComponent, RouterTestingModule, NoopAnimationsModule],
      providers: [
        FormBuilder,
        { provide: DailyEntryService, useValue: { getList: () => of({ success: true, data: { items: [], totalCount: 0 } }), getProfit: () => of({ success: true, data: { totalAavak: 0, totalJavak: 0, profit: 0 } }), create: () => of({ success: true }), delete: () => of({ success: true }) } },
        { provide: MasterDataService, useValue: { getMainLedgers: () => of({ success: true, data: [] }), getSubLedgers: () => of({ success: true, data: [] }), getBanks: () => of({ success: true, data: [] }) } },
        { provide: SiteContextService, useValue: { activeSiteId: () => 'site-1', loadSites: () => of({ success: true, data: [] }) } },
        { provide: ToastService, useValue: { success: () => {}, error: () => {} } },
        { provide: MatDialog, useValue: { open: () => ({ afterClosed: () => of(false) }) } }
      ]
    }).compileComponents();
  });

  it('should create and mark form invalid when empty', () => {
    const fixture = TestBed.createComponent(DailyEntryComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance).toBeTruthy();
    expect(fixture.componentInstance.form.invalid).toBeTrue();
  });
});
